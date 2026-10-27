using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Iconrrousel.Main.Configuration;

namespace Iconrrousel.Main
{
    // Todas las medidas/offsets de la UI viven aca. El code-behind (App.xaml.cs,
    // MainWindow.xaml.cs) y el XAML (via x:Static) leen de esta clase en vez de
    // repetir numeros sueltos.
    public static class UIConfiguration
    {
        // "Chrome" de la ventana: Border, Grid interno y sombra.
        public static class WindowChrome
        {
            public const double MinWidth = 150;
            public static readonly CornerRadius BorderCornerRadius = new CornerRadius(15);
            public static readonly Thickness BorderMargin = new Thickness(8);
            public static readonly Thickness BorderPadding = new Thickness(4);
            public const double ContentRowMinHeight = 110;
            public static readonly Thickness InnerGridMargin = new Thickness(2, 0, 2, 0);
            public const double ShadowDirection = 270;
            public const double ShadowDepth = 5;
            public const double ShadowBlurRadius = 20;
            public const double ShadowOpacity = 0.7;

            // Overhead vertical fijo (padding/margenes del Border + espacio para el
            // DropShadowEffect) que se suma a rows*IconGrid.RowPitch para el alto real
            // de la ventana - ya hacia que 1 fila (100) diera la altura original (140).
            public const double HeightOverhead = 40;
        }

        // ScrollViewer que envuelve la tabla de iconos (IconsPanel).
        public static class IconViewer
        {
            public const double MinWidth = 60;
            public static readonly Thickness Padding = new Thickness(10, 0, 10, 0);

            // Margen extra reservado en SharedData.IconViewerWidth para que el Padding
            // de arriba no termine recortando la primera/ultima columna de iconos.
            public const double ExtraWidthForPadding = 30;
        }

        // Botones ScrollLeft / ScrollRight.
        public static class ScrollButton
        {
            public const double Width = 30;
            public const double Height = 80;
            public static readonly Thickness LeftMargin = new Thickness(0, 0, 2, 0);
            public static readonly Thickness RightMargin = new Thickness(2, 0, 0, 0);
            public static readonly CornerRadius ButtonCornerRadius = new CornerRadius(10);
            public static readonly Thickness ContentPadding = new Thickness(2);
            public const double FontSize = 16;
        }

        // Boton individual de icono (IconButtonStyle).
        public static class IconButtonChrome
        {
            public static readonly CornerRadius ButtonCornerRadius = new CornerRadius(8);
        }

        // Icono individual dentro de la tabla (imagen + nombre).
        public static class Icon
        {
            public const double ButtonWidth = 90;
            public const double ButtonHeight = 100;
            public const double ImageHeight = 64;
            public const double ImageHeightWithLabel = 54;
            public const double LabelFontSize = 11;
            public const double LabelTopMargin = 4;
            public const double LabelExtraWidth = 10; // ancho maximo del label = ButtonWidth + esto
            public static readonly Thickness ButtonMargin = new Thickness(4, 0, 4, 0);
        }

        // Tabla de iconos (UniformGrid): colapsada 1 fila, expandida hasta 5.
        public static class IconGrid
        {
            public const double ColumnPitch = Icon.ButtonWidth + 8; // 90 + margen (4+4) de cada icono
            public const double RowPitch = Icon.ButtonHeight;       // 100, sin margen vertical
            public const int MaxExpandedRows = 5;
        }

        // Filtros / Expandir: botones flotantes sobre los iconos.
        public static class ActionButtons
        {
            public const double Size = 18;
            public static readonly CornerRadius ButtonCornerRadius = new CornerRadius(9);
            public const double BottomOffset = 5;  // distancia fija al borde inferior de la pill
            public const double IconGlyphSize = 10;
            public const double IconStrokeThickness = 2;
            public const double FiltrosLeftOffset = 40;    // distancia fija al borde IZQUIERDO, igual en todos los tamanos
            public const double ExpandirRightOffset = 40;  // distancia fija al borde DERECHO, igual en todos los tamanos
        }

        // Un tamano de ventana = cuantos iconos entran por fila + el ancho maximo
        // de la pill para esa cantidad. Reemplaza el switch-case repetido que
        // habia antes en SharedData.ApplyWindowSize.
        public readonly struct WindowSizeTier
        {
            public WindowSizeTier(int visibleIconCount, double windowMaxWidth)
            {
                VisibleIconCount = visibleIconCount;
                WindowMaxWidth = windowMaxWidth;
            }

            public int VisibleIconCount { get; }
            public double WindowMaxWidth { get; }
        }

        public static class WindowSizeTiers
        {
            public static readonly SettingsFields.WindowSizeOption Default = SettingsFields.WindowSizeOption.Medium;

            public static readonly IReadOnlyDictionary<SettingsFields.WindowSizeOption, WindowSizeTier> All =
                new Dictionary<SettingsFields.WindowSizeOption, WindowSizeTier>
                {
                    { SettingsFields.WindowSizeOption.Small, new WindowSizeTier(3, 400) },
                    { SettingsFields.WindowSizeOption.Medium, new WindowSizeTier(5, 596) },
                    { SettingsFields.WindowSizeOption.Large, new WindowSizeTier(8, 890) },
                    { SettingsFields.WindowSizeOption.ExtraLarge, new WindowSizeTier(10, 1086) },
                };

            public static WindowSizeTier Get(SettingsFields.WindowSizeOption option) =>
                All.TryGetValue(option, out var tier) ? tier : All[Default];
        }

        // Temas predefinidos: cada uno es solo un color base "de acceso rapido";
        // la paleta completa se genera igual que en el tema Custom (ThemePalette).
        public static class ThemeColors
        {
            public static readonly IReadOnlyDictionary<SettingsFields.ThemeOption, Color> PresetBaseColors =
                new Dictionary<SettingsFields.ThemeOption, Color>
                {
                    { SettingsFields.ThemeOption.Dark, Color.FromRgb(0x55, 0x55, 0x55) },
                    { SettingsFields.ThemeOption.Light, Color.FromRgb(0xFF, 0xFF, 0xFF) },
                    { SettingsFields.ThemeOption.Beige, Color.FromRgb(0xD8, 0xC3, 0xA5) },
                    { SettingsFields.ThemeOption.Blue, Color.FromRgb(0x4A, 0x90, 0xE2) },
                    { SettingsFields.ThemeOption.Pink, Color.FromRgb(0xF5, 0xC1, 0xE8) },
                    { SettingsFields.ThemeOption.Red, Color.FromRgb(0xE0, 0x48, 0x48) },
                };

            // Color base del tema: el del predefinido, o el elegido si es Custom.
            public static Color GetBaseColor(SettingsFields.ThemeOption theme, string customColor)
            {
                if (PresetBaseColors.TryGetValue(theme, out var preset))
                    return preset;

                return ThemePalette.TryParseHex(customColor, out var custom) ? custom : ThemePalette.DefaultBaseColor;
            }
        }

        // Fondo del icono al pasar el mouse / presionarlo, segun la plataforma
        // detectada (PlatformDetector). Sin plataforma se usa el hover del tema.
        public static class PlatformColors
        {
            public static readonly IReadOnlyDictionary<GamePlatform, Color> BrandColors =
                new Dictionary<GamePlatform, Color>
                {
                    { GamePlatform.Steam, Color.FromRgb(0x1A, 0x9F, 0xFF) }, // azul de la marca Steam
                    { GamePlatform.Gog, Color.FromRgb(0x86, 0x32, 0x8A) },   // violeta GOG
                    { GamePlatform.Epic, Color.FromRgb(0x12, 0x12, 0x12) },  // negro de Epic
                    { GamePlatform.EA, Color.FromRgb(0xF5, 0x6C, 0x2D) },    // naranja EA/Origin
                    { GamePlatform.Xbox, Color.FromRgb(0x10, 0x7C, 0x10) },  // verde Xbox
                };

            public const byte HoverAlpha = 0x70;
            public const byte PressedAlpha = 0xA0;

            // Plataformas que necesitan otra opacidad: el negro de Epic casi no se
            // distingue sobre el tema oscuro con la opacidad general.
            public static readonly IReadOnlyDictionary<GamePlatform, (byte Hover, byte Pressed)> AlphaOverrides =
                new Dictionary<GamePlatform, (byte Hover, byte Pressed)>
                {
                    { GamePlatform.Epic, (0xB0, 0xD8) },
                };
        }
    }
}
