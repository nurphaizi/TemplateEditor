using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace TemplateEdit;
/// <summary>
/// Interaction logic for SetLineProperties.xaml
/// </summary>
public partial class SetLineProperties : PageFunction<String>, INotifyPropertyChanged
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

    private LineProperties _LineProperties;
    public LineProperties LineProperties
    {
        get => (LineProperties)_LineProperties;
        set
        {
            _LineProperties= value;
            NotifyPropertyChanged(nameof(LineProperties));
        }
    }

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

    public AdornerLayer adornerLayer;
    public SetLineProperties(LineProperties lineProperties,Size pageSize)
    {
        LineProperties = lineProperties;
        PageSize = pageSize;
        InitializeComponent();
        DataContext = this;
    }

    private void Button_OK_Click(object sender, RoutedEventArgs e)
    {
        OnReturn(new ReturnEventArgs<string>(JsonHelper.Serialize<LineProperties>(LineProperties)));
    }

    private void Button_Cancel_Click(object sender, RoutedEventArgs e)
    {
        OnReturn(new ReturnEventArgs<string>(null));
    }

    private void canvas_Loaded(object sender, RoutedEventArgs e)
    {
        adornerLayer = AdornerLayer.GetAdornerLayer(line);
        adornerLayer.Add(new ResizingAdorner(line));

    }
}
