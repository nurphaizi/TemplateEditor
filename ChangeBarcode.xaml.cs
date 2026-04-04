using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using Serilog;

namespace TemplateEdit;
/// <summary>
/// Interaction logic for ChangeBarcode.xaml
/// </summary>
public partial class ChangeBarcode : PageFunction<String>,INotifyPropertyChanged
{
    private BarcodeImageProperties  _barcodeImageProperties = new();
    public BarcodeImageProperties BarcodeProperties
    {
        get { return _barcodeImageProperties; }
        set
        {
            if (_barcodeImageProperties != value)
            {
                _barcodeImageProperties = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private string _ConnectionString;
    public string ConnectionString
    {
        get => _ConnectionString;
        set
        {
            _ConnectionString = value;
            OnPropertyChanged();
        }
    }
    public void DbClient()
    {
        var dbFileName = Properties.Settings.Default.dbFileName;
        string connectionString = $"Data Source={dbFileName}";
        this.ConnectionString = $"{connectionString};Cache=Shared;";
    }

    private ObservableCollection<KeyValueItem> _availableSources = new();
    public ObservableCollection<KeyValueItem> AvailableSources
    {
        get => _availableSources;
        set
        {
            _availableSources = value;
            OnPropertyChanged(nameof(AvailableSources));
        }
    }
    private Dictionary<string, List<KeyValueItem>> _ColumnNamesListOfQueries = [];
    public Dictionary<string, List<KeyValueItem>> ColumnNamesListOfQueries
    {
        get => (Dictionary<string, List<KeyValueItem>>)_ColumnNamesListOfQueries;
        set
        {
            _ColumnNamesListOfQueries = value;
            OnPropertyChanged(nameof(ColumnNamesListOfQueries));
        }
    }
    private void GetColumnNamesOfAllQuerries()
    {
        ColumnNamesListOfQueries.Clear();
        DbClient();

        if (string.IsNullOrEmpty(Properties.Settings.Default.DataSources))
        {
            return;
        }
        _availableSources.Clear();
        _availableSources = JsonHelper.Deserialize<ObservableCollection<KeyValueItem>>(Properties.Settings.Default.DataSources);
        OnPropertyChanged();
        if (_availableSources.Count == 0)
        {
            return;
        }
        foreach (var querry in _availableSources)
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
                sqliteQuerryFields.GetColumnNamesFromQuery(ConnectionString, querry.Value).ForEach(f => columnNames.Add(new KeyValueItem() { Key = f.Name, SourceType = DataSourceType.Database, Value = f.Name, FieldTypeNet = f.type, FieldType = FieldTypeConverter.GetFieldType(f.type) }));
                ColumnNamesListOfQueries[querry.Key] = columnNames;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Ошибка получения колонок для запроса {querry.Key}");
            }
        }
    }

    private ObservableCollection<KeyValueItem> _columnNames =[];
    public ObservableCollection<KeyValueItem> ColumnNames
    {
        get => _columnNames;
        set
        {
            _columnNames = value;
            OnPropertyChanged(nameof(ColumnNames));
        }
    }

    private KeyValueItem _ColumnName = new();
    public KeyValueItem ColumnName
    {
        get => (KeyValueItem)_ColumnName;
        set
        {
            _ColumnName = value;
            OnPropertyChanged(nameof(ColumnName));
        }
    }

    public ChangeBarcode()
    {
        InitializeComponent();
        GetColumnNamesOfAllQuerries();
        DataContext = this;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        OnReturn(new ReturnEventArgs<string>( JsonHelper.Serialize<BarcodeImageProperties>(BarcodeProperties)));
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {

    }
}
