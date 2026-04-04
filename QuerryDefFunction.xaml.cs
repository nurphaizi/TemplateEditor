using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Navigation;
using Microsoft.VisualBasic.FileIO;

namespace TemplateEdit;
/// <summary>
/// Interaction logic for QuerryDefFunction.xaml
/// </summary>
/// 
// 1. Implement INotifyPropertyChanged
public class KeyValueItem : INotifyPropertyChanged, IEquatable<KeyValueItem>
{
    private string _key;
    private DataSourceType _SourceType;
    private System.Type _FieldTypeNet;
    private FieldTypes _FieldType;
    private string _value;
    public string Key
    {
        get => _key;
        set
        {
            if (_key != value)
            {
                _key = value;
                OnPropertyChanged(nameof(Key)); // 3. Fire the event
            }
        }
    }
    public DataSourceType SourceType
    {
        get => _SourceType;
        set
        {
            if (_SourceType != value)
            {
                _SourceType = value;
                OnPropertyChanged(nameof(SourceType)); // 3. Fire the event
            }
        }
    }
    public string Value
    {
        get => _value;
        set
        {
            if (_value != value)
            {
                _value = value;
                OnPropertyChanged(nameof(value)); // 3. Fire the event
            }
        }
    }
    public System.Type FieldTypeNet
    {
        get => _FieldTypeNet;
        set
        {
            if (_FieldTypeNet != value)
            {
                _FieldTypeNet = value;
                OnPropertyChanged(nameof(FieldTypeNet)); // 3. Fire the event
            }
        }
    }
    public FieldTypes FieldType
    {
        get => _FieldType;
        set
        {
            if (_FieldType != value)
            {
                _FieldType = value;
                OnPropertyChanged(nameof(FieldType)); // 3. Fire the event
            }
        }
    }
    // 2. Boilerplate INotifyPropertyChanged implementation
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    public bool Equals(KeyValueItem other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        // Compare all relevant fields by value
        return string.Equals(Key, other.Key, StringComparison.Ordinal) &&
               SourceType == other.SourceType &&
               FieldTypeNet == other.FieldTypeNet &&
               FieldType == other.FieldType &&
               string.Equals(Value, other.Value, StringComparison.Ordinal);
    }
    public override bool Equals(object obj)
    {
        return Equals(obj as KeyValueItem);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Key, SourceType, FieldTypeNet, FieldType, Value);
    }
    // Optional: Overload the equality operators (== and !=)
    public static bool operator ==(KeyValueItem left, KeyValueItem right)
    {
        if (left is null)
        {
            return right is null;
        }
        return left.Equals(right);
    }

    public static bool operator !=(KeyValueItem left, KeyValueItem right)
    {
        return !(left == right);
    }
}
    public class KeyValueItemConverter : IMultiValueConverter
{
    public object Convert(object[] values, System.Type targetType, object parameter, CultureInfo culture)
    {
        return new KeyValueItem
        {
            Key = values[0]?.ToString() ?? "",
            SourceType =values[1] == null || ((int)(values[1])==0) ? DataSourceType.None : (DataSourceType)values[1],
            Value = values[2]?.ToString() ?? ""
        };
    }

    public object[] ConvertBack(object value, System.Type[] targetTypes, object parameter, CultureInfo culture)
    {
        if (value is KeyValueItem item)
            return new object[] { item.Key, item.SourceType, item.Value };
        return new object[] { "", DataSourceType.None,"" };
    }
}


public class FieldDefinition : INotifyPropertyChanged
{
    private string _Name;
    private System.Type _type;

    public string Name
    {
        get => _Name;
        set
        {
            if (_Name != value)
            {
                _Name = value;
                OnPropertyChanged(nameof(Name)); // 3. Fire the event
            }
        }
    }
    public System.Type type
    {
        get => _type;

        set
        {
            if (_type != value)
            {
                _type = value;
                OnPropertyChanged(nameof(type));
            }
        }
    }


    // 2. Boilerplate INotifyPropertyChanged implementation
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}
public partial class QuerryDefFunction : PageFunction<String>, INotifyPropertyChanged
{
    public QuerryDefFunction()
    {
   
        InitializeComponent();
        this.DataContext = this;
    }
    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // Optional: SetProperty helper to reduce redundancy
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private KeyValueItem _querry=new();
    public KeyValueItem Querry
    {
        get => _querry;
        set
        {
            if (_querry != value)
            {
                SetProperty(ref _querry, value);
                DbClient();
                try
                {
                    var sqliteQuerryFields = new SqliteQuerryFields();
                    _availableFields.Clear();
                    sqliteQuerryFields.GetColumnNamesFromQuery(ConnectionString, Querry.Value).ForEach(f => _availableFields.Add(new FieldDefinition() { Name = f.Name, type = f.type }));
                    OnPropertyChanged(nameof(AvailableFields));
                }
                catch (Exception ex)
                {
                }
            }
        }

    }

    private ObservableCollection<FieldDefinition> _availableFields = new();
    public ObservableCollection<FieldDefinition> AvailableFields
    {
        get => _availableFields;
        set => SetProperty(ref _availableFields, value);
    }
    private ObservableCollection<KeyValueItem> _availableSources = new();
    public ObservableCollection<KeyValueItem> AvailableSources
    {
        get => _availableSources;
        set => SetProperty(ref _availableSources, value);
    }

    
    private string _ConnectionString;
    public string ConnectionString
    {
        get => _ConnectionString;
        set => SetProperty(ref _ConnectionString, value);
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        OnReturn(new ReturnEventArgs<string>(null));
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
   
        OnReturn(new ReturnEventArgs<string>(String.Empty));

    }
    public void DbClient()
    {
        var dbFileName = Properties.Settings.Default.dbFileName;
        string connectionString = $"Data Source={dbFileName}";
        this.ConnectionString = $"{connectionString};Cache=Shared;";
    }
    private void PageFunction_Loaded(object sender, RoutedEventArgs e)
    {
        DbClient();

        _availableSources.Clear();
        if (string.IsNullOrEmpty(Properties.Settings.Default.DataSources))
        {
            return;
        }
        _availableSources.Clear();
        try
        {
            _availableSources = JsonHelper.Deserialize<ObservableCollection<KeyValueItem>>(Properties.Settings.Default.DataSources);
        }
        catch(Exception ex)
        {
        }
        if (_availableSources.Count == 0)
        {
            return;
        }
        OnPropertyChanged(nameof(Querry));

        Querry = _availableSources[0];
        OnPropertyChanged(nameof(AvailableSources));
        if (_querry==null||string.IsNullOrEmpty(_querry.Key) || string.IsNullOrWhiteSpace(_querry.Key))
        {
            return;
        }
        try
        {
            var sqliteQuerryFields = new SqliteQuerryFields();
            sqliteQuerryFields.GetColumnNamesFromQuery(ConnectionString, Querry.Value).ForEach(f => _availableFields.Add(new FieldDefinition() { Name = f.Name, type = f.type }));
            OnPropertyChanged(nameof(AvailableFields));
        }
        catch (Exception ex)
        {
        }
    }

   

    private void NewSource(object sender, RoutedEventArgs e)
    {
        AvailableSources.Add(new KeyValueItem() { Key = $"NewSource{AvailableSources.Count}" , SourceType=DataSourceType.None, Value = "SELECT * FROM Orders" });
    }

    private void DeleteSource(object sender, RoutedEventArgs e)
    {
        var selectedItem = (KeyValueItem)availableSourcesList.SelectedItem;
        if (selectedItem != null)
        {
            AvailableSources.Remove(selectedItem);
        }
    }

    private void availableSourcesList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var sqliteQuerryFields = new SqliteQuerryFields();
        _availableFields.Clear();
        sqliteQuerryFields.GetColumnNamesFromQuery(ConnectionString, Querry.Value).ForEach(f => _availableFields.Add(new FieldDefinition() { Name = f.Name, type = f.type }));
        OnPropertyChanged(nameof(AvailableFields));
    }

    private void SaveSourceList(object sender, RoutedEventArgs e)
    {

        Properties.Settings.Default.DataSources = JsonHelper.Serialize<ObservableCollection<KeyValueItem>>(_availableSources);
        Properties.Settings.Default.Save();
    }
}
