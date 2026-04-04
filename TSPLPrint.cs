using System;
using System.Runtime.InteropServices;

namespace WinSDKDemo_TSPL
{
    public class TSPLPrint
    {
        [DllImport("printer.sdk.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern IntPtr InitPrinter(string model);

        [DllImport("printer.sdk.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int ReleasePrinter(IntPtr intPtr);

        [DllImport("printer.sdk.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto)]
        public static extern int OpenPort(IntPtr intPtr, string usb);
        [DllImport("printer.sdk.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto)]
        public static extern int ClosePort(IntPtr intPtr);
        [DllImport("printer.sdk.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int WriteData(IntPtr intPtr, byte[] buffer, int size);

        [DllImport("printer.sdk.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int ReadData(IntPtr intPtr, byte[] buffer, int size);
        [DllImport("printer.sdk.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int TSPL_Text(IntPtr intPtr, int x, int y, string fontName, string content, int rotation = 0, int x_multiplication = 1, int y_multiplication = 1, int alignment = 0);

        [DllImport("printer.sdk.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int TSPL_Print(IntPtr intPtr, int num, int copies);

        [DllImport("printer.sdk.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int TSPL_Bar(IntPtr intPtr, int x, int y, int width, int height);
        [DllImport("printer.sdk.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int TSPL_BarCode(IntPtr intPtr, int x, int y, int barcodeType, string date, int height, int showText, int rotation, int narrow, int wide);

        [DllImport("printer.sdk.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int TSPL_Image(IntPtr intPtr, int x, int y, int mode, string filePath);
        [DllImport("printer.sdk.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int TSPL_Setup(IntPtr intPtr, int printSpeed, int printDensity, int labelWidth, int labelHeight, int labelType, int gapHight, int offset);
        [DllImport("printer.sdk.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int TSPL_ClearBuffer(IntPtr intPtr);
        [DllImport("printer.sdk.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int TSPL_Box(IntPtr intPtr, int x, int y, int x_end, int y_end, int thickness = 1, int radius = 0);

        [DllImport("printer.sdk.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int TSPL_QrCode(IntPtr intPtr, int x, int y, int width, int eccLevel, int mode, int rotate, int model, int mask, string date);

        [DllImport("printer.sdk.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int TSPL_Home(IntPtr intPtr);

        [DllImport("printer.sdk.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int TSPL_GetPrinterStatus(IntPtr intPtr, out int printerStatus);

        [DllImport("printer.sdk.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int TSPL_PDF417(IntPtr intPtr, int x, int y, int width, int height, int rotate, string option, string data);
        [DllImport("printer.sdk.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int TSPL_Block(IntPtr intPtr, int x, int y, int width, int height, string fontName, string data, int rotation, int x_multiplication = 1, int y_multiplication = 1, int alignment = 0);

        [DllImport("printer.sdk.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int TSPL_Dmatrix(IntPtr intPtr, int x, int y, int width, int height, string data, int blockSize = 0, int row = 10, int col = 10);

    }
}
