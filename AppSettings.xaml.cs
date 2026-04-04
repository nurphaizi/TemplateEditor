using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Printing;
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
using ZXing.QrCode;



namespace TemplateEdit;
/// <summary>
/// Interaction logic for AppSettings.xaml
/// </summary>


public partial class AppSettings : Page, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    public NavigationWindow NavigationWindow
    {
        get;
        set;
    }
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
    private string _WBImagesLogFiles;
    public string WBImagesLogFiles
    {
        get => _WBImagesLogFiles;
        set
        {
            if (_WBImagesLogFiles != value)
            {
                _WBImagesLogFiles = value;
                NotifyPropertyChanged();
            }
        }
    }
    private string _DbFileName;
    public string DbFileName
    {
        get => _DbFileName;
        set
        {
            if (_DbFileName != value)
            {
                _DbFileName = value;
                NotifyPropertyChanged();
            }
        }
    }
    // Список установленных принтеров
    private ObservableCollection<String> _InstalledPrinters=new();
    public ObservableCollection<String> InstalledPrinters
    {
        get=>_InstalledPrinters;
        set
        {
            if (_InstalledPrinters != value)
            {
                _InstalledPrinters = value;
                NotifyPropertyChanged();
            }
        }
    }

    //Штрих код
    private String _BarcodePrinter;
    public String BarcodePrinter
    {
        get => _BarcodePrinter;
        set
        {
            if (_BarcodePrinter != value)
            {
                _BarcodePrinter = value;
                if (!string.IsNullOrWhiteSpace(value)) BarcodePrintQueue = PagePrint.GetPrintQueue(value);
                BarcodePrintSettings = new PrintSettings();
                NotifyPropertyChanged();
            }
        }
    }

    private PrintQueue _BarcodePrintQueue;
    public PrintQueue BarcodePrintQueue
    {
        get => _BarcodePrintQueue;
        set
        {
            if (BarcodePrintQueue != value)
            {
                _BarcodePrintQueue = value;
                if (value != null)
                {
                    BarcodePrintPageMediaSizeCapability = value.GetPageMediaSizeCapabality();
                    BarcodePrintPageResolutionCapability = value.GetPageResolutionCapabality();
                    BarcodePrintPageOrientationCapability = value.GetPrintPageOrientationCapabality();
                }
                NotifyPropertyChanged();
            }
        }
    }
    private TemplateEdit.PrintSettings _BarcodePrintSettings;
    public TemplateEdit.PrintSettings BarcodePrintSettings
    {
        get => _BarcodePrintSettings;
        set
        {
            if (_BarcodePrintSettings != value)
            {
                
                _BarcodePrintSettings = value;
                NotifyPropertyChanged();
            }
        }
    }

    private ObservableCollection<CustomPageMediaSize> _BarcodePrintPageMediaSizeCapability;
    public ObservableCollection<CustomPageMediaSize> BarcodePrintPageMediaSizeCapability
    {
        get => _BarcodePrintPageMediaSizeCapability; set
        {
            if (_BarcodePrintPageMediaSizeCapability != value)
            {
                _BarcodePrintPageMediaSizeCapability = value;
                NotifyPropertyChanged();
            }
        }
    }
    private ObservableCollection<CustomPageOrientation> _BarcodePrintPageOrientationCapability;
    public ObservableCollection<CustomPageOrientation> BarcodePrintPageOrientationCapability
    {
        get => _BarcodePrintPageOrientationCapability; set
        {
            if (_BarcodePrintPageOrientationCapability != value)
            {
                _BarcodePrintPageOrientationCapability = value;
                NotifyPropertyChanged();
            }
        }
    }
    private ObservableCollection<CustomPageResolution> _BarcodePrintPageResolutionCapability;
    public ObservableCollection<CustomPageResolution> BarcodePrintPageResolutionCapability
    {
        get => _BarcodePrintPageResolutionCapability; set
        {
            if (_BarcodePrintPageResolutionCapability != value)
            {
                _BarcodePrintPageResolutionCapability = value;
                NotifyPropertyChanged();
            }
        }
    }

    public AppSettings()
    {
        InitializeComponent();
        DataContext = this;


    }
    public Boolean Compare(string first, string second)
    {
        var result =  string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
        return result;
    }
    public AppSettings(ref NavigationWindow navigationWindow)
    {
        this.NavigationWindow = navigationWindow;
        InitializeComponent();

        _InstalledPrinters = new ObservableCollection<string>();
        PrinterSettings.InstalledPrinters.Cast<string>().ToList().ForEach(printerName =>
        {
            Log.Information($"Добавление принтера {printerName}");
            _InstalledPrinters.Add(printerName);
        });
        NotifyPropertyChanged("InstalledPrinters");

        try
        {
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.barcodePrinter)) BarcodePrinter = Properties.Settings.Default.barcodePrinter;
            if (!string.IsNullOrWhiteSpace(BarcodePrinter) && !string.IsNullOrWhiteSpace(Properties.Settings.Default.BarcodePrintSettings))
            {
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(Properties.Settings.Default.BarcodePrintSettings)))
                {
                    try
                    {
                        ms.Position = 0;
                        BarcodePrintSettings = new PrintSettings(ms);
                        if (BarcodePrintSettings == null)
                        {
                            Log.Error(" Barcode print ticket is null, using default settings");
                            BarcodePrintSettings = new PrintSettings();
                        }
                        else
                        {
                            try
                            {
                                //          barcode_printer.SelectedIndex = InstalledPrinters.IndexOf(InstalledPrinters.FirstOrDefault(item => item==BarcodePrinter));
                                //barcode_pageSize.SelectedIndex = BarcodePrintPageMediaSizeCapability.IndexOf(BarcodePrintPageMediaSizeCapability.FirstOrDefault(item => item.ToString()== BarcodePrintSettings.PageMediaSize.ToString()));
                                //barcode_pageResolution.SelectedIndex = BarcodePrintPageResolutionCapability.IndexOf(BarcodePrintPageResolutionCapability.FirstOrDefault(item => item.ToString() == BarcodePrintSettings.PageResolution.ToString()));
                              //  barcode_pageOrientation.SelectedIndex = BarcodePrintPageOrientationCapability.IndexOf(BarcodePrintPageOrientationCapability.FirstOrDefault(item => item.ToString() == BarcodePrintSettings.PageOrientation.ToString()));
                            }
                            catch
                            {
                                barcode_printer.SelectedIndex = -1;
                                barcode_printer.Text = null;
                            }
                            Log.Information($" Barcode print ticket loaded successfully for printer {BarcodePrinter}");
                        }
                    }
                    catch (ArgumentNullException arg)
                    {
                        Log.Error($" Barcode print ticket {arg.Message}");
                        BarcodePrintSettings = new PrintSettings();
                    }
                    catch (FormatException frm)
                    {
                        Log.Error($" Barcode print ticket {frm.Message}");
                        BarcodePrintSettings = new PrintSettings();
                    }
                    catch (Exception ex)
                    {
                        Log.Error($" Barcode print ticket {ex.Message}");
                        BarcodePrintSettings = new PrintSettings();
                    }
                }
            }
            WBImagesLogFiles = Properties.Settings.Default.WBImagesLogFiles;
            DbFileName = Properties.Settings.Default.dbFileName;
        }
        catch (Exception ex) {
            Log.Error(ex, "Ошибка при инициализации принтеров");
        }
        DataContext = this;
    }
    private void Button_Click(object sender, RoutedEventArgs e)
    {
        NavigationWindow.Close();
    }

    private void btSaveAndClose_Click(object sender, RoutedEventArgs e)
    {
        Properties.Settings.Default.barcodePrinter = BarcodePrinter;
        Properties.Settings.Default.dbFileName = DbFileName;
        Properties.Settings.Default.WBImagesLogFiles = WBImagesLogFiles;
        using (var ms = new MemoryStream())
        {
            BarcodePrintSettings.SaveTo(ms);
            ms.Position = 0;
            Properties.Settings.Default.BarcodePrintSettings = Encoding.UTF8.GetString(ms.ToArray());
        }
        Properties.Settings.Default.Save();
        var currentExecutablePath = Environment.ProcessPath;
        Process.Start(currentExecutablePath);
        Application.Current.Shutdown();
        NavigationWindow.Close();
    }
    private void Button_Click_1(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "База данных (*.sqlite)|*.sqlite|Все файлы (*.*)|*.*",
            Title = "Выберите файл базы данных"
        };
        if (openFileDialog.ShowDialog() == true)
        {
            var name = sender is Button button ? button.Name : "";

            Log.Information($"Пользователь выбрал файл базы данных {openFileDialog.FileName} через кнопку {name}");
            if (name == "btDbFile")
            {
                DbFileName = openFileDialog.FileName;
                return;
            }
            if (name == "btWBImagesLogFiles")
            {
                WBImagesLogFiles = openFileDialog.FileName;
                return;
            }

        }

    }
        
}
