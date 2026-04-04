using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
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
using SixLabors.ImageSharp.PixelFormats;
using Svg;
using ZXing.QrCode.Internal;

namespace TemplateEdit;
/// <summary>
/// Interaction logic for QRCodePropertiesFunction.xaml
/// </summary>
public partial class QRCodePropertiesFunction : PageFunction<String>,INotifyPropertyChanged
{
    private string _QRCodeSource;
    public string QRCodeSource
    {
        get => (string)_QRCodeSource;
        set
        {
            _QRCodeSource = value;
            NotifyPropertyChanged(nameof(QRCodeSource));
        }
    }

    private QRCodeImageProperties _QRCodeImageProperties;
    public QRCodeImageProperties QRCodeImageProperties
    {
        get => (QRCodeImageProperties)_QRCodeImageProperties;
        set
        {
            _QRCodeImageProperties = value;
            NotifyPropertyChanged(nameof(QRCodeImageProperties));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        if (PropertyChanged != null)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    // Define data sources 


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

    public void AutoGenerateColumns(ListView listView)
    {
        VirtualizingPanel.SetIsVirtualizing(listView, true);
        VirtualizingPanel.SetVirtualizationMode(listView, VirtualizationMode.Recycling);
        ScrollViewer.SetCanContentScroll(listView, true);
        listView.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
        // SVG Column
        var svgColumn = new GridViewColumn
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
            Converter = new SvgBlobCachedConverter()
        };

        factory.SetBinding(Image.SourceProperty, binding);

        var template = new DataTemplate
        {
            VisualTree = factory
        };

        svgColumn.CellTemplate = template;


        var gridView = new GridView();
        {
            gridView.Columns.Add(svgColumn);
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
        var query = DataSourceName.Value+" LIMIT 100";
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }
        var sqliteHelper = new SQLiteHelper(ConnectionString);
        return sqliteHelper.ExecuteQueryWithResults(query);
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

    private KeyValueItem _ColumnName;
    private void OnColumnNameChanged()
    {
            switch (DataSourceType)
            {
                case DataSourceType.Database:
                    var dataTable = ReportData();
                    if (dataTable != null && ColumnName!=null && dataTable.Columns.Contains(ColumnName?.Key))
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
    
    private DataSourceType _DataSourceType = DataSourceType.None;
    private void OnDataSourceTypeChanged()
    {
            ColumnNames=null;
            ColumnName = null;
            DataSourceName = null;

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

            ColumnNames = null;
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
            _DataSourceName = value;
            NotifyPropertyChanged(nameof(DataSourceName));
            OnDataSourceNameChanged();
        }
    }
    
    // End data sources
    public QRCodePropertiesFunction()
    {
        InitializeComponent();
        QRCodeImageProperties = new();
        QRCodeImageProperties.Value = """
            <?xml version="1.0"?>
            <svg width="580" height="400"
                 viewBox="0 0 580 400"
                 xmlns="http://www.w3.org/2000/svg"
                 xmlns:xlink="http://www.w3.org/1999/xlink">
            <g transform="rotate(270) translate(-400 0)">
            <rect x="0" y="0" width="400" height="580" style="fill:white"/>
            <rect x="20" y="20" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="24" y="20" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="28" y="20" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="32" y="20" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="36" y="20" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="40" y="20" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="44" y="20" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="68" y="20" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="72" y="20" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="76" y="20" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="80" y="20" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="84" y="20" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="92" y="20" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="96" y="20" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="100" y="20" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="104" y="20" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="108" y="20" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="112" y="20" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="116" y="20" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="20" y="24" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="44" y="24" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="60" y="24" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="92" y="24" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="116" y="24" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="20" y="28" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="28" y="28" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="32" y="28" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="36" y="28" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="44" y="28" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="52" y="28" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="56" y="28" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="72" y="28" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="80" y="28" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="92" y="28" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="100" y="28" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="104" y="28" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="108" y="28" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="116" y="28" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="20" y="32" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="28" y="32" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="32" y="32" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="36" y="32" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="44" y="32" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="52" y="32" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="64" y="32" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="80" y="32" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="92" y="32" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="100" y="32" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="104" y="32" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="108" y="32" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="116" y="32" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="20" y="36" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="28" y="36" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="32" y="36" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="36" y="36" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="44" y="36" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="52" y="36" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="60" y="36" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="64" y="36" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="68" y="36" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="72" y="36" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="76" y="36" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="80" y="36" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="84" y="36" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="92" y="36" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="100" y="36" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="104" y="36" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="108" y="36" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="116" y="36" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="20" y="40" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="44" y="40" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="56" y="40" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="64" y="40" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="76" y="40" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="80" y="40" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="84" y="40" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="92" y="40" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="116" y="40" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="20" y="44" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="24" y="44" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="28" y="44" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="32" y="44" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="36" y="44" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="40" y="44" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="44" y="44" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="52" y="44" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="60" y="44" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="68" y="44" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="76" y="44" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="84" y="44" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="92" y="44" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="96" y="44" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="100" y="44" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="104" y="44" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="108" y="44" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="112" y="44" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="116" y="44" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="56" y="48" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="60" y="48" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="64" y="48" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="72" y="48" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="76" y="48" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="84" y="48" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="20" y="52" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="24" y="52" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="28" y="52" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="40" y="52" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="44" y="52" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="48" y="52" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="52" y="52" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="68" y="52" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="76" y="52" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="80" y="52" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="84" y="52" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="88" y="52" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="100" y="52" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="104" y="52" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="108" y="52" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="28" y="56" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="32" y="56" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="36" y="56" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="40" y="56" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="48" y="56" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="60" y="56" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="84" y="56" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="96" y="56" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="100" y="56" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="104" y="56" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="112" y="56" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="24" y="60" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="44" y="60" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="64" y="60" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="68" y="60" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="72" y="60" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="92" y="60" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="96" y="60" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="104" y="60" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="24" y="64" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="32" y="64" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="36" y="64" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="48" y="64" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="52" y="64" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="56" y="64" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="72" y="64" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="76" y="64" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="88" y="64" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="92" y="64" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="96" y="64" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="100" y="64" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="104" y="64" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="108" y="64" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="112" y="64" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="116" y="64" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="36" y="68" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="40" y="68" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="44" y="68" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="48" y="68" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="64" y="68" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="68" y="68" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="76" y="68" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="92" y="68" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="96" y="68" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="104" y="68" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="108" y="68" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="116" y="68" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="36" y="72" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="40" y="72" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="48" y="72" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="56" y="72" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="72" y="72" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="76" y="72" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="80" y="72" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="84" y="72" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="88" y="72" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="92" y="72" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="96" y="72" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="100" y="72" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="104" y="72" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="112" y="72" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="20" y="76" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="24" y="76" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="32" y="76" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="36" y="76" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="40" y="76" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="44" y="76" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="48" y="76" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="56" y="76" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="60" y="76" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="72" y="76" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="76" y="76" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="80" y="76" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="92" y="76" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="100" y="76" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="108" y="76" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="116" y="76" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="28" y="80" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="32" y="80" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="40" y="80" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="52" y="80" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="56" y="80" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="72" y="80" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="92" y="80" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="96" y="80" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="100" y="80" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="108" y="80" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="112" y="80" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="28" y="84" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="32" y="84" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="36" y="84" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="40" y="84" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="44" y="84" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="48" y="84" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="56" y="84" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="64" y="84" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="72" y="84" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="80" y="84" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="84" y="84" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="88" y="84" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="92" y="84" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="96" y="84" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="100" y="84" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="116" y="84" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="52" y="88" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="60" y="88" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="68" y="88" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="80" y="88" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="84" y="88" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="100" y="88" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="108" y="88" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="112" y="88" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="20" y="92" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="24" y="92" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="28" y="92" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="32" y="92" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="36" y="92" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="40" y="92" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="44" y="92" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="52" y="92" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="56" y="92" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="72" y="92" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="76" y="92" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="80" y="92" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="84" y="92" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="92" y="92" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="100" y="92" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="104" y="92" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="108" y="92" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="20" y="96" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="44" y="96" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="52" y="96" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="68" y="96" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="72" y="96" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="76" y="96" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="80" y="96" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="84" y="96" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="100" y="96" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="108" y="96" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="116" y="96" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="20" y="100" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="28" y="100" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="32" y="100" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="36" y="100" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="44" y="100" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="60" y="100" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="80" y="100" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="84" y="100" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="88" y="100" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="92" y="100" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="96" y="100" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="100" y="100" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="104" y="100" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="112" y="100" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="116" y="100" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="20" y="104" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="28" y="104" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="32" y="104" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="36" y="104" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="44" y="104" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="72" y="104" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="84" y="104" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="88" y="104" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="92" y="104" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="100" y="104" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="104" y="104" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="108" y="104" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="116" y="104" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="20" y="108" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="28" y="108" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="32" y="108" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="36" y="108" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="44" y="108" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="52" y="108" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="56" y="108" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="60" y="108" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="68" y="108" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="76" y="108" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="88" y="108" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="92" y="108" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="100" y="108" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="108" y="108" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="20" y="112" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="44" y="112" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="52" y="112" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="60" y="112" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="76" y="112" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="96" y="112" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="116" y="112" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="20" y="116" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="24" y="116" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="28" y="116" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="32" y="116" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="36" y="116" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="40" y="116" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="44" y="116" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="52" y="116" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="56" y="116" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="60" y="116" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="68" y="116" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="72" y="116" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="76" y="116" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="92" y="116" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="96" y="116" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="104" y="116" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="108" y="116" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="112" y="116" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="116" y="116" width="4" height="4" style="fill:black;stroke:none"/>
            <text x="140" y="100" style="fill:rgb(245,86,159);font-size:60pt;text-anchor:start;font-family:arial" >WB</text>
            <rect x="280" y="20" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="284" y="20" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="288" y="20" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="292" y="20" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="296" y="20" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="300" y="20" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="304" y="20" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="328" y="20" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="332" y="20" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="336" y="20" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="340" y="20" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="344" y="20" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="352" y="20" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="356" y="20" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="360" y="20" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="364" y="20" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="368" y="20" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="372" y="20" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="376" y="20" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="280" y="24" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="304" y="24" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="320" y="24" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="352" y="24" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="376" y="24" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="280" y="28" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="288" y="28" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="292" y="28" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="296" y="28" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="304" y="28" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="312" y="28" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="316" y="28" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="332" y="28" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="340" y="28" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="352" y="28" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="360" y="28" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="364" y="28" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="368" y="28" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="376" y="28" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="280" y="32" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="288" y="32" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="292" y="32" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="296" y="32" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="304" y="32" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="312" y="32" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="324" y="32" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="340" y="32" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="352" y="32" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="360" y="32" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="364" y="32" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="368" y="32" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="376" y="32" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="280" y="36" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="288" y="36" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="292" y="36" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="296" y="36" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="304" y="36" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="312" y="36" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="320" y="36" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="324" y="36" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="328" y="36" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="332" y="36" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="336" y="36" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="340" y="36" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="344" y="36" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="352" y="36" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="360" y="36" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="364" y="36" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="368" y="36" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="376" y="36" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="280" y="40" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="304" y="40" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="316" y="40" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="324" y="40" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="336" y="40" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="340" y="40" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="344" y="40" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="352" y="40" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="376" y="40" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="280" y="44" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="284" y="44" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="288" y="44" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="292" y="44" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="296" y="44" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="300" y="44" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="304" y="44" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="312" y="44" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="320" y="44" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="328" y="44" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="336" y="44" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="344" y="44" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="352" y="44" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="356" y="44" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="360" y="44" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="364" y="44" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="368" y="44" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="372" y="44" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="376" y="44" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="316" y="48" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="320" y="48" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="324" y="48" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="332" y="48" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="336" y="48" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="344" y="48" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="280" y="52" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="284" y="52" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="288" y="52" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="300" y="52" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="304" y="52" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="308" y="52" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="312" y="52" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="328" y="52" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="336" y="52" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="340" y="52" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="344" y="52" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="348" y="52" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="360" y="52" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="364" y="52" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="368" y="52" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="288" y="56" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="292" y="56" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="296" y="56" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="300" y="56" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="308" y="56" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="320" y="56" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="344" y="56" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="356" y="56" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="360" y="56" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="364" y="56" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="372" y="56" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="284" y="60" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="304" y="60" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="324" y="60" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="328" y="60" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="332" y="60" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="352" y="60" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="356" y="60" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="364" y="60" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="284" y="64" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="292" y="64" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="296" y="64" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="308" y="64" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="312" y="64" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="316" y="64" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="332" y="64" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="336" y="64" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="348" y="64" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="352" y="64" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="356" y="64" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="360" y="64" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="364" y="64" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="368" y="64" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="372" y="64" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="376" y="64" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="296" y="68" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="300" y="68" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="304" y="68" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="308" y="68" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="324" y="68" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="328" y="68" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="336" y="68" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="352" y="68" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="356" y="68" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="364" y="68" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="368" y="68" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="376" y="68" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="296" y="72" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="300" y="72" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="308" y="72" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="316" y="72" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="332" y="72" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="336" y="72" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="340" y="72" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="344" y="72" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="348" y="72" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="352" y="72" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="356" y="72" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="360" y="72" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="364" y="72" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="372" y="72" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="280" y="76" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="284" y="76" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="292" y="76" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="296" y="76" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="300" y="76" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="304" y="76" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="308" y="76" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="316" y="76" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="320" y="76" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="332" y="76" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="336" y="76" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="340" y="76" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="352" y="76" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="360" y="76" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="368" y="76" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="376" y="76" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="288" y="80" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="292" y="80" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="300" y="80" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="312" y="80" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="316" y="80" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="332" y="80" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="352" y="80" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="356" y="80" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="360" y="80" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="368" y="80" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="372" y="80" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="288" y="84" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="292" y="84" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="296" y="84" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="300" y="84" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="304" y="84" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="308" y="84" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="316" y="84" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="324" y="84" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="332" y="84" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="340" y="84" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="344" y="84" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="348" y="84" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="352" y="84" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="356" y="84" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="360" y="84" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="376" y="84" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="312" y="88" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="320" y="88" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="328" y="88" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="340" y="88" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="344" y="88" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="360" y="88" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="368" y="88" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="372" y="88" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="280" y="92" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="284" y="92" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="288" y="92" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="292" y="92" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="296" y="92" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="300" y="92" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="304" y="92" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="312" y="92" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="316" y="92" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="332" y="92" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="336" y="92" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="340" y="92" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="344" y="92" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="352" y="92" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="360" y="92" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="364" y="92" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="368" y="92" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="280" y="96" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="304" y="96" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="312" y="96" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="328" y="96" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="332" y="96" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="336" y="96" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="340" y="96" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="344" y="96" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="360" y="96" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="368" y="96" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="376" y="96" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="280" y="100" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="288" y="100" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="292" y="100" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="296" y="100" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="304" y="100" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="320" y="100" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="340" y="100" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="344" y="100" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="348" y="100" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="352" y="100" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="356" y="100" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="360" y="100" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="364" y="100" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="372" y="100" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="376" y="100" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="280" y="104" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="288" y="104" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="292" y="104" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="296" y="104" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="304" y="104" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="332" y="104" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="344" y="104" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="348" y="104" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="352" y="104" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="360" y="104" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="364" y="104" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="368" y="104" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="376" y="104" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="280" y="108" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="288" y="108" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="292" y="108" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="296" y="108" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="304" y="108" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="312" y="108" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="316" y="108" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="320" y="108" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="328" y="108" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="336" y="108" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="348" y="108" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="352" y="108" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="360" y="108" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="368" y="108" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="280" y="112" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="304" y="112" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="312" y="112" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="320" y="112" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="336" y="112" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="356" y="112" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="376" y="112" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="280" y="116" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="284" y="116" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="288" y="116" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="292" y="116" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="296" y="116" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="300" y="116" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="304" y="116" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="312" y="116" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="316" y="116" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="320" y="116" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="328" y="116" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="332" y="116" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="336" y="116" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="352" y="116" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="356" y="116" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="364" y="116" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="368" y="116" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="372" y="116" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="376" y="116" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="0" y="140" width="55" height="5" style="fill:black"/>
            <rect x="0" y="147" width="55" height="2" style="fill:black"/>
            <rect x="0" y="154" width="55" height="2" style="fill:black"/>
            <rect x="0" y="165" width="55" height="5" style="fill:black"/>
            <rect x="0" y="174" width="55" height="2" style="fill:black"/>
            <rect x="0" y="183" width="55" height="2" style="fill:black"/>
            <rect x="0" y="190" width="55" height="2" style="fill:black"/>
            <rect x="0" y="199" width="55" height="2" style="fill:black"/>
            <rect x="0" y="203" width="55" height="5" style="fill:black"/>
            <rect x="0" y="214" width="55" height="3" style="fill:black"/>
            <rect x="0" y="223" width="55" height="7" style="fill:black"/>
            <rect x="0" y="232" width="55" height="5" style="fill:black"/>
            <rect x="0" y="239" width="55" height="2" style="fill:black"/>
            <rect x="0" y="243" width="55" height="9" style="fill:black"/>
            <rect x="0" y="257" width="55" height="2" style="fill:black"/>
            <rect x="0" y="264" width="55" height="2" style="fill:black"/>
            <rect x="0" y="268" width="55" height="5" style="fill:black"/>
            <rect x="0" y="275" width="55" height="7" style="fill:black"/>
            <rect x="0" y="288" width="55" height="2" style="fill:black"/>
            <rect x="0" y="295" width="55" height="2" style="fill:black"/>
            <rect x="0" y="306" width="55" height="5" style="fill:black"/>
            <rect x="0" y="313" width="55" height="2" style="fill:black"/>
            <rect x="0" y="317" width="55" height="7" style="fill:black"/>
            <rect x="0" y="331" width="55" height="4" style="fill:black"/>
            <rect x="0" y="338" width="55" height="8" style="fill:black"/>
            <rect x="0" y="349" width="55" height="6" style="fill:black"/>
            <rect x="0" y="358" width="55" height="2" style="fill:black"/>
            <rect x="0" y="362" width="55" height="5" style="fill:black"/>
            <rect x="0" y="373" width="55" height="3" style="fill:black"/>
            <rect x="0" y="378" width="55" height="7" style="fill:black"/>
            <rect x="0" y="387" width="55" height="2" style="fill:black"/>
            <rect x="0" y="398" width="55" height="4" style="fill:black"/>
            <rect x="0" y="407" width="55" height="2" style="fill:black"/>
            <rect x="0" y="411" width="55" height="5" style="fill:black"/>
            <rect x="0" y="423" width="55" height="6" style="fill:black"/>
            <rect x="0" y="432" width="55" height="2" style="fill:black"/>
            <rect x="0" y="436" width="55" height="4" style="fill:black"/>
            <rect x="75" y="165" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="85" y="165" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="95" y="165" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="105" y="165" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="115" y="165" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="125" y="165" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="135" y="165" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="195" y="165" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="205" y="165" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="215" y="165" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="225" y="165" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="235" y="165" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="255" y="165" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="265" y="165" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="275" y="165" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="285" y="165" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="295" y="165" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="305" y="165" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="315" y="165" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="75" y="175" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="135" y="175" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="175" y="175" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="255" y="175" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="315" y="175" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="75" y="185" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="95" y="185" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="105" y="185" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="115" y="185" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="135" y="185" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="155" y="185" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="165" y="185" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="205" y="185" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="225" y="185" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="255" y="185" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="275" y="185" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="285" y="185" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="295" y="185" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="315" y="185" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="75" y="195" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="95" y="195" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="105" y="195" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="115" y="195" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="135" y="195" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="155" y="195" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="185" y="195" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="225" y="195" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="255" y="195" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="275" y="195" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="285" y="195" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="295" y="195" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="315" y="195" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="75" y="205" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="95" y="205" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="105" y="205" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="115" y="205" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="135" y="205" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="155" y="205" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="175" y="205" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="185" y="205" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="195" y="205" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="205" y="205" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="215" y="205" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="225" y="205" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="235" y="205" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="255" y="205" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="275" y="205" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="285" y="205" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="295" y="205" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="315" y="205" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="75" y="215" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="135" y="215" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="165" y="215" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="185" y="215" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="215" y="215" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="225" y="215" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="235" y="215" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="255" y="215" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="315" y="215" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="75" y="225" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="85" y="225" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="95" y="225" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="105" y="225" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="115" y="225" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="125" y="225" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="135" y="225" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="155" y="225" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="175" y="225" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="195" y="225" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="215" y="225" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="235" y="225" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="255" y="225" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="265" y="225" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="275" y="225" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="285" y="225" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="295" y="225" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="305" y="225" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="315" y="225" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="165" y="235" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="175" y="235" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="185" y="235" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="205" y="235" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="215" y="235" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="235" y="235" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="75" y="245" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="85" y="245" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="95" y="245" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="125" y="245" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="135" y="245" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="145" y="245" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="155" y="245" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="195" y="245" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="215" y="245" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="225" y="245" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="235" y="245" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="245" y="245" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="275" y="245" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="285" y="245" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="295" y="245" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="95" y="255" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="105" y="255" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="115" y="255" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="125" y="255" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="145" y="255" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="175" y="255" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="235" y="255" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="265" y="255" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="275" y="255" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="285" y="255" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="305" y="255" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="85" y="265" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="135" y="265" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="185" y="265" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="195" y="265" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="205" y="265" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="255" y="265" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="265" y="265" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="285" y="265" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="85" y="275" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="105" y="275" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="115" y="275" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="145" y="275" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="155" y="275" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="165" y="275" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="205" y="275" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="215" y="275" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="245" y="275" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="255" y="275" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="265" y="275" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="275" y="275" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="285" y="275" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="295" y="275" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="305" y="275" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="315" y="275" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="115" y="285" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="125" y="285" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="135" y="285" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="145" y="285" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="185" y="285" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="195" y="285" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="215" y="285" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="255" y="285" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="265" y="285" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="285" y="285" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="295" y="285" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="315" y="285" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="115" y="295" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="125" y="295" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="145" y="295" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="165" y="295" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="205" y="295" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="215" y="295" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="225" y="295" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="235" y="295" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="245" y="295" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="255" y="295" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="265" y="295" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="275" y="295" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="285" y="295" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="305" y="295" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="75" y="305" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="85" y="305" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="105" y="305" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="115" y="305" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="125" y="305" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="135" y="305" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="145" y="305" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="165" y="305" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="175" y="305" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="205" y="305" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="215" y="305" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="225" y="305" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="255" y="305" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="275" y="305" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="295" y="305" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="315" y="305" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="95" y="315" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="105" y="315" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="125" y="315" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="155" y="315" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="165" y="315" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="205" y="315" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="255" y="315" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="265" y="315" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="275" y="315" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="295" y="315" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="305" y="315" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="95" y="325" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="105" y="325" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="115" y="325" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="125" y="325" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="135" y="325" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="145" y="325" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="165" y="325" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="185" y="325" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="205" y="325" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="225" y="325" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="235" y="325" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="245" y="325" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="255" y="325" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="265" y="325" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="275" y="325" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="315" y="325" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="155" y="335" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="175" y="335" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="195" y="335" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="225" y="335" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="235" y="335" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="275" y="335" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="295" y="335" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="305" y="335" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="75" y="345" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="85" y="345" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="95" y="345" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="105" y="345" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="115" y="345" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="125" y="345" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="135" y="345" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="155" y="345" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="165" y="345" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="205" y="345" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="215" y="345" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="225" y="345" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="235" y="345" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="255" y="345" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="275" y="345" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="285" y="345" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="295" y="345" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="75" y="355" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="135" y="355" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="155" y="355" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="195" y="355" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="205" y="355" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="215" y="355" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="225" y="355" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="235" y="355" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="275" y="355" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="295" y="355" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="315" y="355" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="75" y="365" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="95" y="365" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="105" y="365" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="115" y="365" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="135" y="365" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="175" y="365" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="225" y="365" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="235" y="365" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="245" y="365" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="255" y="365" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="265" y="365" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="275" y="365" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="285" y="365" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="305" y="365" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="315" y="365" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="75" y="375" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="95" y="375" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="105" y="375" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="115" y="375" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="135" y="375" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="205" y="375" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="235" y="375" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="245" y="375" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="255" y="375" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="275" y="375" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="285" y="375" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="295" y="375" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="315" y="375" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="75" y="385" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="95" y="385" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="105" y="385" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="115" y="385" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="135" y="385" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="155" y="385" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="165" y="385" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="175" y="385" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="195" y="385" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="215" y="385" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="245" y="385" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="255" y="385" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="275" y="385" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="295" y="385" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="75" y="395" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="135" y="395" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="155" y="395" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="175" y="395" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="215" y="395" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="265" y="395" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="315" y="395" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="75" y="405" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="85" y="405" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="95" y="405" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="105" y="405" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="115" y="405" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="125" y="405" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="135" y="405" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="155" y="405" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="165" y="405" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="175" y="405" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="195" y="405" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="205" y="405" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="215" y="405" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="255" y="405" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="265" y="405" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="285" y="405" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="295" y="405" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="305" y="405" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="315" y="405" width="10" height="10" style="fill:black;stroke:none"/>
            <rect x="345" y="140" width="55" height="5" style="fill:black"/>
            <rect x="345" y="147" width="55" height="2" style="fill:black"/>
            <rect x="345" y="154" width="55" height="2" style="fill:black"/>
            <rect x="345" y="165" width="55" height="5" style="fill:black"/>
            <rect x="345" y="174" width="55" height="2" style="fill:black"/>
            <rect x="345" y="183" width="55" height="2" style="fill:black"/>
            <rect x="345" y="190" width="55" height="2" style="fill:black"/>
            <rect x="345" y="199" width="55" height="2" style="fill:black"/>
            <rect x="345" y="203" width="55" height="5" style="fill:black"/>
            <rect x="345" y="214" width="55" height="3" style="fill:black"/>
            <rect x="345" y="223" width="55" height="7" style="fill:black"/>
            <rect x="345" y="232" width="55" height="5" style="fill:black"/>
            <rect x="345" y="239" width="55" height="2" style="fill:black"/>
            <rect x="345" y="243" width="55" height="9" style="fill:black"/>
            <rect x="345" y="257" width="55" height="2" style="fill:black"/>
            <rect x="345" y="264" width="55" height="2" style="fill:black"/>
            <rect x="345" y="268" width="55" height="5" style="fill:black"/>
            <rect x="345" y="275" width="55" height="7" style="fill:black"/>
            <rect x="345" y="288" width="55" height="2" style="fill:black"/>
            <rect x="345" y="295" width="55" height="2" style="fill:black"/>
            <rect x="345" y="306" width="55" height="5" style="fill:black"/>
            <rect x="345" y="313" width="55" height="2" style="fill:black"/>
            <rect x="345" y="317" width="55" height="7" style="fill:black"/>
            <rect x="345" y="331" width="55" height="4" style="fill:black"/>
            <rect x="345" y="338" width="55" height="8" style="fill:black"/>
            <rect x="345" y="349" width="55" height="6" style="fill:black"/>
            <rect x="345" y="358" width="55" height="2" style="fill:black"/>
            <rect x="345" y="362" width="55" height="5" style="fill:black"/>
            <rect x="345" y="373" width="55" height="3" style="fill:black"/>
            <rect x="345" y="378" width="55" height="7" style="fill:black"/>
            <rect x="345" y="387" width="55" height="2" style="fill:black"/>
            <rect x="345" y="398" width="55" height="4" style="fill:black"/>
            <rect x="345" y="407" width="55" height="2" style="fill:black"/>
            <rect x="345" y="411" width="55" height="5" style="fill:black"/>
            <rect x="345" y="423" width="55" height="6" style="fill:black"/>
            <rect x="345" y="432" width="55" height="2" style="fill:black"/>
            <rect x="345" y="436" width="55" height="4" style="fill:black"/>
            <rect x="20" y="460" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="24" y="460" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="28" y="460" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="32" y="460" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="36" y="460" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="40" y="460" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="44" y="460" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="68" y="460" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="72" y="460" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="76" y="460" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="80" y="460" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="84" y="460" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="92" y="460" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="96" y="460" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="100" y="460" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="104" y="460" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="108" y="460" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="112" y="460" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="116" y="460" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="20" y="464" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="44" y="464" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="60" y="464" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="92" y="464" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="116" y="464" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="20" y="468" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="28" y="468" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="32" y="468" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="36" y="468" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="44" y="468" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="52" y="468" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="56" y="468" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="72" y="468" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="80" y="468" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="92" y="468" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="100" y="468" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="104" y="468" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="108" y="468" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="116" y="468" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="20" y="472" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="28" y="472" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="32" y="472" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="36" y="472" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="44" y="472" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="52" y="472" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="64" y="472" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="80" y="472" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="92" y="472" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="100" y="472" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="104" y="472" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="108" y="472" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="116" y="472" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="20" y="476" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="28" y="476" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="32" y="476" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="36" y="476" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="44" y="476" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="52" y="476" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="60" y="476" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="64" y="476" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="68" y="476" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="72" y="476" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="76" y="476" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="80" y="476" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="84" y="476" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="92" y="476" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="100" y="476" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="104" y="476" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="108" y="476" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="116" y="476" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="20" y="480" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="44" y="480" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="56" y="480" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="64" y="480" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="76" y="480" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="80" y="480" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="84" y="480" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="92" y="480" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="116" y="480" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="20" y="484" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="24" y="484" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="28" y="484" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="32" y="484" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="36" y="484" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="40" y="484" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="44" y="484" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="52" y="484" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="60" y="484" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="68" y="484" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="76" y="484" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="84" y="484" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="92" y="484" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="96" y="484" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="100" y="484" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="104" y="484" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="108" y="484" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="112" y="484" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="116" y="484" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="56" y="488" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="60" y="488" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="64" y="488" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="72" y="488" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="76" y="488" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="84" y="488" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="20" y="492" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="24" y="492" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="28" y="492" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="40" y="492" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="44" y="492" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="48" y="492" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="52" y="492" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="68" y="492" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="76" y="492" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="80" y="492" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="84" y="492" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="88" y="492" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="100" y="492" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="104" y="492" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="108" y="492" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="28" y="496" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="32" y="496" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="36" y="496" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="40" y="496" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="48" y="496" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="60" y="496" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="84" y="496" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="96" y="496" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="100" y="496" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="104" y="496" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="112" y="496" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="24" y="500" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="44" y="500" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="64" y="500" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="68" y="500" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="72" y="500" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="92" y="500" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="96" y="500" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="104" y="500" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="24" y="504" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="32" y="504" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="36" y="504" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="48" y="504" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="52" y="504" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="56" y="504" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="72" y="504" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="76" y="504" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="88" y="504" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="92" y="504" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="96" y="504" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="100" y="504" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="104" y="504" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="108" y="504" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="112" y="504" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="116" y="504" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="36" y="508" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="40" y="508" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="44" y="508" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="48" y="508" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="64" y="508" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="68" y="508" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="76" y="508" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="92" y="508" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="96" y="508" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="104" y="508" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="108" y="508" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="116" y="508" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="36" y="512" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="40" y="512" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="48" y="512" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="56" y="512" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="72" y="512" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="76" y="512" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="80" y="512" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="84" y="512" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="88" y="512" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="92" y="512" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="96" y="512" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="100" y="512" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="104" y="512" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="112" y="512" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="20" y="516" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="24" y="516" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="32" y="516" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="36" y="516" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="40" y="516" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="44" y="516" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="48" y="516" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="56" y="516" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="60" y="516" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="72" y="516" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="76" y="516" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="80" y="516" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="92" y="516" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="100" y="516" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="108" y="516" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="116" y="516" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="28" y="520" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="32" y="520" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="40" y="520" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="52" y="520" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="56" y="520" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="72" y="520" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="92" y="520" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="96" y="520" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="100" y="520" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="108" y="520" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="112" y="520" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="28" y="524" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="32" y="524" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="36" y="524" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="40" y="524" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="44" y="524" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="48" y="524" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="56" y="524" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="64" y="524" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="72" y="524" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="80" y="524" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="84" y="524" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="88" y="524" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="92" y="524" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="96" y="524" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="100" y="524" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="116" y="524" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="52" y="528" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="60" y="528" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="68" y="528" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="80" y="528" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="84" y="528" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="100" y="528" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="108" y="528" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="112" y="528" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="20" y="532" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="24" y="532" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="28" y="532" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="32" y="532" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="36" y="532" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="40" y="532" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="44" y="532" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="52" y="532" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="56" y="532" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="72" y="532" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="76" y="532" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="80" y="532" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="84" y="532" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="92" y="532" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="100" y="532" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="104" y="532" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="108" y="532" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="20" y="536" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="44" y="536" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="52" y="536" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="68" y="536" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="72" y="536" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="76" y="536" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="80" y="536" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="84" y="536" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="100" y="536" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="108" y="536" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="116" y="536" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="20" y="540" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="28" y="540" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="32" y="540" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="36" y="540" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="44" y="540" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="60" y="540" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="80" y="540" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="84" y="540" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="88" y="540" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="92" y="540" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="96" y="540" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="100" y="540" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="104" y="540" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="112" y="540" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="116" y="540" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="20" y="544" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="28" y="544" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="32" y="544" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="36" y="544" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="44" y="544" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="72" y="544" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="84" y="544" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="88" y="544" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="92" y="544" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="100" y="544" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="104" y="544" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="108" y="544" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="116" y="544" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="20" y="548" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="28" y="548" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="32" y="548" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="36" y="548" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="44" y="548" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="52" y="548" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="56" y="548" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="60" y="548" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="68" y="548" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="76" y="548" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="88" y="548" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="92" y="548" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="100" y="548" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="108" y="548" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="20" y="552" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="44" y="552" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="52" y="552" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="60" y="552" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="76" y="552" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="96" y="552" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="116" y="552" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="20" y="556" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="24" y="556" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="28" y="556" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="32" y="556" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="36" y="556" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="40" y="556" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="44" y="556" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="52" y="556" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="56" y="556" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="60" y="556" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="68" y="556" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="72" y="556" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="76" y="556" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="92" y="556" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="96" y="556" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="104" y="556" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="108" y="556" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="112" y="556" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="116" y="556" width="4" height="4" style="fill:black;stroke:none"/>
            <text x="130" y="500" style="fill:black;font-size:25pt;text-anchor:start;font-family:arial" >2112313</text>
            <text x="131" y="546" style="fill:black;font-size:46pt;text-anchor:start;font-family:arial" >2633</text>
            <rect x="280" y="460" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="284" y="460" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="288" y="460" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="292" y="460" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="296" y="460" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="300" y="460" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="304" y="460" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="328" y="460" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="332" y="460" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="336" y="460" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="340" y="460" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="344" y="460" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="352" y="460" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="356" y="460" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="360" y="460" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="364" y="460" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="368" y="460" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="372" y="460" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="376" y="460" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="280" y="464" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="304" y="464" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="320" y="464" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="352" y="464" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="376" y="464" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="280" y="468" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="288" y="468" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="292" y="468" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="296" y="468" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="304" y="468" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="312" y="468" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="316" y="468" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="332" y="468" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="340" y="468" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="352" y="468" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="360" y="468" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="364" y="468" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="368" y="468" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="376" y="468" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="280" y="472" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="288" y="472" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="292" y="472" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="296" y="472" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="304" y="472" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="312" y="472" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="324" y="472" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="340" y="472" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="352" y="472" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="360" y="472" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="364" y="472" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="368" y="472" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="376" y="472" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="280" y="476" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="288" y="476" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="292" y="476" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="296" y="476" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="304" y="476" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="312" y="476" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="320" y="476" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="324" y="476" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="328" y="476" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="332" y="476" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="336" y="476" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="340" y="476" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="344" y="476" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="352" y="476" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="360" y="476" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="364" y="476" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="368" y="476" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="376" y="476" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="280" y="480" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="304" y="480" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="316" y="480" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="324" y="480" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="336" y="480" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="340" y="480" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="344" y="480" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="352" y="480" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="376" y="480" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="280" y="484" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="284" y="484" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="288" y="484" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="292" y="484" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="296" y="484" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="300" y="484" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="304" y="484" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="312" y="484" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="320" y="484" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="328" y="484" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="336" y="484" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="344" y="484" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="352" y="484" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="356" y="484" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="360" y="484" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="364" y="484" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="368" y="484" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="372" y="484" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="376" y="484" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="316" y="488" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="320" y="488" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="324" y="488" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="332" y="488" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="336" y="488" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="344" y="488" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="280" y="492" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="284" y="492" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="288" y="492" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="300" y="492" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="304" y="492" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="308" y="492" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="312" y="492" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="328" y="492" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="336" y="492" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="340" y="492" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="344" y="492" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="348" y="492" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="360" y="492" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="364" y="492" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="368" y="492" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="288" y="496" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="292" y="496" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="296" y="496" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="300" y="496" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="308" y="496" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="320" y="496" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="344" y="496" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="356" y="496" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="360" y="496" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="364" y="496" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="372" y="496" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="284" y="500" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="304" y="500" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="324" y="500" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="328" y="500" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="332" y="500" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="352" y="500" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="356" y="500" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="364" y="500" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="284" y="504" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="292" y="504" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="296" y="504" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="308" y="504" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="312" y="504" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="316" y="504" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="332" y="504" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="336" y="504" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="348" y="504" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="352" y="504" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="356" y="504" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="360" y="504" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="364" y="504" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="368" y="504" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="372" y="504" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="376" y="504" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="296" y="508" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="300" y="508" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="304" y="508" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="308" y="508" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="324" y="508" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="328" y="508" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="336" y="508" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="352" y="508" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="356" y="508" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="364" y="508" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="368" y="508" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="376" y="508" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="296" y="512" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="300" y="512" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="308" y="512" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="316" y="512" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="332" y="512" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="336" y="512" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="340" y="512" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="344" y="512" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="348" y="512" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="352" y="512" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="356" y="512" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="360" y="512" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="364" y="512" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="372" y="512" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="280" y="516" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="284" y="516" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="292" y="516" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="296" y="516" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="300" y="516" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="304" y="516" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="308" y="516" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="316" y="516" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="320" y="516" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="332" y="516" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="336" y="516" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="340" y="516" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="352" y="516" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="360" y="516" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="368" y="516" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="376" y="516" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="288" y="520" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="292" y="520" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="300" y="520" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="312" y="520" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="316" y="520" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="332" y="520" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="352" y="520" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="356" y="520" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="360" y="520" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="368" y="520" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="372" y="520" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="288" y="524" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="292" y="524" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="296" y="524" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="300" y="524" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="304" y="524" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="308" y="524" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="316" y="524" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="324" y="524" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="332" y="524" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="340" y="524" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="344" y="524" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="348" y="524" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="352" y="524" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="356" y="524" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="360" y="524" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="376" y="524" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="312" y="528" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="320" y="528" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="328" y="528" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="340" y="528" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="344" y="528" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="360" y="528" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="368" y="528" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="372" y="528" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="280" y="532" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="284" y="532" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="288" y="532" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="292" y="532" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="296" y="532" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="300" y="532" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="304" y="532" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="312" y="532" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="316" y="532" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="332" y="532" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="336" y="532" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="340" y="532" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="344" y="532" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="352" y="532" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="360" y="532" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="364" y="532" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="368" y="532" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="280" y="536" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="304" y="536" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="312" y="536" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="328" y="536" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="332" y="536" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="336" y="536" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="340" y="536" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="344" y="536" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="360" y="536" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="368" y="536" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="376" y="536" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="280" y="540" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="288" y="540" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="292" y="540" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="296" y="540" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="304" y="540" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="320" y="540" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="340" y="540" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="344" y="540" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="348" y="540" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="352" y="540" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="356" y="540" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="360" y="540" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="364" y="540" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="372" y="540" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="376" y="540" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="280" y="544" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="288" y="544" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="292" y="544" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="296" y="544" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="304" y="544" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="332" y="544" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="344" y="544" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="348" y="544" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="352" y="544" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="360" y="544" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="364" y="544" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="368" y="544" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="376" y="544" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="280" y="548" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="288" y="548" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="292" y="548" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="296" y="548" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="304" y="548" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="312" y="548" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="316" y="548" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="320" y="548" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="328" y="548" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="336" y="548" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="348" y="548" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="352" y="548" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="360" y="548" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="368" y="548" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="280" y="552" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="304" y="552" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="312" y="552" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="320" y="552" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="336" y="552" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="356" y="552" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="376" y="552" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="280" y="556" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="284" y="556" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="288" y="556" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="292" y="556" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="296" y="556" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="300" y="556" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="304" y="556" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="312" y="556" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="316" y="556" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="320" y="556" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="328" y="556" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="332" y="556" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="336" y="556" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="352" y="556" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="356" y="556" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="364" y="556" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="368" y="556" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="372" y="556" width="4" height="4" style="fill:black;stroke:none"/>
            <rect x="376" y="556" width="4" height="4" style="fill:black;stroke:none"/>
            </g>
            </svg>
            """;
        GetColumnNamesOfAllQuerries();
        this.DataContext = this;
    }
    public QRCodePropertiesFunction( string json)
    {
        InitializeComponent();
        if (!string.IsNullOrEmpty(json))
        {
            QRCodeImageProperties = JsonHelper.Deserialize<QRCodeImageProperties>(json);
        }
        else
        {
            QRCodeImageProperties = new QRCodeImageProperties();
        }

        QRCodeSource = QRCodeImageProperties.Value;
        functionTitle.Content = "Редактирование поля с QR кодом";
        btSave.Content = "Сохранить";
        GetColumnNamesOfAllQuerries();
        DataSourceType = QRCodeImageProperties.DataSourceType;
        DataSourceName = AvailableSources.FirstOrDefault(s => s.Key == QRCodeImageProperties.DataSourceName);
        // safe in ctor after TextFieldValue deserialization and column population
        ColumnName = new KeyValueItem
        {
            Key = QRCodeImageProperties.DataFieldName,
            SourceType = QRCodeImageProperties.DataSourceType,
            Value = QRCodeImageProperties.DataFieldName,
            FieldType = QRCodeImageProperties.DataSourceFieldType,
            FieldTypeNet = FieldTypeConverter.GetNetType(QRCodeImageProperties.DataSourceFieldType)
        };
        int index = -1;
        for (int i = 0; i < ColumnNames.Count; i++)
        {
            if (ColumnNames[i].Key == QRCodeImageProperties.DataFieldName)
            {
                index = i;
                break;
            }
        }
        dataFieldName.SelectedIndex = index;
        this.DataContext = this;
    }
    private void okButton_Click(object sender, RoutedEventArgs e)
    {
        QRCodeImageProperties.Value = QRCodeSource;
        QRCodeImageProperties.DataSourceType = DataSourceType;
        QRCodeImageProperties.DataSourceName = DataSourceName == null ? null : DataSourceName.Key;
        QRCodeImageProperties.DataFieldName = ColumnName == null ? null : ColumnName.Value;
        QRCodeImageProperties.DataSourceFieldType = ColumnName == null ? FieldTypes.None : ColumnName.FieldType;
        if(QRCodeImageProperties.DataSourceType == DataSourceType.None)
        {
            QRCodeImageProperties.DataSourceName = null;
            QRCodeImageProperties.DataFieldName = null;
            QRCodeImageProperties.DataSourceFieldType = FieldTypes.None;
        }
        var json = JsonHelper.Serialize<QRCodeImageProperties>(QRCodeImageProperties);
        OnReturn(new ReturnEventArgs<string>(json));
    }

    private void cancelButton_Click(object sender, RoutedEventArgs e)
    {
        // Return a value to the calling page
        OnReturn(new ReturnEventArgs<string>(null));
    }

    private void columnDataList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var row = (DataRowView)columnDataList.SelectedItem;
        if (row != null)
        {
            if (ColumnName != null && ColumnName.FieldType == FieldTypes.ByteArray) 
            {
                QRCodeImageProperties.Value = Encoding.UTF8.GetString(row[ColumnName.Key] as byte[]);
                QRCodeSource = QRCodeImageProperties.Value;
            }
            else
                QRCodeImageProperties.Value = (string)row[ColumnName.Key];
        }
    }
}
