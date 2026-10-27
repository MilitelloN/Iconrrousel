using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.RegularExpressions;

namespace Iconrrousel.Main
{
    public enum GamePlatform
    {
        None,
        Steam,
        Epic,
        Gog,
        EA,
        Xbox
    }

    // Detecta a que plataforma pertenece un icono guardado (.url, .lnk o .exe):
    //  - .url: por el esquema de la URL (steam://, com.epicgames.launcher://, ...).
    //  - .lnk: por el ejecutable/argumentos de destino, las carpetas de instalacion
    //    que cada launcher registra (registro/manifiestos) y, si no hay ruta de
    //    destino, por el AUMID de shell:AppsFolder (juegos de Xbox/Store).
    //  - .exe: igual que el destino de un .lnk.
    // El resultado se cachea por ruta; las carpetas de instalacion se leen una sola vez.
    public static class PlatformDetector
    {
        private static readonly Dictionary<string, GamePlatform> _cache =
            new Dictionary<string, GamePlatform>(StringComparer.OrdinalIgnoreCase);

        private static List<(string Root, GamePlatform Platform)> _installRoots;

        private static readonly (string Prefix, GamePlatform Platform)[] _urlSchemes =
        {
            ("steam:", GamePlatform.Steam),
            ("com.epicgames.launcher:", GamePlatform.Epic),
            ("goggalaxy:", GamePlatform.Gog),
            ("origin:", GamePlatform.EA),
            ("origin2:", GamePlatform.EA),
            ("link2ea:", GamePlatform.EA),
            ("eadm:", GamePlatform.EA),
            ("msgamelaunch:", GamePlatform.Xbox), // accesos directos de la app Xbox
            ("msxbox:", GamePlatform.Xbox),
            ("xbox:", GamePlatform.Xbox),
            ("ms-xbl-", GamePlatform.Xbox),
        };

        // Ejecutables de los launchers: accesos directos del tipo "steam.exe -applaunch 123".
        private static readonly Dictionary<string, GamePlatform> _launcherExes =
            new Dictionary<string, GamePlatform>(StringComparer.OrdinalIgnoreCase)
            {
                ["steam.exe"] = GamePlatform.Steam,
                ["EpicGamesLauncher.exe"] = GamePlatform.Epic,
                ["GalaxyClient.exe"] = GamePlatform.Gog,
                ["EADesktop.exe"] = GamePlatform.EA,
                ["EALauncher.exe"] = GamePlatform.EA,
                ["EALaunchHelper.exe"] = GamePlatform.EA,
                ["Origin.exe"] = GamePlatform.EA,
                ["XboxPcApp.exe"] = GamePlatform.Xbox,
                ["gamelaunchhelper.exe"] = GamePlatform.Xbox,
            };

        // Respaldo si el launcher no dejo nada en el registro: nombres de carpeta tipicos.
        private static readonly (string Marker, GamePlatform Platform)[] _pathMarkers =
        {
            (@"\steamapps\", GamePlatform.Steam),
            (@"\Epic Games\", GamePlatform.Epic),
            (@"\GOG Galaxy\Games\", GamePlatform.Gog),
            (@"\GOG Games\", GamePlatform.Gog),
            (@"\EA Games\", GamePlatform.EA),
            (@"\Origin Games\", GamePlatform.EA),
            (@"\XboxGames\", GamePlatform.Xbox),
            (@"\WindowsApps\", GamePlatform.Xbox),
            (@"\ModifiableWindowsApps\", GamePlatform.Xbox),
        };

        public static GamePlatform Detect(string path)
        {
            if (string.IsNullOrEmpty(path))
                return GamePlatform.None;

            if (_cache.TryGetValue(path, out var cached))
                return cached;

            GamePlatform result;
            try
            {
                switch (Path.GetExtension(path).ToLowerInvariant())
                {
                    case ".url":
                        result = FromUrlFile(path);
                        break;
                    case ".lnk":
                        result = FromShellLink(path);
                        break;
                    default:
                        result = FromTarget(path, null);
                        break;
                }
            }
            catch (Exception ex)
            {
                LoggingConfig.Log($"No se pudo detectar la plataforma de {path}", ex: ex);
                result = GamePlatform.None;
            }

            LoggingConfig.Log($"Plataforma de {path}: {result}");
            _cache[path] = result;
            return result;
        }

        public static void Forget(string path)
        {
            if (path != null)
                _cache.Remove(path);
        }

        public static string DisplayName(GamePlatform platform)
        {
            switch (platform)
            {
                case GamePlatform.Steam: return "Steam";
                case GamePlatform.Epic: return "Epic Games";
                case GamePlatform.Gog: return "GOG";
                case GamePlatform.EA: return "EA";
                case GamePlatform.Xbox: return "Xbox";
                default: return "Sin plataforma";
            }
        }

        #region .url
        private static GamePlatform FromUrlFile(string path)
        {
            var urlLine = File.ReadLines(path)
                .Select(l => l.Trim())
                .FirstOrDefault(l => l.StartsWith("URL=", StringComparison.OrdinalIgnoreCase));
            if (urlLine == null)
                return GamePlatform.None;

            var url = urlLine.Substring(4).Trim();
            var platform = FromUrl(url);
            if (platform != GamePlatform.None)
                return platform;

            if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.IsFile)
                return FromTarget(uri.LocalPath, null);

            return GamePlatform.None;
        }

        private static GamePlatform FromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return GamePlatform.None;

            foreach (var (prefix, platform) in _urlSchemes)
            {
                if (url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return platform;
            }
            return GamePlatform.None;
        }
        #endregion

        #region .lnk
        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        private class ShellLink
        {
        }

        // Solo se declaran los metodos hasta GetArguments: el orden de la vtable
        // tiene que coincidir, pero no hace falta declarar los que siguen.
        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        private interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cch, IntPtr pfd, uint fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cch);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cch);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cch);
        }

        private const uint SLGP_RAWPATH = 0x4;
        private const uint SIGDN_DESKTOPABSOLUTEPARSING = 0x80028000;

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHGetNameFromIDList(IntPtr pidl, uint sigdnName, out IntPtr ppszName);

        private static GamePlatform FromShellLink(string path)
        {
            var link = (IShellLinkW)new ShellLink();
            try
            {
                ((IPersistFile)link).Load(path, 0); // STGM_READ

                var target = new StringBuilder(1024);
                link.GetPath(target, target.Capacity, IntPtr.Zero, SLGP_RAWPATH);
                var args = new StringBuilder(2048);
                link.GetArguments(args, args.Capacity);

                var targetPath = Environment.ExpandEnvironmentVariables(target.ToString());
                if (!string.IsNullOrWhiteSpace(targetPath))
                    return FromTarget(targetPath, args.ToString());

                // Sin ruta de destino: el destino es una URL (la app Xbox crea
                // accesos directos a msgamelaunch://...) o un item de shell:AppsFolder
                // (apps empaquetadas), cuyo nombre de parseo termina en el AUMID
                // "PackageFamilyName!AppId".
                return FromShellItem(GetIdListName(link));
            }
            finally
            {
                Marshal.ReleaseComObject(link);
            }
        }

        private static string GetIdListName(IShellLinkW link)
        {
            link.GetIDList(out var pidl);
            if (pidl == IntPtr.Zero)
                return null;

            try
            {
                if (SHGetNameFromIDList(pidl, SIGDN_DESKTOPABSOLUTEPARSING, out var namePtr) != 0)
                    return null;
                try
                {
                    return Marshal.PtrToStringUni(namePtr);
                }
                finally
                {
                    Marshal.FreeCoTaskMem(namePtr);
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(pidl);
            }
        }

        private static GamePlatform FromShellItem(string parsingName)
        {
            if (string.IsNullOrEmpty(parsingName))
                return GamePlatform.None;

            LoggingConfig.Log($"Acceso directo sin ruta, destino de shell: {parsingName}");
            var fromUrl = FromUrl(parsingName);
            if (fromUrl != GamePlatform.None)
                return fromUrl;

            var aumid = parsingName.Substring(parsingName.LastIndexOf('\\') + 1);
            return aumid.Contains('!') ? GamePlatform.Xbox : GamePlatform.None;
        }
        #endregion

        #region Ruta de destino
        private static GamePlatform FromTarget(string targetPath, string arguments)
        {
            if (_launcherExes.TryGetValue(Path.GetFileName(targetPath), out var launcher))
                return launcher;

            // ej. "explorer.exe steam://rungameid/123" o "cmd /c start link2ea://..."
            if (!string.IsNullOrEmpty(arguments))
            {
                foreach (var token in arguments.Split(new[] { ' ', '"' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var fromArg = FromUrl(token);
                    if (fromArg != GamePlatform.None)
                        return fromArg;
                }
            }

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(targetPath);
            }
            catch (Exception)
            {
                fullPath = targetPath;
            }

            foreach (var (root, platform) in GetInstallRoots())
            {
                if (fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                    return platform;
            }

            foreach (var (marker, platform) in _pathMarkers)
            {
                if (fullPath.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0)
                    return platform;
            }

            return GamePlatform.None;
        }
        #endregion

        #region Carpetas de instalacion
        private static List<(string Root, GamePlatform Platform)> GetInstallRoots()
        {
            if (_installRoots != null)
                return _installRoots;

            var roots = new List<(string Root, GamePlatform Platform)>();
            TryAdd(roots, GamePlatform.Steam, SteamLibraries);
            TryAdd(roots, GamePlatform.Epic, EpicInstallLocations);
            TryAdd(roots, GamePlatform.Gog, GogInstallLocations);
            TryAdd(roots, GamePlatform.EA, EaInstallLocations);

            // La carpeta mas especifica primero (ej. una biblioteca de Steam dentro de otra carpeta).
            _installRoots = roots
                .Where(r => !string.IsNullOrWhiteSpace(r.Root))
                .Select(r => (Root: NormalizeRoot(r.Root), r.Platform))
                .Distinct()
                .OrderByDescending(r => r.Root.Length)
                .ToList();

            foreach (var (root, platform) in _installRoots)
                LoggingConfig.Log($"Carpeta de {platform}: {root}");

            return _installRoots;
        }

        private static void TryAdd(List<(string, GamePlatform)> roots, GamePlatform platform, Func<IEnumerable<string>> source)
        {
            try
            {
                roots.AddRange(source().Select(r => (r, platform)));
            }
            catch (Exception ex)
            {
                LoggingConfig.Log($"No se pudieron leer las carpetas de {platform}", ex: ex);
            }
        }

        private static string NormalizeRoot(string root)
        {
            var full = Path.GetFullPath(Environment.ExpandEnvironmentVariables(root.Trim()));
            return full.EndsWith("\\") ? full : full + "\\";
        }

        // Steam: SteamPath del registro + cada "path" de steamapps\libraryfolders.vdf.
        private static IEnumerable<string> SteamLibraries()
        {
            var steamPath = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string
                ?? Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath", null) as string;
            if (string.IsNullOrEmpty(steamPath))
                yield break;

            var libraries = new List<string> { steamPath.Replace('/', '\\') };
            var vdf = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
            if (File.Exists(vdf))
            {
                foreach (Match m in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s+\"([^\"]+)\""))
                    libraries.Add(m.Groups[1].Value.Replace(@"\\", @"\"));
            }

            foreach (var library in libraries)
                yield return Path.Combine(library, "steamapps");
        }

        // Epic: un manifiesto .item (JSON) por juego, con su InstallLocation.
        private static IEnumerable<string> EpicInstallLocations()
        {
            var manifests = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                @"Epic\EpicGamesLauncher\Data\Manifests");
            if (!Directory.Exists(manifests))
                yield break;

            foreach (var file in Directory.EnumerateFiles(manifests, "*.item"))
            {
                string location = null;
                try
                {
                    location = (string)JObject.Parse(File.ReadAllText(file))["InstallLocation"];
                }
                catch (Exception ex)
                {
                    LoggingConfig.Log($"Manifiesto de Epic invalido: {file}", ex: ex);
                }
                if (!string.IsNullOrEmpty(location))
                    yield return location;
            }
        }

        // GOG Galaxy: HKLM\SOFTWARE\WOW6432Node\GOG.com\Games\<id>\path.
        private static IEnumerable<string> GogInstallLocations()
        {
            using (var games = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\GOG.com\Games"))
            {
                if (games == null)
                    yield break;

                foreach (var id in games.GetSubKeyNames())
                {
                    using (var game = games.OpenSubKey(id))
                    {
                        if (game?.GetValue("path") is string path)
                            yield return path;
                    }
                }
            }
        }

        // EA app / Origin: no tienen un listado propio confiable; se usan las
        // entradas de "Agregar o quitar programas" publicadas por Electronic Arts.
        private static IEnumerable<string> EaInstallLocations()
        {
            const string uninstall = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            const string uninstall32 = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";

            foreach (var keyPath in new[] { uninstall, uninstall32 })
            {
                using (var root = Registry.LocalMachine.OpenSubKey(keyPath))
                {
                    if (root == null)
                        continue;

                    foreach (var name in root.GetSubKeyNames())
                    {
                        using (var app = root.OpenSubKey(name))
                        {
                            var publisher = app?.GetValue("Publisher") as string;
                            var location = app?.GetValue("InstallLocation") as string;
                            if (publisher != null
                                && publisher.IndexOf("Electronic Arts", StringComparison.OrdinalIgnoreCase) >= 0
                                && !string.IsNullOrWhiteSpace(location)
                                // El propio EA app no es un juego (su carpeta no debe marcar todo como EA).
                                && location.IndexOf(@"\Electronic Arts\EA Desktop", StringComparison.OrdinalIgnoreCase) < 0)
                            {
                                yield return location;
                            }
                        }
                    }
                }
            }
        }
        #endregion
    }
}
