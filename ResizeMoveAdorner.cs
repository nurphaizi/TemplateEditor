using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;


namespace TemplateEdit;
public class ResizeMoveAdorner : Adorner
{
    private VisualCollection visuals;

    private Thumb topLeft, topRight, bottomLeft, bottomRight, moveThumb;

    private const double SIZE = 10;

    public ResizeMoveAdorner(UIElement element) : base(element)
    {
        visuals = new VisualCollection(this);

        // Resize thumbs
        topLeft = CreateThumb(Cursors.SizeNWSE);
        topRight = CreateThumb(Cursors.SizeNESW);
        bottomLeft = CreateThumb(Cursors.SizeNESW);
        bottomRight = CreateThumb(Cursors.SizeNWSE);

        // Move thumb (transparent overlay)
        moveThumb = new Thumb
        {
            Cursor = Cursors.SizeAll,
            Opacity = 0
        };

        // Add visuals
        visuals.Add(topLeft);
        visuals.Add(topRight);
        visuals.Add(bottomLeft);
        visuals.Add(bottomRight);
        visuals.Add(moveThumb);

        // Hook events
        topLeft.DragDelta += (s, e) => Resize(-e.HorizontalChange, -e.VerticalChange, e.HorizontalChange, e.VerticalChange);
        topRight.DragDelta += (s, e) => Resize(0, -e.VerticalChange, e.HorizontalChange, e.VerticalChange);
        bottomLeft.DragDelta += (s, e) => Resize(-e.HorizontalChange, 0, e.HorizontalChange, e.VerticalChange);
        bottomRight.DragDelta += (s, e) => Resize(0, 0, e.HorizontalChange, e.VerticalChange);

        moveThumb.DragDelta += Move;
    }

    private Thumb CreateThumb(Cursor cursor)
    {
        return new Thumb
        {
            Width = SIZE,
            Height = SIZE,
            Background = Brushes.White,
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(1),
            Cursor = cursor
        };
    }

    private void Move(object sender, DragDeltaEventArgs e)
    {
        var element = AdornedElement as FrameworkElement;

        double left = Canvas.GetLeft(element);
        double top = Canvas.GetTop(element);

        Canvas.SetLeft(element, left + e.HorizontalChange);
        Canvas.SetTop(element, top + e.VerticalChange);
    }

    private void Resize(double leftChange, double topChange, double widthChange, double heightChange)
    {
        var element = AdornedElement as FrameworkElement;

        double left = Canvas.GetLeft(element);
        double top = Canvas.GetTop(element);

        double newWidth = element.Width + widthChange;
        double newHeight = element.Height + heightChange;

        if (newWidth > 10)
        {
            element.Width = newWidth;
            Canvas.SetLeft(element, left - leftChange);
        }

        if (newHeight > 10)
        {
            element.Height = newHeight;
            Canvas.SetTop(element, top - topChange);
        }
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var element = AdornedElement as FrameworkElement;

        double w = element.ActualWidth;
        double h = element.ActualHeight;

        // Corners
        topLeft.Arrange(new Rect(-SIZE / 2, -SIZE / 2, SIZE, SIZE));
        topRight.Arrange(new Rect(w - SIZE / 2, -SIZE / 2, SIZE, SIZE));
        bottomLeft.Arrange(new Rect(-SIZE / 2, h - SIZE / 2, SIZE, SIZE));
        bottomRight.Arrange(new Rect(w - SIZE / 2, h - SIZE / 2, SIZE, SIZE));

        // Move area (covers whole element)
        moveThumb.Arrange(new Rect(0, 0, w, h));

        return finalSize;
    }

    protected override int VisualChildrenCount => visuals.Count;

    protected override Visual GetVisualChild(int index) => visuals[index];
}