using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media.Animation;
using System.Xml.Serialization;

namespace carrouselProject
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("shell32.dll", EntryPoint = "#727")]
        private static extern int SHGetImageList(int iImageList, ref Guid riid, out IImageList ppv);

        [ComImport]
        [Guid("46EB5926-582E-4017-9FDF-E8998DAA0950")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IImageList
        {
            [PreserveSig]
            int Add(IntPtr hbmImage, IntPtr hbmMask, ref int pi);

            [PreserveSig]
            int ReplaceIcon(int i, IntPtr hicon, ref int pi);

            [PreserveSig]
            int SetOverlayImage(int iImage, int iOverlay);

            [PreserveSig]
            int Replace(int i, IntPtr hbmImage, IntPtr hbmMask);

            [PreserveSig]
            int AddMasked(IntPtr hbmImage, int crMask, ref int pi);

            [PreserveSig]
            int Draw(ref IMAGELISTDRAWPARAMS pimldp);

            [PreserveSig]
            int Remove(int i);

            [PreserveSig]
            int GetIcon(int i, int flags, ref IntPtr picon);
        };

        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGELISTDRAWPARAMS
        {
            public int cbSize;
            public IntPtr himl;
            public int i;
            public IntPtr hdcDst;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public int xBitmap;
            public int yBitmap;
            public int rgbBk;
            public int rgbFg;
            public int fStyle;
            public int dwRop;
            public int fState;
            public int Frame;
            public int crEffect;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        private const uint SHGFI_ICON = 0x100;
        private const uint SHGFI_LARGEICON = 0x0;
        private const uint SHGFI_SMALLICON = 0x1;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x10;
        private const uint SHGFI_SYSICONINDEX = 0x4000;

        private const int SHIL_JUMBO = 0x4;
        private const int SHIL_EXTRALARGE = 0x2;

        private List<string> iconFiles = new List<string>();
        private int currentOffset = 0;
        private const int ICON_SIZE = 80;
        private const int ICON_SPACING = 100;
        private const int SCROLL_COUNT = 3;
        private const string SETTINGS_FILE = "carousel_icons.xml";

        public MainWindow()
        {
            InitializeComponent();
            CarouselViewport.SizeChanged += CarouselViewport_SizeChanged;
            LoadIconPaths();
        }

        private void LoadIconPaths()
        {
            try
            {
                string settingsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SETTINGS_FILE);
                if (File.Exists(settingsPath))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(List<string>));
                    using (FileStream fs = new FileStream(settingsPath, FileMode.Open))
                    {
                        iconFiles = (List<string>)serializer.Deserialize(fs);
                        iconFiles.Sort();
                    }
                    RebuildCarousel();
                    UpdateArrowVisibility();
                }
            }
            catch
            {
                // If loading fails, start with empty list
                iconFiles = new List<string>();
            }
        }

        private void SaveIconPaths()
        {
            try
            {
                string settingsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SETTINGS_FILE);
                XmlSerializer serializer = new XmlSerializer(typeof(List<string>));
                using (FileStream fs = new FileStream(settingsPath, FileMode.Create))
                {
                    serializer.Serialize(fs, iconFiles);

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving icons: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CarouselViewport_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateCarouselDisplay();
        }

        private void CarouselScrollViewer_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void CarouselScrollViewer_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void CarouselScrollViewer_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                
                foreach (string file in files)
                {
                    if (!iconFiles.Contains(file))
                    {
                        iconFiles.Add(file);
                    }
                }
                
                SaveIconPaths();
                RebuildCarousel();
                UpdateArrowVisibility();
            }
        }

        private void RebuildCarousel()
        {
            CarouselPanel.Children.Clear();
            
            if (iconFiles.Count == 0)
            {
                PlaceholderText.Visibility = Visibility.Visible;
                return;
            }
            
            PlaceholderText.Visibility = Visibility.Collapsed;

            // Only add each icon once (no duplication)
            foreach (string filePath in iconFiles)
            {
                var iconElement = CreateIconElement(filePath);
                CarouselPanel.Children.Add(iconElement);
            }
            
            UpdateCarouselDisplay();
        }

        private BitmapSource GetFileIcon(string filePath)
        {
            try
            {
                // Try to get jumbo icon (256x256) for highest quality
                SHFILEINFO shfi = new SHFILEINFO();
                IntPtr hSuccess = SHGetFileInfo(filePath, 0, ref shfi, (uint)Marshal.SizeOf(shfi), SHGFI_SYSICONINDEX);

                if (hSuccess != IntPtr.Zero && shfi.iIcon >= 0)
                {
                    Guid iidImageList = new Guid("46EB5926-582E-4017-9FDF-E8998DAA0950");
                    IImageList iml;

                    // Try to get JUMBO icon list (256x256)
                    int hResult = SHGetImageList(SHIL_JUMBO, ref iidImageList, out iml);

                    if (hResult == 0)
                    {
                        IntPtr hIcon = IntPtr.Zero;
                        hResult = iml.GetIcon(shfi.iIcon, 0x1, ref hIcon);

                        if (hIcon != IntPtr.Zero)
                        {
                            try
                            {
                                BitmapSource bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                                    hIcon,
                                    Int32Rect.Empty,
                                    BitmapSizeOptions.FromEmptyOptions());

                                bitmapSource.Freeze();
                                return bitmapSource;
                            }
                            finally
                            {
                                DestroyIcon(hIcon);
                            }
                        }
                    }
                }

                // Fallback to standard large icon
                shfi = new SHFILEINFO();
                IntPtr hIcon2 = SHGetFileInfo(filePath, 0, ref shfi, (uint)Marshal.SizeOf(shfi), SHGFI_ICON | SHGFI_LARGEICON);

                if (shfi.hIcon != IntPtr.Zero)
                {
                    try
                    {
                        BitmapSource bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                            shfi.hIcon,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());

                        bitmapSource.Freeze();
                        return bitmapSource;
                    }
                    finally
                    {
                        DestroyIcon(shfi.hIcon);
                    }
                }
            }
            catch
            {
                // Silently fail and return null
            }

            return null;
        }

        private Border CreateIconElement(string filePath)
        {
            // Capture filePath in local variable to avoid closure issues
            string capturedPath = filePath;
            
            var iconContainer = new Border
            {
                Width = ICON_SPACING,
                Height = 85,
                Margin = new Thickness(5, 2.5, 5, 2.5),
                Background = Brushes.Transparent,
                CornerRadius = new CornerRadius(8),
                Cursor = Cursors.Hand,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(0)
            };

            var grid = new Grid
            {
                Width = ICON_SPACING,
                Height = 85
            };
            
            // Extract and display icon
            BitmapSource iconSource = GetFileIcon(capturedPath);
            
            if (iconSource != null)
            {
                var image = new System.Windows.Controls.Image
                {
                    Source = iconSource,
                    Width = ICON_SIZE,
                    Height = ICON_SIZE,
                    Stretch = Stretch.Fill,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    ToolTip = System.IO.Path.GetFileName(capturedPath)
                };

                grid.Children.Add(image);
            }
            else
            {
                // Fallback: show a placeholder if icon extraction fails
                var placeholder = new TextBlock
                {
                    Text = "📄",
                    FontSize = 64,
                    Width = ICON_SIZE,
                    Height = ICON_SIZE,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = Brushes.Gray,
                    ToolTip = System.IO.Path.GetFileName(capturedPath)
                };
                grid.Children.Add(placeholder);
            }

            iconContainer.Child = grid;
            iconContainer.Tag = capturedPath;

            // Hover effect - subtle glow
            iconContainer.MouseEnter += (s, e) =>
            {
                var dropShadow = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = System.Windows.Media.Color.FromRgb(0, 122, 204),
                    BlurRadius = 15,
                    ShadowDepth = 0,
                    Opacity = 0.8
                };
                iconContainer.Effect = dropShadow;
            };

            iconContainer.MouseLeave += (s, e) =>
            {
                iconContainer.Effect = null;
            };

            // Click to open - use capturedPath directly
            iconContainer.MouseLeftButtonUp += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(capturedPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not open file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                e.Handled = true;
            };

            // Right-click context menu
            var contextMenu = new ContextMenu();
            contextMenu.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 48));
            contextMenu.Foreground = Brushes.White;
            
            var removeMenuItem = new MenuItem 
            { 
                Header = "Remove from Carousel",
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 48)),
                Foreground = Brushes.White
            };
            
            // Use capturedPath for removal
            removeMenuItem.Click += (s, e) =>
            {
                iconFiles.Remove(capturedPath);
                SaveIconPaths();
                RebuildCarousel();
                UpdateArrowVisibility();
            };
            
            contextMenu.Items.Add(removeMenuItem);
            iconContainer.ContextMenu = contextMenu;

            return iconContainer;
        }

        private void UpdateCarouselDisplay()
        {
            if (iconFiles.Count == 0) return;

            // Position the carousel based on current offset
            double position = -currentOffset * ICON_SPACING;
            Canvas.SetLeft(CarouselPanel, position);
        }

        private void UpdateArrowVisibility()
        {
            if (iconFiles.Count == 0)
            {
                LeftArrowButton.Visibility = Visibility.Collapsed;
                RightArrowButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                double viewportWidth = CarouselViewport.ActualWidth;
                double totalWidth = iconFiles.Count * ICON_SPACING;
                
                if (totalWidth > viewportWidth)
                {
                    LeftArrowButton.Visibility = Visibility.Visible;
                    RightArrowButton.Visibility = Visibility.Visible;
                }
                else
                {
                    LeftArrowButton.Visibility = Visibility.Collapsed;
                    RightArrowButton.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void LeftArrow_Click(object sender, RoutedEventArgs e)
        {
            if (iconFiles.Count == 0) return;

            currentOffset -= SCROLL_COUNT;
            
            // Infinite scroll: wrap around to the end
            while (currentOffset < 0)
            {
                currentOffset += iconFiles.Count;
            }
            
            AnimateCarousel();
        }

        private void RightArrow_Click(object sender, RoutedEventArgs e)
        {
            if (iconFiles.Count == 0) return;

            currentOffset += SCROLL_COUNT;
            
            // Infinite scroll: wrap around to the beginning
            currentOffset = currentOffset % iconFiles.Count;
            
            AnimateCarousel();
        }

        private void AnimateCarousel()
        {
            double targetPosition = -currentOffset * ICON_SPACING;
            
            // For infinite scroll effect, we need to handle wrapping smoothly
            double currentPosition = Canvas.GetLeft(CarouselPanel);
            
            // Check if we need to reset position for seamless infinite scroll
            double maxScroll = (iconFiles.Count - 1) * ICON_SPACING;
            
            var animation = new DoubleAnimation
            {
                From = currentPosition,
                To = targetPosition,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            CarouselPanel.BeginAnimation(Canvas.LeftProperty, animation);
        }
    }
}
