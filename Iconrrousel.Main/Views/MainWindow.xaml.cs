using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using Image = System.Windows.Controls.Image;


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
        // Rutas guardadas que no se pudieron cargar (ej. disco del juego desconectado).
        // No se muestran, pero se siguen guardando en Paths.json para no perderlas.
        private List<string> _skippedPaths = new List<string>();
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
        // Plataforma elegida en el boton Filtros (null = todas).
        private GamePlatform? _platformFilter = null;

        private static readonly string[] _shortcutExtensions = { ".lnk", ".url" };

        public MainWindow()
        {
            LoggingConfig.Log("Initializing MainWindow");
            DataContext = App.Data;
            App.Data.LoadSettings();
            InitializeComponent();

            PreviewMouseLeftButtonDown += Window_PreviewMouseLeftButtonDown;
            Closed += MainWindow_Closed;
            SizeChanged += MainWindow_SizeChanged;

            this.AllowDrop = true;

            if (File.Exists(Path.Combine(AppPaths.DataDir, _PATHS_FILE)))
            {
                try
                {
                    var json = File.ReadAllText(Path.Combine(AppPaths.DataDir, _PATHS_FILE));
                    var items = JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
                    items.Sort(CompareByDisplayName);
                    bool migrated = false;

                    foreach (var original in items)
                    {
                        var item = original;
                        try
                        {
                        if(File.Exists(item)){
                                var adopted = AdoptShortcut(item);
                                migrated |= adopted != item;
                                item = adopted;
                        }
                            var b = (Button)getButton(item);
                            _paths.Add(item);
                            _iconButtons.Add(b);
                        }
                        catch (Exception ex)
                        {
                            _skippedPaths.Add(original);
                            LoggingConfig.Log($"Icono omitido: {original}", ex: ex);
                        }
                    }

                    if (migrated)
                        updateJson();
                }
                catch (Exception ex)
                {
                    // El proximo updateJson() pisa el archivo: se guarda una copia antes.
                    try
                    {
                        File.Copy(Path.Combine(AppPaths.DataDir, _PATHS_FILE), Path.Combine(AppPaths.DataDir, _PATHS_FILE + ".bak"), true);
                    }
                    catch (Exception copyEx)
                    {
                        LoggingConfig.Log("No se pudo respaldar Paths.json", ex: copyEx);
                    }
                    LoggingConfig.LogAndNotify("No se pudieron cargar los iconos guardados. Se guardo una copia en Paths.json.bak.", ex);
                }
            }

            UpdateIconLayout();
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            LoggingConfig.Log("SizeChanged");
            try
            {
                if (IsLoaded)
                {
                    CenterWindowOnTopOfScreen();
                }
            }
            catch (Exception ex)
            {
                LoggingConfig.Log("Error in SizeChanged", ex: ex);
            }
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            LoggingConfig.Log("Closed event");
            try
            {
                CleanupIconPanel();
            }
            catch (Exception ex)
            {
                LoggingConfig.Log("Error in Closed handler", ex: ex);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoggingConfig.Log("Loaded event");
            try
            {
                CenterWindowOnTopOfScreen();
            }
            catch (Exception ex)
            {
                LoggingConfig.Log("Error in Loaded handler", ex: ex);
            }
        }

        private void CenterWindowOnTopOfScreen()
        {
            LoggingConfig.Log("Centering window");
            try
            {
                var screens = System.Windows.Forms.Screen.AllScreens;
                var screen = screens.Length > 1 ? screens[1] : System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(this).Handle);
                UpdateLayout();

                double screenWidth = screen.WorkingArea.Width;
                double screenLeft = screen.WorkingArea.Left;
                double screenTop = screen.WorkingArea.Top;

                Left = screenLeft + screenWidth / 2 - ActualWidth / 2;
                Top = screenTop;
            }
            catch (Exception ex)
            {
                LoggingConfig.Log("Error centering window", ex: ex);
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
            LoggingConfig.Log("Updating icon layout");
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

                var visibleButtons = GetFilteredButtons();
                int totalIcons = visibleButtons.Count;
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
                    IconsPanel.Children.Add(visibleButtons[pageStart + i]);
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
                LoggingConfig.Log("Error updating icon layout", ex: ex);
            }
        }

        private static GamePlatform GetPlatform(Button btn) => PlatformDetector.Detect(btn.Tag as string);

        // Botones que pasan el filtro de plataforma, en el mismo orden que _iconButtons.
        // Si la plataforma filtrada ya no tiene iconos (ej. se borro el ultimo), se
        // vuelve a mostrar todo en vez de dejar la ventana vacia.
        private List<Button> GetFilteredButtons()
        {
            if (_platformFilter == null)
                return _iconButtons;

            var filtered = _iconButtons.Where(b => GetPlatform(b) == _platformFilter).ToList();
            if (filtered.Count > 0)
                return filtered;

            SetPlatformFilter(null);
            return _iconButtons;
        }

        // Abre un menu debajo del boton con "Todas" + cada plataforma que tenga al
        // menos un icono (con la cantidad), marcando el filtro actual.
        private void FiltrosButton_Click(object sender, RoutedEventArgs e)
        {
            LoggingConfig.Log("Filtros clicked");
            try
            {
                var menu = new ContextMenu
                {
                    PlacementTarget = FiltrosButton,
                    Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom
                };

                menu.Items.Add(CreateFilterItem("Todas", null, _iconButtons.Count));
                menu.Items.Add(new Separator());

                var counts = _iconButtons
                    .GroupBy(GetPlatform)
                    .ToDictionary(g => g.Key, g => g.Count());
                foreach (GamePlatform platform in Enum.GetValues(typeof(GamePlatform)))
                {
                    if (counts.TryGetValue(platform, out int count))
                        menu.Items.Add(CreateFilterItem(PlatformDetector.DisplayName(platform), platform, count));
                }

                menu.IsOpen = true;
            }
            catch (Exception ex)
            {
                LoggingConfig.Log("Error opening filters menu", ex: ex);
            }
        }

        private MenuItem CreateFilterItem(string name, GamePlatform? platform, int count)
        {
            var item = new MenuItem
            {
                Header = $"{name} ({count})",
                IsCheckable = true,
                IsChecked = _platformFilter == platform
            };

            if (platform.HasValue && UIConfiguration.PlatformColors.BrandColors.TryGetValue(platform.Value, out var color))
            {
                item.Icon = new System.Windows.Shapes.Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = new SolidColorBrush(color)
                };
            }

            item.Click += (s, args) =>
            {
                LoggingConfig.Log($"Filtro de plataforma: {platform?.ToString() ?? "Todas"}");
                SetPlatformFilter(platform);
                _pageIndex = 0;
                UpdateIconLayout();
                CenterWindowOnTopOfScreen();
            };
            return item;
        }

        // Guarda el filtro y lo refleja en el boton: tooltip con la plataforma y
        // fondo del color de la plataforma mientras haya un filtro activo.
        private void SetPlatformFilter(GamePlatform? platform)
        {
            _platformFilter = platform;

            if (platform.HasValue)
            {
                FiltrosButton.ToolTip = $"Filtros: {PlatformDetector.DisplayName(platform.Value)}";
                if (UIConfiguration.PlatformColors.BrandColors.TryGetValue(platform.Value, out var color))
                    FiltrosButton.Background = new SolidColorBrush(color);
                else
                    FiltrosButton.ClearValue(BackgroundProperty);
            }
            else
            {
                FiltrosButton.ToolTip = "Filtros";
                FiltrosButton.ClearValue(BackgroundProperty);
            }
        }

        private void ExpandirButton_Click(object sender, RoutedEventArgs e)
        {
            LoggingConfig.Log("Expandir clicked");
            try
            {
                // El boton sigue siempre visible, pero si todos los iconos ya entran en
                // 1 sola fila, expandir no cambiaria nada (misma cantidad de filas) - en
                // ese caso no hace nada en vez de togglear el estado igual.
                int visibleColumns = Math.Max(1, App.Data.VisibleIconCount);
                if (!_expanded && GetFilteredButtons().Count <= visibleColumns)
                {
                    LoggingConfig.Log("Expandir: no corresponde (todos los iconos entran en 1 fila)");
                    return;
                }

                _expanded = !_expanded;
                _pageIndex = 0;
                ExpandirChevronRotation.Angle = _expanded ? 180 : 0;
                UpdateIconLayout();
            }
            catch (Exception ex)
            {
                LoggingConfig.Log("Error toggling expand", ex: ex);
            }
        }

        private UIElement getButton(string item)
        {
            LoggingConfig.Log($"Creating button for {item}");
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
                    FontSize = UIConfiguration.Icon.LabelFontSize,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxWidth = UIConfiguration.Icon.ButtonWidth + UIConfiguration.Icon.LabelExtraWidth,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, UIConfiguration.Icon.LabelTopMargin, 0, 0)
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

                var platform = PlatformDetector.Detect(item);
                var displayName = System.IO.Path.GetFileNameWithoutExtension(item);

                var bttn = new Button
                {
                    Style = (Style)FindResource("IconButtonStyle"),
                    Content = panel,
                    Tag = item,
                    Margin = UIConfiguration.Icon.ButtonMargin,
                    MinWidth = UIConfiguration.Icon.ButtonWidth,
                    MinHeight = UIConfiguration.Icon.ButtonHeight,
                    // El nombre debajo del icono se recorta con "..." (TextTrimming) para no
                    // romper el layout - el tooltip muestra el nombre completo sin recortar.
                    ToolTip = platform == GamePlatform.None
                        ? displayName
                        : $"{displayName} ({PlatformDetector.DisplayName(platform)})"
                };

                PlatformHover.Apply(bttn, platform);
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
                LoggingConfig.Log($"Error creating button for {item}", ex: ex);
                throw;
            }
        }

        private void IconButton_Click(object sender, RoutedEventArgs e)
        {
            LoggingConfig.Log("Icon button clicked");
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
                LoggingConfig.Log("Error opening icon", ex: ex);
            }
        }

        private void DeleteIcon_Click(object sender, RoutedEventArgs e)
        {
            LoggingConfig.Log("Delete icon clicked");
            try
            {
                if (sender is MenuItem menuItem && menuItem.Tag is Button bttn && bttn.Tag is string item)
                {
                    _paths.Remove(item);
                    _iconButtons.Remove(bttn);
                    if (Path.GetDirectoryName(item).Equals(AppPaths.ShortcutsDir, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            File.Delete(item);
                        }
                        catch (Exception ex)
                        {
                            LoggingConfig.Log($"No se pudo borrar {item}", ex: ex);
                        }
                    }
                    IconExtractor.RemoveFromCache(item);
                    PlatformDetector.Forget(item);
                    CleanupButton(bttn);
                    updateJson();
                    _pageIndex = 0;
                    UpdateIconLayout();
                }
            }
            catch (Exception ex)
            {
                LoggingConfig.Log("Error deleting icon", ex: ex);
            }
        }

        private void CleanupButton(Button bttn)
        {
            LoggingConfig.Log("Cleaning up button");
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

                BindingOperations.ClearAllBindings(bttn);
                bttn.Content = null;
                bttn.Tag = null;
            }
            catch (Exception ex)
            {
                LoggingConfig.Log("Error cleaning up button", ex: ex);
            }
        }

        private void CleanupIconPanel()
        {
            LoggingConfig.Log("Cleaning up icon panel");
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
                LoggingConfig.Log("Error cleaning icon panel", ex: ex);
            }
        }

        private void Grid_DragEnter(object sender, DragEventArgs e)
        {
            LoggingConfig.Log("DragEnter");
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    e.Effects = DragDropEffects.Copy;
                else
                    e.Effects = DragDropEffects.None;
            }
            catch (Exception ex)
            {
                LoggingConfig.Log("Error in DragEnter", ex: ex);
            }
        }

        private void Grid_Drop(object sender, DragEventArgs e)
        {
            LoggingConfig.Log("Drop");
            try
            {
                var filesDropped = (string[])e.Data.GetData(DataFormats.FileDrop);

                updatePaths(filesDropped);
                updateJson();
                _pageIndex = 0;
                UpdateIconLayout();
                CenterWindowOnTopOfScreen();
            }
            catch (Exception ex)
            {
                LoggingConfig.Log("Error in Drop", ex: ex);
            }
        }

        private void updatePanel()
        {
            LoggingConfig.Log("Updating panel");
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
                LoggingConfig.Log("Error updating panel", ex: ex);
            }
        }

        public void updateJson()
        {
            LoggingConfig.Log("Persisting paths to JSON");
            try
            {
                string json = JsonConvert.SerializeObject(_paths.Concat(_skippedPaths), Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(Path.Combine(AppPaths.DataDir, _PATHS_FILE), json);
            }
            catch (Exception ex)
            {
                LoggingConfig.LogAndNotify("No se pudieron guardar los cambios en los iconos.", ex);
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

        // Solo crea botones para los paths nuevos - antes cada drop reconstruia
        // TODOS los botones existentes (CleanupIconPanel + recrear todo desde
        // _paths), aunque se haya soltado un unico icono nuevo.
        private void updatePaths(string[] filesDropped)
        {
            LoggingConfig.Log($"Updating paths with {filesDropped?.Length ?? 0} items");

            bool added = false;
            foreach (var dropped in filesDropped)
            {
                try
                {
                    var path = AdoptShortcut(dropped);
                    if (_paths.Contains(path))
                        continue;
                    var b = (Button)getButton(path);
                    _paths.Add(path);
                    _iconButtons.Add(b);
                    _skippedPaths.Remove(path);
                    added = true;
                }
                catch (Exception ex)
                {
                    LoggingConfig.Log($"Failed updating path for {dropped}", ex: ex);
                }
            }

            if (added)
            {
                SortPathsAndButtons();
            }

        }

        // _iconButtons[i] tiene que seguir correspondiendo a _paths[i] (UpdateIconLayout
        // pagina sobre ambas listas por indice) - reordena las dos juntas por nombre
        // visible en vez de volver a crear los botones.
        private void SortPathsAndButtons()
        {
            var order = Enumerable.Range(0, _paths.Count)
                .OrderBy(i => System.IO.Path.GetFileNameWithoutExtension(_paths[i]), StringComparer.OrdinalIgnoreCase)
                .ToList();

            _paths = order.Select(i => _paths[i]).ToList();
            _iconButtons = order.Select(i => _iconButtons[i]).ToList();
        }

        // Ya no existe scroll en pixeles: ambos botones solo cambian de pagina
        // (_pageIndex) y llaman a UpdateIconLayout, que repinta la pagina entera
        // sin dejar ningun icono/columna de la pagina anterior a la vista.
        // ScrollLeft resta una pagina y ScrollRight suma una pagina, de forma
        // infinita/circular (UpdateIconLayout hace wrap en ambos sentidos).
        private void ScrollLeft_Click(object sender, RoutedEventArgs e)
        {
            LoggingConfig.Log("Scroll left (pagina anterior)");
            try
            {
                _pageIndex--;
                UpdateIconLayout();
            }
            catch (Exception ex)
            {
                LoggingConfig.Log("Error scrolling left", ex: ex);
            }
        }

        private void ScrollRight_Click(object sender, RoutedEventArgs e)
        {
            LoggingConfig.Log("Scroll right (pagina siguiente)");
            try
            {
                _pageIndex++;
                UpdateIconLayout();
            }
            catch (Exception ex)
            {
                LoggingConfig.Log("Error scrolling right", ex: ex);
            }
        }

        public void DeleteAllIcons()
        {
            LoggingConfig.Log("Deleting all icons");
            try
            {
                _paths.Clear();
                _skippedPaths.Clear();
                IconExtractor.ClearCache();
                updateJson();
                updatePanel();
            }
            catch (Exception ex)
            {
                LoggingConfig.Log("Error deleting all icons", ex: ex);
            }
        }

        private static string AdoptShortcut(string source)
        {
            var ext = Path.GetExtension(source);
            if (!_shortcutExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
                return source;
            if (Path.GetDirectoryName(Path.GetFullPath(source)).Equals(AppPaths.ShortcutsDir, StringComparison.OrdinalIgnoreCase))
                return source;
            var dest = Path.Combine(AppPaths.ShortcutsDir, Path.GetFileName(source));
            File.Copy(source, dest, overwrite: true);

            try
            {
                File.Delete(source);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
            {
                LoggingConfig.Log($"No se pudo borrar el acceso directo original: {source}", ex: ex);
            }

            return dest;
        }
    }
}
