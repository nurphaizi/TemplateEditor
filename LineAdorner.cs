using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TemplateEdit;
public class LineAdorner : Adorner
{
    private Thumb startThumb, endThumb,middleThumb;
    private VisualCollection visualChildren;
    private Line adornedLine;
    private readonly ScaleTransform? _canvasScale;
    public LineAdorner(Line adornedElement) : base(adornedElement)
    {
        adornedLine = adornedElement;
        visualChildren = new VisualCollection(this);

        startThumb = CreateThumb();
        endThumb = CreateThumb();
        middleThumb = CreateThumb();
        startThumb.DragDelta += (s, e) =>
        {
            var X1 = adornedLine.X1;
            var Y1 = adornedLine.Y1;
            var hDelta = e.HorizontalChange;
            var vDelta = e.VerticalChange;
            X1 += hDelta;
            Y1 += vDelta;
            adornedLine.X1 = X1;
            adornedLine.Y1 = Y1;
        };

        endThumb.DragDelta += (s, e) =>
        {
            var hDelta = e.HorizontalChange;
            var vDelta = e.VerticalChange ;
            var X2 = adornedLine.X2;
            var Y2 = adornedLine.Y2;
            X2 += hDelta;
            Y2 += vDelta;
            adornedLine.X2 = X2;
             adornedLine.Y2 = Y2;
        };
        middleThumb.DragDelta += (s, e) =>
        {
            var hDelta = e.HorizontalChange;
            var vDelta = e.VerticalChange;
            var X1 = adornedLine.X1;
            var Y1 = adornedLine.Y1;
            X1 += hDelta;
            Y1 += vDelta;
            adornedLine.X1 = X1;
            adornedLine.Y1 = Y1;
            var X2 = adornedLine.X2;
            var Y2 = adornedLine.Y2;
            X2 += hDelta;
            Y2 += vDelta;
            adornedLine.X2 = X2;
            adornedLine.Y2 = Y2;
        };

        visualChildren.Add(startThumb);
        visualChildren.Add(endThumb);
        visualChildren.Add(middleThumb);
    }
    
    private Thumb CreateThumb()
    {
        return new Thumb
        {
            Width = 10,
            Height = 10,
            Background = Brushes.Red
        };
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        startThumb.Arrange(new Rect(new Point(adornedLine.X1 - 5, adornedLine.Y1 - 5), startThumb.DesiredSize));
        endThumb.Arrange(new Rect(new Point(adornedLine.X2 - 5, adornedLine.Y2 - 5), endThumb.DesiredSize));
        middleThumb.Arrange(new Rect(new Point((adornedLine.X1+ adornedLine.X2)/2 - 5, (adornedLine.Y1+adornedLine.Y2)/2 - 5), middleThumb.DesiredSize));
        return finalSize;
    }

    protected override int VisualChildrenCount => visualChildren.Count;
    protected override Visual GetVisualChild(int index) => visualChildren[index];
}
