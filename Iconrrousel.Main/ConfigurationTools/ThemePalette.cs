using System;
using System.Globalization;
using System.Windows.Media;

namespace Iconrrousel.Main
{
    // Genera toda la paleta de un tema a partir de un unico color base. La usan
    // tanto el tema Custom como los predefinidos (que son solo colores base
    // fijos, ver UIConfiguration.ThemeColors.PresetBaseColors), asi todos los
    // temas siguen las mismas reglas.
    // Trabaja en HSL: se mantiene el tono (H) del color base y se mueven saturacion
    // y luminosidad, asi todos los colores quedan dentro de la misma familia.
    //   - Fondo: poco saturado; oscuro, o claro si el color base es claro (pastel/blanco).
    //   - Botones de la barra: el mismo tono, semitransparente sobre el fondo.
    //   - Scroll (acento): el color base, con hover/pressed a 8% / 15% de luminosidad,
    //     siempre alejandose del color del texto (asi el contraste nunca baja).
    //   - Texto: blanco o negro, el que tenga mas contraste (WCAG, minimo 4.5:1).
    public sealed class ThemePalette
    {
        public static readonly Color DefaultBaseColor = Color.FromRgb(0x3A, 0x7B, 0xD5);

        // A partir de esta luminosidad del color base el tema pasa a ser claro.
        private const double LightThemeThreshold = 0.70;

        // Limites para que colores extremos (gris puro, casi blanco, casi negro,
        // saturacion 100%) no generen un tema ilegible o chillon.
        // El acento va apagado (poca saturacion, luminosidad cerca del fondo) para
        // que los botones acompanen el tema en vez de resaltar.
        private const double AccentMinSaturation = 0.15;
        private const double AccentMaxSaturation = 0.40;
        private const double DarkThemeAccentMinLightness = 0.30;
        private const double DarkThemeAccentMaxLightness = 0.40;
        private const double LightThemeAccentMinLightness = 0.65;
        private const double LightThemeAccentMaxLightness = 0.75;

        private const double BackgroundSaturationFactor = 0.35;
        private const double BackgroundMaxSaturation = 0.30;
        private const double DarkBackgroundLightness = 0.10;
        private const double LightBackgroundLightness = 0.95;
        private const byte DarkBackgroundAlpha = 0xE0;
        private const byte LightBackgroundAlpha = 0xF0;

        private const double HoverLightnessStep = 0.08;
        private const double PressedLightnessStep = 0.15;

        private const double TopButtonMaxSaturation = 0.50;
        private const double DarkThemeTopButtonLightness = 0.75;
        private const double LightThemeTopButtonLightness = 0.25;
        private const byte TopButtonAlpha = 0x30;
        private const byte TopButtonHoverAlpha = 0x50;
        private const byte TopButtonPressedAlpha = 0x70;

        // Texto deshabilitado: el color de texto a medio camino del fondo.
        private const byte DisabledTextAlpha = 0x99;

        public Color ScrollButtonBackground { get; private set; }
        public Color ScrollButtonHover { get; private set; }
        public Color ScrollButtonPressed { get; private set; }
        public Color ScrollButtonForeground { get; private set; }

        public Color TopButtonBackground { get; private set; }
        public Color TopButtonHover { get; private set; }
        public Color TopButtonPressed { get; private set; }
        public Color TopButtonForeground { get; private set; }

        public Color BorderBackground { get; private set; }
        public Color ShadowColor { get; private set; }
        public Color TextForeground { get; private set; }
        public Color DisabledForeground { get; private set; }

        public static ThemePalette FromBaseColor(Color baseColor)
        {
            ToHsl(baseColor, out double h, out double s, out double l);
            bool lightTheme = l >= LightThemeThreshold;

            // Acento = color base, recortado a un rango usable. Un gris (casi sin
            // saturacion) se respeta como gris: forzarle saturacion lo teñiria de rojo (H = 0).
            double accentS = s < 0.05 ? 0 : Clamp(s, AccentMinSaturation, AccentMaxSaturation);
            double accentL = lightTheme
                ? Clamp(l, LightThemeAccentMinLightness, LightThemeAccentMaxLightness)
                : Clamp(l, DarkThemeAccentMinLightness, DarkThemeAccentMaxLightness);
            Color accent = FromHsl(h, accentS, accentL);
            Color accentForeground = ReadableTextColor(accent);
            double step = accentForeground == Colors.White ? -1 : 1;

            double bgS = Math.Min(s * BackgroundSaturationFactor, BackgroundMaxSaturation);
            double bgL = lightTheme ? LightBackgroundLightness : DarkBackgroundLightness;
            byte bgAlpha = lightTheme ? LightBackgroundAlpha : DarkBackgroundAlpha;

            double topS = Math.Min(s, TopButtonMaxSaturation);
            double topL = lightTheme ? LightThemeTopButtonLightness : DarkThemeTopButtonLightness;

            var palette = new ThemePalette
            {
                ScrollButtonBackground = accent,
                ScrollButtonForeground = accentForeground,
                ScrollButtonHover = FromHsl(h, accentS, accentL + step * HoverLightnessStep),
                ScrollButtonPressed = FromHsl(h, accentS, accentL + step * PressedLightnessStep),

                BorderBackground = FromHsl(h, bgS, bgL, bgAlpha),
                ShadowColor = lightTheme ? FromHsl(h, bgS, 0.20, 0x44) : FromHsl(h, bgS, 0.03),

                TopButtonBackground = FromHsl(h, topS, topL, TopButtonAlpha),
                TopButtonHover = FromHsl(h, topS, topL, TopButtonHoverAlpha),
                TopButtonPressed = FromHsl(h, topS, topL, TopButtonPressedAlpha),
            };

            // El texto se elige contra el color que realmente se ve (los semitransparentes
            // se mezclan con el fondo, y el fondo con lo que haya detras de la ventana).
            Color opaqueBackground = Blend(palette.BorderBackground, lightTheme ? Colors.White : Colors.Black);
            palette.TopButtonForeground = ReadableTextColor(Blend(palette.TopButtonBackground, opaqueBackground));
            palette.TextForeground = ReadableTextColor(opaqueBackground);

            Color text = palette.TextForeground;
            palette.DisabledForeground = Blend(Color.FromArgb(DisabledTextAlpha, text.R, text.G, text.B), opaqueBackground);
            return palette;
        }

        public static bool TryParseHex(string text, out Color color)
        {
            color = default;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            string hex = text.Trim().TrimStart('#');
            if (hex.Length != 6 || !int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rgb))
                return false;

            color = Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
            return true;
        }

        public static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

        // Blanco o negro, el que tenga mayor contraste contra el fondo.
        public static Color ReadableTextColor(Color background)
        {
            double lum = RelativeLuminance(background);
            double contrastWhite = 1.05 / (lum + 0.05);
            double contrastBlack = (lum + 0.05) / 0.05;
            return contrastWhite >= contrastBlack ? Colors.White : Color.FromRgb(0x11, 0x11, 0x11);
        }

        // Luminancia relativa segun WCAG 2.x.
        private static double RelativeLuminance(Color c)
        {
            double Channel(byte v)
            {
                double x = v / 255.0;
                return x <= 0.03928 ? x / 12.92 : Math.Pow((x + 0.055) / 1.055, 2.4);
            }

            return 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
        }

        // Mezcla un color semitransparente sobre un fondo opaco.
        private static Color Blend(Color top, Color bottom)
        {
            double a = top.A / 255.0;
            byte Mix(byte t, byte b) => (byte)Math.Round(t * a + b * (1 - a));
            return Color.FromRgb(Mix(top.R, bottom.R), Mix(top.G, bottom.G), Mix(top.B, bottom.B));
        }

        private static void ToHsl(Color c, out double h, out double s, out double l)
        {
            double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            l = (max + min) / 2;
            if (delta == 0)
            {
                h = 0;
                s = 0;
                return;
            }

            s = l > 0.5 ? delta / (2 - max - min) : delta / (max + min);
            if (max == r)
                h = (g - b) / delta + (g < b ? 6 : 0);
            else if (max == g)
                h = (b - r) / delta + 2;
            else
                h = (r - g) / delta + 4;
            h *= 60;
        }

        private static Color FromHsl(double h, double s, double l, byte alpha = 0xFF)
        {
            s = Clamp(s, 0, 1);
            l = Clamp(l, 0, 1);

            double c = (1 - Math.Abs(2 * l - 1)) * s;
            double x = c * (1 - Math.Abs(h / 60 % 2 - 1));
            double m = l - c / 2;

            double r, g, b;
            if (h < 60) { r = c; g = x; b = 0; }
            else if (h < 120) { r = x; g = c; b = 0; }
            else if (h < 180) { r = 0; g = c; b = x; }
            else if (h < 240) { r = 0; g = x; b = c; }
            else if (h < 300) { r = x; g = 0; b = c; }
            else { r = c; g = 0; b = x; }

            byte ToByte(double v) => (byte)Math.Round(Clamp(v + m, 0, 1) * 255);
            return Color.FromArgb(alpha, ToByte(r), ToByte(g), ToByte(b));
        }

        private static double Clamp(double value, double min, double max) =>
            value < min ? min : value > max ? max : value;
    }
}
