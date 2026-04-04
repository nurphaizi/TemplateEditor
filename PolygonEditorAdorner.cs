using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Input;

namespace TemplateEdit;
public interface IEditorTool
{
    void OnMouseDown(Point pos);
    void OnMouseMove(Point pos);
    void OnMouseUp(Point pos);
}
public class MoveTool : IEditorTool
{
    private SelectionManager _selection;
    private Point _last;

    public MoveTool(SelectionManager selection)
    {
        _selection = selection;
    }

    public void OnMouseDown(Point pos)
    {
        _last = pos;
    }

    public void OnMouseMove(Point pos)
    {
        var delta = pos - _last;

        foreach (var s in _selection.Selected)
            s.Move(delta);

        _last = pos;
    }

    public void OnMouseUp(Point pos)
    {
    }
}
public class VertexTool : IEditorTool
{
    private PolygonModel _target;
    private int _index = -1;

    public VertexTool(PolygonModel target)
    {
        _target = target;
    }

    public void OnMouseDown(Point pos)
    {
        for (int i = 0; i < _target.Points.Count; i++)
        {
            if ((_target.Points[i] - pos).Length < 8)
            {
                _index = i;
                break;
            }
        }
    }

    public void OnMouseMove(Point pos)
    {
        if (_index >= 0)
            _target.Points[_index] = pos;
    }

    public void OnMouseUp(Point pos)
    {
        _index = -1;
    }
}
public class VisualHost : FrameworkElement
{
    private VisualCollection _children;

    public VisualHost()
    {
        _children = new VisualCollection(this);
    }

    public void Render(IEnumerable<PolygonModel> shapes)
    {
        _children.Clear();

        foreach (var shape in shapes)
        {
            var visual = new DrawingVisual();

            using (var dc = visual.RenderOpen())
            {
                var geo = new StreamGeometry();

                using (var ctx = geo.Open())
                {
                    ctx.BeginFigure(shape.Points[0], true, true);
                    ctx.PolyLineTo(shape.Points.Skip(1).ToList(), true, true);
                }

                dc.DrawGeometry(
                    shape.IsSelected ? Brushes.LightBlue : Brushes.LightGray,
                    new Pen(Brushes.Black, 1),
                    geo);
            }

            _children.Add(visual);
        }
    }

    protected override int VisualChildrenCount => _children.Count;
    protected override Visual GetVisualChild(int index) => _children[index];
}

public class PolygonModel
{
    public ObservableCollection<Point> Points { get; set; } = new();

    public bool IsSelected
    {
        get; set;
    }

    public Rect Bounds => CalculateBounds();

    public void Move(Vector delta)
    {
        for (int i = 0; i < Points.Count; i++)
            Points[i] += delta;
    }

    private Rect CalculateBounds()
    {
        if (Points.Count == 0) return Rect.Empty;

        double minX = Points.Min(p => p.X);
        double minY = Points.Min(p => p.Y);
        double maxX = Points.Max(p => p.X);
        double maxY = Points.Max(p => p.Y);

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }
}
public class SelectionManager
{
    public ObservableCollection<PolygonModel> Selected { get; } = new();

    public Rect Bounds =>
        Selected.Aggregate(Rect.Empty, (acc, s) =>
        {
            acc.Union(s.Bounds);
            return acc;
        });
}
public class PolygonEditorAdorner : Adorner
{
    private VisualCollection visuals;
    private List<Thumb> handles = new List<Thumb>();

    public static readonly DependencyProperty LineBrushProperty =
            DependencyProperty.Register(
                "LineBrush",
                typeof(Brush),
                typeof(PolygonEditorAdorner),
                new FrameworkPropertyMetadata(
                    Brushes.Blue,
                    FrameworkPropertyMetadataOptions.AffectsRender));

    private static readonly DependencyProperty PolygonModelProperty =
            DependencyProperty.Register(
                "PolygonModel",
                typeof(PolygonModel),
                typeof(PolygonEditorAdorner),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender));
    public PolygonModel PolygonModel
    {
        get => (PolygonModel)GetValue(PolygonModelProperty);
        set => SetValue(PolygonModelProperty, value);
    }


    public Brush LineBrush
    {
        get => (Brush)GetValue(LineBrushProperty);
        set => SetValue(LineBrushProperty, value);
    }

    public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(
                "StrokeThickness",
                typeof(double),
                typeof(PolygonEditorAdorner),
                new FrameworkPropertyMetadata(
                    1.0,
                    FrameworkPropertyMetadataOptions.AffectsRender));

    public double StrokeThickness
    {
        get => (double)GetValue(StrokeThicknessProperty);
        set=> SetValue(StrokeThicknessProperty, value);
    }

    public ObservableCollection<Point> Points {get;} = new ObservableCollection<Point>();
    private VisualHost _visualHost;
    public VisualHost VisualHost
    { get;set; }
    private SelectionManager _selection;
    public SelectionManager Selection
    {
        get => _selection;
        set
        {
            _selection = value;
            if (VisualHost != null)
                VisualHost.Render(_selection.Selected);
        }
    }
    public PolygonEditorAdorner(UIElement adornedElement, Point[] points,double thickness,Color color)
        : base(adornedElement)
    {

        LineBrush = new SolidColorBrush(color);
        StrokeThickness = thickness;
        PolygonModel = new PolygonModel();

        PolygonModel.Points = new ObservableCollection<Point>();
        if (points != null)
        {
            foreach (var p in points)
            {
                PolygonModel.Points.Add(p);
            }
        }
        PolygonModel.Points.CollectionChanged += (s, e) =>
        {
            InvalidateVisual();
        };
        Selection = new SelectionManager();
        Selection.Selected.Add(PolygonModel);

    }
    public void AddPoint(Point p)
    {
        Points.Add(p);

        Thumb handle = CreateHandle(p);
        handles.Add(handle);
        visuals.Add(handle);

        InvalidateVisual();
    }

    Thumb CreateHandle(Point p)
    {
        Thumb t = new Thumb
        {
            Width = 10,
            Height = 10,
            Background = Brushes.Red,
            Cursor = System.Windows.Input.Cursors.SizeAll
        };

        Canvas.SetLeft(t, p.X - 5);
        Canvas.SetTop(t, p.Y - 5);

        t.DragDelta += (s, e) =>
        {
            int index = handles.IndexOf((Thumb)s);

            Point pt = Points[index];
            pt.X += e.HorizontalChange;
            pt.Y += e.VerticalChange;

            Points[index] = pt;

            Canvas.SetLeft(t, pt.X - 5);
            Canvas.SetTop(t, pt.Y - 5);

            InvalidateVisual();
        };

        return t;
    }

    protected override void OnRender(DrawingContext dc)
    {
        VisualHost?.Render(Selection.Selected);
        if (Points.Count < 2)
            return;

        Pen pen = new Pen(LineBrush, StrokeThickness);

        for (int i = 0; i < Points.Count - 1; i++)
            dc.DrawLine(pen, Points[i], Points[i + 1]);

        if (Points.Count > 2)
            dc.DrawLine(pen, Points[Points.Count - 1], Points[0]);
    }

    protected override int VisualChildrenCount => visuals.Count;

    protected override Visual GetVisualChild(int index)
    {
        return visuals[index];
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (Thumb t in handles)
            t.Arrange(new Rect(
                Canvas.GetLeft(t),
                Canvas.GetTop(t),
                t.Width,
                t.Height));

        return finalSize;
    }
    public Point[] GetPoints() => Points.ToArray<Point>();
}
