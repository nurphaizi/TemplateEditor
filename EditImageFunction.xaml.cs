using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
using Serilog;

namespace TemplateEdit;
/// <summary>
/// Interaction logic for EditImageFunction.xaml
/// </summary>
public partial class EditImageFunction : PageFunction<String>, INotifyPropertyChanged

{
    public EditImageFunction()
    {

        InitializeComponent();
        ImageProperties = new ImageProperties();
        functionTitle.Content = "Создание поля с изображением";
        GetColumnNamesOfAllQuerries();
        this.DataContext = this;
    }
    public EditImageFunction(string jsonString)
    {
        InitializeComponent();
        functionTitle.Content = "Редактирование свойств поля с изображением";
        if (string.IsNullOrWhiteSpace(jsonString))
        {
            ImageProperties = new ImageProperties();
        }
        else
        {
            ImageProperties = JsonHelper.Deserialize<ImageProperties>(jsonString);
        }
        ImagePath = ImageProperties.ImagePath;
        ImageLeft = ImageProperties.Left;
        ImageTop = ImageProperties.Top;
        ImageWidth = ImageProperties.Width;
        ImageHeight = ImageProperties.Height;
        ImageAngle = ImageProperties.Angle;
        DataSourceType = ImageProperties.DataSourceType;
        GetColumnNamesOfAllQuerries();
        if (AvailableSources != null && !String.IsNullOrWhiteSpace(ImageProperties.DataSourceName))
        {
            DataSourceName = AvailableSources.FirstOrDefault(s => s.Key == ImageProperties.DataSourceName)!;
        }
        DataSourceFieldType = ImageProperties.DataSourceFieldType;
        ColumnName = new KeyValueItem
        {
            Key = ImageProperties.DataFieldName,
            SourceType = ImageProperties.DataSourceType,
            Value = ImageProperties.DataFieldName,
            FieldType = ImageProperties.DataSourceFieldType,
            FieldTypeNet = FieldTypeConverter.GetNetType(ImageProperties.DataSourceFieldType)
        };

        int index = -1;
        for (int i = 0; i < ColumnNames.Count; i++)
        {
            if (ColumnNames[i].Key == ImageProperties.DataFieldName)
            {
                index = i;
                break;
            }
        }
        dataFieldName.SelectedIndex = index;
   
        DataContext = this;
    }


    public event PropertyChangedEventHandler PropertyChanged;
    protected void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private BitmapImage _ImageData;
    public BitmapImage ImageData
    {
        get => (BitmapImage)_ImageData;
        set
        {
            _ImageData = value;
            NotifyPropertyChanged(nameof(ImageData));
        }
    }


    private ImageProperties _ImageProperties;

    public ImageProperties ImageProperties
    {
        get => (ImageProperties)_ImageProperties;
        set
        {
            _ImageProperties = value;
            NotifyPropertyChanged(nameof(ImageProperties));
        }
    }

    private string _ImagePath;
    public string ImagePath
    {
        get => (string)_ImagePath;
        set
        {
            _ImagePath = value;
            NotifyPropertyChanged(nameof(ImagePath));
        }
    }

    private double _ImageLeft;
    public double ImageLeft
    {
        get => (double)_ImageLeft;
        set
        {
            _ImageLeft = value;
            NotifyPropertyChanged(nameof(ImageLeft));
        }
    }

    private double _ImageTop;
    public double ImageTop
    {
        get => (double)_ImageTop;
        set
        {
            _ImageTop = value;
            NotifyPropertyChanged(nameof(ImageTop));
        }
    }

    private double _ImageWidth;
    public double ImageWidth
    {
        get => (double)_ImageWidth;
        set
        {
            _ImageWidth = value;
            NotifyPropertyChanged(nameof(ImageWidth));
        }
    }

    private double _ImageHeight;
    public double ImageHeight
    {
        get => (double)_ImageHeight;
        set
        {
            _ImageHeight = value;
            NotifyPropertyChanged(nameof(ImageHeight));
        }
    }

    private double _ImageAngle;
    public double ImageAngle
    {
        get => (double)_ImageAngle;
        set
        {
            _ImageAngle = value;
            NotifyPropertyChanged(nameof(ImageAngle));
        }
    }
    // Источники данных
    private ObservableCollection<KeyValueItem> _AvailableSources = [];
    public ObservableCollection<KeyValueItem> AvailableSources
    {
        get => (ObservableCollection<KeyValueItem>)_AvailableSources;
        set
        {
            _AvailableSources = value;
            NotifyPropertyChanged(nameof(AvailableSources));
        }
    }
    private DataView _DataSource;
    public DataView DataSource
    {
        get => (DataView)_DataSource;
        set
        {
            _DataSource = value;
            NotifyPropertyChanged(nameof(DataSource));
        }
    }

    private Dictionary<string, List<KeyValueItem>> _ColumnNamesListOfQueries = [];
    public Dictionary<string, List<KeyValueItem>> ColumnNamesListOfQueries
    {
        get => (Dictionary<string, List<KeyValueItem>>)_ColumnNamesListOfQueries;
        set
        {
            _ColumnNamesListOfQueries = value;
            NotifyPropertyChanged(nameof(ColumnNamesListOfQueries));
        }
    }

    private ObservableCollection<KeyValueItem> _ColumnNames = [];
    public ObservableCollection<KeyValueItem> ColumnNames
    {
        get => (ObservableCollection<KeyValueItem>)_ColumnNames;
        set
        {
            _ColumnNames = value;
            NotifyPropertyChanged(nameof(ColumnNames));
        }
    }


    private KeyValueItem _ColumnName;

    private void OnColumnNameChanged()
    {
            switch (DataSourceType)
            {
                case DataSourceType.Database:
                    var dataTable = ReportData();
                    if (dataTable != null&& ColumnName!=null && dataTable.Columns.Contains(ColumnName?.Key))
                    {
                        var columnType = dataTable.Columns[ColumnName.Key].DataType;
                        DataSourceFieldType = FieldTypeConverter.GetFieldType(columnType);
                        dataTable.DefaultView.RowFilter = $"[{ColumnName.Key}] IS NOT NULL";
                        DataSource = dataTable.DefaultView;
                        AutoGenerateColumns(columnDataList);
                    }
                    else
                    {
                        DataSourceFieldType = FieldTypes.None;
                    }
                    break;
                case DataSourceType.File:
                    break;
                default:
                    break;
            }
    }

    public KeyValueItem ColumnName
    {
        get => (KeyValueItem)_ColumnName;
        set
        {
            _ColumnName = value;
            NotifyPropertyChanged(nameof(ColumnName));
            OnColumnNameChanged();
        }
    }

    private DataSourceType _DataSourceType = DataSourceType.None;

    private void OnDataSourceTypeChanged()
    {
        // 1. Call an method to handle logic
        ColumnNames = null;
        DataSourceName = null;
        ColumnName = null;
    }
    public DataSourceType DataSourceType
    {
        get => (DataSourceType)_DataSourceType;
        set
        {
            _DataSourceType = value;
            NotifyPropertyChanged(nameof(DataSourceType));
            OnDataSourceTypeChanged();
        }
    }


    private KeyValueItem _DataSourceName;

    private void OnDataSourceNameChanged()
    {
            ColumnNames=null;
            ColumnName = null;
            if (DataSourceType == DataSourceType.Database && DataSourceName != null && !string.IsNullOrWhiteSpace(DataSourceName.Key))
            {
                dataSourceFieldType.IsEnabled = false;
                ColumnNames = new();
                foreach (var columnName in ColumnNamesListOfQueries)
                {
                    if (columnName.Key == DataSourceName.Key)
                    {
                        foreach (var col in columnName.Value)
                        {
                            ColumnNames.Add(new KeyValueItem() { Key = col.Key, Value = col.Value, SourceType = col.SourceType, FieldTypeNet = col.FieldTypeNet, FieldType = col.FieldType });
                        }
                        break;
                    }
                }
            }

    }

    public KeyValueItem DataSourceName
    {
        get => (KeyValueItem)_DataSourceName;
        set
        {
            _DataSourceName= value;
            NotifyPropertyChanged(nameof(DataSourceName));
            OnDataSourceNameChanged();
        }
    }
    private FieldTypes _DataSourceFieldType;
    public FieldTypes DataSourceFieldType
    {
        get => (FieldTypes)_DataSourceFieldType;
        set
        {
            _DataSourceFieldType = value;
            NotifyPropertyChanged(nameof(DataSourceFieldType));
        }
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
        ColumnNamesListOfQueries = new();
        ColumnNames = new();
        DbClient();

        if (string.IsNullOrEmpty(Properties.Settings.Default.DataSources))
        {
            return;
        }
        AvailableSources = new();
        AvailableSources = JsonHelper.Deserialize<ObservableCollection<KeyValueItem>>(Properties.Settings.Default.DataSources);
        NotifyPropertyChanged(nameof(AvailableSources));
        if (AvailableSources.Count == 0)
        {
            return;
        }
        foreach (var querry in AvailableSources)
        {
            try
            {
                if (querry.Value == null || string.IsNullOrWhiteSpace(querry.Value))
                {
                    continue;
                }
                if (!querry.Value.Trim().ToLower().StartsWith("select"))
                {
                    continue;
                }
                var sqliteQuerryFields = new SqliteQuerryFields();
                var columnNames = new List<KeyValueItem>();
                var sqlText = querry.Value;
                var pos = sqlText.IndexOf("where", StringComparison.OrdinalIgnoreCase);
                if (pos > 0)
                {
                    sqlText = sqlText.Substring(0, pos).Trim();
                }
                sqliteQuerryFields.GetColumnNamesFromQuery(ConnectionString, sqlText).ForEach(f => columnNames.Add(new KeyValueItem() { Key = f.Name, SourceType = DataSourceType.Database, Value = f.Name, FieldTypeNet = f.type, FieldType = FieldTypeConverter.GetFieldType(f.type) }));
                ColumnNamesListOfQueries[querry.Key] = columnNames;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Ошибка получения колонок для запроса {querry.Key}");
            }
        }
    }
    
    public void AutoGenerateColumns(ListView listView)
    {
        VirtualizingPanel.SetIsVirtualizing(listView, true);
        VirtualizingPanel.SetVirtualizationMode(listView, VirtualizationMode.Recycling);
        ScrollViewer.SetCanContentScroll(listView, true);
        listView.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
        // img Column
        var imgColumn = new GridViewColumn
        {
            Header = ColumnName.Key,
            Width = 128
        };

        var factory = new FrameworkElementFactory(typeof(Image));
        factory.SetValue(Image.WidthProperty, 128.0);
        factory.SetValue(Image.HeightProperty, 64.0);

        var binding = new Binding(ColumnName.Key)
        {
            //Converter = new SvgBlobToImageConverter()
            Converter = new ByteArrayToBitmapImageConverter()
        };

        factory.SetBinding(Image.SourceProperty, binding);

        var template = new DataTemplate
        {
            VisualTree = factory
        };

        imgColumn.CellTemplate = template;


        var gridView = new GridView();
        {
            gridView.Columns.Add(imgColumn);
        }
        listView.View = gridView;
        listView.ItemsSource = DataSource;
    }
    
    public DataTable ReportData()
    {
        if (DataSourceType != DataSourceType.Database || DataSourceName == null || string.IsNullOrWhiteSpace(DataSourceName.Key))
        {
            return null;
        }


        var query = DataSourceName.Value;
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }
        var sqliteHelper = new SQLiteHelper(ConnectionString);
        return sqliteHelper.ExecuteQueryWithResults(query);
    }
    private void Button_Click_Cancel(object sender, RoutedEventArgs e)
    {
        OnReturn(new ReturnEventArgs<string>(string.Empty));
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        var vm = new FileOpenViewModel(new FileDialogService());
        vm.Filter = "Image files (*.png;*.jpg;*.jpeg;*.webp;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.webp;*.bmp;*.gif|All files (*.*)|*.*";
        if (!string.IsNullOrWhiteSpace(vm.SelectFileCommand()))
        {
            ImagePath = vm.FilePath;
            image.Source = new BitmapImage(new System.Uri(ImagePath));

        }

    }
    public static byte[] ImageSourceToByteArray(ImageSource imageSource)
    {
        var bitmapSource = imageSource as BitmapSource;
        if (bitmapSource == null)
            return null;


        if (bitmapSource.CanFreeze && !bitmapSource.IsFrozen)
            bitmapSource.Freeze();
        var encoder = new PngBitmapEncoder(); // You can use JpegBitmapEncoder or others
        encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

        using (var stream = new MemoryStream())
        {
            encoder.Save(stream);
            return stream.ToArray();
        }
    }

    public static byte[] BitmapImageToPngBytes(BitmapImage bitmapImage)
    {
        if (bitmapImage == null)
            return null;

        if (bitmapImage.CanFreeze && !bitmapImage.IsFrozen)
            bitmapImage.Freeze();

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmapImage));

        using (var stream = new MemoryStream())
        {
            encoder.Save(stream);
            return stream.ToArray();
        }
    }
    private async void Button_Click_SaveImage(object sender, RoutedEventArgs e)
    {
        ImageProperties.Left = ImageLeft;
        ImageProperties.Top = ImageTop;
        ImageProperties.Width = ImageWidth;
        ImageProperties.Height = ImageHeight;
        ImageProperties.Angle = ImageAngle;
        ImageProperties.ImagePath = ImagePath;
        ImageProperties.ImageSource = ImageSourceToByteArray(image.Source);
        ImageProperties.DataFieldName = ColumnName?.Value;
        ImageProperties.DataSourceFieldType = ColumnName == null ? FieldTypes.None : ColumnName.FieldType;
        ImageProperties.DataSourceType = DataSourceType;
        ImageProperties.DataSourceName = DataSourceName?.Key;
        var result = JsonHelper.Serialize<ImageProperties>(ImageProperties);
        OnReturn(new ReturnEventArgs<string>(result));
        return;
    }

    private async void columnDataList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var row = (DataRowView)columnDataList.SelectedItem;
     
        if (row != null && DataSourceType == DataSourceType.Database)
        {
            ImageProperties.DataSourceType = DataSourceType;
            ImageProperties.DataSourceName = DataSourceName?.Key;
            ImageProperties.DataFieldName = ColumnName?.Value;
            ImageProperties.DataSourceFieldType = ColumnName == null ? FieldTypes.None : ColumnName.FieldType; var loader = new ImagePipelineLoader();
            switch (DataSourceFieldType)
            {
                case FieldTypes.URL:
                    {
                        var url = row[ImageProperties.DataFieldName]?.ToString();
                        if (!string.IsNullOrEmpty(url))
                        {
                        }
                    }
                    break;
                case FieldTypes.Base64String:
                    {
                        var base64String = row[ImageProperties.DataFieldName]?.ToString();
                        if (!string.IsNullOrEmpty(base64String))
                        {
                            ImageProperties.ImageSource = Convert.FromBase64String(base64String);
                        }
                    }
                    break;
                case FieldTypes.ByteArray:
                    {
                        ImageProperties.ImageSource = row[ImageProperties.DataFieldName] as byte[];
                        ImageData = ByteArrayToBitmapImageConverter.ConvertByteArrayToBitMapImage(ImageProperties.ImageSource);
                    }
                    break;
                case FieldTypes.String:
                    {
                        ImageProperties.ImageSource = null;
                        var url = row[ImageProperties.DataFieldName]?.ToString();
                        if (!string.IsNullOrEmpty(url))
                        {
                            var resolvedUrl = InputResolver.Resolve(url);
                            if (resolvedUrl != null)
                            {
                                if (resolvedUrl.Kind == InputKind.ArbitraryText)
                                {
                                    //var bitmap = TextToBitmapConverter.ConvertTextToBitmap(resolvedUrl.Original,new System.Drawing.Font("Arial",12),
                                    //    System.Drawing.Color.Red,System.Drawing.Color.Transparent,0);

                                    var bitmap = BitmapExtensions.ErrorMessageBitmap((int)ImageProperties.Width, (int)ImageProperties.Height, resolvedUrl.Original);
                                    ImageProperties.ImageSource = TextToBitmapConverter.BitmapToBytes(bitmap);
                                    break;
                                }
                            }

                            try
                            {
                                ImageData = await loader.LoadForPrintAsync(url);
                                ImageProperties.ImageSource = ImagePipelineLoader.BitmapImageToBytes(ImageData);

                            }
                            catch (Exception ex)
                            {
                                // Log or handle the error
                            }


                        }
                    }
                    break;
                default:
                    Log.Information($"Code for default case{ImageProperties.DataSourceFieldType}");  // Code for default case
                    break;
            }

        }

    }

    
}