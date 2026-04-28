using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace TemplateEdit;
public class RulerCursorAdorner : Adorner
{
    public double X
    {
        get; set;
    }
    public double Y
    {
        get; set;
    }

    public RulerCursorAdorner(UIElement adornedElement)
        : base(adornedElement)
    {
        IsHitTestVisible = false;
    }

    protected override void OnRender(DrawingContext dc)
    {
        Pen pen = new Pen(Brushes.Red, 1);

        // Вертикальная линия
        dc.DrawLine(pen, new Point(X, 0), new Point(X, AdornedElement.RenderSize.Height));

        // Горизонтальная линия
        dc.DrawLine(pen, new Point(0, Y), new Point(AdornedElement.RenderSize.Width, Y));
    }
}
public class HorizontalRulerCursorAdorner : Adorner
{
    public double X
    {
        get; set;
    }
  
    public HorizontalRulerCursorAdorner(UIElement adornedElement)
        : base(adornedElement)
    {
        IsHitTestVisible = false;
    }
    protected override void OnRender(DrawingContext dc)
    {
        Pen pen = new Pen(Brushes.Red, 1);
        // Горизонтальная линия
        dc.DrawLine(pen, new Point(X, 0), new Point(X,AdornedElement.RenderSize.Height));
    }
}
public class VerticalRulerCursorAdorner : Adorner
{
  
    public double Y
    {
        get; set;
    }
    public VerticalRulerCursorAdorner(UIElement adornedElement)
        : base(adornedElement)
    {
        IsHitTestVisible = false;
    }
    protected override void OnRender(DrawingContext dc)
    {
        Pen pen = new Pen(Brushes.Red, 1);
        // Вертикальная линия
        dc.DrawLine(pen, new Point(0, Y), new Point(AdornedElement.RenderSize.Width, Y));
    }
}

//универсальный класс RulerAdorner

public class RulerAdorner : Adorner
{
    public double Position
    {
        get; set;
    }
    public Orientation Orientation
    {
        get;
    }

    private readonly Pen _pen = new Pen(Brushes.Red, 1);

    public RulerAdorner(UIElement adornedElement, Orientation orientation)
        : base(adornedElement)
    {
        Orientation = orientation;
        IsHitTestVisible = false;
    }

    protected override void OnRender(DrawingContext dc)
    {
        double w = AdornedElement.RenderSize.Width;
        double h = AdornedElement.RenderSize.Height;

        if (Orientation == Orientation.Horizontal)
        {
            // Вертикальная линия на горизонтальной линейке
            dc.DrawLine(_pen, new Point(Position, 0), new Point(Position, h));
        }
        else
        {
            // Горизонтальная линия на вертикальной линейке
            dc.DrawLine(_pen, new Point(0, Position), new Point(w, Position));
        }
    }
}

