using Microsoft.WindowsAPICodePack.Shell;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using Image = System.Windows.Controls.Image;
using Iconrrousel.Main.Services;


namespace Iconrrousel.Main
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region DragMoveWindow
        [DllImport("user32.dll")]
        static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(
            IntPtr hWnd,
            int Msg,
            int wParam,
            int lParam
        );

        const int WM_NCLBUTTONDOWN = 0xA1;
        const int HTCAPTION = 0x2;

        private void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Si el click cae DENTRO de un Button (en cualquier parte de su
            // Template - Border, Path, Image, TextBlock, etc.) -> NO mover
            // ventana, dejar que el click le llegue al boton. Antes solo se
            // chequeaba el tipo exacto de OriginalSource (Button/Image/
            // TextBlock), lo que dejaba afuera botones con otro contenido
            // (por ej. Filtros/Expandir, que usan un Path) - un click sobre
            // el Path o el borde del circulo no matcheaba ninguno de esos
            // tipos y terminaba disparando el drag de ventana, comiendose el
            // click antes de que llegara al Button.Click.
            if (e.OriginalSource is DependencyObject source && FindAncestorButton(source) != null)
                return;

            ReleaseCapture();
            SendMessage(
                new WindowInteropHelper(this).Handle,
                0xA1,
                0x2,
                0
            );
        }

        private static Button FindAncestorButton(DependencyObject source)
        {
            while (source != null)
            {
                if (source is Button btn)
                    return btn;

                var parent = VisualTreeHelper.GetParent(source);
                source = parent ?? LogicalTreeHelper.GetParent(source);
            }
            return null;
        }
        #endregion

        List<string> _paths = new List<string>();
        // Un Button persistente por icono, en el mismo orden que _paths. UpdateIconLayout
        // decide cuales de estos entran en IconsPanel.Children (los de la pagina actual
        // nomas, sea colapsado o expandido) - moverlos entre paneles no los destruye, asi
        // que se reusan en vez de recrearlos en cada cambio de pagina.
        private List<Button> _iconButtons = new List<Button>();
        private string _PATHS_FILE = "Paths.json";
        private bool _expanded = false;
        // Pagina actual (colapsado: 1 fila x VisibleIconCount iconos por pagina;
        // expandido: hasta MaxExpandedRows x VisibleIconCount). Ya no hay scroll en
        // pixeles: los botones ScrollLeft/ScrollRight solo cambian esta pagina.
        private int _pageIndex = 0;
        public TimeRangeService TimeRangeService { get; private set; }

        private void Log(string message, [CallerMemberName] string caller = null, Exception ex = null)
        {
            LoggingConfig.EnsureConfigured();
            var baseMsg = $"[{DateTime.Now:O}] {caller}: {message}";
            if (ex != null)
            {
                baseMsg += $" | Exception: {ex}";
            }
            Trace.WriteLine(baseMsg);
        }

        private static void LogStatic(string message, [CallerMemberName] string caller = null, Exception ex = null)
        {
            LoggingConfig.EnsureConfigured();
            var baseMsg = $"[{DateTime.Now:O}] {caller}: {message}";
            if (ex != null)
            {
                baseMsg += $" | Exception: {ex}";
            }
            Trace.WriteLine(baseMsg);
        }

        public MainWindow()
        {
            Log("Initializing MainWindow");
            DataContext = App.Data;
            var settings = App.Data.LoadSettings();
            InitializeComponent();

            TimeRangeService = new TimeRangeService();
            if (settings._timeRanges != null && settings._timeRanges.Count > 0)
            {
                TimeRangeService.UpdateTimeRanges(settings._timeRanges);
            }

            PreviewMouseLeftButtonDown += Window_PreviewMouseLeftButtonDown;
            App.IconService.OnDeleteAllIcons += DeleteAllIcons;
            Closed += MainWindow_Closed;
            SizeChanged += MainWindow_SizeChanged;

            this.AllowDrop = true;

            if (File.Exists(Path.Combine(AppPaths.DataDir, _PATHS_FILE)))
            {
                try
                {
                    var json = File.ReadAllText(Path.Combine(AppPaths.DataDir, _PATHS_FILE));
                    var items = JsonConvert.DeserializeObject<List<string>>(json);
                    items.Sort(CompareByDisplayName);

                    foreach (var item in items)
                    {
                        _paths.Add(item);
                        _iconButtons.Add((Button)getButton(item));
                    }
                }
                catch (Exception ex)
                {
                    Log("Error loading paths file", ex: ex);
                }
            }

            UpdateIconLayout();
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Log("SizeChanged");
            try
            {
                if (IsLoaded)
                {
                    CenterWindowOnTopOfScreen();
                }
            }
            catch (Exception ex)
            {
                Log("Error in SizeChanged", ex: ex);
            }
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            Log("Closed event");
            try
            {
                App.IconService.OnDeleteAllIcons -= DeleteAllIcons;
                TimeRangeService?.Stop();
                CleanupIconPanel();
            }
            catch (Exception ex)
            {
                Log("Error in Closed handler", ex: ex);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Log("Loaded event");
            try
            {
                CenterWindowOnTopOfScreen();
            }
            catch (Exception ex)
            {
                Log("Error in Loaded handler", ex: ex);
            }
        }

        private void CenterWindowOnTopOfScreen()
        {
            Log("Centering window");
            try
            {
                var screens = System.Windows.Forms.Screen.AllScreens;
                var screen = screens.Length > 1 ? screens[1] : System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(this).Handle);
                UpdateLayout();

                double screenWidth = screen.WorkingArea.Width;
                double screenLeft = screen.WorkingArea.Left;
                double screenTop = screen.WorkingArea.Top;

                Left = screenLeft + (screenWidth - ActualWidth) / 2;
                Top = screenTop;
            }
            catch (Exception ex)
            {
                Log("Error centering window", ex: ex);
            }
        }

        // Recalcula la tabla de iconos (UniformGrid) como PAGINAS: colapsada, 1
        // fila x VisibleIconCount iconos por pagina; expandida, hasta
        // MaxExpandedRows filas x VisibleIconCount columnas por pagina. No hay
        // scroll en pixeles - ScrollLeft/Right (mas abajo) solo cambian
        // _pageIndex y esto vuelve a pintar esa pagina desde cero (sin
        // solapar ningun icono/columna con la pagina anterior).
        //
        // Solo la PRIMERA pagina (_pageIndex == 0) es "adaptable": si no hay
        // suficientes iconos guardados para llenarla, la ventana se achica en
        // vez de mostrar celdas vacias (alto si esta expandida, ancho si esta
        // colapsada). Cualquier otra pagina - incluida una ultima pagina
        // incompleta - siempre usa el tamano maximo (MaxExpandedRows x
        // VisibleIconCount), con celdas vacias si le faltan iconos para
        // llenarla, para que la ventana no cambie de tamano al pasar de pagina.
        //
        // Se llama al cargar/agregar/quitar iconos, al togglear Expandir, al
        // cambiar de pagina y al cambiar el tamano de ventana elegido (desde
        // App.xaml.cs).
        public void UpdateIconLayout()
        {
            Log("Updating icon layout");
            try
            {
                // LoadSettings() (y por lo tanto ApplyWindowSize) corre ANTES de
                // InitializeComponent() en el constructor - en ese momento
                // IconsPanel todavia es null. El constructor vuelve a llamar a
                // UpdateIconLayout() el mismo una vez que ya esta inicializada.
                if (IconsPanel == null)
                {
                    return;
                }

                int totalIcons = _iconButtons.Count;
                int visibleColumns = Math.Max(1, App.Data.VisibleIconCount);
                int maxRows = _expanded ? UIConfiguration.IconGrid.MaxExpandedRows : 1;
                int pageCapacity = maxRows * visibleColumns;
                int totalPages = Math.Max(1, (int)Math.Ceiling(totalIcons / (double)pageCapacity));

                if (_pageIndex >= totalPages)
                    _pageIndex = 0;
                if (_pageIndex < 0)
                    _pageIndex = totalPages - 1;

                int pageStart = _pageIndex * pageCapacity;
                int pageCount = Math.Max(0, Math.Min(pageCapacity, totalIcons - pageStart));

                int rows;
                int columns;
                if (_pageIndex == 0)
                {
                    columns = _expanded ? visibleColumns : Math.Max(1, pageCount);
                    rows = _expanded ? Math.Max(1, (int)Math.Ceiling(pageCount / (double)visibleColumns)) : 1;
                }
                else
                {
                    columns = visibleColumns;
                    rows = maxRows;
                }

                IconsPanel.Children.Clear();
                for (int i = 0; i < pageCount; i++)
                {
                    IconsPanel.Children.Add(_iconButtons[pageStart + i]);
                }

                IconsPanel.Rows = rows;
                IconsPanel.Columns = columns;
                IconsPanel.Width = columns * UIConfiguration.IconGrid.ColumnPitch;
                IconsPanel.Height = rows * UIConfiguration.IconGrid.RowPitch;

                // +40 = mismo "chrome" vertical (padding/margenes del Border + espacio
                // para el DropShadowEffect) que ya hacia que 1 fila (100) diera la
                // altura original de la ventana (140).
                Height = rows * UIConfiguration.IconGrid.RowPitch + 40;
            }
            catch (Exception ex)
            {
                Log("Error updating icon layout", ex: ex);
            }
        }

        private void FiltrosButton_Click(object sender, RoutedEventArgs e)
        {
            // Sin funcionalidad todavia (ver ProximosPasos.txt #4) - el boton
            // solo necesita existir y estar en la posicion correcta por ahora.
            Log("Filtros clicked (sin funcionalidad todavia)");
        }

        private void ExpandirButton_Click(object sender, RoutedEventArgs e)
        {
            Log("Expandir clicked");
            try
            {
                _expanded = !_expanded;
                _pageIndex = 0;
                ExpandirChevronRotation.Angle = _expanded ? 180 : 0;
                UpdateIconLayout();
            }
            catch (Exception ex)
            {
                Log("Error toggling expand", ex: ex);
            }
        }

        private UIElement getButton(string item)
        {
            Log($"Creating button for {item}");
            try
            {
                var panel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var sizeBinding = new Binding("ShowIconNames")
                {
                    Source = App.Data,
                    Converter = (IValueConverter)FindResource("ShowNamesToSize")
                };

                var img = new System.Windows.Controls.Image
                {
                    Tag = item,
                    Stretch = System.Windows.Media.Stretch.Uniform,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                img.SetBinding(FrameworkElement.WidthProperty, sizeBinding);
                img.SetBinding(FrameworkElement.HeightProperty, sizeBinding);
                img.Source = IconExtractor.GetJumboIcon(item);
                RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);

                var nameBlock = new TextBlock
                {
                    Text = System.IO.Path.GetFileNameWithoutExtension(item),
                    FontSize = 11,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxWidth = UIConfiguration.Icon.ButtonWidth + 10,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 4, 0, 0)
                };
                nameBlock.SetBinding(TextBlock.ForegroundProperty, new Binding("IconTextForeground") { Source = App.Data });

                var visibilityBinding = new Binding("ShowIconNames")
                {
                    Source = App.Data,
                    Converter = new BooleanToVisibilityConverter()
                };
                nameBlock.SetBinding(TextBlock.VisibilityProperty, visibilityBinding);

                panel.Children.Add(img);
                panel.Children.Add(nameBlock);

                var bttn = new Button
                {
                    Style = (Style)FindResource("IconButtonStyle"),
                    Content = panel,
                    Tag = item,
                    Margin = UIConfiguration.Icon.ButtonMargin,
                    MinWidth = UIConfiguration.Icon.ButtonWidth,
                    MinHeight = UIConfiguration.Icon.ButtonHeight
                };

                bttn.Click += IconButton_Click;

                var menu = new ContextMenu();
                var deleteItem = new MenuItem { Header = "Delete Icon" };
                deleteItem.Click += DeleteIcon_Click;
                deleteItem.Tag = bttn;

                menu.Items.Add(deleteItem);
                bttn.ContextMenu = menu;

                return bttn;
            }
            catch (Exception ex)
            {
                Log($"Error creating button for {item}", ex: ex);
                throw;
            }
        }

        private void IconButton_Click(object sender, RoutedEventArgs e)
        {
            Log("Icon button clicked");
            try
            {
                if (sender is Button btn && btn.Tag is string path)
                {
                    var timeRanges = App.Data.TimeRanges;
                    var currentTime = DateTime.Now.TimeOfDay;
                    bool executeAllowed = true;
                    foreach (var timeRange in timeRanges)
                    {
                        executeAllowed = executeAllowed && !timeRange.IsInRange(currentTime);
                    }

                    if (!executeAllowed)
                        MessageBox.Show("HACETE UN CURSO CAPO. VOLVE CUANDO ESTES REALMENTE AL PEDO", "No es tiempo de jugar ahora", MessageBoxButton.OK);
                    else
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = path,
                            UseShellExecute = true
                        });
                    }

                }
            }
            catch (Exception ex)
            {
                Log("Error opening icon", ex: ex);
            }
        }

        private void DeleteIcon_Click(object sender, RoutedEventArgs e)
        {
            Log("Delete icon clicked");
            try
            {
                if (sender is MenuItem menuItem && menuItem.Tag is Button bttn && bttn.Tag is string item)
                {
                    _paths.Remove(item);
                    _iconButtons.Remove(bttn);
                    IconExtractor.RemoveFromCache(item);
                    CleanupButton(bttn);
                    updateJson();
                    _pageIndex = 0;
                    UpdateIconLayout();
                }
            }
            catch (Exception ex)
            {
                Log("Error deleting icon", ex: ex);
            }
        }

        private void CleanupButton(Button bttn)
        {
            Log("Cleaning up button");
            try
            {
                if (bttn.ContextMenu != null)
                {
                    foreach (MenuItem item in bttn.ContextMenu.Items)
                    {
                        item.Click -= DeleteIcon_Click;
                        item.Tag = null;
                    }
                    bttn.ContextMenu.Items.Clear();
                    bttn.ContextMenu = null;
                }

                bttn.Click -= IconButton_Click;

                if (bttn.Content is StackPanel panel)
                {
                    foreach (var child in panel.Children)
                    {
                        if (child is System.Windows.Controls.Image img)
                        {
                            BindingOperations.ClearAllBindings(img);
                            img.Source = null;
                        }
                        else if (child is TextBlock tb)
                        {
                            BindingOperations.ClearAllBindings(tb);
                        }
                    }
                    panel.Children.Clear();
                }

                bttn.Content = null;
                bttn.Tag = null;
            }
            catch (Exception ex)
            {
                Log("Error cleaning up button", ex: ex);
            }
        }

        private void CleanupIconPanel()
        {
            Log("Cleaning up icon panel");
            try
            {
                // Recorre _iconButtons (no IconsPanel.Children): cuando esta expandido,
                // solo la pagina actual esta montada en el panel, pero igual hay que
                // liberar TODOS los botones existentes.
                foreach (var btn in _iconButtons)
                {
                    CleanupButton(btn);
                }
                _iconButtons.Clear();
                IconsPanel.Children.Clear();
            }
            catch (Exception ex)
            {
                Log("Error cleaning icon panel", ex: ex);
            }
        }

        private void Grid_DragEnter(object sender, DragEventArgs e)
        {
            Log("DragEnter");
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    e.Effects = DragDropEffects.Copy;
                else
                    e.Effects = DragDropEffects.None;
            }
            catch (Exception ex)
            {
                Log("Error in DragEnter", ex: ex);
            }
        }

        private void Grid_Drop(object sender, DragEventArgs e)
        {
            Log("Drop");
            try
            {
                var filesDropped = (string[])e.Data.GetData(DataFormats.FileDrop);

                updatePaths(filesDropped);
                updateJson();
                updatePanel();
                CenterWindowOnTopOfScreen();
            }
            catch (Exception ex)
            {
                Log("Error in Drop", ex: ex);
            }
        }

        private void updatePanel()
        {
            Log("Updating panel");
            try
            {
                CleanupIconPanel();

                foreach (var path in _paths)
                {
                    _iconButtons.Add((Button)getButton(path));
                }
                _pageIndex = 0;
                UpdateIconLayout();
                CenterWindowOnTopOfScreen();
            }
            catch (Exception ex)
            {
                Log("Error updating panel", ex: ex);
            }
        }

        public void updateJson()
        {
            Log("Persisting paths to JSON");
            try
            {
                string json = JsonConvert.SerializeObject(_paths, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(Path.Combine(AppPaths.DataDir, _PATHS_FILE), json);
            }
            catch (Exception ex)
            {
                Log("Error writing paths file", ex: ex);
            }
        }

        // Ordena por el nombre que se ve debajo de cada icono (sin extension), no
        // por la ruta completa - sino, iconos en carpetas distintas quedan
        // agrupados por carpeta en vez de por nombre visible, lo que se ve como
        // "desordenado" al pasar de pagina.
        private static int CompareByDisplayName(string a, string b)
        {
            return string.Compare(
                Path.GetFileNameWithoutExtension(a),
                Path.GetFileNameWithoutExtension(b),
                StringComparison.OrdinalIgnoreCase);
        }

        private void updatePaths(string[] filesDropped)
        {
            Log($"Updating paths with {filesDropped?.Length ?? 0} items");
            try
            {
                foreach (var path in filesDropped)
                {
                    if (!_paths.Contains(path))
                        _paths.Add(path);
                }

                _paths.Sort(CompareByDisplayName);
            }
            catch (Exception ex)
            {
                Log("Error updating paths", ex: ex);
            }
        }

        public static ImageSource GetHighQualityIcon(string path)
        {
            LogStatic($"Getting high quality icon for {path}");
            try
            {
                using (var shellFile = ShellFile.FromFilePath(path))
                using (var bitmap = shellFile.Thumbnail.ExtraLargeBitmap)
                {
                    var hBitmap = bitmap.GetHbitmap();
                    try
                    {
                        var source = Imaging.CreateBitmapSourceFromHBitmap(
                            hBitmap,
                            IntPtr.Zero,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                        source.Freeze();
                        return source;
                    }
                    finally
                    {
                        IconExtractor.DeleteObject(hBitmap);
                    }
                }
            }
            catch (Exception ex)
            {
                LogStatic("Error getting high quality icon", ex: ex);
                throw;
            }
        }


        // Ya no existe scroll en pixeles: ambos botones solo cambian de pagina
        // (_pageIndex) y llaman a UpdateIconLayout, que repinta la pagina entera
        // sin dejar ningun icono/columna de la pagina anterior a la vista.
        //
        // La direccion es la pedida explicitamente (no es la intuitiva "derecha
        // = siguiente"): ScrollRight retrocede una pagina (de la primera pasa a
        // la ultima) y ScrollLeft avanza una pagina (de la ultima pasa a la
        // primera) - en ambos casos de forma infinita/circular.
        private void ScrollLeft_Click(object sender, RoutedEventArgs e)
        {
            Log("Scroll left (pagina siguiente)");
            try
            {
                _pageIndex--;
                UpdateIconLayout();
            }
            catch (Exception ex)
            {
                Log("Error scrolling left", ex: ex);
            }
        }

        private void ScrollRight_Click(object sender, RoutedEventArgs e)
        {
            Log("Scroll right (pagina anterior)");
            try
            {
                _pageIndex++;
                UpdateIconLayout();
            }
            catch (Exception ex)
            {
                Log("Error scrolling right", ex: ex);
            }
        }

        private void DeleteAllIcons()
        {
            Log("Deleting all icons");
            try
            {
                _paths.Clear();
                IconExtractor.ClearCache();
                updateJson();
                updatePanel();
            }
            catch (Exception ex)
            {
                Log("Error deleting all icons", ex: ex);
            }
        }
    }
}
