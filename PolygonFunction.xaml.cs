using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ExCSS;
using FigmaLikeEditor;

namespace TemplateEdit;
/// <summary>
/// Interaction logic for PolygonFunction.xaml
/// </summary>
public partial class PolygonFunction : PageFunction<String>, INotifyPropertyChanged
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

    public PolygonProperties Polygon { get; set; }

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
    private PolygonModel _PolygonModel;
    public PolygonModel PolygonModel
    {
        get => (PolygonModel)_PolygonModel;
        set
        {
            _PolygonModel = value;
            NotifyPropertyChanged(nameof(PolygonModel));
        }
    }

    private double _StrokeThickness;
    public double StrokeThickness
    {
        get => (double)_StrokeThickness;
        set
        {
            _StrokeThickness = value;
            NotifyPropertyChanged(nameof(StrokeThickness));
        }
    }
    private System.Windows.Media.Brush _Stroke = new SolidColorBrush(System.Windows.Media.Colors.Black);
    
    public Brush Stroke
    {
        get => (Brush)_Stroke;
        set
        {
            _Stroke =value;
            NotifyPropertyChanged(nameof(Stroke));
        }
    }

    private System.Windows.Media.Brush _Fill = new SolidColorBrush(System.Windows.Media.Colors.Transparent);
    public Brush Fill
    {
        get => (Brush)_Fill;
        set
        {
            _Fill = value;
            NotifyPropertyChanged(nameof(Fill));
        }
    }



    private EditorControl _PolygonEditor;
    public EditorControl PolygonEditor
    {
        get => (EditorControl)_PolygonEditor;
        set
        {
            _PolygonEditor = value;
            NotifyPropertyChanged(nameof(PolygonEditor));
        }
    }
    public PolygonFunction()
    {
        InitializeComponent();
        DataContext = this;
    }

    public PolygonFunction(Size pageSize)
    {
        PageSize = pageSize;
        Polygon = new PolygonProperties()
        {
            StrokeThickness = 1.0,
            Stroke = System.Windows.Media.Colors.Black,
            Foreground = System.Windows.Media.Colors.Black,
            Background = System.Windows.Media.Colors.Transparent,
            Vertices = new System.Windows.Point[]
        {
            new System.Windows.Point(pageSize.Width/3, pageSize.Height / 3),
            new System.Windows.Point(pageSize.Width/3, 2*pageSize.Height/3),
            new System.Windows.Point(2*pageSize.Width/3, 2 * pageSize.Height/3),
        }

        };
        PolygonModel = new PolygonModel 
        {
            Points = new ObservableCollection<System.Windows.Point>(Polygon.Vertices)
        };

        InitializeComponent();
        DataContext = this;
    }
    public PolygonFunction( string polyLines, Size pageSize)
    {
        Polygon = JsonHelper.Deserialize<PolygonProperties>(polyLines);
        PolygonModel = new PolygonModel
        {
            
            Points = new ObservableCollection<System.Windows.Point>(Polygon.Vertices)
        };
        PageSize = pageSize;
        StrokeThickness = Polygon.StrokeThickness;
        Stroke = new SolidColorBrush(Polygon.Stroke);
        Fill = new SolidColorBrush(Polygon.Background);
        InitializeComponent();
        DataContext = this;
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        Polygon.Vertices = PolygonEditor.Shapes.FirstOrDefault().Points.ToArray();
        Polygon.Height = PolygonEditor.Shapes.FirstOrDefault().Bounds.Height;
        Polygon.Width = PolygonEditor.Shapes.FirstOrDefault().Bounds.Width;
        Polygon.Left = PolygonEditor.Shapes.FirstOrDefault().Bounds.Left;
        Polygon.Top = PolygonEditor.Shapes.FirstOrDefault().Bounds.Top;
        Polygon.StrokeThickness = StrokeThickness;
        Polygon.Stroke = (Stroke as SolidColorBrush).Color;
        Polygon.Foreground = (Stroke as SolidColorBrush).Color;
        Polygon.Background = (Fill as SolidColorBrush).Color;
        var result = JsonHelper.Serialize(Polygon);
        OnReturn(new ReturnEventArgs<string>(result));
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        OnReturn(new ReturnEventArgs<string>(String.Empty));
    }
    private void polygonCanvas_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is Canvas canvas)
        {
            borderCanvas.Width = PageSize.Width;
            borderCanvas.Height = PageSize.Height;
            PolygonEditor = new EditorControl();
            PolygonEditor.Shapes.Add(PolygonModel);
            PolygonEditor.Selected.Add(PolygonModel);
            PolygonEditor.Render();
            canvas.Children.Add(PolygonEditor);
        }

    }
    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {

    }
}
