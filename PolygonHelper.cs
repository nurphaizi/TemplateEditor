using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Shapes;

namespace TemplateEdit;
public static class PolygonHelper
{
    public static readonly DependencyProperty ShowTopLeftToolTipProperty =
        DependencyProperty.RegisterAttached(
            "ShowTopLeftToolTip",
            typeof(bool),
            typeof(PolygonHelper),
            new PropertyMetadata(false, OnChanged));

    public static void SetShowTopLeftToolTip(DependencyObject obj, bool value)
        => obj.SetValue(ShowTopLeftToolTipProperty, value);

    public static bool GetShowTopLeftToolTip(DependencyObject obj)
        => (bool)obj.GetValue(ShowTopLeftToolTipProperty);

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Polygon polygon && (bool)e.NewValue)
        {
            polygon.MouseMove += (s, _) =>
            {
                if (polygon.Points.Count == 0) return;

                double minX = polygon.Points.Min(p => p.X);
                double minY = polygon.Points.Min(p => p.Y);

                polygon.ToolTip = $"Top-Left: ({minX:0}, {minY:0})";
            };
        }
    }
}
