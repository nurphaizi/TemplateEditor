
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Management;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media;
using Serilog;
using static System.Net.Mime.MediaTypeNames;
using static TemplateEdit.PrinterFonts;
using FontFamily = System.Windows.Media.FontFamily;
namespace TemplateEdit;
public class PrinterPortFinder
{
    public static string GetPrinterPort(string printerName)
    {
        string query = $"SELECT * FROM Win32_Printer WHERE Name = '{printerName.Replace("\\", "\\\\")}'";
        using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
        {
            foreach (ManagementObject printer in searcher.Get())
            {
                return printer["PortName"]?.ToString();
            }
        }
        return null;
    }
}

public class PrinterFonts
{
public delegate int FontEnumProc(ref LOGFONT lpelfe, ref TEXTMETRIC lpntme, uint FontType, IntPtr lParam);

    [DllImport("gdi32.dll")]
    public static extern IntPtr CreateDC(string lpszDriver, string lpszDevice, string lpszOutput, IntPtr lpInitData);


    [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
    public static extern int EnumFontFamiliesEx(
       IntPtr hdc,
       [In] ref LOGFONT lpLogfont,
       EnumFontExDelegate lpEnumFontFamExProc,
       IntPtr lParam,
       uint dwFlags);

    [DllImport("gdi32.dll")]
    public static extern bool DeleteDC(IntPtr hdc);


    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct LOGFONT
    {
        public int lfHeight;
        public int lfWidth;
        public int lfEscapement;
        public int lfOrientation;
        public int lfWeight;
        public byte lfItalic;
        public byte lfUnderline;
        public byte lfStrikeOut;
        public byte lfCharSet;
        public byte lfOutPrecision;
        public byte lfClipPrecision;
        public byte lfQuality;
        public byte lfPitchAndFamily;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string lfFaceName;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TEXTMETRIC
    {
        public int tmHeight, tmAscent, tmDescent, tmInternalLeading, tmExternalLeading;
        public int tmAveCharWidth, tmMaxCharWidth, tmWeight;
        public int tmOverhang, tmDigitizedAspectX, tmDigitizedAspectY;
        public char tmFirstChar, tmLastChar, tmDefaultChar, tmBreakChar;
        public byte tmItalic, tmUnderlined, tmStruckOut, tmPitchAndFamily, tmCharSet;
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct ENUMLOGFONTEX
    {
        public LOGFONT elfLogFont;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string elfFullName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string elfStyle;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string elfScript;
    }
    public delegate int EnumFontExDelegate(ref ENUMLOGFONTEX lpelfe, IntPtr lpntme, int FontType, IntPtr lParam);

    private static StringBuilder fontNames = new StringBuilder();
    private static  List<string> fontNamesList = new();
    private static readonly List<System.Windows.Media.FontFamily> installedFonts = new();
    public static void ListPrinterFonts(string printerName)
    {
        if(string.IsNullOrEmpty(printerName))
        {
            Log.Error("Printer name cannot be null or empty.");
            return;
        }
        using (PrintDocument pd = new PrintDocument())
        {
            pd.PrinterSettings.PrinterName = printerName;
            using (Graphics g = pd.PrinterSettings.CreateMeasurementGraphics())
            {
                IntPtr hdc = g.GetHdc();
                LOGFONT lf = new LOGFONT
                {
                    lfCharSet = 1, // DEFAULT_CHARSET
                    lfFaceName = string.Empty
                };
                EnumFontExDelegate callback = new EnumFontExDelegate(FontEnumCallback);
                EnumFontFamiliesEx(hdc, ref lf, callback, IntPtr.Zero, 0);
                g.ReleaseHdc(hdc);
            }
        }

        Console.WriteLine(fontNames.ToString());
    }
    private static bool IsFontInstalled(string fontName)
    {
        return Fonts.SystemFontFamilies.Any(f => f.Source.Equals(fontName, StringComparison.OrdinalIgnoreCase) || fontName.StartsWith(f.Source));
    }

    private static int FontEnumCallback(ref ENUMLOGFONTEX lpelfe, IntPtr lpntme, int FontType, IntPtr lParam)
    {
        installedFonts.Add(ConvertToFontFamily(lpelfe));
        var elfFullName = lpelfe.elfLogFont.lfFaceName; // elfFullName;
        fontNamesList.Add(elfFullName);
        //if ((FontType & 0x0002) != 0) // DEVICE_FONTTYPE
        //{
        //    var elfFullName = lpelfe.elfLogFont.lfFaceName; // elfFullName;
        //    if (!IsFontInstalled(elfFullName) && (!elfFullName.StartsWith("@")))
        //    {
        //        fontNamesList.Add(elfFullName);
        //    }
        //}
        return 1;
    }

    public static List<System.Windows.Media.FontFamily> GetPrinterFontFamilies(string printerName)
    {
        installedFonts.Clear();
        ListPrinterFonts(printerName);
        return installedFonts;
    }
    public static List<string> GetPrinterFonts(string printerName)
    {
        fontNamesList.Clear();
        ListPrinterFonts(printerName);
        var list = fontNamesList.Distinct().OrderBy(name => name).ToList();
        return list;
    }
    public static FontFamily ConvertToFontFamily(ENUMLOGFONTEX enumLogFontEx)
    {
        // Use the face name from LOGFONT to create a WPF FontFamily
        return new FontFamily(enumLogFontEx.elfLogFont.lfFaceName);
    }
}

