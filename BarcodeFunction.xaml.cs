using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Navigation;
using Serilog;
using ZXing;

namespace TemplateEdit
{
    /// <summary>
    /// Interaction logic for BarcodeFunction.xaml
    /// </summary>

    public partial class BarcodeFunction : PageFunction<String>, INotifyPropertyChanged
    {
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
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private BarcodeFormat _BarcodeFormatSelected;
        public BarcodeFormat BarcodeFormatSelected
        {
            get => _BarcodeFormatSelected;
            set
            {
                _BarcodeFormatSelected = value;
                NotifyPropertyChanged(nameof(BarcodeFormatSelected));
            }
        }
        private ObservableCollection<BarcodeFormat> _BarcodeFormatCollection = [];
        public ObservableCollection<BarcodeFormat> BarcodeFormatCollection
        {
            get => (ObservableCollection<BarcodeFormat>)_BarcodeFormatCollection;
            set
            {
                _BarcodeFormatCollection = value;
                NotifyPropertyChanged(nameof(BarcodeFormatCollection));
            }
        }

        private double _BarcodeLeft= 70.0;
        public double BarcodeLeft
        {
            get => (double)_BarcodeLeft;
            set
            {
                _BarcodeLeft = value;
                NotifyPropertyChanged(nameof(BarcodeLeft));
            }
        }

        private double _BarcodeTop = 35.0;
        public double BarcodeTop
        {
            get => (double)_BarcodeTop;
            set
            {
                _BarcodeTop = value;
                NotifyPropertyChanged(nameof(BarcodeTop));
            }
        }


        private double _BarcodeHeight = 151.0;
        public double BarcodeHeight
        {
            get => (double)_BarcodeHeight;
            set
            {
                _BarcodeHeight = value;
                NotifyPropertyChanged(nameof(BarcodeHeight));
             }
        }

        private double _BarcodeWidth = 219.0;
        public double BarcodeWidth
        {
            get => (double)_BarcodeWidth;
            set
            {
                _BarcodeWidth = value;
                NotifyPropertyChanged(nameof(BarcodeWidth));
            }
        }

        private string _FontSelected;
        public string FontSelected
        {
            get => (string)_FontSelected;
            set
            {
                _FontSelected = value;
                NotifyPropertyChanged(nameof(FontSelected));
            }
        }

        private float _FontSizeSelected = 12.0f;
        public float FontSizeSelected
        {
            get => (float)_FontSizeSelected;
            set
            {
                _FontSizeSelected = value;
                NotifyPropertyChanged(nameof(FontSizeSelected));
            }
        }
        //
        // Источники данных
        //
        private FieldTypes _DataSourceFieldType;
        public FieldTypes DataSourceFieldType
        {
            get => (FieldTypes)_DataSourceFieldType;
            set
            {
                _DataSourceFieldType= value;
                NotifyPropertyChanged(nameof(DataSourceFieldType));
            }
        }


        private ObservableCollection<KeyValueItem> _AvailableSources;
        public ObservableCollection<KeyValueItem> AvailableSources
        {
            get => (ObservableCollection<KeyValueItem>)_AvailableSources;
            set 
            {
                _AvailableSources= value;
                NotifyPropertyChanged(nameof(AvailableSources));
            }
        }
        private DataSourceType _DataSourceType =  DataSourceType.None;
        public DataSourceType DataSourceType
        {
            get => (DataSourceType)_DataSourceType;
            set
            {
                _DataSourceType = value;
                NotifyPropertyChanged(nameof(DataSourceType));
                // 1. Call an method to handle logic
                this.ColumnNames = null;
                this.DataSourceName = null;
                this.ColumnName = null;
            }
        }

        private KeyValueItem _ColumnName;
        public KeyValueItem ColumnName
        {
            get => (KeyValueItem)_ColumnName;
            set 
            {
                _ColumnName=value;
                NotifyPropertyChanged(nameof(ColumnName));
                OnColumnNameChanged();
            }
        }
        private void OnColumnNameChanged()
        {
            switch (DataSourceType)
            {
                case DataSourceType.Database:
                    var dataTable = ReportData();
                    if (dataTable != null && ColumnName != null && ColumnName?.Key != null && dataTable.Columns.Contains(ColumnName?.Key))
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
        private DataView _DataSource;
        public DataView DataSource
        {
            get => (DataView)_DataSource;
            set
            {
                _DataSource= value;NotifyPropertyChanged(nameof(DataSource));
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
        private KeyValueItem _DataSourceName;
        public KeyValueItem DataSourceName
        {
            get => (KeyValueItem)_DataSourceName;
            set
            {
                _DataSourceName= value; NotifyPropertyChanged(nameof(DataSourceName));
                OnDataSourceNameChanged();
            }
        }



        private double _Angle=0.0;
        public double Angle
        {
            get => (double)_Angle;
            set 
            {
                _Angle= value;
                NotifyPropertyChanged(nameof(Angle));
            }
        }
        private System.Windows.Media.Color _BarcodeBackground = Colors.Transparent;
        public System.Windows.Media.Color BarcodeBackground
        {
            get => (System.Windows.Media.Color)_BarcodeBackground;
            set
            {
                _BarcodeBackground = value;
                NotifyPropertyChanged(nameof(BarcodeBackground));
            }
        }
        private System.Windows.Media.Color _BarcodeForeground = Colors.Black;
        public System.Windows.Media.Color BarcodeForeground
        {
            get => (System.Windows.Media.Color)_BarcodeForeground;
            set 
            {
                _BarcodeForeground = value; 
                NotifyPropertyChanged(nameof(BarcodeForeground));
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

        private Dictionary<string, List<KeyValueItem>> _ColumnNamesListOfQueries=[];
        public Dictionary<string, List<KeyValueItem>> ColumnNamesListOfQueries
        {
            get => (Dictionary<string, List<KeyValueItem>>)_ColumnNamesListOfQueries;
            set
            {
                _ColumnNamesListOfQueries= value;
                NotifyPropertyChanged(nameof(ColumnNamesListOfQueries));
            }
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

        private BarcodeImageProperties _BarcodeProperties;
        public BarcodeImageProperties BarcodeProperties
        {
            get => (BarcodeImageProperties)_BarcodeProperties;
            set
            {
                _BarcodeProperties = value;
                NotifyPropertyChanged(nameof(BarcodeProperties));
            }
        }
        public BarcodeFunction()
        {
            InitializeComponent();
            BarcodeProperties= new();
            BarcodeFormatCollection = new();
            foreach (BarcodeFormat barcodeFormat in Enum.GetValues(typeof(BarcodeFormat)))
            {
                BarcodeFormatCollection.Add(barcodeFormat);
            }
            GetColumnNamesOfAllQuerries();
            this.DataContext = this;
        }
        public BarcodeFunction(string jsonBarcodeProperties)
        {
            if (string.IsNullOrEmpty(jsonBarcodeProperties))
            {
                InitializeComponent();
                return;
            }
            BarcodeProperties = JsonHelper.Deserialize<BarcodeImageProperties>(jsonBarcodeProperties);

            InitializeComponent();
            functionTitle.Content = "Редактирование свойств штрихкода";
            btSave.Content = "Сохранить изменения"; 
            BarcodeFormatCollection = new();
            foreach (BarcodeFormat barcodeFormat in Enum.GetValues(typeof(BarcodeFormat)))
            {
                BarcodeFormatCollection.Add(barcodeFormat);
            }
            GetColumnNamesOfAllQuerries();
            BarcodeLeft = BarcodeProperties.Left;
            BarcodeTop = BarcodeProperties.Top;
            BarcodeHeight = BarcodeProperties.Height;
            BarcodeWidth = BarcodeProperties.Width;
            BarcodeFormatSelected = BarcodeProperties.BarcodeFormat;
            FontSelected = BarcodeProperties.FontFamily;
            FontSizeSelected = (float)BarcodeProperties.FontSize;
            DataSourceType = BarcodeProperties.DataSourceType;
            DataSourceName = AvailableSources.FirstOrDefault(s => s.Key == BarcodeProperties.DataSourceName);
            ColumnName = new KeyValueItem
            {
                Key = BarcodeProperties.DataFieldName,
                SourceType = BarcodeProperties.DataSourceType,
                Value = BarcodeProperties.DataFieldName,
                FieldType = BarcodeProperties.DataSourceFieldType,
                FieldTypeNet = FieldTypeConverter.GetNetType(BarcodeProperties.DataSourceFieldType)
            };
            int index = -1;
            if (ColumnNames != null)
            {
                for (int i = 0; i < ColumnNames.Count; i++)
                {
                    if (ColumnNames[i].Key == this.BarcodeProperties.DataFieldName)
                    {
                        index = i;
                        break;
                    }
                }
            }
            dataFieldName.SelectedIndex = index;
            Angle = BarcodeProperties.Angle;
            Barcode = BarcodeProperties.Barcode;
            BarcodeBackground = BarcodeProperties.BarcodeBackground;
            BarcodeForeground = BarcodeProperties.BarcodeForeground;
            this.DataContext = this;
        }
        private string _Barcode;
        public string Barcode
        {
            get => _Barcode;
            set
            {
                if (value != _Barcode)
                {
                    _Barcode = value;
                    NotifyPropertyChanged(nameof(Barcode));
                }
            }
        }
        private void Button_Click_OK(object sender, RoutedEventArgs e)
        {

            BarcodeProperties.Left = BarcodeLeft;
            BarcodeProperties.Top = BarcodeTop;
            BarcodeProperties.Height = BarcodeHeight;
            BarcodeProperties.Width = BarcodeWidth;
            BarcodeProperties.BarcodeFormat = BarcodeFormatSelected;
            BarcodeProperties.FontFamily = FontSelected;
            BarcodeProperties.FontSize = FontSizeSelected;
            BarcodeProperties.DataSourceType = DataSourceType;
            BarcodeProperties.DataSourceName = (DataSourceType == null || DataSourceType == DataSourceType.None || DataSourceName==null) ?null: DataSourceName.Key;
            BarcodeProperties.DataFieldName = (DataSourceType == null || DataSourceType == DataSourceType.None||ColumnName==null)?null:ColumnName.Value;
            BarcodeProperties.Angle = Angle;
            BarcodeProperties.Barcode = Barcode;
            BarcodeProperties.BarcodeBackground = BarcodeBackground;
            BarcodeProperties.BarcodeForeground = BarcodeForeground;
            if (DataSourceType != null && DataSourceType != DataSourceType.None && ColumnName != null)
            {
                BarcodeProperties.DataSourceFieldType = ColumnName.FieldType;
            }
            if(string.IsNullOrWhiteSpace(BarcodeProperties.DataSourceName))
            {
                BarcodeProperties.DataSourceFieldType = FieldTypes.None;
                BarcodeProperties.DataFieldName = null;
                BarcodeProperties.DataSourceType = DataSourceType.None;
            }
            var json = JsonHelper.Serialize<BarcodeImageProperties>(BarcodeProperties);
            OnReturn(new ReturnEventArgs<string>(json));
        }
        private void Button_Click_Cancel(object sender, RoutedEventArgs e)
        {
            OnReturn(new ReturnEventArgs<string>(string.Empty));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click_View(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click_BarcodeImage(object sender, RoutedEventArgs e)
        {

        }

        private void OnNavigateButtonClick(object sender, RoutedEventArgs e)
        {

            MenuItem menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                ContextMenu contextMenu = menuItem.Parent as ContextMenu;
                if (contextMenu != null)
                {
                    if (contextMenu.PlacementTarget != null)
                    {
                        TextBox owner = contextMenu.PlacementTarget as TextBox;
                        if (owner == null)
                        {
                            return;
                        }
                        var navigationWindow = new NavigationWindow();
                        var textFieldValue = new TextFieldValue()
                        {
                            FontSize = owner.FontSize,
                            FontFamily = owner.FontFamily.Source,
                            Foreground = ((SolidColorBrush)owner.Foreground).Color,
                            Background = ((SolidColorBrush)owner.Background).Color,
                            Value = owner.Text,
                            Width = owner.Width,
                            Height = owner.Height,
                        };
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            Converters = {
                                        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                                      },
                            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
                        };
                        var jsonString = JsonSerializer.Serialize<TextFieldValue>(textFieldValue, options);

                        var textFieldPropertiesEditFunction = new ChangeTextFieldProperties(jsonString);
                        textFieldPropertiesEditFunction.Return += new ReturnEventHandler<string>(GetTextFieldPropertiesEditFunction_Returned);
                        this.NavigationService.Navigate(textFieldPropertiesEditFunction);
                    }

                }
            }
        }

        private void GetTextFieldPropertiesEditFunction_Returned(object sender, ReturnEventArgs<string> e)
        {
            var result = e.Result as string;
            if (string.IsNullOrEmpty(result))
            {
                return;
            }

            var textFieldValue = JsonHelper.Deserialize<TextFieldValue>(result);
            if (textFieldValue == null)
            {
                return;
            }
            barcode.FontSize = textFieldValue.FontSize;
            barcode.FontFamily = new FontFamily(textFieldValue.FontFamily);
            barcode.Foreground = new SolidColorBrush(textFieldValue.Foreground);
            barcode.Background = new SolidColorBrush(textFieldValue.Background);
            barcode.Text = textFieldValue.Value;
            barcode.FontStyle = textFieldValue.FontStyle;
            barcode.FontWeight = textFieldValue.FontWeight;
        }

        private void columnDataList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var row = (DataRowView)columnDataList.SelectedItem;
            if (row != null)
            {
                Barcode = (string)row[ColumnName.Key];
            }
        }
    }
}
