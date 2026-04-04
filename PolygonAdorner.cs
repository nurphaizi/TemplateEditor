using System;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TemplateEdit;

public class PolygonAdorner : Adorner
{
    private readonly Polygon _polygon;

    private int _dragIndex = -1;
    private int _hoverIndex = -1;
    
    private const double Radius = 6;
    private Point _Start;
    public PolygonAdorner(Polygon polygon) : base(polygon)
    {
        _polygon = polygon;
        IsHitTestVisible = true;

        MouseMove += OnMouseMove;
        MouseDown += OnMouseDown;
        MouseUp += OnMouseUp;
        MouseRightButtonDown += OnRightClick;
    }

    // ================= RENDER =================
    protected override void OnRender(DrawingContext dc)
    {
        var points = _polygon.Points;

        // вершины
        for (int i = 0; i < points.Count; i++)
        {
            Brush fill = i == _hoverIndex ? Brushes.Orange : Brushes.White;

            dc.DrawEllipse(
                fill,
                new Pen(Brushes.Blue, 1),
                points[i],
                Radius,
                Radius);
        }
    }

    // ================= INPUT =================

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        var pos = e.GetPosition(_polygon);

        _dragIndex = HitVertex(pos);

        // ➕ double click → добавить точку
        if (e.ClickCount == 2 && _dragIndex < 0)
        {
            int edge = HitEdge(pos);
            if (edge >= 0)
            {
                _polygon.Points.Insert(edge + 1, pos);
                RefreshPoints();
                InvalidateVisual();
                return;
            }
        }

        if (_dragIndex >= 0)
            CaptureMouse();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(_polygon);

        // hover
        _hoverIndex = HitVertex(pos);

        // drag
        if (_dragIndex >= 0 && e.LeftButton == MouseButtonState.Pressed)
        {
            var pts = _polygon.Points;
            pts[_dragIndex] = pos;
            RefreshPoints();
        }

        InvalidateVisual();
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        _dragIndex = -1;
        ReleaseMouseCapture();
    }

    private void OnRightClick(object sender, MouseButtonEventArgs e)
    {
        var pos = e.GetPosition(_polygon);

        int i = HitVertex(pos);

        if (i >= 0 && _polygon.Points.Count > 3)
        {
            _polygon.Points.RemoveAt(i);
            RefreshPoints();
        }

        InvalidateVisual();
    }

    // ================= HELPERS =================

    private int HitVertex(Point p)
    {
        for (int i = 0; i < _polygon.Points.Count; i++)
        {
            if ((p - _polygon.Points[i]).Length < Radius + 2)
                return i;
        }
        return -1;
    }

    private int HitEdge(Point p)
    {
        var pts = _polygon.Points;

        for (int i = 0; i < pts.Count; i++)
        {
            var a = pts[i];
            var b = pts[(i + 1) % pts.Count];

            if (DistanceToSegment(p, a, b) < Radius)
                return i;
        }
        return -1;
    }

    private double DistanceToSegment(Point p, Point a, Point b)
    {
        var ap = p - a;
        var ab = b - a;

        double ab2 = ab.X * ab.X + ab.Y * ab.Y;
        double t = Math.Max(0, Math.Min(1, (ap.X * ab.X + ap.Y * ab.Y) / ab2));

        var proj = new Point(a.X + ab.X * t, a.Y + ab.Y * t);
        return (proj - p).Length;
    }

    private void RefreshPoints()
    {
        // важно для обновления UI
        _polygon.Points = new PointCollection(_polygon.Points);
    }
}