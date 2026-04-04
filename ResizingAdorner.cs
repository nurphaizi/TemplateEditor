using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SharpVectors.Dom;

namespace TemplateEdit;
public class ResizingAdorner : Adorner
{
    // Resizing adorner uses Thumbs for visual elements.  
    // The Thumbs have built-in mouse input handling.
    //Thumb topLeft, topRight, bottomLeft, bottomRight;

    private Thumb startThumb;
    private Thumb endThumb;
    private Line selectedLine;
    private Point startPoint;
    private Point endPoint;

    // To store and manage the adorner's visual children.
    VisualCollection visualChildren;
    bool IsControlModeOn = false;
    // Override the VisualChildrenCount and GetVisualChild properties to interface with 
    // the adorner's visual collection.
    private Point origin;

    private void SelectedLineOnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;

        Line line = (Line)sender;
        line.CaptureMouse();

        startPoint = new Point(line.X1, line.Y1);
        endPoint = new Point(line.X2, line.Y2);

        origin = e.GetPosition(line);

        base.OnMouseLeftButtonDown(e);
    }

    private void SelectedLineOnMouseMove(object sender, MouseEventArgs e)
    {
        base.OnMouseMove(e);
        Line line = (Line)sender;
        if (e.MouseDevice.LeftButton == MouseButtonState.Pressed)
        {
            var parent = VisualTreeHelper.GetParent(line) as FrameworkElement;

            Point positionOnParent = e.GetPosition(parent);
            if(positionOnParent.X < 0 || positionOnParent.Y < 0 || positionOnParent.X > parent.ActualWidth || positionOnParent.Y > parent.ActualHeight)
            {
                line.ReleaseMouseCapture();
                return;
            }   
            Point position = e.GetPosition(this);
            e.Handled = true;
            double horizontalDelta =position.X - origin.X;
            double verticalDelta = position.Y - origin.Y;
            line.X1 = startPoint.X + horizontalDelta;
            line.X2 = endPoint.X + horizontalDelta;
            line.Y1 = startPoint.Y + verticalDelta;
            line.Y2 = endPoint.Y + verticalDelta;
            InvalidateArrange();
        }
        else
        {
            line.ReleaseMouseCapture();
        }
    }

    private void SelectedLineOnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        Line line = (Line)sender;
        line.ReleaseMouseCapture();
        e.Handled = true;

        base.OnMouseLeftButtonUp(e);
    }
    protected override int VisualChildrenCount
    {
        get
        {
            return visualChildren.Count;
        }
    }
    protected override Visual GetVisualChild(int index)
    {
        return visualChildren[index];
    }
    public void BuildAdornerCorners(ref Thumb cornerThumb, Cursor customizedCursors)
    {
        //adding new thumbs for adorner to visual childern collection
        if (cornerThumb != null) return;
        cornerThumb = new Thumb() { Cursor = customizedCursors, Height = 10, Width = 10, Opacity = 0.5, Background = new SolidColorBrush(Colors.Red) };
        visualChildren.Add(cornerThumb);
    }

    // Initialize the ResizingAdorner.
    public ResizingAdorner(UIElement adornedElement)
        : base(adornedElement)
    {
        visualChildren = new VisualCollection(this);
        selectedLine = AdornedElement as Line;
        BuildAdornerCorners(ref startThumb, Cursors.SizeNWSE);
        BuildAdornerCorners(ref endThumb, Cursors.SizeNWSE);
        startThumb.DragDelta += StartDragDelta;
        endThumb.DragDelta += EndDragDelta;

        startThumb.DragCompleted += new DragCompletedEventHandler(startThumb_DragCompleted);
        endThumb.DragCompleted += new DragCompletedEventHandler(endThumb_DragCompleted);
        selectedLine.MouseLeftButtonDown += SelectedLineOnMouseLeftButtonDown;
        selectedLine.MouseMove += SelectedLineOnMouseMove;
        selectedLine.MouseLeftButtonUp += SelectedLineOnMouseLeftButtonUp;
    }


    public event EndDragDeltaEvent endDragDeltaEvent;
    public delegate void EndDragDeltaEvent(object obj, DragCompletedEventArgs e, bool isEnd);

    void startThumb_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (endDragDeltaEvent != null)
            endDragDeltaEvent(selectedLine, e, false);
    }

    void endThumb_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (endDragDeltaEvent != null)
            endDragDeltaEvent(selectedLine, e, true);
    }

    // Arrange the Adorners.
    protected override Size ArrangeOverride(Size finalSize)
    {
        selectedLine = AdornedElement as Line;

        double left = Math.Min(selectedLine.X1, selectedLine.X2);
        double top = Math.Min(selectedLine.Y1, selectedLine.Y2);

        var startRect = new Rect(selectedLine.X1 - (startThumb.Width / 2), selectedLine.Y1 - (startThumb.Width / 2), startThumb.Width, startThumb.Height);
        startThumb.Arrange(startRect);

        var endRect = new Rect(selectedLine.X2 - (endThumb.Width / 2), selectedLine.Y2 - (endThumb.Height / 2), endThumb.Width, endThumb.Height);
        endThumb.Arrange(endRect);

        return finalSize;
    }

    private void StartDragDelta(object sender, DragDeltaEventArgs e)
    {
        Point position = Mouse.GetPosition(this);

        selectedLine.X1 = position.X;
        selectedLine.Y1 = position.Y;
    }

    // Event for the Thumb End Point
    private void EndDragDelta(object sender, DragDeltaEventArgs e)
    {
        Point position = Mouse.GetPosition(this);

        selectedLine.X2 = position.X;
        selectedLine.Y2 = position.Y;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        if (AdornedElement is Line)
        {
            selectedLine = AdornedElement as Line;
            startPoint = new Point(selectedLine.X1, selectedLine.Y1);
            endPoint = new Point(selectedLine.X2, selectedLine.Y2);
        }
    }
}