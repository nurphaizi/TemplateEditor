using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TemplateEdit;
public class LineAdorner : Adorner
{
    private Thumb startThumb, endThumb;
    private VisualCollection visualChildren;
    private Line adornedLine;

    public LineAdorner(Line adornedElement) : base(adornedElement)
    {
        adornedLine = adornedElement;
        visualChildren = new VisualCollection(this);

        startThumb = CreateThumb();
        endThumb = CreateThumb();

        startThumb.DragDelta += (s, e) =>
        {
            adornedLine.X1 += e.HorizontalChange;
            adornedLine.Y1 += e.VerticalChange;
        };

        endThumb.DragDelta += (s, e) =>
        {
            adornedLine.X2 += e.HorizontalChange;
            adornedLine.Y2 += e.VerticalChange;
        };

        visualChildren.Add(startThumb);
        visualChildren.Add(endThumb);
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
        return finalSize;
    }

    protected override int VisualChildrenCount => visualChildren.Count;
    protected override Visual GetVisualChild(int index) => visualChildren[index];
}
