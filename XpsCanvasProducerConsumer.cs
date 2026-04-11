using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;

namespace TemplateEdit;
public class XpsCanvasProducerConsumer
{
    private readonly BlockingCollection<Canvas> _queue = new BlockingCollection<Canvas>(boundedCapacity: 50); // ограничение буфера

    // Producer: добавляет Canvas в очередь
    public void Produce(Canvas canvas)
    {
        if (canvas == null) return;
        _queue.Add(canvas); // блокирующий вызов, если очередь полная
    }

    // Producer: сигнализирует, что больше Canvas не будет
    public void CompleteProducing()
    {
        _queue.CompleteAdding();
    }

    // Consumer: обрабатывает все Canvas и сохраняет в XPS
    public async Task ConsumeAndSaveAsync(string xpsFilePath,
        double pageWidth = 793.7,      // A4
        double pageHeight = 1122.5,
        double margin = 30)
    {
        using (XpsDocument xpsDoc = new XpsDocument(xpsFilePath, FileAccess.ReadWrite))
        {
            XpsDocumentWriter writer = XpsDocument.CreateXpsDocumentWriter(xpsDoc);

            FixedPage currentPage = null;
            double currentY = 0;

            // GetConsumingEnumerable() автоматически завершается, когда CompleteAdding() вызван
            foreach (var originalCanvas in _queue.GetConsumingEnumerable())
            {
                Canvas canvasCopy = CloneCanvas(originalCanvas);

                // Если не влезает — сохраняем текущую страницу и создаём новую
                if (currentPage == null || currentY + canvasCopy.Height + margin * 2 > pageHeight)
                {
                    if (currentPage != null)
                    {
                        FinalizeAndWritePage(currentPage, writer, pageWidth, pageHeight);
                    }

                    currentPage = CreateNewPage(pageWidth, pageHeight);
                    currentY = margin;
                }

                FixedPage.SetLeft(canvasCopy, margin);
                FixedPage.SetTop(canvasCopy, currentY);
                currentPage.Children.Add(canvasCopy);

                currentY += canvasCopy.Height + margin;
            }

            // Сохраняем последнюю страницу
            if (currentPage != null)
            {
                FinalizeAndWritePage(currentPage, writer, pageWidth, pageHeight);
            }
        }

        Console.WriteLine($"XPS документ создан: {xpsFilePath}");
    }

    private static FixedPage CreateNewPage(double width, double height)
    {
        return new FixedPage { Width = width, Height = height, Background = Brushes.White };
    }

    private static void FinalizeAndWritePage(FixedPage page, XpsDocumentWriter writer, double width, double height)
    {
        page.Measure(new Size(width, height));
        page.Arrange(new Rect(0, 0, width, height));
        page.UpdateLayout();
        writer.Write(page);
    }

    private static Canvas CloneCanvas(Canvas original)
    {
        string xaml = System.Windows.Markup.XamlWriter.Save(original);
        return (Canvas)System.Windows.Markup.XamlReader.Parse(xaml);
    }
}