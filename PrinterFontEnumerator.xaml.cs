using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.IO;
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

namespace TemplateEdit;
/// <summary>
/// Interaction logic for EnumeratePrinterFonts.xaml
/// </summary>
public partial class PrinterFontEnumerator : Page
{
    private void LoadPrinters()
    {
        foreach (string printer in PrinterSettings.InstalledPrinters)
        {
            PrinterComboBox.Items.Add(printer);
        }
        if (PrinterComboBox.Items.Count > 0)
            PrinterComboBox.SelectedIndex = 0;
    }
    public PrinterFontEnumerator()
    {
        InitializeComponent();
        LoadPrinters();
    }
    private void GetFonts_Click(object sender, RoutedEventArgs e)
    {
        string printerName = PrinterComboBox.SelectedItem?.ToString();
        if (string.IsNullOrEmpty(printerName)) return;

        var fonts = PrinterFonts.GetPrinterFonts(printerName);
        FontListBox.Items.Clear();

        string logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "PrinterFonts.log");
        using var writer = new StreamWriter(logPath, append: false);

        foreach (var font in fonts)
        {
            FontListBox.Items.Add(font);
            writer.WriteLine(font);
        }

        MessageBox.Show($"Found {fonts.Count} fonts.\nLogged to:\n{logPath}", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}

