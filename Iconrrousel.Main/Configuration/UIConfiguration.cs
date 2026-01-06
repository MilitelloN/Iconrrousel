using System.Windows;
using System.Windows.Media;

namespace Iconrrousel.Main
{
    public static class UIConfiguration
    {
        // Window Configuration
        public static class Window
        {
            public const double DefaultWidth = 320;
            public const double DefaultHeight = 190;
            public const double MinWidth = 320;
        }

        // Main Border Configuration
        public static class MainBorder
        {
            public const double CornerRadius = 15;
            public const double MarginSize = 10;
            public const double PaddingSize = 6;
            public static readonly Color BackgroundColor = Color.FromArgb(0xE0, 0x00, 0x00, 0x00);
            
            // Shadow Effect
            public const double ShadowDepth = 5;
            public const double ShadowBlurRadius = 20;
            public const double ShadowOpacity = 0.7;
        }

        // Top Button (Config Button)
        public static class TopButton
        {
            public const double Width = 35;
            public const double Height = 26;
            public const double BottomMargin = 6;
            public const string Icon = "?";
            
            // Colors
            public static readonly Color BackgroundColor = Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF);
            public static readonly Color HoverColor = Color.FromArgb(0x50, 0xFF, 0xFF, 0xFF);
            public static readonly Color PressedColor = Color.FromArgb(0x70, 0xFF, 0xFF, 0xFF);
        }

        // Scroll Buttons Configuration
        public static class ScrollButtons
        {
            public const double Width = 40;
            public const double Height = 80;
            public const double CornerRadius = 10;
            public const double HorizontalMargin = 10;
            public const string LeftIcon = "?";
            public const string RightIcon = "?";
            
            // Colors
            public static readonly Color BackgroundColor = Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF);
            public static readonly Color HoverColor = Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF);
            public static readonly Color PressedColor = Color.FromArgb(0x60, 0xFF, 0xFF, 0xFF);
            
            // Scroll Amount
            public const double ScrollOffset = 200;
        }

        // Icon Panel Configuration
        public static class IconPanel
        {
            public const double MinWidth = 50;
            public const double MaxWidth = 600;
            public const double ScrollViewerHorizontalMargin = 4;
        }

        // Individual Icon Configuration
        public static class Icon
        {
            public const double ButtonWidth = 70;
            public const double ButtonHeight = 90;
            public const double ImageWidth = 64;
            public const double ImageHeight = 64;
            public const double ImageWidthWithLabel = 54;
            public const double ImageHeightWithLabel = 54;
            
            // Margins
            public static readonly Thickness ButtonMargin = new Thickness(10, 6, 10, 6);
        }
    }
}
