using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Iconrrousel.Main
{
    // Brushes de hover/pressed por boton de icono. IconButtonStyle los lee con
    // {Binding (local:PlatformHover.HoverBrush), RelativeSource=TemplatedParent}.
    // Con plataforma: el color de la marca (UIConfiguration.PlatformColors).
    // Sin plataforma: binding al hover/pressed del tema, asi sigue los cambios de tema.
    public static class PlatformHover
    {
        public static readonly DependencyProperty HoverBrushProperty =
            DependencyProperty.RegisterAttached("HoverBrush", typeof(Brush), typeof(PlatformHover), new PropertyMetadata(null));

        public static readonly DependencyProperty PressedBrushProperty =
            DependencyProperty.RegisterAttached("PressedBrush", typeof(Brush), typeof(PlatformHover), new PropertyMetadata(null));

        public static Brush GetHoverBrush(DependencyObject obj) => (Brush)obj.GetValue(HoverBrushProperty);
        public static void SetHoverBrush(DependencyObject obj, Brush value) => obj.SetValue(HoverBrushProperty, value);

        public static Brush GetPressedBrush(DependencyObject obj) => (Brush)obj.GetValue(PressedBrushProperty);
        public static void SetPressedBrush(DependencyObject obj, Brush value) => obj.SetValue(PressedBrushProperty, value);

        private static readonly Dictionary<GamePlatform, (Brush Hover, Brush Pressed)> _brushes =
            new Dictionary<GamePlatform, (Brush, Brush)>();

        public static void Apply(DependencyObject button, GamePlatform platform)
        {
            if (UIConfiguration.PlatformColors.BrandColors.TryGetValue(platform, out var color))
            {
                var (hover, pressed) = GetBrushes(platform, color);
                SetHoverBrush(button, hover);
                SetPressedBrush(button, pressed);
            }
            else
            {
                BindingOperations.SetBinding(button, HoverBrushProperty, new Binding("TopButtonHover") { Source = App.Data });
                BindingOperations.SetBinding(button, PressedBrushProperty, new Binding("TopButtonPressed") { Source = App.Data });
            }
        }

        private static (Brush Hover, Brush Pressed) GetBrushes(GamePlatform platform, Color color)
        {
            if (!_brushes.TryGetValue(platform, out var brushes))
            {
                if (!UIConfiguration.PlatformColors.AlphaOverrides.TryGetValue(platform, out var alpha))
                    alpha = (UIConfiguration.PlatformColors.HoverAlpha, UIConfiguration.PlatformColors.PressedAlpha);

                brushes = (CreateBrush(color, alpha.Hover), CreateBrush(color, alpha.Pressed));
                _brushes[platform] = brushes;
            }
            return brushes;
        }

        private static Brush CreateBrush(Color color, byte alpha)
        {
            var brush = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
            brush.Freeze();
            return brush;
        }
    }
}
