using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Net.Http.Json;
using System.Printing;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using ExCSS;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
//using OpenTK.Input;
using Serilog;
using Xceed.Wpf.Toolkit.Core.Converters;
using Xceed.Wpf.Toolkit.Primitives;
using ZXing;
using ZXing.Common;
using ZXing.Rendering;
using static System.Net.WebRequestMethods;
using static ImageCanvasElement;
using static ZXing.QrCode.Internal.Mode;
using Brushes = System.Windows.Media.Brushes;
using File = System.IO.File;
using FontFamily = System.Windows.Media.FontFamily;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Image = System.Windows.Controls.Image;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;
using VerticalAlignment = System.Windows.VerticalAlignment;
namespace TemplateEdit;
/// <summary>
/// Interaction logic for MainPage.xaml
/// </summary>
public record ReportSections (ObservableCollection<CrystalReportSection> Sections,CrystalReportSection CrystalReportSection);

[ValueConversion(typeof(Point[]), typeof(PointCollection))]
public class PointsToPointCollectionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {

        if (value is Point[] pts)
            return new PointCollection(pts);

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is PointCollection pc)
        {
            return pc.ToArray<Point>();
        }
        else
        {
            return null;
        }
    }
}

public class EnumDescriptionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Enum e ? e.GetDescription() : value?.ToString();

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class RelayCommand : ICommand
{
    private readonly Action<object> _execute;
    private readonly Func<object, bool> _canExecute;

    public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object parameter) => _execute(parameter);

    public event EventHandler CanExecuteChanged;
    public void RaiseCanExecuteChanged()
        => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}


public partial class MainPage : Page, INotifyPropertyChanged
{
    public bool isDown, isDragging, isSelected;
    public UIElement selectedElement;
    public double originalLeft, originalTop;
    public Point startPoint;
    public AdornerLayer adornerLayer;
    public event PropertyChangedEventHandler PropertyChanged;

    // This method is called by the Set accessor of each property.
    // The CallerMemberName attribute that is applied to the optional propertyName
    // parameter causes the property name of the caller to be substituted as an argument.
    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        if (PropertyChanged != null)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public FontFamily _FontFamily = new FontFamily("Arial");
    public FontFamily FontFamily
    {
        get => (FontFamily)_FontFamily;
        set
        {
            _FontFamily = value;
            NotifyPropertyChanged(nameof(FontFamily));
        }
    }



    private double _FontSize = 12.0;
    public double FontSize
    {
        get => (double)_FontSize;
        set
        {
            _FontSize = value;
            NotifyPropertyChanged(nameof(FontSize));

        }
    }

    private System.Windows.FontWeight _FontWeight = System.Windows.FontWeights.Normal;
    public System.Windows.FontWeight FontWeight
    {
        get => (System.Windows.FontWeight)_FontWeight;
        set
        {
            _FontWeight= value;
            NotifyPropertyChanged(nameof(FontWeight));
        }
    }


    private System.Windows.FontStyle _FontStyle = System.Windows.FontStyles.Normal;
    public System.Windows.FontStyle FontStyle
    {
        get => (System.Windows.FontStyle)_FontStyle;
        set
        {
            _FontStyle = value;
            NotifyPropertyChanged(nameof(FontStyle));
        }
    }
    private System.Windows.Media.Color _ForegroundColor = System.Windows.Media.Colors.Black;
    public System.Windows.Media.Color ForegroundColor
    {
        get => (System.Windows.Media.Color)_ForegroundColor;
        set
        {
            _ForegroundColor = value;
            NotifyPropertyChanged(nameof(ForegroundColor));
        }
    }

    private System.Windows.Media.Color _BackgroundColor = System.Windows.Media.Colors.Transparent;
    public System.Windows.Media.Color BackgroundColor
    {
        get => (System.Windows.Media.Color)_BackgroundColor;
        set
        {
            _BackgroundColor = value;
            NotifyPropertyChanged(nameof(BackgroundColor));
        }
    }


    private System.Double _Angle;
    public System.Double Angle
    {
        get => (System.Double)_Angle;
        set
        {
            _Angle = value;
            NotifyPropertyChanged(nameof(Angle));
        }
    }
    public System.Version _Version
    {
        get; set;
    }

    private BarcodeImageProperties _BarcodeImage;
    public BarcodeImageProperties BarcodeImage
    {
        get => (BarcodeImageProperties)_BarcodeImage;
        set
        {
            _BarcodeImage = value;
            NotifyPropertyChanged(nameof(BarcodeImage));
        }
    }

    private System.Version GetMsixPackageVersion()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var manifestPath = assembly.Location.Replace(assembly.ManifestModule.Name, "") + "\\AppxManifest.xml";
        if (File.Exists(manifestPath))
        {
            var xDoc = XDocument.Load(manifestPath);
            return new System.Version(xDoc.Descendants().First(e => e.Name.LocalName == "Identity").Attributes()
                .First(a => a.Name.LocalName == "Version").Value);
        }

        return new System.Version(0, 0, 0, 0);
    }

    private ObservableCollection<System.Windows.Media.FontFamily> _InstalledFonts = [];
    public ObservableCollection<System.Windows.Media.FontFamily> InstalledFonts
    {
        get => (ObservableCollection<System.Windows.Media.FontFamily>)_InstalledFonts;
        set {
            _InstalledFonts = value;
            NotifyPropertyChanged(nameof(InstalledFonts));
        }
    }
    // Источники данных для привязки
    private Dictionary<string,QRCodeImageProperties> _QRCodes =[];
    public Dictionary<string, QRCodeImageProperties> QRCodes
    {
        get => (Dictionary<string, QRCodeImageProperties>)_QRCodes;
        set
        {
            _QRCodes = value;
            NotifyPropertyChanged(nameof(QRCodes));
        }
    }
    //
    private Dictionary<string, BarcodeImageProperties> _Barcodes = [];
    public Dictionary<string, BarcodeImageProperties> Barcodes
    {
        get => (Dictionary<string, BarcodeImageProperties>)_Barcodes;
        set
        {
            _Barcodes = value;
            NotifyPropertyChanged(nameof(Barcodes));
        }
    }

    private Dictionary<string,TextFieldValue> _TextFieldProperties = [];
    public Dictionary<string, TextFieldValue> TextFieldProperties
    {
        get =>(Dictionary<string, TextFieldValue>)_TextFieldProperties;
        set
        {
            _TextFieldProperties = value;
            NotifyPropertyChanged(nameof(TextFieldProperties));
        }
    }

    private Dictionary<string,ImageProperties>  _Images =[];
    public Dictionary<string, ImageProperties> Images
    {
        get =>(Dictionary<string, ImageProperties>)_Images;
        set
        {
            _Images = value;
            NotifyPropertyChanged(nameof(Images));
        }
    }


    private Dictionary<string, RectangleFigureProperties> _RectangleFigures = [];
    public Dictionary<string, RectangleFigureProperties> RectangleFigures
    {
        get => (Dictionary<string, RectangleFigureProperties>)_RectangleFigures;
        set
        {
            _RectangleFigures = value;
            NotifyPropertyChanged(nameof(RectangleFigures));

        }
    }


    private Dictionary<string, LineProperties> _Lines = [];
    public Dictionary<string, LineProperties> Lines
    {
        get => (Dictionary<string, LineProperties>)_Lines;
        set
        {
            _Lines = value;
            NotifyPropertyChanged(nameof(Lines));
        }
    }


    private Dictionary<string, PolygonProperties> _Polygons=[];
    public Dictionary<string, PolygonProperties> Polygons
    {
        get => (Dictionary<string, PolygonProperties>)_Polygons;
        set
        {
            _Polygons = value;
            NotifyPropertyChanged(nameof(Polygons));
        }
    }

    // Page size for design purposes
    private Size _PageSize;
    public Size PageSize
    {
        get => (Size)_PageSize;
        set
        {
            _PageSize = value;
            NotifyPropertyChanged(nameof(PageSize));
        }
    }


    //Column names of querries

    private Dictionary<string, List<KeyValueItem>> _ColumnNamesListOfQuerries=[];
    public Dictionary<string, List<KeyValueItem>> ColumnNamesListOfQuerries
    {
        get => (Dictionary<string, List<KeyValueItem>>)_ColumnNamesListOfQuerries;
        set
        {
            _ColumnNamesListOfQuerries = value;
            NotifyPropertyChanged(nameof(ColumnNamesListOfQuerries));
        }
    }

    // Report sections
    private ObservableCollection<CrystalReportSection> _ReportSections = [];
    public ObservableCollection<CrystalReportSection> ReportSections
    {
        get => (ObservableCollection<CrystalReportSection>)_ReportSections;
        set
        {
            _ReportSections = value;
            NotifyPropertyChanged(nameof(ReportSections));
        }
    }

    private CrystalReportSection _SelectedSection;
    public CrystalReportSection SelectedSection
    {
        get => (CrystalReportSection)_SelectedSection;
        set
        {

            if (_SelectedSection != null)
            {
                Report[_SelectedSection] = CanvasSerializer.CreateTemplateRecord(templateCanvas
              , Barcodes.Values.ToList()
              , TextFieldProperties.Values.ToList()
              , QRCodes.Values.ToList()
              , Images.Values.ToList()
              , RectangleFigures.Values.ToList()
              , Lines.Values.ToList()
              , Polygons.Values.ToList()
              );
                Report[_SelectedSection].Section = _SelectedSection;
            }
            _SelectedSection = value;
            NotifyPropertyChanged(nameof(SelectedSection));
            if (_SelectedSection == null)
            {
                return;
            }

            if (Report.ContainsKey(value))
            {
                var templateRecord = Report[value];
                LoadTemplate(ref templateRecord);
            }
            else
            {
                templateCanvas.Children.Clear();
                Barcodes.Clear();
                TextFieldProperties.Clear();
                QRCodes.Clear();
                Images.Clear();
                RectangleFigures.Clear();
                Lines.Clear();
                Polygons.Clear();
                Report[value] = new TemplateRecord() { Section = value };
            }
            (RemoveCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    // Expose enum values for dynamic menu
    public static Array AllSections => Enum.GetValues(typeof(CrystalReportSection));
    public ICommand AddSpecificCommand
    {
        get;
    }
    public ICommand RemoveCommand
    {
        get;
    }
    public ICommand SetReportSectionCommand
    {
        get;
    }
    private void AddSpecific(object param)
    {
        if (param is CrystalReportSection section)
        {
            if (ReportSections == null)
            {
                ReportSections = new();
            }
            ReportSections.Add(section);
            var sectionsList = ReportSections
                .Cast<CrystalReportSection>()
                .OrderBy(s => (int)s)
                .ToList(); ReportSections.Clear();
            foreach (var sec in sectionsList)
            {
                ReportSections.Add(sec);
            }
            SelectedSection = section;
        }
    }
    private void Remove()
    {
        if (SelectedSection != null)
        {
            var index = ReportSections.IndexOf(SelectedSection);
            Lines.Clear();
            RectangleFigures.Clear();
            TextFieldProperties.Clear();
            Barcodes.Clear();
            QRCodes.Clear();
            Images.Clear();
            Report[SelectedSection].elements.Clear();
            Report[SelectedSection].lineProperties.Clear();
            Report[SelectedSection].imageCanvasElements.Clear();
            Report[SelectedSection].textBoxCanvasElements.Clear();
            Report[SelectedSection].rectangleFigures.Clear();
            Report[SelectedSection].barcodeImage.Clear();
            Report[SelectedSection].qRCodeImages.Clear();
            templateCanvas.Children.Clear();
            Report.Remove(SelectedSection);
            ReportSections.Remove(SelectedSection);
            if (ReportSections.Count > 0)
            {
                if (index - 1 >= 0)
                {
                    SelectedSection = ReportSections[index - 1];
                }
                else
                {
                    SelectedSection = ReportSections[0];
                }
            }
        }
    }
    private void SetReportSection(object param)
    {
        if (SelectedSection is CrystalReportSection section && SelectedSection != null)
        {
            SelectedSection = section;
            reportSectionSelector.SelectedValue = section;
            reportSectionSelector.SelectedIndex = ReportSections.IndexOf(section);


        }
    }

    private static readonly DependencyProperty ReportProperty = DependencyProperty.Register("Report"
       , typeof(Dictionary<CrystalReportSection , TemplateRecord>), typeof(MainPage)
       , new FrameworkPropertyMetadata(new Dictionary<CrystalReportSection, TemplateRecord>(), FrameworkPropertyMetadataOptions.AffectsRender));
    public Dictionary<CrystalReportSection, TemplateRecord> Report
    {
        get => (Dictionary<CrystalReportSection, TemplateRecord>)GetValue(ReportProperty);
        set
        {
            SetValue(ReportProperty, value);
        }
    }

    public MainPage()
    {
        _Version = GetMsixPackageVersion();
        var logDirectory = Properties.Settings.Default.WBImagesLogFiles;
        if (string.IsNullOrWhiteSpace(logDirectory) || string.IsNullOrEmpty(logDirectory))
        {
            logDirectory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WBImages", "Logs");
            Properties.Settings.Default.WBImagesLogFiles = logDirectory;
            Properties.Settings.Default.Save();
        }
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }
        Log.Logger = new LoggerConfiguration().WriteTo.File(Properties.Settings.Default.WBImagesLogFiles + "\\log-.txt", outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}", rollingInterval: RollingInterval.Day).CreateLogger();
        Log.Information($"Версия {_Version}");
        var fonts = PrinterFonts.GetPrinterFontFamilies(Properties.Settings.Default.barcodePrinter);
        if (InstalledFonts == null)
        {
            InstalledFonts = new ObservableCollection<System.Windows.Media.FontFamily>();
        }
        InstalledFonts.Clear();
        foreach (var font in fonts)
        {
            try
            {
                InstalledFonts.Add(font);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ошибка добавления шрифта");
            }
        }

        GetColumnNamesOfAllQuerries();
        AddSpecificCommand = new RelayCommand(param => AddSpecific(param)); 
        RemoveCommand = new RelayCommand(_ => Remove(), _ => SelectedSection != null);
        SetReportSectionCommand = new RelayCommand(param => SetReportSection(param));
        InitializeComponent();
        this.Loaded += MainPage_OnLoaded;
        DataContext = this;

    }


    private void MainPage_OnLoaded(object sender, RoutedEventArgs e)
    {
        this.MouseLeftButtonDown += MainPage_MouseLeftButtonDown;
        this.MouseLeftButtonUp += MainPage_MouseLeftButtonUp;
        this.MouseMove += MainPage_MouseMove;
        this.MouseLeave += MainPage_MouseLeave;
        templateCanvas.PreviewMouseLeftButtonDown += TemplateCanvas_PreviewMouseLeftButtonDown;
        templateCanvas.PreviewMouseLeftButtonUp += TemplateCanvas_PreviewMouseLeftButtonUp;
    }

    private string _ConnectionString;
    public string ConnectionString
    {
        get => _ConnectionString;
        set
        {
            _ConnectionString = value;
            NotifyPropertyChanged(nameof(ConnectionString));
        }
    }

   

    public void DbClient()
    {
        var dbFileName = Properties.Settings.Default.dbFileName;
        string connectionString = $"Data Source={dbFileName}";
        this.ConnectionString = $"{connectionString};Cache=Shared;";
    }
    private void GetColumnNamesOfAllQuerries()
    {
        if(ColumnNamesListOfQuerries == null)
        {
            ColumnNamesListOfQuerries = new();
        }
        ColumnNamesListOfQuerries.Clear();
        DbClient();

        if (string.IsNullOrEmpty(Properties.Settings.Default.DataSources))
        {
            return;
        }
        ObservableCollection<KeyValueItem> _availableSources = new();
        _availableSources.Clear();
        try
        {

            _availableSources = JsonHelper.Deserialize<ObservableCollection<KeyValueItem>>(Properties.Settings.Default.DataSources);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка десериализации источников данных");
        }
        if (_availableSources.Count == 0)
        {
            return;
        }
        foreach(var querry in _availableSources)
        {
            try
            {
                var sqliteQuerryFields = new SqliteQuerryFields();
                var columnNames = new List<KeyValueItem>();
                sqliteQuerryFields.GetColumnNamesFromQuery(ConnectionString, querry.Value).ForEach(f => columnNames.Add(new KeyValueItem() { Key = f.Name, Value = f.Name }));
                ColumnNamesListOfQuerries[querry.Key] = columnNames;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Ошибка получения колонок для запроса {querry.Key}");
            }
        }
    }

    private void StopDragging()
    {
        if (isDown)
        {
            isDown = isDragging = false;
        }
    }




private void MainPage_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        //remove selected element on mouse down
        if (isSelected)
        {
            isSelected = false;
            if (selectedElement != null)
            {
                adornerLayer.Remove(adornerLayer.GetAdorners(selectedElement)[0]);
                selectedElement = null;
            }
        }
        e.Handled = true;
    }

    private void MainPage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        //stop dragging on mouse left button up
        StopDragging();
        e.Handled = true;
    }
    private void MainPage_MouseMove(object sender, MouseEventArgs e)
    {
        //handling mouse move event and setting canvas top and left value based on mouse movement
        if (isDown)
        {
            if ((!isDragging) &&
                ((Math.Abs(e.GetPosition(templateCanvas).X - startPoint.X) > SystemParameters.MinimumHorizontalDragDistance) ||
                (Math.Abs(e.GetPosition(templateCanvas).Y - startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)))
                isDragging = true;

            if (isDragging)
            {
                Point position = System.Windows.Input.Mouse.GetPosition(templateCanvas);
                Canvas.SetTop(selectedElement, position.Y - (startPoint.Y - originalTop));
                Canvas.SetLeft(selectedElement, position.X - (startPoint.X - originalLeft));
                if (selectedElement is Line)
                {
                    var line = selectedElement as Line;
                    var deltaX = Canvas.GetLeft(selectedElement) - line.X1;
                    var deltaY= Canvas.GetTop(selectedElement) - line.Y1;
                    var name = line.Name;
                    line.X1 = Canvas.GetLeft(selectedElement);
                    line.Y1 = Canvas.GetTop(selectedElement);
                    line.X2 = line.X2 + deltaX;
                    line.Y2 = line.Y2 + deltaY;
                    Lines[name].X1 = line.X1;
                    Lines[name].X2 = line.X2;
                    Lines[name].Y1 = line.Y1;
                    Lines[name].Y2 = line.Y2;
                    Lines[name].Left = Canvas.GetLeft(selectedElement);
                    Lines[name].Top = Canvas.GetTop(selectedElement);
                    var element = Report[SelectedSection].elements
                        .Find(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                    element.X1 = Lines[name].X1;
                    element.X2 = Lines[name].X2;
                    element.Y1 = Lines[name].Y1;
                    element.Y2 = Lines[name].Y2;
                    element.Left = Canvas.GetLeft(selectedElement);
                    element.Top = Canvas.GetTop(selectedElement);

                }
            }
        }
    }
    private void MainPage_MouseLeave(object sender, MouseEventArgs e)
    {
        //stop dragging on mouse leave
        StopDragging();
        e.Handled = true;
    }
    private void TemplateCanvas_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        StopDragging();
        e.Handled = true;
    }

    private void TemplateCanvas_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        //removing selected element
        if (isSelected)
        {
            isSelected = false;
            if (selectedElement != null)
            {
                if (selectedElement != null)
                {
                    if (adornerLayer.GetAdorners(selectedElement) != null)
                    {
                        adornerLayer.Remove(adornerLayer.GetAdorners(selectedElement)[0]);
                    }
                    selectedElement = null;
                }
            }
        }

        // select element if any element is clicked other then canvas
        if (e.Source != templateCanvas)
        {
            isDown = true;
            startPoint = e.GetPosition(templateCanvas);

            selectedElement = e.Source as UIElement;

            originalLeft = Canvas.GetLeft(selectedElement);
            originalTop = Canvas.GetTop(selectedElement);

            //adding adorner on selected element
            adornerLayer = AdornerLayer.GetAdornerLayer(selectedElement);
            switch (selectedElement)
            {
                case Line:
                    originalLeft = (selectedElement as Line).X1;
                    originalTop = (selectedElement as Line).Y1;
                    adornerLayer.Add(new ResizingAdorner(selectedElement));
                    break;
                case Polygon:
                    var polygon = selectedElement as Polygon;
                    //var points = polygon.Points.Select(p => new Point(p.X , p.Y)).ToArray();
                    //originalLeft = points.Min(p => p.X);
                    //originalTop = points.Min(p => p.Y);
                    //Canvas.SetLeft(polygon,originalLeft);
                    //Canvas.SetTop(polygon, originalTop);

                    adornerLayer.Add(new PolygonAdorner(polygon));
                    break;
                default:
                    adornerLayer.Add(new BorderAdorner(selectedElement));
                    break;
            }
            isSelected = true;
            e.Handled = true;
        }
    }

    private void MenuItem_Click(object sender, RoutedEventArgs e)
    {
        Image img = new Image();
        img.Source = new BitmapImage(new Uri("/logo.png", UriKind.Relative));
        img.Width = 50;
        img.Height = 50;
        img.AllowDrop = true;
        img.Stretch = Stretch.UniformToFill;
        img.ContextMenu = new ContextMenu();
        img.ContextMenu.FontSize = 12;
        //Удалить
        MenuItem menuItem = new MenuItem() { Header = "Удалить" };
        menuItem.Click += MenuItem_Click_Remove;
        img.ContextMenu.Items.Add(menuItem);
        //Размеры
        MenuItem menuItemImageProperties = new MenuItem() { Header = "Свойства" };
        menuItemImageProperties.Click += MenuItem_Click_ImageProperties;
        img.ContextMenu.Items.Add(menuItemImageProperties);
        templateCanvas.Children.Add(img);
        Canvas.SetLeft(img, 100);
        Canvas.SetTop(img, 100);
    }

    public void MenuItem_Click_ImageProperties(object sender, RoutedEventArgs e)
    {
        MenuItem menuItem = sender as MenuItem;
        if (menuItem != null)
        {
            ContextMenu contextMenu = menuItem.Parent as ContextMenu;
            if (contextMenu != null)
            {
                Image owner = contextMenu.PlacementTarget as Image;
                if (owner != null)
                {
                    var imageProperties = Images.ContainsKey(owner.Name)?Images[owner.Name]:null;
                    if (imageProperties == null)
                    {
                        imageProperties = new ImageProperties{ Name = owner.Name };
                        Images[owner.Name] = imageProperties;
                    }
                    var jsonString = JsonHelper.Serialize<ImageProperties>(imageProperties);
                    var imagePropertiesEditFunction = new EditImageFunction(jsonString);
                    imagePropertiesEditFunction.Return += new ReturnEventHandler<string>(GetImagePropertiesEditFunction_Returned);
                    this.NavigationService.Navigate(imagePropertiesEditFunction);
                }
            }
        }
    }
    public void ClearAllChildBindings(DependencyObject parent)
    {
        // Clear bindings on the parent itself
        BindingOperations.ClearAllBindings(parent);

        // Recursively clear children
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            ClearAllChildBindings(child);
        }
    }
    private void GetImagePropertiesEditFunction_Returned(object sender, ReturnEventArgs<string> e)
    {
        if (e==null || string.IsNullOrWhiteSpace(e.Result))
        {
            return;
        }
        var imageProperties = JsonHelper.Deserialize<ImageProperties>(e.Result);
        Images[imageProperties.Name] = imageProperties;
        var owner = templateCanvas.Children.OfType<Image>().FirstOrDefault(x => x.Name == imageProperties.Name);
        if (owner != null)
        {
            ClearAllChildBindings(owner);
            ImageSetBinding(ref owner);
            foreach (var bindingExp in owner.BindingGroup.BindingExpressions)
            {
                bindingExp.UpdateTarget();
            }
            var imageCanvasElement = new ImageCanvasElement()
            {
                Name = owner.Name ?? string.Empty,
                Type = owner.GetType().Name,
                Left = Canvas.GetLeft(owner),
                Top = Canvas.GetTop(owner),
                Width = owner.Width,
                Height = owner.Height,
                HorizontalAlignment = JsonHelper.Serialize<System.Windows.HorizontalAlignment>(owner.HorizontalAlignment),
                VerticalAlignment = JsonHelper.Serialize<System.Windows.VerticalAlignment>(owner.VerticalAlignment),
                ImageSourceString = owner.Source is BitmapSource bitmapSource ? ImageCanvasElement.ImageToBase64(bitmapSource) : string.Empty,
            };
            Report[SelectedSection].imageProperties.RemoveAll(tb => tb.Name.Equals(owner.Name, StringComparison.OrdinalIgnoreCase));
            Report[SelectedSection].imageProperties.Add(imageProperties);
            Report[SelectedSection].imageCanvasElements.RemoveAll(tb => tb.Name.Equals(owner.Name, StringComparison.OrdinalIgnoreCase));
            Report[SelectedSection].imageCanvasElements.Add(imageCanvasElement);
        }
    }
    public void MenuItem_Click_Remove(object sender, RoutedEventArgs e)
    {
        MenuItem menuItem = sender as MenuItem;
        if (menuItem != null)
        {
            ContextMenu contextMenu = menuItem.Parent as ContextMenu;
            if (contextMenu != null)
            {
                FrameworkElement owner = contextMenu.PlacementTarget as FrameworkElement;
                if (owner != null)
                {
                    // Remove the selected element from the canvas
                    var xName = owner.Name;
                    if (owner is Line)
                    {
                        Lines.Remove(xName);
                        Report[SelectedSection].lineProperties.RemoveAll(e => e.Name.Equals(xName, StringComparison.OrdinalIgnoreCase));
                    }
                    if (owner is Rectangle)
                    {
                        RectangleFigures.Remove(xName);
                        Report[SelectedSection].rectangleFigures.RemoveAll(e => e.Name.Equals(xName, StringComparison.OrdinalIgnoreCase));
                    }

                    if (owner is TextBox)
                    {
                        TextFieldProperties.Remove(xName);
                        Report[SelectedSection].textFieldValues.RemoveAll(e => e.Name.Equals(xName, StringComparison.OrdinalIgnoreCase));
                        Report[SelectedSection].textBoxCanvasElements.RemoveAll(e => e.Name.Equals(xName, StringComparison.OrdinalIgnoreCase));
                    }
                    if (owner is Image && owner.Name.StartsWith("Image"))
                    {
                        Images.Remove(xName);
                        Report[SelectedSection].imageProperties.RemoveAll(e => e.Name.Equals(xName, StringComparison.OrdinalIgnoreCase));
                        Report[SelectedSection].imageCanvasElements.RemoveAll(e => e.Name.Equals(xName, StringComparison.OrdinalIgnoreCase));
                    }

                    if (owner is Image && owner.Name.StartsWith("BarcodeImage"))
                    {
                        Barcodes.Remove(xName);
                        Report[SelectedSection].barcodeImage.RemoveAll(e => e.Name.Equals(xName, StringComparison.OrdinalIgnoreCase));
                        Report[SelectedSection].imageCanvasElements.RemoveAll(e => e.Name.Equals(xName, StringComparison.OrdinalIgnoreCase));
                    }
                    if (owner is Image && owner.Name.StartsWith("QRCodeImage"))
                    {
                        QRCodes.Remove(xName);
                        Report[SelectedSection].qRCodeImages.RemoveAll(e => e.Name.Equals(xName, StringComparison.OrdinalIgnoreCase));
                        Report[SelectedSection].imageCanvasElements.RemoveAll(e => e.Name.Equals(xName, StringComparison.OrdinalIgnoreCase));
                    }
                    
                    Report[SelectedSection].elements.RemoveAll(e => e.Name.Equals(xName, StringComparison.OrdinalIgnoreCase));
                    templateCanvas.Children.Remove(owner);


                }
            }
        }
    }
    private void MenuItem_Click_1(object sender, RoutedEventArgs e)
    {
    }
    private void MenuItem_Click_AddRect(object sender, RoutedEventArgs e)
    {
        var k = templateCanvas.Children.OfType<Rectangle>().Count();
        var xKey = "Rectangle" + k.ToString();
        var rectangleFigure = new RectangleFigureProperties()
        {
            Name = xKey,
            Width = 100,
            Height = 50,
            Fill = BackgroundColor,
            Stroke = ForegroundColor,
            StrokeThickness = 1,
            Stretch = Stretch.Fill,
            RadiusX = 0,
            RadiusY = 0,
            Angle = 0.0,
            Left = 50,
            Top = 50,
            Opacity=1
        };

        RectangleFigures[xKey] = rectangleFigure;

        Rectangle rect = new Rectangle
        {
            Name = xKey,
            Width = 100,
            Height = 50,
            Fill = new SolidColorBrush(BackgroundColor),
            Stroke = new SolidColorBrush(ForegroundColor),
            StrokeThickness = 1,
            Stretch = Stretch.Fill,
            RadiusX=0,
            RadiusY= 0,
            Opacity = 1
        };

        // Set position on the Canvas
        var origin = new Point(50,50);
        Canvas.SetLeft(rect, origin.X);
        Canvas.SetTop(rect, origin.Y);
        SetBindingRectangleProperties(ref rect);
        foreach (var bindingexp in rect.BindingGroup.BindingExpressions)
        {
            bindingexp.UpdateTarget();
        }
        // Add the Rectangle to the Canvas
        templateCanvas.Children.Add(rect);

        //Удалить
        rect.ContextMenu = new ContextMenu();
        rect.ContextMenu.FontSize = 12;
        MenuItem menuItem = new MenuItem() { Header = "Удалить"};
        menuItem.Click += MenuItem_Click_Remove;
        rect.ContextMenu.Items.Add(menuItem);
        //Свойства
        MenuItem menuItemRectProperties = new MenuItem() { Header = "Свойства прямоугольника"};
        menuItemRectProperties.Click += MenuItem_Click_SetRectangleProperties;
        rect.ContextMenu.Items.Add(menuItemRectProperties);
        //Zorder
        MenuItem menuItemZorderPlus = new MenuItem() { Header = "Z+" };
        menuItemZorderPlus.Click += MenuItem_Click_ZorderPlus;
        rect.ContextMenu.Items.Add(menuItemZorderPlus);

        MenuItem menuItemZorderMinus = new MenuItem() { Header = "Z-" };
        menuItemZorderMinus.Click += MenuItem_Click_ZorderMinus;
        rect.ContextMenu.Items.Add(menuItemZorderMinus);

        Report[SelectedSection].rectangleFigures.Add(rectangleFigure);
        Report[SelectedSection].elements.Add(new CanvasElement()
        {
            Name = rect.Name,
            Type = "Rectangle",
            Left = Canvas.GetLeft(rect),
            Top = Canvas.GetTop(rect),
            Width = rect.Width,
            Height = rect.Height,
            Stroke = JsonHelper.Serialize<Brush>(rect.Stroke),
            StrokeThickness = rect.StrokeThickness,
            Fill = JsonHelper.Serialize<Brush>(rect.Fill),
            Stretch = JsonHelper.Serialize<Stretch>(rect.Stretch),
            RadiusX = rect.RadiusX,
            RadiusY = rect.RadiusY,
            Opacity = rect.Opacity,
        });

    }
    public void MenuItem_Click_SetRectangleProperties(object sender, RoutedEventArgs e)
    {
        MenuItem menuItem = sender as MenuItem;
        if (menuItem != null)
        {
            ContextMenu contextMenu = menuItem.Parent as ContextMenu;
            if (contextMenu != null)
            {
                if (contextMenu.PlacementTarget != null)
                {
                    Rectangle owner = contextMenu.PlacementTarget as Rectangle;
                    var rectangleFigure = RectangleFigures[owner.Name];
                    SetRectangleProperties pageFunction = new SetRectangleProperties(rectangleFigure);
                    pageFunction.Return += new ReturnEventHandler<string>(GetRectanglePageFunction_Returned);
                    this.NavigationService.Navigate(pageFunction);
                }
            }
        }

    }
    private void GetRectanglePageFunction_Returned(object sender, ReturnEventArgs<string> e)
    {
        // Handle the returned string here
        string returnedString = e.Result;
        if(String.IsNullOrEmpty(returnedString))
        {
            return;
        }
        // Do something with the returned string
        var rectangleFigure = JsonHelper.Deserialize<RectangleFigureProperties>(returnedString);
        if (rectangleFigure != null)
        {
            RectangleFigures[rectangleFigure.Name] = rectangleFigure;
            var rectangle = templateCanvas.Children.OfType<Rectangle>().FirstOrDefault(r => r.Name == rectangleFigure.Name);
            if (rectangle != null)
            {
               BindingOperations.ClearAllBindings(rectangle);
               SetBindingRectangleProperties(ref rectangle);
                foreach (var bindingExp in rectangle.BindingGroup.BindingExpressions)
                {
                    bindingExp.UpdateTarget();
                }
                Report[SelectedSection].rectangleFigures.RemoveAll(rf => rf.Name.Equals(rectangleFigure.Name, StringComparison.OrdinalIgnoreCase));
                Report[SelectedSection].rectangleFigures.Add(rectangleFigure);
                Report[SelectedSection].elements.RemoveAll(e => e.Name.Equals(rectangleFigure.Name, StringComparison.OrdinalIgnoreCase));
                Report[SelectedSection].elements.Add( new CanvasElement()
                {
                    Name = rectangle.Name,
                    Type = "Rectangle",
                    Left = Canvas.GetLeft(rectangle),
                    Top = Canvas.GetTop(rectangle),
                    Width = rectangle.Width,
                    Height = rectangle.Height,
                    Stroke = JsonHelper.Serialize<Brush>(rectangle.Stroke),
                    StrokeThickness = rectangle.StrokeThickness,
                    Fill = JsonHelper.Serialize<Brush>(rectangle.Fill),
                    Stretch = JsonHelper.Serialize<Stretch>(rectangle.Stretch),
                    RadiusX = rectangle.RadiusX,
                    RadiusY = rectangle.RadiusY,
                    Opacity = rectangle.Opacity,
                });
            }
        }
    }
    private void MenuItem_Click_2(object sender, RoutedEventArgs e)
    {
    }
    private void MenuItem_Click_AddTextBox(object sender, RoutedEventArgs e)
    {
        var textBox = new TextBox() { Text = "Образец текста" };
        var pixelsPerDip = VisualTreeHelper.GetDpi(textBox).PixelsPerDip;
        // Create a DrawingVisual to draw on
        var formattedText = new FormattedText(
                    textBox.Text,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(textBox.FontFamily, textBox.FontStyle, textBox.FontWeight, textBox.FontStretch),
                    textBox.FontSize,
                    textBox.Foreground, pixelsPerDip);
        var width = formattedText.WidthIncludingTrailingWhitespace;
        var height = formattedText.Height;
        var pageFunction = new ChangeTextFieldProperties
            (
                JsonHelper.Serialize<TextFieldValue>
                (
                    new TextFieldValue()
                    {
                        Name = String.Empty,
                        Value = "Образец текста",
                        Width = width,
                        Height = height,
                        FontSize = FontSize,
                        FontFamily = (FontFamily ?? new FontFamily("Arial")).Source,
                        FontStyle = FontStyle,
                        FontWeight = FontWeight,
                        TextWrapping = TextWrapping.NoWrap,
                        AcceptsReturn = false,
                        Foreground = ForegroundColor,
                        Background = BackgroundColor,
                        Left = 0,
                        Top = 0,
                        TextAlignment = TextAlignment.Left,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top
                    }
                )          
            );
        pageFunction.Return += new ReturnEventHandler<string>(GetNewTextFieldFromPageFunction_Returned);
        this.NavigationService.Navigate(pageFunction);
    }
    private void AddNewLine(double X1, double Y1, double X2, double Y2, System.Windows.Media.Brush stroke, double thickness, string Name = "")
    {
        var line = new Line();
        line.X1 = X1;
        line.Y1 = Y1;
        line.X2 = X2;
        line.Y2 = Y2;
        line.Stroke = stroke;
        line.StrokeThickness = thickness;
        if (String.IsNullOrWhiteSpace(Name))
        {
            line.Name = "Line" + templateCanvas.Children.Count;
        }
        else
        {
            line.Name = Name + templateCanvas.Children.Count;
        }
        // Set position on the Canvas
        Canvas.SetLeft(line, 50);
        Canvas.SetTop(line, 50);
        // Add the Line to the Canvas
        templateCanvas.Children.Add(line);
        //Удалить
        line.ContextMenu = new ContextMenu();
        line.ContextMenu.FontSize = 12;
        MenuItem menuItem = new MenuItem() { Header = "Удалить" };
        menuItem.Click += MenuItem_Click_Remove;
        line.ContextMenu.Items.Add(menuItem);
        //Размеры
        MenuItem menuItemRect = new MenuItem() { Header = "Размеры" };
        menuItemRect.Click += MenuItem_Click_Rectangle;
        line.ContextMenu.Items.Add(menuItemRect);

        if (!Lines.ContainsKey(line.Name))
        {
            var lineProperties = new LineProperties()
            {
                Name = line.Name,
                X1 = X1,
                Y1 = Y1,
                X2 = X2,
                Y2 = Y2,
                Stroke = ((SolidColorBrush)stroke).Color,
                StrokeThickness = thickness,
                Angle = 0.0,
                Left = X1,
                Top = Y1
            };
            Lines[line.Name] = lineProperties;
            Report[SelectedSection].lineProperties.Add(lineProperties);
        }
        var canvaselement= new CanvasElement() {
        Name=line.Name,
        Type="Line",    
        X1=line.X1,  
        Y1= line.Y1,
        X2 = line.X2,
        Y2= line.Y2,
        Stroke= ((SolidColorBrush)line.Stroke).Color.ToString(),
        StrokeThickness=line.StrokeThickness,
        };
        Report[SelectedSection].elements.Add(canvaselement);


    }
    private void MenuItem_Click_AddLine(object sender, RoutedEventArgs e)
    {
        var lineProperties = new LineProperties();
        var k = templateCanvas.Children.OfType<Line>().Count();
        var lastLine = templateCanvas.Children.OfType<Line>().LastOrDefault();
        if (lastLine != null)
        {
            lineProperties.X1 = lastLine.X1 + 10;
            lineProperties.Y1 = lastLine.Y1 + 10;
            lineProperties.X2 = lastLine.X2 + 10;
            lineProperties.Y2 = lastLine.Y2 + 10;
            lineProperties.Angle = lastLine.RenderTransform is RotateTransform rt ? rt.Angle : 0;
            lineProperties.Angle+=5;
        }
        else
        {
            lineProperties.X1 = 10;
            lineProperties.Y1 = 10;
            lineProperties.X2 = 100;
            lineProperties.Y2 = 100;
            lineProperties.Angle = 5;
        }

        lineProperties.Name = "Line" + k;
        lineProperties.Stroke = ForegroundColor;
        lineProperties.StrokeThickness = 1;
        lineProperties.Left = lineProperties.X1;
        lineProperties.Top = lineProperties.Y1;
        Lines[lineProperties.Name] = lineProperties;
        var line = new Line();
        line.Name = lineProperties.Name;
        Canvas.SetLeft(line, lineProperties.X1);
        Canvas.SetTop(line, lineProperties.Y1);
        SetBindingLineProperties(ref line);
        foreach (var bindingexp in line.BindingGroup.BindingExpressions)
        {
            bindingexp.UpdateTarget();
        }
        line.ContextMenu = new ContextMenu();
        templateCanvas.Children.Add(line);
        line.ContextMenu.FontSize = 12;
        MenuItem menuItem = new MenuItem() { Header = "Удалить" };
        menuItem.Click += MenuItem_Click_Remove;
        line.ContextMenu.Items.Add(menuItem);
        //Размеры
        MenuItem menuItemLineProperties = new MenuItem() { Header = "Свойства" };
        menuItemLineProperties.Click += MenuItem_Click_LineProperties;
        line.ContextMenu.Items.Add(menuItemLineProperties);
        //Zorder
        MenuItem menuItemZorderPlus = new MenuItem() { Header = "Z+" };
        menuItemZorderPlus.Click += MenuItem_Click_ZorderPlus;
        line.ContextMenu.Items.Add(menuItemZorderPlus);

        MenuItem menuItemZorderMinus = new MenuItem() { Header = "Z-" };
        menuItemZorderMinus.Click += MenuItem_Click_ZorderMinus;
        line.ContextMenu.Items.Add(menuItemZorderMinus);

        Report[SelectedSection].lineProperties.Add(lineProperties);
        Report[SelectedSection].elements.Add(new CanvasElement()
        {
            Name = line.Name,
            Type = "Line",
            X1 = line.X1,
            Y1 = line.Y1,
            X2 = line.X2,
            Y2 = line.Y2,
            Left = line.X1,
            Top = line.Y1,
            Width = Math.Abs(line.X2 - line.X1),
            Height= Math.Abs(line.Y2 - line.Y1),
            Stroke = JsonHelper.Serialize<Brush>(line.Stroke),
            StrokeThickness = line.StrokeThickness,
            StrokeStartLineCap = JsonHelper.Serialize<PenLineCap>(line.StrokeStartLineCap),
            StrokeEndLineCap = JsonHelper.Serialize<PenLineCap>(line.StrokeEndLineCap),
        });
    }

    private void MenuItem_Click_ZorderMinus(object sender, RoutedEventArgs e)
    {
        MenuItem menuItem = sender as MenuItem;
        if (menuItem != null)
        {
            ContextMenu contextMenu = menuItem.Parent as ContextMenu;
            if (contextMenu != null)
            {
                if (contextMenu.PlacementTarget != null)
                {
                    var owner = contextMenu.PlacementTarget ;
                    if (owner != null)
                    {
                        int currentIndex = Panel.GetZIndex  (owner);
                        Panel.SetZIndex(owner, currentIndex - 1);
                    }
                }
            }
        }
    }
    private void MenuItem_Click_ZorderPlus(object sender, RoutedEventArgs e)
    {
        MenuItem menuItem = sender as MenuItem;
        if (menuItem != null)
        {
            ContextMenu contextMenu = menuItem.Parent as ContextMenu;
            if (contextMenu != null)
            {
                if (contextMenu.PlacementTarget != null)
                {
                    var owner = contextMenu.PlacementTarget ;
                    if (owner != null)
                    {
                        int currentIndex = Panel.GetZIndex  (owner);
                        Panel.SetZIndex(owner, currentIndex + 1);
                    }
                }
            }
        }
    }
    public void MenuItem_Click_LineProperties(object sender, RoutedEventArgs e)
    {
        MenuItem menuItem = sender as MenuItem;
        if (menuItem != null)
        {
            ContextMenu contextMenu = menuItem.Parent as ContextMenu;
            if (contextMenu != null)
            {
                if (contextMenu.PlacementTarget != null)
                {
                    Line owner = contextMenu.PlacementTarget as Line;
                    var lineFigure = Lines[owner.Name];
                    SetLineProperties pageFunction = new SetLineProperties(lineFigure,PageSize);
                    pageFunction.Return += new ReturnEventHandler<string>(GetLinePageFunction_Returned);
                    this.NavigationService.Navigate(pageFunction);
                }
            }
        }

    }
    private void GetLinePageFunction_Returned(object sender, ReturnEventArgs<string> e)
    {
        // Handle the returned string here
        string returnedString = e.Result;
        if (String.IsNullOrEmpty(returnedString))
        {
            return;
        }
        // Do something with the returned string
        var lineFigure = JsonHelper.Deserialize<LineProperties>(returnedString);
        if (lineFigure != null)
        {
            lineFigure.X1 = Math.Max(0, Math.Min(lineFigure.X1, PageSize.Width));
            lineFigure.X2 = Math.Max(0, Math.Min(lineFigure.X2, PageSize.Width));
            lineFigure.Y1 = Math.Max(0, Math.Min(lineFigure.Y1, PageSize.Height));
            lineFigure.Y2 = Math.Max(0, Math.Min(lineFigure.Y2, PageSize.Height));
            Lines[lineFigure.Name] = lineFigure;
            Report[SelectedSection].lineProperties.RemoveAll(l => l.Name.Equals(lineFigure.Name, StringComparison.OrdinalIgnoreCase));
            Report[SelectedSection].lineProperties.Add(lineFigure);
            var line = templateCanvas.Children.OfType<Line>().FirstOrDefault(r => r.Name.Equals(lineFigure.Name,StringComparison.OrdinalIgnoreCase));
            if (line != null)
            {
                Canvas.SetLeft(line, lineFigure.X1);
                Canvas.SetTop(line, lineFigure.Y1);
                BindingOperations.ClearAllBindings(line);
                SetBindingLineProperties(ref line);
                foreach(var bindingExp in line.BindingGroup.BindingExpressions)
                {
                    bindingExp.UpdateTarget();
                }
                Report[SelectedSection].elements.RemoveAll(l => l.Name.Equals(line.Name, StringComparison.OrdinalIgnoreCase));
                Report[SelectedSection].elements.Add(new CanvasElement()
                {
                    Name = line.Name,
                    Type = "Line",
                    X1 = line.X1,
                    Y1 = line.Y1,
                    X2 = line.X2,
                    Y2 = line.Y2,
                    Left = line.X1,
                    Top = line.Y1,
                    Width = Math.Abs(line.X2 - line.X1),
                    Height = Math.Abs(line.Y2 - line.Y1),
                    Stroke = JsonHelper.Serialize<Brush>(line.Stroke),
                    StrokeThickness = line.StrokeThickness,
                    StrokeStartLineCap = JsonHelper.Serialize<PenLineCap>(line.StrokeStartLineCap),
                    StrokeEndLineCap = JsonHelper.Serialize<PenLineCap>(line.StrokeEndLineCap),
                });

            }
        }

    }

    private void MenuItem_Click_AddPolygon(object sender, RoutedEventArgs e)
    {   
        var pageFunction = new PolygonFunction(PageSize);
        pageFunction.Return += new ReturnEventHandler<string>(PolygonFunction_Returned);
        this.NavigationService.Navigate(pageFunction);

    }

    private void PolygonFunction_Returned(object sender, ReturnEventArgs<string> e)
    {
        var returnedString = e.Result;
        if (String.IsNullOrEmpty(returnedString))
        {
            return;
        }
        var k = templateCanvas.Children.OfType<Polygon>().Count();
        k++;
        string name = "Polygon" + k;    
        var polygonProperties = JsonHelper.Deserialize<PolygonProperties>(returnedString);
        polygonProperties.Name = name; 
        Polygons[name] = polygonProperties;
        Polygon polygon = new Polygon()
        {
            Name = name,
            Points = new PointCollection(polygonProperties.Vertices),
            Stroke = new SolidColorBrush(ForegroundColor),
            Fill = new SolidColorBrush(BackgroundColor),
            StrokeThickness = 1,
            Opacity = 1,
        };
        Canvas.SetLeft(polygon, polygonProperties.Left);
        Canvas.SetTop(polygon, polygonProperties.Top);
        templateCanvas.Children.Add(polygon);
        SetBindingPolygonProperties(ref polygon);
        foreach (var bindingExp in polygon.BindingGroup.BindingExpressions)
        {
            bindingExp.UpdateTarget();
        }
        Report[SelectedSection].polygonProperties.Add(polygonProperties);
        Report[SelectedSection].elements.Add(new CanvasElement()
        {
            Name = polygon.Name,
            Type = "Polygon",
            Stroke = JsonHelper.Serialize<Brush>(polygon.Stroke),
            StrokeThickness = polygon.StrokeThickness,
            Fill = JsonHelper.Serialize<Brush>(polygon.Fill),
            Stretch = JsonHelper.Serialize<Stretch>(polygon.Stretch),
            Opacity = polygon.Opacity,
            Points = polygonProperties.Vertices,
            Left = polygonProperties.Left,
            Top = polygonProperties.Top,
            Width = polygonProperties.Width,
            Height = polygonProperties.Height
        });
        polygon.ContextMenu = new ContextMenu();
        MenuItem menuItem = new MenuItem() { Header = "Удалить" };
        menuItem.Click += MenuItem_Click_Remove;
        polygon.ContextMenu.Items.Add(menuItem);
        //Размеры
        MenuItem menuItemLineProperties = new MenuItem() { Header = "Свойства" };
        menuItemLineProperties.Click += MenuItem_Click_PolygonProperties;
        polygon.ContextMenu.Items.Add(menuItemLineProperties);
        //Zorder
        MenuItem menuItemZorderPlus = new MenuItem() { Header = "Z+" };
        menuItemZorderPlus.Click += MenuItem_Click_ZorderPlus;
        polygon.ContextMenu.Items.Add(menuItemZorderPlus);
        MenuItem menuItemZorderMinus = new MenuItem() { Header = "Z-" };
        menuItemZorderMinus.Click += MenuItem_Click_ZorderMinus;
        polygon.ContextMenu.Items.Add(menuItemZorderMinus);
    }

    public void MenuItem_Click_PolygonProperties(object sender, RoutedEventArgs e)
    {
        MenuItem menuItem = sender as MenuItem;
        if (menuItem != null)
        {
            ContextMenu contextMenu = menuItem.Parent as ContextMenu;
            if (contextMenu != null)
            {
                if (contextMenu.PlacementTarget != null)
                {
                    Polygon owner = contextMenu.PlacementTarget as Polygon;
                    var polygonProps =   JsonHelper.Serialize<PolygonProperties>( Polygons[owner.Name]);
                    var pageFunction = new PolygonFunction(polygonProps, PageSize);
                    pageFunction.Return += new ReturnEventHandler<string>(GetPolygonFunction_Returned);
                    this.NavigationService.Navigate(pageFunction);
                }
            }
        }
    }

    private void GetPolygonFunction_Returned(object sender, ReturnEventArgs<string> e)
    {
    
        var returnedString = e.Result;
        if (String.IsNullOrEmpty(returnedString))
        {
            return;
        }
        var polygonProperties = JsonHelper.Deserialize<PolygonProperties>(returnedString);
        Polygons[polygonProperties.Name] = polygonProperties;
        Report[SelectedSection].polygonProperties.RemoveAll(p => p.Name.Equals(polygonProperties.Name, StringComparison.OrdinalIgnoreCase));
        Report[SelectedSection].polygonProperties.Add(polygonProperties);
        var polygon = templateCanvas.Children.OfType<Polygon>().FirstOrDefault(p => p.Name.Equals(polygonProperties.Name, StringComparison.OrdinalIgnoreCase));
        if (polygon != null)
        {
            BindingOperations.ClearAllBindings(polygon);
            SetBindingPolygonProperties(ref polygon);
            foreach (var bindingExp in polygon.BindingGroup.BindingExpressions)
            {
                bindingExp.UpdateTarget();
            }
            Report[SelectedSection].elements.RemoveAll(p => p.Name.Equals(polygon.Name, StringComparison.OrdinalIgnoreCase));
            Report[SelectedSection].elements.Add(new CanvasElement()
            {
                Name = polygon.Name,
                Type = "Polygon",
                Stroke = JsonHelper.Serialize<Brush>(polygon.Stroke),
                StrokeThickness = polygon.StrokeThickness,
                Fill = JsonHelper.Serialize<Brush>(polygon.Fill),
                Stretch = JsonHelper.Serialize<Stretch>(polygon.Stretch),
                Opacity = polygon.Opacity,
                Points = polygonProperties.Vertices,
                Width = polygonProperties.Width,
                Height = polygonProperties.Height,
                Left = polygonProperties.Left,
                Top = polygonProperties.Top,
            });
        }
    }

    private void MenuItem_Click_HonistSign(object sender, RoutedEventArgs e)
    {
        Image img = new Image();
        string svg = """
            <svg data-name="logos / cz-logo-default" xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 32 32"><path d="M8.629 0h14.742c3 0 4.088.312 5.185.9A6.116 6.116 0 0 1 31.1 3.443c.587 1.1.9 2.185.9 5.185v14.743c0 3-.312 4.088-.9 5.185a6.116 6.116 0 0 1-2.543 2.544c-1.1.587-2.185.9-5.185.9H8.629c-3 0-4.088-.312-5.185-.9A6.116 6.116 0 0 1 .9 28.557c-.587-1.1-.9-2.185-.9-5.185V8.629c0-3 .312-4.088.9-5.185A6.116 6.116 0 0 1 3.443.9C4.54.312 5.628 0 8.629 0z" fill="#f2eb3b"/><path data-name="Fill 78" d="M23.993 6.492a1.527 1.527 0 0 1 1.521 1.527v3.791h3.179V8.019a4.709 4.709 0 0 0-4.7-4.709h-3.8v3.182z" fill="#63666a"/><path data-name="Fill 80" d="M25.514 24.001a1.523 1.523 0 0 1-1.521 1.52h-3.8v3.172h3.8a4.7 4.7 0 0 0 4.7-4.692v-3.808h-3.179z" fill="#63666a"/><path data-name="Fill 82" d="M6.485 8.018A1.527 1.527 0 0 1 8.01 6.492h3.8V3.31h-3.8a4.708 4.708 0 0 0-4.7 4.708v3.792h3.175z" fill="#63666a"/><path data-name="Fill 84" d="M8.01 25.522a1.524 1.524 0 0 1-1.523-1.52v-3.809H3.31v3.808a4.7 4.7 0 0 0 4.7 4.692h3.8v-3.171z" fill="#63666a"/><path data-name="Fill 86" d="M14.17 23.614l-6.777-6.783 2.242-2.243 4.535 4.536 8.194-8.2 2.242 2.244z" fill="#63666a"/></svg>
            """;

        img.Source = SvgStringToBitmapImageConverter.ConvertStringToBitMapImage(svg);
        img.Width = 50;
        img.Height = 50;
        img.AllowDrop = true;
        img.Stretch = Stretch.UniformToFill;
        img.ContextMenu = new ContextMenu();
        img.ContextMenu.FontSize = 12;
        //Удалить
        MenuItem menuItem = new MenuItem() { Header = "Удалить" };
        menuItem.Click += MenuItem_Click_Remove;
        img.ContextMenu.Items.Add(menuItem);
        //Размеры
        MenuItem menuItemRect = new MenuItem() { Header = "Размеры" };
        menuItemRect.Click += MenuItem_Click_Rectangle;
        img.ContextMenu.Items.Add(menuItemRect);

        templateCanvas.Children.Add(img);
        Canvas.SetLeft(img, 100);
        Canvas.SetTop(img, 100);
    }
    private void MenuItem_Click_QRCode(object sender, RoutedEventArgs e)
    {
        QRCodePropertiesFunction pageFunction = new QRCodePropertiesFunction();
        pageFunction.Return += new ReturnEventHandler<string>(QRCodePropertiesFunction_Returned);
        this.NavigationService.Navigate(pageFunction);
    }
    private void QRCodePropertiesFunction_Returned(object sender, ReturnEventArgs<string> e)
    {
        var k = templateCanvas.Children.OfType<Image>().Count() + 1;
        if (e==null || string.IsNullOrWhiteSpace(e.Result))
        {
            return;
        }
        var qrCodeImageProperty = JsonHelper.Deserialize<QRCodeImageProperties>(e.Result as string);
        qrCodeImageProperty.Name = "QRCodeImage_" + k.ToString();
        QRCodes[qrCodeImageProperty.Name] = qrCodeImageProperty;
        Image img = new Image();
        img.Name = qrCodeImageProperty.Name;
        img.AllowDrop = true;
        img.Stretch = Stretch.Fill;
        img.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
        img.VerticalAlignment = System.Windows.VerticalAlignment.Center;
        img.ContextMenu = new ContextMenu();
        img.ContextMenu.FontSize = 12;
        //Удалить
        MenuItem menuItem = new MenuItem() { Header = "Удалить" };
        menuItem.Click += MenuItem_Click_Remove;
        img.ContextMenu.Items.Add(menuItem);
        //ZOrder
        MenuItem menuItemZorderPlus = new MenuItem() { Header = "Z+" };
        menuItemZorderPlus.Click += MenuItem_Click_ZorderPlus;
        img.ContextMenu.Items.Add(menuItemZorderPlus);
        MenuItem menuItemZorderMinus = new MenuItem() { Header = "Z-" };
        menuItemZorderMinus.Click += MenuItem_Click_ZorderMinus;
        img.ContextMenu.Items.Add(menuItemZorderMinus);


        // Свойства
        MenuItem menuItemQRCode = new MenuItem() { Header = "Свойства" };
        menuItemQRCode.Click += MenuItem_Click_ChangeQRCodeProperties;
        img.ContextMenu.Items.Add(menuItemQRCode);
        var i = templateCanvas.Children.Add(img);
        QRCodeSetBinding(ref img);
        foreach (var bindingExp in img.BindingGroup.BindingExpressions)
        {
            bindingExp.UpdateTarget();
        }

        Report[SelectedSection].qRCodeImages.Add(qrCodeImageProperty);
        Report[SelectedSection].imageCanvasElements.Add(
            new ImageCanvasElement()
            {
                Name = qrCodeImageProperty.Name,
                Type = img.GetType().Name,
                Left = Canvas.GetLeft(img),
                Top = Canvas.GetTop(img),
                Width = img.Width,
                Height = img.Height,
                HorizontalAlignment = JsonHelper.Serialize<System.Windows.HorizontalAlignment>(img.HorizontalAlignment),
                VerticalAlignment = JsonHelper.Serialize<System.Windows.VerticalAlignment>(img.VerticalAlignment),
                Opacity = img.Opacity,
                Stretch = JsonHelper.Serialize<Stretch>(img.Stretch),
                ImageSourceString = img.Source is BitmapSource bitmapSource ? ImageCanvasElement.ImageToBase64(bitmapSource) : string.Empty,
            }
        );


    }
    private void MenuItem_Click_ChangeQRCodeProperties(object sender, RoutedEventArgs e)
    {
        MenuItem menuItem = sender as MenuItem;
        if (menuItem != null)
        {
            ContextMenu contextMenu = menuItem.Parent as ContextMenu;
            if (contextMenu != null)
            {
                if (contextMenu.PlacementTarget != null)
                {
                    Image owner = contextMenu.PlacementTarget as Image;
                    if (owner != null && owner.Name.StartsWith("QRCodeImage"))
                    {
                        QRCodePropertiesFunction pageFunction = new QRCodePropertiesFunction(JsonHelper.Serialize<QRCodeImageProperties>(QRCodes[owner.Name]));
                        pageFunction.Return += new ReturnEventHandler<string>(GetChangedQRCodeProperties);
                        this.NavigationService.Navigate(pageFunction);
                    }
                }
            }
        }
    }
    private void GetChangedQRCodeProperties(object sender, ReturnEventArgs<string> e)

    {
        if (e == null)
        {
            return;
        }
        if (string.IsNullOrWhiteSpace(e.Result))
        {
            return;
        }   
        var qrCodeImageProperty = JsonHelper.Deserialize<QRCodeImageProperties>(e.Result as string);
        QRCodes[qrCodeImageProperty.Name] = qrCodeImageProperty;
        var img = templateCanvas.Children.OfType<Image>().FirstOrDefault(x => x.Name == qrCodeImageProperty.Name);
        if (img != null)
        {
            if (img.Name == qrCodeImageProperty.Name)
            {
                QRCodeSetBinding(ref img);
                foreach(var bindingExp in img.BindingGroup.BindingExpressions)
                {
                    bindingExp.UpdateTarget();
                }
                Report[SelectedSection].qRCodeImages.RemoveAll(qr => qr.Name.Equals(qrCodeImageProperty.Name, StringComparison.OrdinalIgnoreCase));
                Report[SelectedSection].qRCodeImages.Add(qrCodeImageProperty);
                Report[SelectedSection].imageCanvasElements.RemoveAll(ice => ice.Name.Equals(qrCodeImageProperty.Name, StringComparison.OrdinalIgnoreCase));
                Report[SelectedSection].imageCanvasElements.Add(
                    new ImageCanvasElement()
                    {
                        Name = qrCodeImageProperty.Name,
                        Type = img.GetType().Name,
                        Left = Canvas.GetLeft(img),
                        Top = Canvas.GetTop(img),
                        Width = img.Width,
                        Height = img.Height,
                        HorizontalAlignment = JsonHelper.Serialize<System.Windows.HorizontalAlignment>(img.HorizontalAlignment),
                        VerticalAlignment = JsonHelper.Serialize<System.Windows.VerticalAlignment>(img.VerticalAlignment),
                        Opacity = img.Opacity,
                        Stretch = JsonHelper.Serialize<Stretch>(img.Stretch),
                        ImageSourceString = img.Source is BitmapSource bitmapSource ? ImageCanvasElement.ImageToBase64(bitmapSource) : string.Empty,
                    });
            }
        }
    }
    public SvgRenderer.SvgImage GenerateSvgBarcode(string content)
    {

        var barcodeWriter = new BarcodeWriterSvg
        {
            Format = BarcodeFormat.EAN_13,

            Options = new EncodingOptions
            {
                Width = 151,
                Height = 75,
                Margin = 0,
                PureBarcode = true,
                GS1Format = true
            },
            Renderer = new SvgRenderer()
        };
        var svgImage = barcodeWriter.Write(content);
        return svgImage; // Returns SVG markup as a string
    }
    private void MenuItem_Click_DataSources(object sender, RoutedEventArgs e)
    {
        QuerryDefFunction pageFunction = new QuerryDefFunction();
        pageFunction.Return += new ReturnEventHandler<string>(GetQuerryDef);
        this.NavigationService.Navigate(pageFunction);
    }

    private void GetQuerryDef(object sender, ReturnEventArgs<string> e)
    {
        if (e.Result == null)
        {
            return;
        }

    }

    private void MenuItem_Click_Service(object sender, RoutedEventArgs e)
    {

    }
    private void MenuItem_Click_Printer(object sender, RoutedEventArgs e)
    {
        var navigationWindow = new NavigationWindow();
        var appSettingsPage = new AppSettings(ref navigationWindow);
        navigationWindow.Content = appSettingsPage;
        navigationWindow.Activate();
        var result = navigationWindow.ShowDialog();
        if (result.HasValue && result.Value)
        {

        }
        else
        {

        }
    }
    private void MenuItem_Click_SaveTemplate(object sender, RoutedEventArgs e)
    {
        string json = CanvasSerializer.SerializeCanvas(templateCanvas
            , Barcodes.Values.ToList()
            , TextFieldProperties.Values.ToList()
            , QRCodes.Values.ToList()
            , Images.Values.ToList()
            , RectangleFigures.Values.ToList()
            , Lines.Values.ToList()
            ,Polygons.Values.ToList()
            );
        SaveFileDialog saveFileDialog = new SaveFileDialog();
        saveFileDialog.FileName = "Template"; // Default file name
        saveFileDialog.DefaultExt = ".json"; // Default file extension
        saveFileDialog.Filter = "Text documents (.json)|*.json"; // Filter files by extension
        if (saveFileDialog.ShowDialog() == true)
        {
            File.WriteAllText(saveFileDialog.FileName, json);
        }
    }
    private void MenuItem_Click_NewTemplate(object sender, RoutedEventArgs e)
    {

    }
    private void Button_Click_Save(object sender, RoutedEventArgs e)
    {
        foreach(var section in Report.Keys)
        {
            var index = ReportSections.IndexOf(section);
            if (index<0)
            {
                Report[section].elements.Clear();
                Report[section].textFieldValues.Clear();
                Report[section].lineProperties.Clear();
                Report[section].rectangleFigures.Clear();
                Report[section].imageCanvasElements.Clear();
                Report[section].barcodeImage.Clear();
                Report[section].imageCanvasElements.Clear();
                Report[section].qRCodeImages.Clear();
                Report[section].textBoxCanvasElements.Clear();
                Report[section].polygonProperties.Clear();
                Report.Remove(section);
            }

        }
        string json = JsonHelper.Serialize<Dictionary<CrystalReportSection, TemplateRecord>>(Report);
        SaveFileDialog saveFileDialog = new SaveFileDialog();
        saveFileDialog.FileName = "Template"; // Default file name
        saveFileDialog.DefaultExt = ".json"; // Default file extension
        saveFileDialog.Filter = "Text documents (.json)|*.json"; // Filter files by extension
        if (saveFileDialog.ShowDialog() == true)
        {
            File.WriteAllText(saveFileDialog.FileName, json);
        }
    }
    private void Button_Click_SaveAndClose(object sender, RoutedEventArgs e)
    {
        string json = JsonHelper.Serialize<Dictionary<CrystalReportSection,TemplateRecord>>(Report);
        SaveFileDialog saveFileDialog = new SaveFileDialog();
        saveFileDialog.FileName = "Template"; // Default file name
        saveFileDialog.DefaultExt = ".json"; // Default file extension
        saveFileDialog.Filter = "Text documents (.json)|*.json"; // Filter files by extension
        if (saveFileDialog.ShowDialog() == true)
        {
            File.WriteAllText(saveFileDialog.FileName, json);
        }
    }
    private void btPrint_Click(object sender, RoutedEventArgs e)
    {
        var navigationWindow = new NavigationWindow();
        string json = JsonHelper.Serialize<Dictionary<CrystalReportSection, TemplateRecord>>(Report);
        var pagePrint = new PagePrint(navigationWindow,json);
        navigationWindow.Content = pagePrint;
        navigationWindow.Activate();
        var result = navigationWindow.ShowDialog();
    }
    public  void SetBindingLineProperties(ref Line line)
    {
        var bindingGroup = new BindingGroup();
        line.BindingGroup = bindingGroup;
        var lineFigure = Lines[line.Name];
        Binding bindingStrokeEndLineCap = new Binding();
        bindingStrokeEndLineCap.Source = Lines;
        bindingStrokeEndLineCap.Path = new PropertyPath("[" + lineFigure.Name + "].StrokeEndLineCap");
        bindingStrokeEndLineCap.Mode = BindingMode.TwoWay;
        bindingStrokeEndLineCap.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        line.SetBinding(Line.StrokeEndLineCapProperty, bindingStrokeEndLineCap);

        Binding bindingStrokeStartLineCap = new Binding();
        bindingStrokeStartLineCap.Source = Lines;
        bindingStrokeStartLineCap.Path = new PropertyPath("[" + lineFigure.Name + "].StrokeStartLineCap");
        bindingStrokeStartLineCap.Mode = BindingMode.TwoWay;
        bindingStrokeStartLineCap.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        line.SetBinding(Line.StrokeStartLineCapProperty, bindingStrokeStartLineCap);

        Binding bindingThickness = new Binding();
        bindingThickness.Source = Lines;
        bindingThickness.Path = new PropertyPath("[" + lineFigure.Name + "].StrokeThickness");
        bindingThickness.Mode = BindingMode.TwoWay;
        bindingThickness.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        line.SetBinding(Line.StrokeThicknessProperty, bindingThickness);

        Binding bindingX1 = new Binding();
        bindingX1.Source = Lines;
        bindingX1.Path = new PropertyPath("[" + lineFigure.Name + "].X1");
        bindingX1.Mode = BindingMode.TwoWay;
        bindingX1.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        line.SetBinding(Line.X1Property, bindingX1);

        Binding bindingY1 = new Binding();
        bindingY1.Source = Lines;
        bindingY1.Path = new PropertyPath("[" + lineFigure.Name + "].Y1");
        bindingY1.Mode = BindingMode.TwoWay;
        bindingY1.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        line.SetBinding(Line.Y1Property, bindingY1);

        Binding bindingX2 = new Binding();
        bindingX2.Source = Lines;
        bindingX2.Path = new PropertyPath("[" + lineFigure.Name + "].X2");
        bindingX2.Mode = BindingMode.TwoWay;
        bindingX2.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        line.SetBinding(Line.X2Property, bindingX2);

        Binding bindingY2 = new Binding();
        bindingY2.Source = Lines;
        bindingY2.Path = new PropertyPath("[" + lineFigure.Name + "].Y2");
        bindingY2.Mode = BindingMode.TwoWay;
        bindingY2.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        line.SetBinding(Line.Y2Property, bindingY2);

        Binding bindingStroke = new Binding();
        bindingStroke.Source = Lines;
        bindingStroke.Converter = new ColorToBrushConverter();
        bindingStroke.Path = new PropertyPath("[" + lineFigure.Name + "].Stroke");
        bindingStroke.Mode = BindingMode.TwoWay;
        bindingStroke.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        line.SetBinding(Line.StrokeProperty, bindingStroke);
        
        Binding bindingAngle = new Binding();
        bindingAngle.Source = Lines;
        bindingAngle.Path = new PropertyPath("[" + lineFigure.Name + "].Angle");
        bindingAngle.Mode = BindingMode.TwoWay;
        bindingAngle.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;

        var rotate = new RotateTransform();
        BindingOperations.SetBinding(rotate, RotateTransform.AngleProperty, bindingAngle);
        line.RenderTransform = rotate;

        Binding bindingLeft = new Binding()
        {
            Mode = BindingMode.TwoWay,
            Source = Lines,
            Path = new PropertyPath("[" + lineFigure.Name + "].Left"),
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        };
        BindingOperations.SetBinding(line, Canvas.LeftProperty, bindingLeft);
        Binding bindingTop = new Binding()
        {
            Mode = BindingMode.TwoWay,
            Source = Lines,
            Path = new PropertyPath("[" + lineFigure.Name + "].Top"),
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        };
        BindingOperations.SetBinding(line, Canvas.TopProperty, bindingTop);
    }
    public void SetBindingRectangleProperties(ref Rectangle rectangle)
    {
        var bindingGroup = new BindingGroup();
        rectangle.BindingGroup = bindingGroup;
        var rectangleFigure = RectangleFigures[rectangle.Name];
        Binding bindingWidth = new Binding();
        bindingWidth.Source = RectangleFigures;
        bindingWidth.Path = new PropertyPath("[" + rectangleFigure.Name + "].Width");
        bindingWidth.Mode = BindingMode.TwoWay;
        bindingWidth.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        rectangle.SetBinding(Rectangle.WidthProperty, bindingWidth);
        BindingOperations.SetBinding(rectangle, Canvas.LeftProperty, new Binding()
        {
            Source = RectangleFigures,
            Path = new PropertyPath("[" + rectangleFigure.Name + "].Left"),
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        BindingOperations.SetBinding(rectangle, Canvas.TopProperty, new Binding()
        {
            Source = RectangleFigures,
            Path = new PropertyPath("[" + rectangleFigure.Name + "].Top"),
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });

        Binding bindingHeight = new Binding();
        bindingHeight.Source = RectangleFigures;
        bindingHeight.Path = new PropertyPath("[" + rectangleFigure.Name + "].Height");
        bindingHeight.Mode = BindingMode.TwoWay;
        bindingHeight.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        rectangle.SetBinding(Rectangle.HeightProperty, bindingHeight);


        Binding bindingThickness = new Binding();
        bindingThickness.Source = RectangleFigures;
        bindingThickness.Path = new PropertyPath("[" + rectangleFigure.Name + "].StrokeThickness");
        bindingThickness.Mode = BindingMode.TwoWay;
        bindingThickness.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        rectangle.SetBinding(Rectangle.StrokeThicknessProperty, bindingThickness);

        Binding bindingRadiusX = new Binding();
        bindingRadiusX.Source = RectangleFigures;
        bindingRadiusX.Path = new PropertyPath("[" + rectangleFigure.Name + "].RadiusX");
        bindingRadiusX.Mode = BindingMode.TwoWay;
        bindingRadiusX.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        rectangle.SetBinding(Rectangle.RadiusXProperty, bindingRadiusX);

        Binding bindingRadiusY = new Binding();
        bindingRadiusY.Source = RectangleFigures;
        bindingRadiusY.Path = new PropertyPath("[" + rectangleFigure.Name + "].RadiusY");
        bindingRadiusY.Mode = BindingMode.TwoWay;
        bindingRadiusY.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        rectangle.SetBinding(Rectangle.RadiusYProperty, bindingRadiusY);

        Binding bindingStroke = new Binding();
        bindingStroke.Source = RectangleFigures;
        bindingStroke.Converter = new ColorToBrushConverter();
        bindingStroke.Path = new PropertyPath("[" + rectangleFigure.Name + "].Stroke");
        bindingStroke.Mode = BindingMode.TwoWay;
        bindingStroke.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        rectangle.SetBinding(Rectangle.StrokeProperty, bindingStroke);

        Binding bindingFill = new Binding();
        bindingFill.Source = RectangleFigures;
        bindingFill.Converter = new ColorToBrushConverter();
        bindingFill.Path = new PropertyPath("[" + rectangleFigure.Name + "].Fill");
        bindingFill.Mode = BindingMode.TwoWay;
        bindingFill.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        BindingOperations.SetBinding(rectangle, Rectangle.FillProperty, bindingFill);
        Binding bindingStretch = new Binding();
        bindingStretch.Source = RectangleFigures;
        bindingStretch.Path = new PropertyPath("[" + rectangleFigure.Name + "].Stretch");
        bindingStretch.Mode = BindingMode.TwoWay;
        bindingStretch.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        rectangle.SetBinding(Rectangle.StretchProperty, bindingStretch);

        Binding bindingAngle = new Binding();
        bindingAngle.Source = RectangleFigures;
        bindingAngle.Path = new PropertyPath("[" + rectangleFigure.Name + "].Angle");
        bindingAngle.Mode = BindingMode.TwoWay;
        bindingAngle.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;

        var rotate = new RotateTransform();
        BindingOperations.SetBinding(rotate, RotateTransform.AngleProperty, bindingAngle);
        rectangle.RenderTransform = rotate;

    }
    public void SetBindingPolygonProperties(ref Polygon polygon)
    {
        var bindingGroup = new BindingGroup();
        bindingGroup.Name = "BindingGroup_" + polygon.Name;
        polygon.BindingGroup = bindingGroup;
        var polygonFigure = Polygons[polygon.Name];

        Binding bindingPoints = new Binding
        {
            Source = Polygons,
            Path = new PropertyPath($"[{polygonFigure.Name}].Points"),
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        };

        BindingOperations.SetBinding(
            polygon,
            Polygon.PointsProperty,
            bindingPoints);


        Binding bindingThickness = new Binding();
        bindingThickness.Source = Polygons;
        bindingThickness.Path = new PropertyPath($"[{polygonFigure.Name}].StrokeThickness");
        bindingThickness.Mode = BindingMode.TwoWay;
        bindingThickness.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        bindingThickness.BindingGroupName = bindingGroup.Name;
        polygon.SetBinding(Polygon.StrokeThicknessProperty, bindingThickness);

            Binding bindingStroke = new Binding();
            bindingStroke.Source = Polygons;
            bindingStroke.Converter = new ColorToBrushConverter();
            bindingStroke.Path = new PropertyPath($"[{polygonFigure.Name}].Stroke");
            bindingStroke.Mode = BindingMode.TwoWay;
            bindingStroke.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            polygon.SetBinding(Polygon.StrokeProperty, bindingStroke);

            Binding bindingFill = new Binding();
            bindingFill.Source = Polygons;
            bindingFill.Converter = new ColorToBrushConverter();
            bindingFill.Path = new PropertyPath($"[{polygonFigure.Name}].Background");
            bindingFill.Mode = BindingMode.TwoWay;
            bindingFill.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            polygon.SetBinding(Polygon.FillProperty, bindingFill);


        Binding bindingLeft = new Binding()
        {
            Mode = BindingMode.TwoWay,
            Source = Polygons,
            Path = new PropertyPath($"[{ polygonFigure.Name }].Left"),
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        };
        BindingOperations.SetBinding(polygon, Canvas.LeftProperty, bindingLeft);
        Binding bindingTop = new Binding()
        {
            Mode = BindingMode.TwoWay,
            Source = Polygons,
            Path = new PropertyPath($"[{polygonFigure.Name}].Top"),
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        };
        BindingOperations.SetBinding(polygon, Canvas.TopProperty, bindingTop);
    }
   
    public void SetBinding(ref TextBox textBox)
    {
        var bindingGroup = new BindingGroup();
        textBox.BindingGroup = bindingGroup;
        var textFieldValue = TextFieldProperties[textBox.Name];
        textBox.SetBinding(TextBox.TextProperty, new Binding()
        {
            Source = TextFieldProperties,
            Path = new PropertyPath("[" + textFieldValue.Name + "].Value"),
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        Binding bindingWidth = new Binding();
        bindingWidth.Source = TextFieldProperties;
        bindingWidth.Path = new PropertyPath("[" + textFieldValue.Name + "].Width");
        bindingWidth.Mode = BindingMode.TwoWay;
        bindingWidth.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        textBox.SetBinding(TextBox.WidthProperty, bindingWidth);

        Binding bindingHeight = new Binding();
        bindingHeight.Source = TextFieldProperties;
        bindingHeight.Path = new PropertyPath("[" + textFieldValue.Name + "].Height");
        bindingHeight.Mode = BindingMode.TwoWay;
        bindingHeight.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        textBox.SetBinding(TextBox.HeightProperty, bindingHeight);

        Binding bindingFontFamily = new Binding();
        bindingFontFamily.Source = TextFieldProperties;
        bindingFontFamily.Converter = new StringToFontFamilyConverter();
        bindingFontFamily.Path = new PropertyPath("[" + textFieldValue.Name + "].FontFamily");
        bindingFontFamily.Mode = BindingMode.TwoWay;
        bindingFontFamily.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        textBox.SetBinding(TextBox.FontFamilyProperty, bindingFontFamily);

        Binding bindingFontSize = new Binding();
        bindingFontSize.Source = TextFieldProperties;
        bindingFontSize.Path = new PropertyPath("[" + textFieldValue.Name + "].FontSize");
        bindingFontSize.Mode = BindingMode.TwoWay;
        bindingFontSize.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        textBox.SetBinding(TextBox.FontSizeProperty, bindingFontSize);

        Binding bindingForeground = new Binding();
        bindingForeground.Source = TextFieldProperties;
        bindingForeground.Converter = new ColorToSolidColorBrushConverter();
        bindingForeground.Path = new PropertyPath("[" + textFieldValue.Name + "].Foreground");
        bindingForeground.Mode = BindingMode.TwoWay;
        bindingForeground.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        textBox.SetBinding(TextBox.ForegroundProperty, bindingForeground);

        Binding bindingAlignment = new Binding();
        bindingAlignment.Source = TextFieldProperties;
        bindingAlignment.Path = new PropertyPath("[" + textFieldValue.Name + "].TextAlignment");
        bindingAlignment.Mode = BindingMode.TwoWay;
        bindingAlignment.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        textBox.SetBinding(TextBox.TextAlignmentProperty, bindingAlignment);

        Binding bindingHorizontalAlignment = new Binding();
        bindingHorizontalAlignment.Source = TextFieldProperties;
        bindingHorizontalAlignment.Path = new PropertyPath("[" + textFieldValue.Name + "].HorizontalAlignment");
        bindingHorizontalAlignment.Mode = BindingMode.TwoWay;
        bindingHorizontalAlignment.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        textBox.SetBinding(TextBox.HorizontalAlignmentProperty, bindingHorizontalAlignment);

        Binding bindingVerticalAlignment = new Binding();
        bindingVerticalAlignment.Source = TextFieldProperties;
        bindingVerticalAlignment.Path = new PropertyPath("[" + textFieldValue.Name + "].VerticalAlignment");
        bindingVerticalAlignment.Mode = BindingMode.TwoWay;
        bindingVerticalAlignment.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        textBox.SetBinding(TextBox.VerticalAlignmentProperty, bindingVerticalAlignment);


        Binding bindingBackground = new Binding();
        bindingBackground.Source = TextFieldProperties;
        bindingBackground.Converter = new ColorToSolidColorBrushConverter();
        bindingBackground.Path = new PropertyPath("[" + textFieldValue.Name + "].Background");
        bindingBackground.Mode = BindingMode.TwoWay;
        bindingBackground.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        textBox.SetBinding(TextBox.BackgroundProperty, bindingBackground);

        textBox.RenderTransformOrigin = new Point(0.5, 0.5);
        Binding bindingAngle = new Binding();
        bindingAngle.Converter = new AngleToRotationConverter();
        bindingAngle.Mode = BindingMode.TwoWay;
        bindingAngle.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        bindingAngle.Source = TextFieldProperties;
        bindingAngle.Path = new PropertyPath("[" + textFieldValue.Name + "].Angle");
        textBox.SetBinding(TextBox.RenderTransformProperty, bindingAngle);

        Binding bindingLeft = new Binding()
        {
            Mode = BindingMode.TwoWay,
            Source = TextFieldProperties,
            Path = new PropertyPath("[" + textFieldValue.Name + "].Left"),
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        };
        BindingOperations.SetBinding(textBox,Canvas.LeftProperty, bindingLeft);

        Binding bindingTop = new Binding()
        {
            Mode = BindingMode.TwoWay,
            Source = TextFieldProperties,
            Path = new PropertyPath("[" + textFieldValue.Name + "].Top"),
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        };
        BindingOperations.SetBinding(textBox, Canvas.TopProperty, bindingTop);
    }
    public void BarcodeSetBinding(ref Image barcodeImage)
    {
        BindingGroup group = new BindingGroup();
        barcodeImage.BindingGroup = group;
        var barcodeImageProperties = Barcodes[barcodeImage.Name];
        Binding binding = new Binding();
        binding.Converter = new BarcodeImagePropertiesToBitmapImageConverter();
        binding.Mode = BindingMode.OneWay;
        binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        binding.Source = Barcodes;
        binding.ConverterParameter = barcodeImageProperties.Name;
        barcodeImage.SetBinding(Image.SourceProperty, binding);

        barcodeImage.RenderTransformOrigin = new Point(0.5, 0.5);
        var bindingAngle = new Binding();
        bindingAngle.Converter = new AngleToRotationConverter();
        bindingAngle.Source = Barcodes;
        bindingAngle.Path = new PropertyPath("[" + barcodeImageProperties.Name + "].Angle");
        bindingAngle.Mode = BindingMode.TwoWay;
        bindingAngle.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        barcodeImage.SetBinding(Image.RenderTransformProperty, bindingAngle);

        Binding bindingWidth = new Binding()
        {
            Source = Barcodes,
            Path = new PropertyPath("[" + barcodeImage.Name + "].Width"),
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        };
        barcodeImage.SetBinding(Image.WidthProperty, bindingWidth);

        var bindingHeight = new Binding()
        {
            Source = Barcodes,
            Path = new PropertyPath("[" + barcodeImage.Name + "].Height"),
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        };
        barcodeImage.SetBinding(Image.HeightProperty, bindingHeight);

        Binding bindingLeft = new Binding()
        {
            Mode = BindingMode.TwoWay,
            Source = Barcodes,
            Path = new PropertyPath("[" + barcodeImageProperties.Name + "].Left"),
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        };
        BindingOperations.SetBinding(barcodeImage, Canvas.LeftProperty, bindingLeft);
        Binding bindingTop = new Binding()
        {
            Mode = BindingMode.TwoWay,
            Source = Barcodes,
            Path = new PropertyPath("[" + barcodeImageProperties.Name + "].Top"),
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        };
        BindingOperations.SetBinding(barcodeImage, Canvas.TopProperty, bindingTop);

    }
    public void QRCodeSetBinding(ref Image qrcodeImage)
    { 
        var qrCodeImageProperties = QRCodes[qrcodeImage.Name];
        var group = new BindingGroup();
        qrcodeImage.BindingGroup= group;

        Binding binding = new Binding();
        binding.Converter = new SvgStringToBitmapImageConverter();
        binding.Mode = BindingMode.OneWay;
        binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        binding.Source = QRCodes;
        binding.Path = new PropertyPath("[" + qrCodeImageProperties.Name + "].Value");
        qrcodeImage.SetBinding(Image.SourceProperty, binding);

        qrcodeImage.RenderTransformOrigin = new Point(0.5, 0.5);
        var bindingAngle = new Binding();
        bindingAngle.Converter = new AngleToRotationConverter();
        bindingAngle.Source = QRCodes;
        bindingAngle.Path = new PropertyPath("[" + qrCodeImageProperties.Name + "].Angle");
        bindingAngle.Mode = BindingMode.TwoWay;
        bindingAngle.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        BindingOperations.SetBinding(qrcodeImage, Image.RenderTransformProperty, bindingAngle);

        Binding bindingWidth = new Binding();
        bindingWidth.Converter = new QRCodePropertiesToImagePropertieConverter();
        bindingWidth.ConverterParameter = qrCodeImageProperties.Name;
        bindingWidth.Mode = BindingMode.TwoWay;
        bindingWidth.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        bindingWidth.Source = QRCodes;
        bindingWidth.Path = new PropertyPath("[" + qrcodeImage.Name + "].Width");
        qrcodeImage.SetBinding(Image.WidthProperty, bindingWidth);

        Binding bindingHeight = new Binding();
        bindingHeight.Converter = new QRCodePropertiesToImagePropertieConverter();
        bindingHeight.ConverterParameter = qrcodeImage.Name;
        bindingHeight.Mode = BindingMode.TwoWay;
        bindingHeight.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        bindingHeight.Source = QRCodes;
        bindingHeight.Path = new PropertyPath("[" + qrCodeImageProperties.Name + "].Height");
        qrcodeImage.SetBinding(Image.HeightProperty, bindingHeight);

        Binding bindingLeft = new Binding()
        {
            Mode = BindingMode.TwoWay,
            Source = QRCodes,
            Path = new PropertyPath("[" + qrCodeImageProperties.Name + "].Left"),
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        };
        BindingOperations.SetBinding(qrcodeImage, Canvas.LeftProperty, bindingLeft);
        Binding bindingTop = new Binding()
        {
            Mode = BindingMode.TwoWay,
            Source = QRCodes,
            Path = new PropertyPath("[" + qrCodeImageProperties.Name + "].Top"),
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        };
        BindingOperations.SetBinding(qrcodeImage, Canvas.TopProperty, bindingTop);

    }
    public void ImageSetBinding(ref Image image)
    {
        var imageProperties = Images[image.Name];
        var bindingGroup = new BindingGroup();
        image.BindingGroup = bindingGroup;
        Binding binding = new Binding();
        binding.Converter = new ByteArrayToBitmapImageConverter();
        binding.Mode = BindingMode.OneWay;
        binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        binding.Source = Images;
        binding.Path = new PropertyPath("[" + imageProperties.Name + "].ImageSource");
        image.SetBinding(Image.SourceProperty, binding);

        image.RenderTransformOrigin = new Point(0.5, 0.5);
        var bindingAngle = new Binding();
        bindingAngle.Converter = new AngleToRotationConverter();
        bindingAngle.Source = Images;
        bindingAngle.Path = new PropertyPath("[" + imageProperties.Name + "].Angle");
        bindingAngle.Mode = BindingMode.TwoWay;
        bindingAngle.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        image.SetBinding(Image.RenderTransformProperty, bindingAngle);

        Binding bindingWidth = new Binding();
        bindingWidth.Mode = BindingMode.TwoWay;
        bindingWidth.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        bindingWidth.Source = Images;
        bindingWidth.Path = new PropertyPath("[" + imageProperties.Name + "].Width");
        image.SetBinding(Image.WidthProperty, bindingWidth);

        Binding bindingHeight = new Binding();
        bindingHeight.Mode = BindingMode.TwoWay;
        bindingHeight.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        bindingHeight.Source = Images;
        bindingHeight.Path = new PropertyPath("[" + imageProperties.Name + "].Height");
        image.SetBinding(Image.HeightProperty, bindingHeight);
        Binding bindingLeft = new Binding()
        {
            Mode = BindingMode.TwoWay,
            Source = Images,
            Path = new PropertyPath("[" + imageProperties.Name + "].Left"),
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        };
        BindingOperations.SetBinding(image, Canvas.LeftProperty, bindingLeft);
        Binding bindingTop = new Binding()
        {
            Mode = BindingMode.TwoWay,
            Source = Images,
            Path = new PropertyPath("[" + imageProperties.Name + "].Top"),
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        };
        BindingOperations.SetBinding(image, Canvas.TopProperty, bindingTop);
    }
    private void LoadTemplate(ref TemplateRecord template)
    {
        templateCanvas.BeginInit();
        templateCanvas.Children.Clear(); // Очистка текущего холста перед загрузкой нового макета
        foreach (var canvasElement in template.elements)
        {
            switch (canvasElement.Type)
            {
                case "Rectangle":
                    {
                        var rectangle = new System.Windows.Shapes.Rectangle
                        {
                            Name = canvasElement.Name,
                            Width = canvasElement.Width,
                            Height = canvasElement.Height,
                            Fill = JsonHelper.Deserialize<Brush>(canvasElement.Fill),
                            Stroke = JsonHelper.Deserialize<Brush>(canvasElement.Stroke),
                            StrokeThickness = canvasElement.StrokeThickness,
                            RadiusX = canvasElement.RadiusX,
                            RadiusY = canvasElement.RadiusY,
                            HorizontalAlignment = canvasElement.HorizontalAlignment != null ? JsonHelper.Deserialize<System.Windows.HorizontalAlignment>(canvasElement.HorizontalAlignment) : System.Windows.HorizontalAlignment.Left,
                            VerticalAlignment = canvasElement.VerticalAlignment != null ? JsonHelper.Deserialize<System.Windows.VerticalAlignment>(canvasElement.VerticalAlignment) : System.Windows.VerticalAlignment.Top,
                            Opacity = canvasElement.Opacity,
                            Stretch = JsonHelper.Deserialize<Stretch>(canvasElement.Stretch)
                        };
                        Canvas.SetLeft(rectangle, canvasElement.Left);
                        Canvas.SetTop(rectangle, canvasElement.Top);
                        templateCanvas.Children.Add(rectangle);
                        //Удалить
                        rectangle.ContextMenu = new ContextMenu();
                        rectangle.ContextMenu.FontSize = 12;
                        MenuItem menuItem = new MenuItem() { Header = "Удалить" };
                        menuItem.Click += MenuItem_Click_Remove;
                        rectangle.ContextMenu.Items.Add(menuItem);
                        //Свойства
                        MenuItem menuItemRectProperties = new MenuItem() { Header = "Свойства прямоугольника" };
                        menuItemRectProperties.Click += MenuItem_Click_SetRectangleProperties;
                        rectangle.ContextMenu.Items.Add(menuItemRectProperties);
                        //Zorder
                        MenuItem menuItemZorderPlus = new MenuItem() { Header = "Z+" };
                        menuItemZorderPlus.Click += MenuItem_Click_ZorderPlus;
                        rectangle.ContextMenu.Items.Add(menuItemZorderPlus);

                        MenuItem menuItemZorderMinus = new MenuItem() { Header = "Z-" };
                        menuItemZorderMinus.Click += MenuItem_Click_ZorderMinus;
                        rectangle.ContextMenu.Items.Add(menuItemZorderMinus);
                    }
                    break;
                case "Line":
                    {
                        var line = new System.Windows.Shapes.Line
                        {
                            Name = canvasElement.Name,
                            X1 = canvasElement.X1,
                            Y1 = canvasElement.Y1,
                            X2 = canvasElement.X2,
                            Y2 = canvasElement.Y2,
                            Stroke = JsonHelper.Deserialize<Brush>(canvasElement.Stroke),
                            StrokeThickness = canvasElement.StrokeThickness,
                            StrokeStartLineCap = JsonHelper.Deserialize<PenLineCap>(canvasElement.StrokeStartLineCap),
                            StrokeEndLineCap = JsonHelper.Deserialize<PenLineCap>(canvasElement.StrokeEndLineCap),
                        };
                        Canvas.SetLeft(line, canvasElement.Left);
                        Canvas.SetTop(line, canvasElement.Top);
                        templateCanvas.Children.Add(line);
                        //Удалить
                        line.ContextMenu = new ContextMenu();
                        line.ContextMenu.FontSize = 12;
                        MenuItem menuItem = new MenuItem() { Header = "Удалить" };
                        menuItem.Click += MenuItem_Click_Remove;
                        line.ContextMenu.Items.Add(menuItem);
                        //Размеры
                        MenuItem menuItemLineProperties = new MenuItem() { Header = "Свойства" };
                        menuItemLineProperties.Click += MenuItem_Click_LineProperties;
                        line.ContextMenu.Items.Add(menuItemLineProperties);
                        //Zorder
                        MenuItem menuItemZorderPlus = new MenuItem() { Header = "Z+" };
                        menuItemZorderPlus.Click += MenuItem_Click_ZorderPlus;
                        line.ContextMenu.Items.Add(menuItemZorderPlus);

                        MenuItem menuItemZorderMinus = new MenuItem() { Header = "Z-" };
                        menuItemZorderMinus.Click += MenuItem_Click_ZorderMinus;
                        line.ContextMenu.Items.Add(menuItemZorderMinus);
                    }
                    break;
                case "Polygon":
                    {
                        var polygon = new System.Windows.Shapes.Polygon
                        {
                            Name = canvasElement.Name,
                            Points = new PointCollection( canvasElement.Points),
                            Fill = JsonHelper.Deserialize<Brush>(canvasElement.Fill),
                            Stroke = JsonHelper.Deserialize<Brush>(canvasElement.Stroke),
                            StrokeThickness = canvasElement.StrokeThickness,
                            Opacity = canvasElement.Opacity,
                            Stretch = JsonHelper.Deserialize<Stretch>(canvasElement.Stretch),
                        };

                        Canvas.SetLeft(polygon, canvasElement.Left);
                        Canvas.SetTop(polygon, canvasElement.Top);
                        templateCanvas.Children.Add(polygon);
                        //Удалить
                        polygon.ContextMenu = new ContextMenu();
                        polygon.ContextMenu.FontSize = 12;
                        MenuItem menuItem = new MenuItem() { Header = "Удалить" };
                        menuItem.Click += MenuItem_Click_Remove;
                        polygon.ContextMenu.Items.Add(menuItem);
                        //Свойства
                        MenuItem menuItemRectProperties = new MenuItem() { Header = "Свойства полигона" };
                        menuItemRectProperties.Click += MenuItem_Click_PolygonProperties;
                        polygon.ContextMenu.Items.Add(menuItemRectProperties);
                        //Zorder
                        MenuItem menuItemZorderPlus = new MenuItem() { Header = "Z+" };
                        menuItemZorderPlus.Click += MenuItem_Click_ZorderPlus;
                        polygon.ContextMenu.Items.Add(menuItemZorderPlus);

                        MenuItem menuItemZorderMinus = new MenuItem() { Header = "Z-" };
                        menuItemZorderMinus.Click += MenuItem_Click_ZorderMinus;
                        polygon.ContextMenu.Items.Add(menuItemZorderMinus);
                    }
                    break;
                case "Polyline":
                    {
                        var polyline = new System.Windows.Shapes.Polyline
                        {
                            Name = canvasElement.Name,
                            Points = new PointCollection(canvasElement.Points),
                            Fill = JsonHelper.Deserialize<Brush>(canvasElement.Fill),
                            Stroke = JsonHelper.Deserialize<Brush>(canvasElement.Stroke),
                            StrokeThickness = canvasElement.StrokeThickness,
                            Opacity = canvasElement.Opacity,
                        };
                        templateCanvas.Children.Add(polyline);
                        //Удалить
                        polyline.ContextMenu = new ContextMenu();
                        polyline.ContextMenu.FontSize = 12;
                        MenuItem menuItem = new MenuItem() { Header = "Удалить" };
                        menuItem.Click += MenuItem_Click_Remove;
                        polyline.ContextMenu.Items.Add(menuItem);
                        //Zorder
                        MenuItem menuItemZorderPlus = new MenuItem() { Header = "Z+" };
                        menuItemZorderPlus.Click += MenuItem_Click_ZorderPlus;
                        polyline.ContextMenu.Items.Add(menuItemZorderPlus);
                        MenuItem menuItemZorderMinus = new MenuItem() { Header = "Z-" };
                        menuItemZorderMinus.Click += MenuItem_Click_ZorderMinus;
                        polyline.ContextMenu.Items.Add(menuItemZorderMinus);
                    }
                    break;
            }
        }

        foreach (var textBoxElement in template.textBoxCanvasElements)
        {
            var textBox = new TextBox
            {
                Name = textBoxElement.Name,
                Text = textBoxElement.Text,
                Width = textBoxElement.Width,
                Height = textBoxElement.Height,
                Background = string.IsNullOrWhiteSpace(textBoxElement.Background)?new SolidColorBrush(System.Windows.Media.Colors.Transparent):JsonHelper.Deserialize<Brush>(textBoxElement.Background),
                Foreground = string.IsNullOrWhiteSpace(textBoxElement.Foreground)? new SolidColorBrush(System.Windows.Media.Colors.Black) : JsonHelper.Deserialize<Brush>(textBoxElement.Foreground),
                FontFamily = new FontFamily(!string.IsNullOrWhiteSpace(textBoxElement.FontFamily) ?textBoxElement.FontFamily:"Arial"),
                FontSize = textBoxElement.FontSize,
                FontStyle = string.IsNullOrWhiteSpace(textBoxElement.FontStyle) ? new System.Windows.FontStyle():JsonHelper.Deserialize<System.Windows.FontStyle>(textBoxElement.FontStyle),
                FontWeight = string.IsNullOrWhiteSpace(textBoxElement.FontWeight) ?System.Windows.FontWeights.Normal: JsonHelper.Deserialize<System.Windows.FontWeight>(textBoxElement.FontWeight),
                FontStretch = string.IsNullOrWhiteSpace(textBoxElement.FontStretch) ?System.Windows.FontStretches.Normal:JsonHelper.Deserialize<System.Windows.FontStretch>(textBoxElement.FontStretch),
                TextWrapping = string.IsNullOrWhiteSpace(textBoxElement.TextWrapping) ? System.Windows.TextWrapping.NoWrap:JsonHelper.Deserialize<TextWrapping>(textBoxElement.TextWrapping),
                RenderTransform = new RotateTransform(textBoxElement.Angle)
            };
            Canvas.SetLeft(textBox, textBoxElement.Left);
            Canvas.SetTop(textBox, textBoxElement.Top);
            //Удалить
            textBox.ContextMenu = new ContextMenu();
            textBox.ContextMenu.FontSize = 12;
            MenuItem menuItem = new MenuItem() { Header = "Удалить" };
            menuItem.Click += MenuItem_Click_Remove;
            textBox.ContextMenu.Items.Add(menuItem);
            //ZOrder
            MenuItem menuItemZorderPlus = new MenuItem() { Header = "Z+" };
            menuItemZorderPlus.Click += MenuItem_Click_ZorderPlus;
            textBox.ContextMenu.Items.Add(menuItemZorderPlus);
            MenuItem menuItemZorderMinus = new MenuItem() { Header = "Z-" };
            menuItemZorderMinus.Click += MenuItem_Click_ZorderMinus;
            textBox.ContextMenu.Items.Add(menuItemZorderMinus);

            //Значение
            MenuItem menuItemText = new MenuItem() { Header = "Текст" };
            menuItemText.Click += OnNavigateButtonClick;
            textBox.ContextMenu.Items.Add(menuItemText);
            templateCanvas.Children.Add(textBox);
        }
        foreach (var imageElement in template.imageCanvasElements)
        {
            var image = new System.Windows.Controls.Image
            {
                Name = imageElement.Name,
                Width = imageElement.Width,
                Height = imageElement.Height,
                HorizontalAlignment = JsonHelper.Deserialize<System.Windows.HorizontalAlignment>(imageElement.HorizontalAlignment),
                VerticalAlignment = JsonHelper.Deserialize<System.Windows.VerticalAlignment>(imageElement.VerticalAlignment),
                Opacity = imageElement.Opacity,
                Stretch = JsonHelper.Deserialize<Stretch>(imageElement.Stretch),
                Source = (string.IsNullOrEmpty(imageElement.ImageSourceString)) ? JsonHelper.Deserialize<ImageSource>(imageElement.Source) : ImageCanvasElement.Base64ToImage(imageElement.ImageSourceString)
            };

            Canvas.SetLeft(image, imageElement.Left);
            Canvas.SetTop(image, imageElement.Top);
            //Удалить
            image.ContextMenu = new ContextMenu();
            image.ContextMenu.FontSize = 12;
            MenuItem menuItem = new MenuItem() { Header = "Удалить" };
            menuItem.Click += MenuItem_Click_Remove;
            image.ContextMenu.Items.Add(menuItem);
            //Zorder
            MenuItem menuItemZorderPlus = new MenuItem() { Header = "Z+" };
            menuItemZorderPlus.Click += MenuItem_Click_ZorderPlus;
            image.ContextMenu.Items.Add(menuItemZorderPlus);

            MenuItem menuItemZorderMinus = new MenuItem() { Header = "Z-" };
            menuItemZorderMinus.Click += MenuItem_Click_ZorderMinus;
            image.ContextMenu.Items.Add(menuItemZorderMinus);
            // Свойства
            if (image.Name != null && image.Name.StartsWith("Image"))
            {
                MenuItem menuItemImageProperties = new MenuItem() { Header = "Свойства" };
                menuItemImageProperties.Click += MenuItem_Click_ImageProperties;
                image.ContextMenu.Items.Add(menuItemImageProperties);
            }
            if (image.Name != null && image.Name.StartsWith("BarcodeImage"))
            {
                // BarcodeFunction string
                MenuItem menuItemBarcode = new MenuItem() { Header = "Свойства штрих кода" };
                menuItemBarcode.Click += MenuItem_Click_ChangeBarcode;
                image.ContextMenu.Items.Add(menuItemBarcode);
            }
            if (image.Name != null && image.Name.StartsWith("QRCodeImage"))
            {
                // BarcodeFunction string
                MenuItem menuItemQRCode = new MenuItem() { Header = "Свойства QR кода" };
                menuItemQRCode.Click += MenuItem_Click_ChangeQRCodeProperties;
                image.ContextMenu.Items.Add(menuItemQRCode);
            }

            templateCanvas.Children.Add(image);
        }
        // Десериализация и загрузка макета
        Barcodes.Clear();
        foreach (var barcodeImageProperty in template.barcodeImage)
        {
            Barcodes[barcodeImageProperty.Name] = barcodeImageProperty;
            var image = templateCanvas.Children.OfType<Image>().FirstOrDefault(x => x.Name == barcodeImageProperty.Name);
            if (image == null)
            {
                continue;
            }
            BarcodeSetBinding(ref image);
            foreach (var bindingExp in image.BindingGroup.BindingExpressions)
            {
                bindingExp.UpdateTarget();
            }
        }
        QRCodes.Clear();
        foreach (var qrcodeImageProperty in template.qRCodeImages)
        {
            QRCodes[qrcodeImageProperty.Name] = qrcodeImageProperty;
            var image = templateCanvas.Children.OfType<Image>().FirstOrDefault(x => x.Name == qrcodeImageProperty.Name);
            if (image == null)
            {
                continue;
            }
            QRCodeSetBinding(ref image);
            foreach (var bindingExp in image.BindingGroup.BindingExpressions)
            {
                bindingExp.UpdateTarget();
            }
        }


        TextFieldProperties.Clear();
        foreach (var textFieldValue in template.textFieldValues)
        {
            TextFieldProperties[textFieldValue.Name] = textFieldValue;
            var textBox = templateCanvas.Children.OfType<TextBox>().FirstOrDefault(x => x.Name == textFieldValue.Name);
            if (textBox == null)
            {
                continue;
            }
            SetBinding(ref textBox);
            foreach (var bindingExp in textBox.BindingGroup.BindingExpressions)
            {
                bindingExp.UpdateTarget();
            }
        }
        Images.Clear();
        foreach (ImageProperties imageProperty in template.imageProperties)
        {
            Images[imageProperty.Name] = imageProperty;
            var image = templateCanvas.Children.OfType<Image>().FirstOrDefault(x => x.Name == imageProperty.Name);
            if (image == null)
            {
                continue;
            }
            ImageSetBinding(ref image);
            foreach (var bindingexp in image.BindingGroup.BindingExpressions)
            {
                bindingexp.UpdateTarget();
            }
        }

        RectangleFigures.Clear();
        foreach (RectangleFigureProperties rectangleProperty in template.rectangleFigures)
        {
            RectangleFigures[rectangleProperty.Name] = rectangleProperty;
            var rectangle = templateCanvas.Children.OfType<Rectangle>().FirstOrDefault(x => x.Name == rectangleProperty.Name);
            if (rectangle == null)
            {
                continue;
            }
            SetBindingRectangleProperties(ref rectangle);
            foreach (var bindingexp in rectangle.BindingGroup.BindingExpressions)
            {
                bindingexp.UpdateTarget();
            }
        }

        Lines.Clear();
        foreach (LineProperties lineProperty in template.lineProperties)
        {
            Lines[lineProperty.Name] = lineProperty;
            var line = templateCanvas.Children.OfType<Line>().FirstOrDefault(x => x.Name.Equals(lineProperty.Name, StringComparison.OrdinalIgnoreCase));
            if (line == null)
            {
                continue;
            }
            SetBindingLineProperties(ref line);
            var group = line.BindingGroup;
            if (group != null)
            {
                foreach (var exp in group.BindingExpressions)
                {
                    exp.UpdateTarget();
                }
            }
        }


        Polygons.Clear();
        foreach (PolygonProperties polygonProperty in template.polygonProperties)
        {
            Polygons[polygonProperty.Name] = polygonProperty;
            var polygon = templateCanvas.Children.OfType<Polygon>().FirstOrDefault(x => x.Name.Equals(polygonProperty.Name, StringComparison.OrdinalIgnoreCase));
            if (polygon == null)
            {
                continue;
            }
            SetBindingPolygonProperties(ref polygon);
            foreach (var bindingExp in polygon.BindingGroup.BindingExpressions)
            {
                bindingExp.UpdateTarget();
            }
        }
        templateCanvas.EndInit();
    }

    private void MenuItem_Click_LoadTemplate(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "Text files (*.json)|*.json|All files (*.*)|*.*"; // Filter file types

        if (openFileDialog.ShowDialog() == true)
        {
            string jsonContent = File.ReadAllText(openFileDialog.FileName);
            templateCanvas.Children.Clear(); // Очистка текущего холста перед загрузкой нового макета
            var barcodeImageProperties = new List<BarcodeImageProperties>(); // Сохранение текущих свойств штрих-кодов;
            var textFieldValues = new List<TextFieldValue>();                // Сохранение текущих значений текстовых полей
            var qrCodeProperties = new List<QRCodeImageProperties>();        // Сохранение текущих значений qr кодов
            var imageProperties = new List<ImageProperties>();               // Сохранение текущих значений изображений
            var rectangleProperties = new List<RectangleFigureProperties>(); // Сохранение текущих значений прямоугольников
            var lineProperties = new List<LineProperties>();                 // Сохранение текущих значений линий
            var polygonProperties = new List<PolygonProperties>();             // Сохранение текущих значений полигонов

            CanvasSerializer.DeserializeCanvas(this, ref templateCanvas, jsonContent, ref barcodeImageProperties, ref textFieldValues
                ,ref qrCodeProperties
                ,ref imageProperties
                ,ref rectangleProperties
                ,ref lineProperties
                ,ref polygonProperties
                );                                                           // Десериализация и загрузка макета
            Barcodes.Clear();
            foreach (var barcodeImageProperty in barcodeImageProperties)
            {
                Barcodes[barcodeImageProperty.Name]=barcodeImageProperty;
                var image = templateCanvas.Children.OfType<Image>().FirstOrDefault(x => x.Name == barcodeImageProperty.Name);
                if (image == null)
                {
                    continue;
                }
                BarcodeSetBinding(ref image);
            }

            TextFieldProperties.Clear();
            foreach (var textFieldValue in textFieldValues)
            {
                TextFieldProperties[textFieldValue.Name] = textFieldValue;
                var textBox = templateCanvas.Children.OfType<TextBox>().FirstOrDefault(x => x.Name == textFieldValue.Name);
                if (textBox == null)
                {
                    continue;
                }
                SetBinding(ref textBox);
            }
            Images.Clear();
            foreach (ImageProperties imageProperty in imageProperties)
            {
                Images[imageProperty.Name]= imageProperty;
                var image = templateCanvas.Children.OfType<Image>().FirstOrDefault(x => x.Name == imageProperty.Name);
                if (image == null)
                {
                    continue;
                }
                ImageSetBinding(ref image);
            }
            RectangleFigures.Clear();
            foreach (RectangleFigureProperties rectangleProperty in rectangleProperties)
            {
                RectangleFigures[rectangleProperty.Name] = rectangleProperty;
                var rectangle = templateCanvas.Children.OfType<Rectangle>().FirstOrDefault(x => x.Name == rectangleProperty.Name);
                if (rectangle == null)
                {
                    continue;
                }
                SetBindingRectangleProperties(ref rectangle);
            }
            Lines.Clear();
            foreach (LineProperties lineProperty in lineProperties)
            {
                Lines[lineProperty.Name] = lineProperty;
                var line = templateCanvas.Children.OfType<Line>().FirstOrDefault(x => x.Name == lineProperty.Name);
                if (line == null)
                {
                    continue;
                }
                SetBindingLineProperties(ref line);
            }
            Polygons.Clear();
            foreach (PolygonProperties polygonProperty in polygonProperties)
            {
                Polygons[polygonProperty.Name] = polygonProperty;
                var polygon = templateCanvas.Children.OfType<Polygon>().FirstOrDefault(x => x.Name == polygonProperty.Name);
                if (polygon == null)
                {
                    continue;
                }
                SetBindingPolygonProperties(ref polygon);
            }

        }
    }
    void GetStringPageFunction_Returned(object sender, ReturnEventArgs<string> e)
    {
        var k = templateCanvas.Children.OfType<Image>().Count() + 1;
        if (e.Result == null)
        {
            return;
        }
        var barcodeImageProperty = JsonHelper.Deserialize<BarcodeImageProperties>(e.Result as string);
        barcodeImageProperty.Name = "BarcodeImage_" + k.ToString();
        Barcodes[barcodeImageProperty.Name] = barcodeImageProperty;

        Image img = new Image();
        img.Name = barcodeImageProperty.Name;
        img.AllowDrop = true;
        img.Stretch = Stretch.Fill;
        img.HorizontalAlignment = HorizontalAlignment.Center;
        img.VerticalAlignment = VerticalAlignment.Center;
        img.ContextMenu = new ContextMenu();
        img.ContextMenu.FontSize = 12;



        //Удалить
        MenuItem menuItem = new MenuItem() { Header = "Удалить" };
        menuItem.Click += MenuItem_Click_Remove;
        img.ContextMenu.Items.Add(menuItem);
        //Z order
        MenuItem menuItemZorderPlus = new MenuItem() { Header = "Z+" };
        menuItemZorderPlus.Click += MenuItem_Click_ZorderPlus;
        img.ContextMenu.Items.Add(menuItemZorderPlus);
        MenuItem menuItemZorderMinus = new MenuItem() { Header = "Z-" };
        menuItemZorderMinus.Click += MenuItem_Click_ZorderMinus;
        img.ContextMenu.Items.Add(menuItemZorderMinus);
        // BarcodeFunction string
        MenuItem menuItemBarcode = new MenuItem() { Header = "Свойства штрих кода" };
            menuItemBarcode.Click += MenuItem_Click_ChangeBarcode;
            img.ContextMenu.Items.Add(menuItemBarcode);
        var i = templateCanvas.Children.Add(img);
        BarcodeSetBinding(ref img);
        var group = img.BindingGroup;
        foreach (var bindingExp in group.BindingExpressions)
        {
            bindingExp.UpdateTarget();
        }
        Report[SelectedSection].barcodeImage.Add(barcodeImageProperty);
        Report[SelectedSection].imageCanvasElements.Add(
            new ImageCanvasElement()
            {
                Name = barcodeImageProperty.Name,
                Type = img.GetType().Name,
                Left = Canvas.GetLeft(img),
                Top = Canvas.GetTop(img),
                Width = img.Width,
                Height = img.Height,
                HorizontalAlignment = JsonHelper.Serialize<System.Windows.HorizontalAlignment>(img.HorizontalAlignment),
                VerticalAlignment = JsonHelper.Serialize<System.Windows.VerticalAlignment>(img.VerticalAlignment),
                Opacity = img.Opacity,
                Stretch = JsonHelper.Serialize<Stretch>(img.Stretch),
                ImageSourceString = img.Source is BitmapSource bitmapSource ? ImageCanvasElement.ImageToBase64(bitmapSource) : string.Empty,
            }
        );

    }

    private void MenuItem_Click_AddBarCode(object sender, RoutedEventArgs e)
    {
        BarcodeFunction pageFunction = new BarcodeFunction();
        pageFunction.Return += new ReturnEventHandler<string>(GetStringPageFunction_Returned);
        this.NavigationService.Navigate(pageFunction);
    }

    private void MenuItem_Click_AddImage(object sender, RoutedEventArgs e)
    {
        EditImageFunction  pageFunction = new EditImageFunction();
        pageFunction.Return += new ReturnEventHandler<string>(GetImageFromPageFunction_Returned);
        this.NavigationService.Navigate(pageFunction);
    }

    private void GetImageFromPageFunction_Returned(object sender, ReturnEventArgs<string> e)
    {
        if (e==null || string.IsNullOrWhiteSpace(e.Result))
        {
            return;
        }
        var imageProperty = JsonHelper.Deserialize<ImageProperties>(e.Result as string);
        var k = templateCanvas.Children.OfType<Image>().Count() + 1;
        imageProperty.Name = "Image_" + k.ToString();
        Images[imageProperty.Name] = imageProperty;
        Image img = new Image();
        img.Name = imageProperty.Name;
        ImageSetBinding(ref img);
        img.AllowDrop = true;
        img.Stretch = Stretch.Fill;
        img.HorizontalAlignment = HorizontalAlignment.Center;
        img.VerticalAlignment = VerticalAlignment.Center;
        img.ContextMenu = new ContextMenu();
        img.ContextMenu.FontSize = 12;
        img.Opacity = 1.0;
        //Удалить
        MenuItem menuItem = new MenuItem() { Header = "Удалить" };
        menuItem.Click += MenuItem_Click_Remove;
        img.ContextMenu.Items.Add(menuItem);
        //Zorder
        MenuItem menuItemZorderPlus = new MenuItem() { Header = "Z+" };
        menuItemZorderPlus.Click += MenuItem_Click_ZorderPlus;
        img.ContextMenu.Items.Add(menuItemZorderPlus);
        MenuItem menuItemZorderMinus = new MenuItem() { Header = "Z-" };
        menuItemZorderMinus.Click += MenuItem_Click_ZorderMinus;
        img.ContextMenu.Items.Add(menuItemZorderMinus);
        // Свойства
        MenuItem menuItemImageProperties = new MenuItem() { Header = "Свойства" };
        menuItemImageProperties.Click += MenuItem_Click_ImageProperties;
        img.ContextMenu.Items.Add(menuItemImageProperties);
        var i = templateCanvas.Children.Add(img);
        Canvas.SetLeft(img, imageProperty.Left);
        Canvas.SetTop(img, imageProperty.Top);
        ImageSetBinding(ref img);
        foreach (var bindingExp in img.BindingGroup.BindingExpressions)
        {
            bindingExp.UpdateTarget();
        }

        Report[SelectedSection].imageProperties.Add(imageProperty);
        Report[SelectedSection].imageCanvasElements.Add(
            new ImageCanvasElement()
            {
                Name= imageProperty.Name,
                Type = img.GetType().Name,
                Left = Canvas.GetLeft(img),
                Top = Canvas.GetTop(img),
                Width = img.Width,
                Height = img.Height,
                HorizontalAlignment = JsonHelper.Serialize<System.Windows.HorizontalAlignment>(img.HorizontalAlignment),
                VerticalAlignment = JsonHelper.Serialize<System.Windows.VerticalAlignment>(img.VerticalAlignment),
                Opacity = img.Opacity,
                Stretch = JsonHelper.Serialize<Stretch>(img.Stretch),
                ImageSourceString = img.Source is BitmapSource bitmapSource ? ImageCanvasElement.ImageToBase64(bitmapSource) : string.Empty,
            }
        );
    }
    private void StackPanel_Loaded(object sender, RoutedEventArgs e)
    {
        var printer = Properties.Settings.Default.barcodePrinter;
        if (string.IsNullOrWhiteSpace(printer))
        {
            return;
        }
        var printQueue = PagePrint.GetPrintQueue(printer);
        var ticket = PagePrint.GetPrintTicket(printQueue);
        PrintSettings printSettings = null;
        if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.BarcodePrintSettings))
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(Properties.Settings.Default.BarcodePrintSettings)))
            {
                printSettings = new PrintSettings(ms);

                var barcodePrintTicket = PagePrint.CreateDeltaPrintTicket(printSettings);
                if (barcodePrintTicket.PageMediaSize != null)
                {
                    var result = printQueue.MergeAndValidatePrintTicket(ticket, barcodePrintTicket);
                    ticket = result.ValidatedPrintTicket;
                    ticket.PageOrientation = barcodePrintTicket.PageOrientation;
                }
                else
                {
                    var printCapabilities = printQueue.GetPrintCapabilities(ticket);
                    ticket.PageMediaSize = new PageMediaSize(printCapabilities.PageImageableArea.ExtentWidth, printCapabilities.PageImageableArea.ExtentHeight);
                }

            }
        }


        if (ticket.PageOrientation == PageOrientation.Landscape || ticket.PageOrientation == PageOrientation.ReverseLandscape)
        {
            templateCanvasBorder.SetValue(HeightProperty, ticket.PageMediaSize.Width);
            templateCanvasBorder.SetValue(WidthProperty, ticket.PageMediaSize.Height);
            PageSize = new Size((double)ticket.PageMediaSize.Height, (double)ticket.PageMediaSize.Width);
        }
        else
        {
            templateCanvasBorder.SetValue(WidthProperty, ticket.PageMediaSize.Width);
            templateCanvasBorder.SetValue(HeightProperty, ticket.PageMediaSize.Height);
            PageSize = new Size((double)ticket.PageMediaSize.Width, (double)ticket.PageMediaSize.Height);
        }
    }
    

    private void MenuItem_Click_AddTextBlock(object sender, RoutedEventArgs e)
    {
        var json = JsonHelper.Serialize<TextFieldValue>(new TextFieldValue()
        {
            Name = String.Empty,
            Value = String.Empty,
            Width = 200,
            Height = 50,
            FontSize = FontSize,
            FontFamily = (FontFamily ?? new FontFamily("Arial")).Source,
            FontStyle = FontStyle,
            FontWeight = FontWeight,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            Foreground = ForegroundColor,
            Background = BackgroundColor,
            Left = 0,
            Top = 0,
            TextAlignment = TextAlignment.Left,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        });
        var pageFunction = new ChangeTextFieldProperties( json);
        pageFunction.Return += new ReturnEventHandler<string>(GetNewTextFieldFromPageFunction_Returned);
        this.NavigationService.Navigate(pageFunction);
    }

    private void GetNewTextFieldFromPageFunction_Returned(object sender, ReturnEventArgs<string> e)
    {
        var result = e.Result as string;
        if (string.IsNullOrEmpty(result) || string.IsNullOrWhiteSpace(result))
        {
            return;
        }
        var textFieldProperties = JsonHelper.Deserialize<TextFieldValue>(result);
        if (textFieldProperties == null)
        {
            return;
        }
        int k = templateCanvas.Children.OfType<TextBox>().Count();
        var xKey = "TextBox" + k.ToString();
        textFieldProperties.Name = xKey;
        TextFieldProperties[xKey] = textFieldProperties;
        TextBox textBox = new TextBox
        {
            Name = xKey,
            Text = textFieldProperties.Value,
            Width = textFieldProperties.Width,
            Height = textFieldProperties.Height,
            TextWrapping = textFieldProperties.TextWrapping,
            AcceptsReturn = textFieldProperties.AcceptsReturn,
            FontSize = textFieldProperties.FontSize,
            FontFamily = new FontFamily(string.IsNullOrWhiteSpace(textFieldProperties.FontFamily)?"Arial":textFieldProperties.FontFamily),
            FontStyle = textFieldProperties.FontStyle,
            FontWeight = textFieldProperties.FontWeight,
            Foreground = new SolidColorBrush(textFieldProperties.Foreground),
            Background = new SolidColorBrush(textFieldProperties.Background),
            TextAlignment = textFieldProperties.TextAlignment,
            HorizontalAlignment = textFieldProperties.HorizontalAlignment,
            VerticalAlignment = textFieldProperties.VerticalAlignment,
        };
        //Удалить
        textBox.ContextMenu = new ContextMenu();
        textBox.ContextMenu.FontSize = 12;
        MenuItem menuItem = new MenuItem() { Header = "Удалить" };
        menuItem.Click += MenuItem_Click_Remove;
        textBox.ContextMenu.Items.Add(menuItem);
        //Zorder
        MenuItem menuItemZorderPlus = new MenuItem() { Header = "Z+" };
        menuItemZorderPlus.Click += MenuItem_Click_ZorderPlus;
        textBox.ContextMenu.Items.Add(menuItemZorderPlus);
        MenuItem menuItemZorderMinus = new MenuItem() { Header = "Z-" };
        menuItemZorderMinus.Click += MenuItem_Click_ZorderMinus;
        textBox.ContextMenu.Items.Add(menuItemZorderMinus);

        // Свойства
        MenuItem menuItemNavigate = new MenuItem() { Header = "Свойства" };
        menuItemNavigate.Click += OnNavigateButtonClick;
        textBox.ContextMenu.Items.Add(menuItemNavigate);

        templateCanvas.Children.Add(textBox);
        SetBinding(ref textBox);
        var group = textBox.BindingGroup;
        foreach (var bindingExp in group.BindingExpressions)
        {
            bindingExp.UpdateTarget();
        }
        Report[SelectedSection].textFieldValues.Add(textFieldProperties);
        Report[SelectedSection].textBoxCanvasElements.Add(new TextBoxCanvasElement()
        {
            Name = textBox.Name,
            Type = "TextBox",
            Left = textFieldProperties.Left,
            Top = textFieldProperties.Top,
            Width = textFieldProperties.Width,
            Height = textFieldProperties.Height,
            FontFamily = textFieldProperties.FontFamily,
            FontSize = textFieldProperties.FontSize,
            FontStyle = JsonHelper.Serialize(textFieldProperties.FontStyle),
            FontWeight = JsonHelper.Serialize(textFieldProperties.FontWeight),
            Foreground = JsonHelper.Serialize(textFieldProperties.Foreground),
            Background = JsonHelper.Serialize(textFieldProperties.Background),
            TextWrapping = JsonHelper.Serialize(textFieldProperties.TextWrapping),
            AcceptsReturn = textFieldProperties.AcceptsReturn,
            Text = textFieldProperties.Value,
            HorizontalAlignment = JsonHelper.Serialize<HorizontalAlignment>(textFieldProperties.HorizontalAlignment),
            VerticalAlignment = JsonHelper.Serialize<VerticalAlignment>(textFieldProperties.VerticalAlignment),
        });

    }
    private void MenuItem_Click_PrinterFonts(object sender, RoutedEventArgs e)
    {
        var navigationWindow = new NavigationWindow();
        var printerFontsPage = new PrinterFontEnumerator();
        navigationWindow.Content = printerFontsPage;
        navigationWindow.Activate();
        var result = navigationWindow.ShowDialog();
        if (result.HasValue && result.Value)
        {
        }
        else
        {

        }
    }

    private void FontSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
    }

    public void OnNavigateButtonClick(object sender, RoutedEventArgs e)
    {

        MenuItem menuItem = sender as MenuItem;
        if (menuItem != null)
        {
            ContextMenu contextMenu = menuItem.Parent as ContextMenu;
            if (contextMenu != null)
            {
                if (contextMenu.PlacementTarget != null)
                {
                    var owner = contextMenu.PlacementTarget as TextBox;
                    var jsonString = JsonHelper.Serialize<TextFieldValue>(TextFieldProperties[owner.Name]);
                    var textFieldPropertiesEditFunction = new ChangeTextFieldProperties(jsonString);
                    textFieldPropertiesEditFunction.Return += new ReturnEventHandler<string>(GetTextFieldPropertiesEditFunction_Returned);
                    this.NavigationService.Navigate(textFieldPropertiesEditFunction);
                }

            }
        }
    }

    private void MenuItem_Click_4(object sender, RoutedEventArgs e)
    {
    }

    private void MenuItem_Click_NewReport(object sender, RoutedEventArgs e)
    {
        CreateReport pageFunction = new CreateReport(PageSize);
        pageFunction.Return += new ReturnEventHandler<string>(GetNewReport);
        this.NavigationService.Navigate(pageFunction);
    }

    private void GetNewReport(object sender, ReturnEventArgs<string> e)
    {
        if(e==null)
        {
            return;
        }
        var result = e.Result as string;
        if (string.IsNullOrEmpty(result))
        {
           return;
        }
        Report.Clear();
        var reportSections = JsonHelper.Deserialize<ReportSections>(result);
        ReportSections.Clear();
        foreach (var section in reportSections.Sections)
        {
            ReportSections.Add(section);
            Report[section]= new TemplateRecord();
        }
        SelectedSection = reportSections.CrystalReportSection;


    }
    private void MenuItem_Click_SaveReport(object sender, RoutedEventArgs e)
    {
    }

    private void MenuItem_Click_LoadReport(object sender, RoutedEventArgs e)
    {
    }


    private void Button_Click_Open(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "Text files  (*.rpt)| *.json|All files (*.*)|*.*"; // Filter file types

        if (openFileDialog.ShowDialog() == true)
        {
            Log.Information($"Открытие файла: {openFileDialog.FileName}");
            var jsonContent = File.ReadAllText(openFileDialog.FileName);
            Report.Clear();
            ReportSections.Clear();
            Lines.Clear();
            RectangleFigures.Clear();
            TextFieldProperties.Clear();
            Barcodes.Clear();
            QRCodes.Clear();
            Images.Clear();
            Polygons.Clear();
            templateCanvas.Children.Clear(); // Очистка текущего холста перед загрузкой нового макета
            Report = JsonHelper.Deserialize<Dictionary<CrystalReportSection, TemplateRecord>>(jsonContent);
            ReportSections = new ObservableCollection<CrystalReportSection>();
            foreach (var section in Report.Keys)
            {
                ReportSections.Add(section);
            }
            var templateRecord = Report[ReportSections.First()];
            LoadTemplate(ref templateRecord);
            SelectedSection = ReportSections.First();
        }

    }

    private void GetTextFieldPropertiesEditFunction_Returned(object sender, ReturnEventArgs<string> e)
    { 
        if (e == null)
        {
            return;
        }
        var result = e.Result as string;
        if (string.IsNullOrEmpty(result))
        {
            return;
        }
        var textFieldProperties = JsonHelper.Deserialize<TextFieldValue>(result);
        if (textFieldProperties == null)
        {
            return;
        }
        TextFieldProperties[textFieldProperties.Name] = textFieldProperties;
        var owner = templateCanvas.Children.OfType<TextBox>().FirstOrDefault(x => x.Name == textFieldProperties.Name);
        if (owner != null)
        {
            ClearAllChildBindings(owner);
            SetBinding(ref owner);
            foreach (var bindingExp in owner.BindingGroup.BindingExpressions)
            {
                bindingExp.UpdateTarget();
            }
            var textBoxCanvaselement = new TextBoxCanvasElement()
            {
                Name = owner.Name ?? string.Empty,
                Type = owner.GetType().Name,
                Text = owner.Text,
                Left = textFieldProperties.Left,
                Top = textFieldProperties.Top,
                Width = textFieldProperties.Width,
                Height = textFieldProperties.Height,
                Background = JsonHelper.Serialize(textFieldProperties.Background),
                Foreground = JsonHelper.Serialize(textFieldProperties.Foreground),
                FontFamily = JsonHelper.Serialize(textFieldProperties.FontFamily),
                FontSize = owner.FontSize,
                FontStyle = JsonHelper.Serialize(textFieldProperties.FontStyle),
                FontWeight = JsonHelper.Serialize(textFieldProperties.FontWeight),
                Angle = textFieldProperties.Angle,
                TextWrapping = JsonHelper.Serialize(textFieldProperties.TextWrapping),
                HorizontalAlignment = JsonHelper.Serialize<System.Windows.HorizontalAlignment>(textFieldProperties.HorizontalAlignment),
                VerticalAlignment = JsonHelper.Serialize<System.Windows.VerticalAlignment>(textFieldProperties.VerticalAlignment),
                AcceptsReturn = textFieldProperties.AcceptsReturn
            };
            Report[SelectedSection].textFieldValues.RemoveAll(tb => tb.Name.Equals(owner.Name, StringComparison.OrdinalIgnoreCase));
            Report[SelectedSection].textFieldValues.Add(textFieldProperties);
            Report[SelectedSection].textBoxCanvasElements.RemoveAll(tb => tb.Name.Equals(owner.Name, StringComparison.OrdinalIgnoreCase));
            Report[SelectedSection].textBoxCanvasElements.Add(textBoxCanvaselement);
        }
    }

    private void MenuItem_Click_ForcedPageEject(object sender, RoutedEventArgs e)
    {
        templateCanvas.Measure(new Size(_PageSize.Width, _PageSize.Height));
        templateCanvas.Arrange(new Rect(new System.Windows.Point(), new Size(_PageSize.Width, _PageSize.Height)));
        templateCanvas.UpdateLayout();

        double maxY = 0;
        foreach (var child in templateCanvas.Children.OfType<UIElement>())
        {
            double childBottom = Canvas.GetTop(child) + (child is FrameworkElement fe ? fe.ActualHeight : 0);
            if (childBottom > maxY)
            {
                maxY = childBottom;
            }
        }
        Polyline polyline = new Polyline();

        polyline.Name = "PageBreak_" + (templateCanvas.Children.OfType<Polyline>().Count() + 1).ToString();
        polyline.ContextMenu = new ContextMenu();
        polyline.Fill = Brushes.Transparent;
        polyline.Stroke = Brushes.Red;
        polyline.StrokeThickness = 2;
        polyline.StrokeDashArray = new DoubleCollection() { 4, 2 };
        polyline.Points = new PointCollection()
        {
            new Point(0, maxY),
            new Point(PageSize.Width, maxY)
        };
        //Удалить
        MenuItem menuItem = new MenuItem() { Header = "Удалить" };
        menuItem.Click += MenuItem_Click_Remove;
        polyline.ContextMenu.Items.Add(menuItem);
        templateCanvas.Children.Add(polyline);
        
        Report[SelectedSection].elements.Add(new CanvasElement()
        {
            Name = polyline.Name,
            Type = "PolyLine",
            Stroke = JsonHelper.Serialize<Brush>(polyline.Stroke),
            Fill = JsonHelper.Serialize<Brush>(polyline.Fill),
            StrokeThickness = polyline.StrokeThickness,
            Points = polyline.Points.Select(p => new Point() { X = p.X, Y = p.Y }).ToArray(),
            StrokeDashArray = polyline.StrokeDashArray != null ? polyline.StrokeDashArray.ToArray() : null,
            Opacity = polyline.Opacity,
        });

    }

    internal void MenuItem_Click_ChangeBarcode(object sender, RoutedEventArgs e)
    {
        MenuItem menuItem = sender as MenuItem;
        if (menuItem != null)
        {
            ContextMenu contextMenu = menuItem.Parent as ContextMenu;
            if (contextMenu != null)
            {
                if (contextMenu.PlacementTarget != null)
                {
                    Image owner = contextMenu.PlacementTarget as Image;
                    if (!Barcodes.ContainsKey(owner.Name))
                    {
                        return;
                    }
                    var json = JsonHelper.Serialize<BarcodeImageProperties>(Barcodes[owner.Name]);
                    var pageFunction = new BarcodeFunction(json);
                    pageFunction.Return += new ReturnEventHandler<string>(GetBarcodePageFunction_Returned);
                    this.NavigationService.Navigate(pageFunction);

                }

            }
        }
    }

    private void GetBarcodePageFunction_Returned(object sender, ReturnEventArgs<string> e)
    {
        if (e==null || e.Result == null)
        {
            return;
        }
        var barcodeImageProperty = JsonHelper.Deserialize<BarcodeImageProperties>(e.Result as string);
        Barcodes[barcodeImageProperty.Name]= barcodeImageProperty;
        var img = templateCanvas.Children.OfType<Image>().FirstOrDefault(x => x.Name == barcodeImageProperty.Name);
        if (img != null)
        {
            BarcodeSetBinding(ref img);
            var group = img.BindingGroup;
            foreach (var bindingExp in group.BindingExpressions)
            {
                bindingExp.UpdateTarget();
            }
            Report[SelectedSection].barcodeImage.RemoveAll(bc => bc.Name.Equals(barcodeImageProperty.Name, StringComparison.OrdinalIgnoreCase));
            Report[SelectedSection].barcodeImage.Add(barcodeImageProperty);
            Report[SelectedSection].imageCanvasElements.RemoveAll(ic => ic.Name.Equals(barcodeImageProperty.Name, StringComparison.OrdinalIgnoreCase));
            Report[SelectedSection].imageCanvasElements.Add(
                new ImageCanvasElement()
                {
                    Name = barcodeImageProperty.Name,
                    Type = img.GetType().Name,
                    Left = Canvas.GetLeft(img),
                    Top = Canvas.GetTop(img),
                    Width = img.Width,
                    Height = img.Height,
                    HorizontalAlignment = JsonHelper.Serialize<System.Windows.HorizontalAlignment>(img.HorizontalAlignment),
                    VerticalAlignment = JsonHelper.Serialize<System.Windows.VerticalAlignment>(img.VerticalAlignment),
                    Opacity = img.Opacity,
                    Stretch = JsonHelper.Serialize<Stretch>(img.Stretch),
                    ImageSourceString = img.Source is BitmapSource bitmapSource ? ImageCanvasElement.ImageToBase64(bitmapSource) : string.Empty,
                }
            );

        }
    }

    internal void MenuItem_Click_Rectangle(object sender, RoutedEventArgs e) => throw new NotImplementedException();
}

