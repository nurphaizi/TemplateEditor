using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xceed.Wpf.Toolkit.Converters;
using Xceed.Wpf.Toolkit.Core.Converters;

namespace TemplateEdit;
/// <summary>
/// Interaction logic for SetRectangleProperties.xaml
/// </summary>


public partial class SetRectangleProperties : PageFunction<String>, INotifyPropertyChanged
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
    private RectangleFigureProperties _RectangleFigure;
    public RectangleFigureProperties RectangleFigure
    {
        get => (RectangleFigureProperties)_RectangleFigure;
        set
        {
            _RectangleFigure = value ;
            NotifyPropertyChanged(nameof(RectangleFigure));
        }
    }

    public NavigationWindow NavigationWindow
    {
        get; set;
    }
    public SetRectangleProperties()
    {
        InitializeComponent();
        this.DataContext = this;
    }


    public SetRectangleProperties( RectangleFigureProperties rectangleFigure)
    {
        InitializeComponent();
        this.RectangleFigure = rectangleFigure;
        this.DataContext = this;
    }

   

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        OnReturn(new ReturnEventArgs<string>(JsonHelper.Serialize<RectangleFigureProperties>(RectangleFigure)));
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        OnReturn(new ReturnEventArgs<string>(null));
    }
}