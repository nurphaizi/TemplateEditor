// =========================================
// Figma-like Polygon EditorControl PRO (WPF)
// =========================================
// Added:
// - Multi-selection (Shift)
// - Rubber-band selection
// - Snap-to-grid
// - Resize handles
// - Basic Zoom/Pan
// - Tool system (Move, Vertex, Selection)
// =========================================

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using FigmaLikeEditor;

namespace FigmaLikeEditor
{
    // ================= MODEL =================
    public class PolygonModel
    {
        public ObservableCollection<Point> Points { get; set; } = new();
        public bool IsSelected { get; set; }

        public Rect Bounds
        {
            get
            {
                if (Points.Count == 0) return Rect.Empty;
                double minX = Points.Min(p => p.X);
                double minY = Points.Min(p => p.Y);
                double maxX = Points.Max(p => p.X);
                double maxY = Points.Max(p => p.Y);
                return new Rect(minX, minY, maxX - minX, maxY - minY);
            }
        }

        public void Move(Vector delta)
        {
            for (int i = 0; i < Points.Count; i++)
                Points[i] += delta;
        }

        public void Scale(Rect bounds, Vector scale)
        {
            for (int i = 0; i < Points.Count; i++)
            {
                var p = Points[i];
                double x = bounds.X + (p.X - bounds.X) * scale.X;
                double y = bounds.Y + (p.Y - bounds.Y) * scale.Y;
                Points[i] = new Point(x, y);
            }
        }
    }

    // ================= TOOL SYSTEM =================
    public interface ITool
    {
        void MouseDown(EditorControl e, Point p, int clickCount);
        void MouseMove(EditorControl e, Point p);
        void MouseUp(EditorControl e, Point p);
    }

    public class MoveTool : ITool
    {
        private Point _last;

        public void MouseDown(EditorControl e, Point p, int clickCount) => _last = p;

        public void MouseMove(EditorControl e, Point p)
        {
            var delta = p - _last;
            foreach (var s in e.Selected)
                s.Move(delta);
            _last = p;
        }

        public void MouseUp(EditorControl e, Point p) { }
    }

    // 🔥 NEW: Vertex Editing Tool (PRO++)
    public class VertexTool : ITool
    {
        private PolygonModel _shape;
        private int _index = -1;
        private int _hoverIndex = -1;
        private const double HitRadius = 8;

        public void MouseDown(EditorControl e, Point p, int clickCount)
        {
            _shape = e.HitTestShape(p);
            if (_shape == null) return;

            _index = HitVertex(_shape, p);

            // ➕ Add vertex on double click (on edge)
            if (clickCount == 2 && _index < 0)
            {
                int edge = HitEdge(_shape, p);
                if (edge >= 0)
                {
                    _shape.Points.Insert(edge + 1, p);
                    _index = edge + 1;
                }
            }
        }

        public void MouseMove(EditorControl e, Point p)
        {
            if (_shape == null) return;

            // hover
            _hoverIndex = HitVertex(_shape, p);

            // drag vertex
            if (_index >= 0 && Mouse.LeftButton == MouseButtonState.Pressed)
            {
                _shape.Points[_index] = p;
            }

            // 🔗 edge drag (move two points)
            if (_index < 0 && Mouse.LeftButton == MouseButtonState.Pressed)
            {
                int edge = HitEdge(_shape, p);
                if (edge >= 0)
                {
                    var p1 = _shape.Points[edge];
                    var p2 = _shape.Points[(edge + 1) % _shape.Points.Count];
                    var mid = new Point((p1.X + p2.X)/2, (p1.Y + p2.Y)/2);
                    var delta = p - mid;

                    _shape.Points[edge] += delta;
                    _shape.Points[(edge + 1) % _shape.Points.Count] += delta;
                }
            }
        }

        public void MouseUp(EditorControl e, Point p)
        {
            _index = -1;
            _shape = null;
        }

        public void OnRightClick(EditorControl e, Point p, int clickCount)
        {
            var shape = e.HitTestShape(p);
            if (shape == null) return;

            int i = HitVertex(shape, p);
            if (i >= 0 && shape.Points.Count > 3)
                shape.Points.RemoveAt(i);
        }

        private int HitVertex(PolygonModel shape, Point p)
        {
            for (int i = 0; i < shape.Points.Count; i++)
                if ((shape.Points[i] - p).Length < HitRadius)
                    return i;
            return -1;
        }

        private int HitEdge(PolygonModel shape, Point p)
        {
            for (int i = 0; i < shape.Points.Count; i++)
            {
                var a = shape.Points[i];
                var b = shape.Points[(i + 1) % shape.Points.Count];
                if (DistanceToSegment(p, a, b) < HitRadius)
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

        public void DeleteEdge(EditorControl e, Point p)
        {
            var shape = e.HitTestShape(p);
            if (shape == null || shape.Points.Count <= 3)
                return;

            int edge = HitEdge(shape, p);

            if (edge >= 0)
            {
                // удаляем одну из точек ребра
                shape.Points.RemoveAt((edge + 1) % shape.Points.Count);
            }
        }

        public void CollapseEdge(PolygonModel shape, int edge)
        {
            var a = shape.Points[edge];
            var b = shape.Points[(edge + 1) % shape.Points.Count];

            var mid = new Point(
                (a.X + b.X) / 2,
                (a.Y + b.Y) / 2);

            shape.Points[edge] = mid;
            shape.Points.RemoveAt((edge + 1) % shape.Points.Count);
        }
    }

    public class SelectionTool : ITool
    {
        private Point _start;
        public Rect SelectionRect;

        public void MouseDown(EditorControl e, Point p, int clickCount)
        {
            _start = p;
            SelectionRect = new Rect(p, p);
        }

        public void MouseMove(EditorControl e, Point p)
        {
            SelectionRect = new Rect(_start, p);
        }

        public void MouseUp(EditorControl e, Point p)
        {
            e.ClearSelection();
            foreach (var s in e.Shapes)
            {
                if (SelectionRect.IntersectsWith(s.Bounds))
                {
                    s.IsSelected = true;
                    e.Selected.Add(s);
                }
            }
        }
    }


    // ================= TOOL MANAGER =================
    public class ToolManager
    {
        public ITool CurrentTool { get; private set; }

        public ITool MoveTool { get; } = new MoveTool();
        public ITool VertexTool { get; } = new VertexTool();
        public ITool SelectionTool { get; } = new SelectionTool();

        public ToolManager()
        {
            CurrentTool = MoveTool;
        }

        public void SetMove() => CurrentTool = MoveTool;
        public void SetVertex() => CurrentTool = VertexTool;
        public void SetSelection() => CurrentTool = SelectionTool;
    }

    // ================= CONTROL =================
    public class EditorControl : FrameworkElement
    {
        private VisualCollection _visuals;
        private Matrix _matrix = Matrix.Identity;
        public ObservableCollection<PolygonModel> Shapes { get; } = new();
        public ObservableCollection<PolygonModel> Selected { get; } = new();

        private ToolManager _toolManager = new ToolManager();



        public EditorControl()
        {
            _visuals = new VisualCollection(this);
            Focusable = true;

            MouseDown += (s, e) => OnMouseDown(e);
            MouseMove += (s, e) => OnMouseMove(e);
            MouseUp += (s, e) => OnMouseUp(e);
            MouseWheel += OnMouseWheel;
            MouseRightButtonDown += (s, e) =>
            {
                var pos = e.GetPosition(this);

                if (_toolManager.CurrentTool is VertexTool vt)
                {
                    vt.DeleteEdge(this, pos);
                }

                Render();
            };
        }

        // ================= RENDER =================
        public void Render()
        {
            _visuals.Clear();

            foreach (var shape in Shapes)
            {
                var visual = new DrawingVisual();

                using (var dc = visual.RenderOpen())
                {
                    if (shape.Points.Count < 2) continue;

                    var geo = new StreamGeometry();
                    using (var ctx = geo.Open())
                    {
                        ctx.BeginFigure(shape.Points[0], true, true);
                        ctx.PolyLineTo(shape.Points.Skip(1).ToList(), true, true);
                    }

                    dc.PushTransform(new MatrixTransform(_matrix));

                    dc.DrawGeometry(shape.IsSelected ? Brushes.LightBlue : Brushes.LightGray,
                                     new Pen(Brushes.Black, 1), geo);

                    if (shape.IsSelected)
                    {
                        foreach (var p in shape.Points)
                        {
                            dc.DrawRectangle(Brushes.White,
                             new Pen(Brushes.Blue, 1),
                             new Rect(p.X - 4, p.Y - 4, 8, 8));
                        }
                    }
                    dc.Pop();
                }
                _visuals.Add(visual);
            }
        }

        protected override int VisualChildrenCount => _visuals.Count;
        protected override Visual GetVisualChild(int index) => _visuals[index];

        // ================= INPUT =================
        private void OnMouseDown(MouseButtonEventArgs e)
        {
            Focus();
            var pos = e.GetPosition(this);

            var hit = HitTestShape(pos);

            // 🔥 1. ALT → VertexTool
            if (Keyboard.IsKeyDown(Key.LeftAlt))
            {
                _toolManager.SetVertex();
            }
            // 🔲 2. Клик по фигуре → MoveTool
            else if (hit != null)
            {
                SelectSingle(hit);
                _toolManager.SetMove();
            }
            // 🟦 3. Клик по пустому → SelectionTool
            else if (hit == null && !Keyboard.IsKeyDown(Key.LeftShift))
            {
                ClearSelection();
                _toolManager.SetSelection();
                Debug.WriteLine("SelectionTool activated");
            }
       
            _toolManager.CurrentTool.MouseDown(this, pos, e.ClickCount);
            CaptureMouse();
            Render();
        }

        private void OnMouseMove(MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            var pos = e.GetPosition(this);
            pos = Snap(pos);
            _toolManager.CurrentTool.MouseMove(this, pos);
            Render();
        }

        private void OnMouseUp(MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(this);
            _toolManager.CurrentTool.MouseUp(this, pos);
            ReleaseMouseCapture();
            Render();
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            double scale = e.Delta > 0 ? 1.1 : 0.9;
            _matrix.ScaleAt(scale, scale, e.GetPosition(this).X, e.GetPosition(this).Y);
            Render();
        }

        // ================= HELPERS =================
        public PolygonModel HitTestShape(Point pos)
        {
            return Shapes.LastOrDefault(s => s.Bounds.Contains(pos));
        }

        private void SelectSingle(PolygonModel shape)
        {
            ClearSelection();
            shape.IsSelected = true;
            Selected.Add(shape);
        }

        public void ClearSelection()
        {
            foreach (var s in Shapes)
                s.IsSelected = false;
            Selected.Clear();
        }

        private static Point Snap(Point p, double grid = 10)
        {
            return new Point(
                Math.Round(p.X / grid) * grid,
                Math.Round(p.Y / grid) * grid);
        }
    }
}

// ================= USAGE =================
/*
var editor = new EditorControl();
editor.Shapes.Add(new PolygonModel
{
    Points = new ObservableCollection<Point>
    {
        new Point(100,100),
        new Point(200,100),
        new Point(200,200),
        new Point(100,200)
    }
});
editor.Render();
RootGrid.Children.Add(editor);
*/
