using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
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
using System.Windows.Media;
namespace TemplateEdit
{
    /// <summary>
    /// Interaction logic for ChangeTextFieldProperties.xaml
    /// </summary>


    public class FieldTypeDescriptionConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not KeyValueItem keyValue) return null;
            return $"""
                Net type "{keyValue.FieldTypeNet}". DataBase type "{keyValue.FieldType}"
                """;
        }
        object IValueConverter.ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            return value;

        }

    }

    public class DictionaryToObservableConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not Dictionary<string, List<KeyValueItem>> columnNamesDictionary) return null;
            if (columnNamesDictionary.Count == 0) return null;
            if (parameter is not string name || string.IsNullOrWhiteSpace(name)) return null;
            var columnNames = columnNamesDictionary.FirstOrDefault(x => x.Key == name);
            return columnNames.Value;

        }
        object IValueConverter.ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            return value;

        }

    }

    public class DictionaryLookupConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // 1. Safety Check: Wait for bindings to settle
            if (values[0] == DependencyProperty.UnsetValue || values[1] == DependencyProperty.UnsetValue)
                return null;

            // 2. Extract the Key (values[0])
            // Ensure this matches the data type of the keys in your dictionary
            var key = values[0];

            // 3. Extract the Dictionary (values[1]) safely
            // Casting to IDictionary is safer than Dictionary<string, string> 
            // because it handles covariance and some dynamic types better.
            var dictionary = values[1] as IDictionary;

            if (dictionary == null)
                return "Error: Dictionary not found or invalid type";

            // 4. Perform the Lookup
            if (dictionary.Contains(key))
            {
                return dictionary[key];
            }

            return "Key not found"; // Or return Binding.DoNothing
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }



    [ValueConversion(typeof(String), typeof(System.Windows.Media.FontFamily))]
    public class StringToFontFamilyConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            if(value==null)
            {
                return new System.Windows.Media.FontFamily("Arial");
            }
            var source = value as string;
            if (string.IsNullOrEmpty(source))
            {
                return new System.Windows.Media.FontFamily("Arial");
            }
            return new System.Windows.Media.FontFamily(source);
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            if(value == null) {return "Arial"; }    
            var fontFamily = (System.Windows.Media.FontFamily)value;
            if (fontFamily == null || string.IsNullOrEmpty(fontFamily.Source))
            {
                return "Arial"; // Default font family if null or empty
            }
            return fontFamily.Source;
        }
    }

    [ValueConversion(typeof(string), typeof(double))]
    public class ConverterStringToDouble : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return 0;
            }
            string s = value as string;
            if (string.IsNullOrWhiteSpace(s))
            {
                return 0;
            }
            double number;
            bool result = double.TryParse(s, out number);
            if (result) { return number; }
            return 0;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return 0;
            }
            double number = (double)value;
            return number.ToString();
        }
    }
    [ValueConversion(typeof(float), typeof(string))]
    public class ConverterFloatToString : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return String.Empty;
            }
            float number = (float)value;
            return number.ToString();
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return 0;
            }
            string s = (string)value;

            float number = 0.0f;
            if (float.TryParse(s, out number))
            {
                return number;
            }
            return 0.0f;
        }
    }

    public partial class ChangeTextFieldProperties : PageFunction<String>, INotifyPropertyChanged
    {
        public ChangeTextFieldProperties()
        {
            InitializeComponent();
            this.DataContext = this;
        }
        public ChangeTextFieldProperties(string sTextFieldValue)
        {
            InitializeComponent();
            TextFieldValue = JsonHelper.Deserialize<TextFieldValue>(sTextFieldValue);
            TextValue = TextFieldValue.Value;
            if (!string.IsNullOrEmpty(TextFieldValue.Name))
            {
                functionTitle.Content = "Редактирование свойств текстового поля";
                btSave.Content = "Сохранить изменения";
            }
            fontSize.Text = TextFieldValue.FontSize.ToString();
            Weights.SelectedIndex = FontWeightList.IndexOf(input_text.FontWeight);
            Styles.SelectedIndex = Styles.Items.IndexOf(input_text.FontStyle);
            colorPickerBg.SelectedColor = TextFieldValue.Background;
            colorPickerFg.SelectedColor = TextFieldValue.Foreground;
            DataSourceType = TextFieldValue.DataSourceType;
            GetColumnNamesOfAllQuerries();
            DataSourceName = AvailableSources.FirstOrDefault(s => s.Key == this.TextFieldValue.DataSourceName);
            // safe in ctor after TextFieldValue deserialization and column population
            ColumnName = new KeyValueItem
            {
                Key = TextFieldValue.DataFieldName,
                SourceType = TextFieldValue.DataSourceType,
                Value = TextFieldValue.DataFieldName,
                FieldType = TextFieldValue.DataSourceFieldType,
                FieldTypeNet = FieldTypeConverter.GetNetType(this.TextFieldValue.DataSourceFieldType)
            };
            int index = -1;
            if (ColumnNames != null)
            {

                for (int i = 0; i < ColumnNames.Count; i++)
                {
                    if (ColumnNames[i].Key == TextFieldValue.DataFieldName)
                    {
                        index = i;
                        break;
                    }
                }
                dataFieldName.SelectedIndex = index;
            }
            DataContext = this;
        }


        private ObservableCollection<string> _InstalledFontsList = [];
        public ObservableCollection<string> InstalledFontsList
        {
            get => (ObservableCollection<string>)_InstalledFontsList;
            set
            {
                _InstalledFontsList = value;
                NotifyPropertyChanged(nameof(InstalledFontsList));
            }
        }
        private ObservableCollection<System.Windows.Media.FontFamily> _InstalledFonts = [];
        public ObservableCollection<System.Windows.Media.FontFamily> InstalledFonts
        {
            get => (ObservableCollection<System.Windows.Media.FontFamily>)_InstalledFonts;
            set
            {
                _InstalledFonts = value;
                NotifyPropertyChanged(nameof(InstalledFonts));
            }
        }
        public List<FontWeight> FontWeightList
        {
            get;
        } = new List<FontWeight>
        {
             FontWeights.Normal,
             FontWeights.Bold,
             FontWeights.Light,
             FontWeights.Heavy
        };
        // Bind this list to your ComboBox
        private double _FontSize;
        private double FontSize
        {
            get => _FontSize;
            set
            {
                _FontSize = value;
                NotifyPropertyChanged(nameof(FontSize));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //Исочники данных

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

        //
        // Тип источника
        //
        private void OnDataSourceTypeChanged()
        {
            // 1. Call an instance method to handle logic
            ColumnNames = null;
            DataSourceName = null;
            ColumnName = null;
        }
        private DataSourceType _DataSourceType = DataSourceType.None;
        public DataSourceType DataSourceType
        {
            get => (DataSourceType)_DataSourceType;
            set
            {
                _DataSourceType = value;
                OnDataSourceTypeChanged();
                NotifyPropertyChanged(nameof(DataSourceType));
            }
        }

        //
        //Имя источника
        //
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
                NotifyPropertyChanged(nameof(ColumnNames));
                NotifyPropertyChanged(nameof(ColumnName));
            }
        }

        private KeyValueItem _DataSourceName;
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
            var gridView = new GridView();
            {
                gridView.Columns.Add(new GridViewColumn
                {
                    Header = ColumnName.Key,
                    DisplayMemberBinding = new Binding(ColumnName.Value)
                });
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

        private KeyValueItem _ColumnName;
        private void OnColumnNameChanged()
        {
                switch (DataSourceType)
                {
                    case DataSourceType.Database:
                        var dataTable = ReportData();
                        if (dataTable != null && ColumnName !=null && ColumnName.Key !=null && dataTable.Columns.Contains(ColumnName?.Key))
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

        private TextFieldValue _TextFieldValue;
        public TextFieldValue TextFieldValue
        {
            get => (TextFieldValue)_TextFieldValue;
            set
            {
                _TextFieldValue = value;
                NotifyPropertyChanged(nameof(TextFieldValue));
            }
        }
        private string _TextValue;
        public string TextValue
        {
            get => (string)_TextValue;
            set
            {
                _TextValue = value;
                NotifyPropertyChanged(nameof(TextValue));
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
        private void Button_Click_Save(object sender, RoutedEventArgs e)
        {
            TextFieldValue.Value = TextValue;
            TextFieldValue.DataSourceType = DataSourceType;
            if (TextFieldValue.DataSourceType==DataSourceType.None )
            {
                TextFieldValue.DataSourceName = null;
                TextFieldValue.DataFieldName = null;
                TextFieldValue.DataSourceFieldType = FieldTypes.None;
                goto ret;
            }
            if (DataSourceName != null)
            {
                TextFieldValue.DataSourceName = DataSourceName.Key;
            }
            else
            {
                TextFieldValue.DataSourceName = null;
                TextFieldValue.DataFieldName = null;
                TextFieldValue.DataSourceFieldType = FieldTypes.None;
                goto ret;
            }
            if (ColumnName != null)
            {
                TextFieldValue.DataFieldName = ColumnName.Key;
                TextFieldValue.DataSourceFieldType = ColumnName.FieldType;
            }
            else
            {
                TextFieldValue.DataSourceName = null;
                TextFieldValue.DataFieldName = null;
                TextFieldValue.DataSourceFieldType = FieldTypes.None;
            }
        ret: OnReturn(new ReturnEventArgs<string>(JsonHelper.Serialize<TextFieldValue>(TextFieldValue)));
        }

        private void Button_Click_Cancel(object sender, RoutedEventArgs e)
        {
            OnReturn(null);
        }

        private void fontSize_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {
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
            var fontsList = PrinterFonts.GetPrinterFonts(Properties.Settings.Default.barcodePrinter);
            if (InstalledFontsList == null)
            {
                InstalledFontsList = new ObservableCollection<string>();
            }
            InstalledFontsList.Clear();
            foreach (var font in fontsList)
            {
                try
                {
                    InstalledFontsList.Add(font);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Ошибка добавления шрифта");
                }
            }
        }

        private void columnDataList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var row = (DataRowView)columnDataList.SelectedItem;
            if (row != null)
            {
                TextFieldValue.Value = (string)row[ColumnName.Key];
                TextValue = (string)row[ColumnName.Key];

            }
        }
    }
}
