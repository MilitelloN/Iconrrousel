# Resumen de la migración: .NET Framework 4.8 → .NET 10

Migración de runtime de Iconrrousel.Main. La app sigue siendo WPF, con la misma UI y el mismo comportamiento; solo cambia la tecnología sobre la que corre.

## 1) Formato del proyecto (csproj)

Se reemplazó el `.csproj` de formato clásico (con `<ItemGroup>` listando cada archivo `.cs`/`.xaml` a mano) por el formato moderno "SDK-style", apuntando a `net10.0-windows`.

- **Cómo afectaba antes:** cada archivo nuevo (`.cs`, `.xaml`) había que agregarlo manualmente al `.csproj`, o Visual Studio lo hacía por vos. Era fácil que un archivo quedara "fantasma" (en el csproj pero borrado del disco, o en el disco pero no compilado).
- **Cómo afecta ahora:** el proyecto incluye automáticamente todo `.cs`/`.xaml` de la carpeta por convención. El csproj pasó de ~140 líneas a ~25. Nada de esto cambia qué código corre, solo cómo se lo referencia.

## 2) Paquete NuGet (Newtonsoft.Json)

Se eliminó `packages.config` y se pasó `Newtonsoft.Json 13.0.4` (misma versión, sin cambios) a `<PackageReference>` dentro del csproj.

- **Cómo afectaba antes:** el paquete se restauraba a una carpeta `packages/` compartida en la solución.
- **Cómo afecta ahora:** se restaura al caché global de NuGet (`~/.nuget/packages`) como hace cualquier proyecto .NET moderno. Es el mismo paquete, misma versión, mismo comportamiento de serialización JSON.

## 3) Archivo de configuración de runtime (App.config)

Se borró `App.config` (solo tenía la declaración de que la app corre sobre .NET Framework 4.8, algo que ya no aplica).

- **Cómo afectaba antes:** Windows leía ese archivo para saber qué versión de .NET Framework usar.
- **Cómo afecta ahora:** .NET genera automáticamente un `Iconrrousel.runtimeconfig.json` con la misma información (qué versión de runtime necesita). No hay ningún comportamiento que dependiera de ese archivo y se haya perdido.

## 4) Scaffolding sin usar (Settings.settings / Resources.resx)

Se eliminaron 4 archivos: `Properties/Settings.settings`, `Properties/Settings.Designer.cs`, `Properties/Resources.resx`, `Properties/Resources.Designer.cs`.

- **Cómo afectaba antes:** eran plantillas vacías generadas por Visual Studio al crear el proyecto (0 configuraciones, 0 recursos definidos). No se usaban en ningún lado del código — la app ya tiene su propio sistema de configuración vía `Settings.json`/`Paths.json`.
- **Cómo afecta ahora:** no existen. Si se hubieran mantenido, habría sido necesario agregar una dependencia nueva (`System.Configuration.ConfigurationManager`) solo para sostener un scaffold que no hace nada. Cero impacto funcional porque nada los usaba.

## 5) Bug real encontrado y corregido: "Iniciar con Windows"

En `Views/SettingsWindow.xaml.cs`, el método que registra la app para iniciar junto con Windows usaba `Assembly.GetExecutingAssembly().Location` para obtener la ruta a guardar en el registro.

- **Cómo afectaba antes:** en .NET Framework, el `.exe` y el ensamblado administrado son el mismo archivo, así que esa ruta apuntaba correctamente a `Iconrrousel.exe`.
- **Cómo afecta ahora:** en .NET moderno, el `.exe` (el lanzador nativo que Windows puede ejecutar) y el `.dll` (el código administrado) son dos archivos separados. `Assembly.GetExecutingAssembly().Location` pasó a devolver la ruta al `.dll`, que Windows no puede ejecutar directamente desde el registro de inicio — la opción "Start with windows" habría quedado silenciosamente rota. Se corrigió usando `Environment.ProcessPath`, que siempre devuelve la ruta del `.exe` que arrancó el proceso. Verificado contra la app real: el registro ahora vuelve a apuntar a `Iconrrousel.exe`, funcionalidad idéntica a antes.

## 6) Warnings de compatibilidad de plataforma (CA1416)

Al migrar aparecieron ~164 warnings de compilación del tipo "esta API solo funciona en Windows", sobre código que ya usaba `System.Drawing`, `System.Windows.Forms` y el registro de Windows (todo esto correcto para esta app, que es exclusiva de Windows).

- **Cómo afectaba antes:** no existía este chequeo; el compilador de .NET Framework no lo hacía.
- **Cómo afecta ahora:** se agregó `[assembly: SupportedOSPlatform("windows")]` en `AssemblyInfo.cs`, que le dice al compilador "este ensamblado es exclusivo de Windows a propósito". Con eso, los 164 warnings desaparecen (0 warnings, 0 errores). No es un cambio de comportamiento, es callar una alerta que no aplicaba a este proyecto.

## 7) Instalador (WiX)

El instalador (`Iconrrousel.Installer`) no necesitó cambios de código — solo se identificó que había que compilarlo apuntando a la plataforma `x64` (`dotnet build Iconrrousel.slnx` o el `.sln` normal ya lo hace automáticamente; compilar el `.wixproj` suelto sin pasar por la solución no).

- **Cómo afectaba antes:** el instalador empaquetaba los binarios de .NET Framework.
- **Cómo afecta ahora:** empaqueta los binarios de .NET 10. La instalación, el ícono, el acceso directo y el desinstalador funcionan igual. Único cambio real de "peso": ahora la PC donde se instale necesita tener el **.NET Desktop Runtime 10** instalado (antes, .NET Framework 4.8 ya viene con Windows). Si se prefiere evitar ese requisito, la alternativa es publicar la app en modo "self-contained" (incluye el runtime dentro del instalador, a costa de un instalador más pesado) — no se hizo porque prioriza menor consumo de disco y es el equivalente más directo al esquema anterior.

## Resultado medible

Build en Release: **0 errores, 0 warnings** en la app y en el instalador. Carpeta de salida (`bin/`) bajó de referenciar ensamblados grandes de interoperabilidad de Windows (probados y descartados durante la migración) a solo 3 archivos: `Iconrrousel.exe`, `Iconrrousel.dll`, `Newtonsoft.Json.dll` — mismo footprint mínimo que antes.

Pendiente si se quiere cuantificar la mejora real de recursos: repetir el protocolo de medición del informe de línea base (arranque, memoria, CPU, GDI/USER) contra este build y comparar contra los números ya registrados con .NET Framework 4.8.
