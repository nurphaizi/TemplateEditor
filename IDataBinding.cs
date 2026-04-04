using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace TemplateEdit;
internal interface IDataBinding
{
    public void SetBindingLineProperties(ref Line line);
    public void SetBindingRectangleProperties(ref Rectangle rectangle);
    public void SetBinding(ref TextBox textBox);
    public void BarcodeSetBinding(ref Image barcodeImage);
    public void QRCodeSetBinding(ref Image qrcodeImage);
    public void ImageSetBinding(ref Image image);
}
