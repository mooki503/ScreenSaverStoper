using System.IO;
using System.Text;
using System.Windows;
using System.Timers;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace ScreenSaverStoper;

[StructLayoutAttribute(LayoutKind.Sequential, Pack = 2)]
public struct BITMAPFILEHEADER
{
    public UInt16  bfType;          //2Byte
    public UInt32  bfSize;          //4Byte
    public UInt16  bfReserved1;     //2Byte
    public UInt16  bfReserved2;     //2Byte
    public UInt32  bfOffBits;       //4Byte
};

[StructLayoutAttribute(LayoutKind.Sequential, Pack = 2)]
struct BITMAPINFOHEADER{//全40Byte
    public UInt32 biSize;           //4Byte
    public UInt32 biWidth;          //4Byte
    public UInt32 biHeight;         //4Byte
    public UInt16 biPlanes;         //2Byte
    public UInt16 biBitCount;       //2Byte
    public UInt32 biCompression;    //4Byte
    public UInt32 biSizeImage;      //4Byte
    public UInt32 biXPelsPerMeter;  //4Byte
    public UInt32 biYPelsPerMeter;  //4Byte
    public UInt32 biClrUsed;        //4Byte
    public UInt32 biClrImportant;   //4Byte
};

enum DesiredAccess : uint
  {
   GENERIC_READ = 0x80000000,
   GENERIC_WRITE = 0x40000000,
   GENERIC_EXECUTE = 0x20000000
}

enum ShareMode : uint
  {
   FILE_SHARE_READ = 0x00000001,
   FILE_SHARE_WRITE = 0x00000002,
   FILE_SHARE_DELETE = 0x00000004
}

enum CreationDisposition : uint
  {
   CREATE_NEW = 1,
   CREATE_ALWAYS = 2,
   OPEN_EXISTING = 3,
   OPEN_ALWAYS = 4,
   TRUNCATE_EXISTING = 5
}

enum FlagsAndAttributes : uint
  {
   FILE_ATTRIBUTE_ARCHIVE = 0x00000020,
   FILE_ATTRIBUTE_ENCRYPTED = 0x00004000,
   FILE_ATTRIBUTE_HIDDEN = 0x00000002,
   FILE_ATTRIBUTE_NORMAL = 0x00000080,
   FILE_ATTRIBUTE_NOT_CONTENT_INDEXED = 0x00002000,
   FILE_ATTRIBUTE_OFFLINE = 0x00001000,
   FILE_ATTRIBUTE_READONLY = 0x00000001,
   FILE_ATTRIBUTE_SYSTEM = 0x00000004,
   FILE_ATTRIBUTE_TEMPORARY = 0x00000100
}

public partial class MainWindow : Window
{
    private static int m_TimeCount = 0;
    private static string m_OutTime = "";
    private static string m_OutFolder = "";
    private static bool m_Output = false;
    private static int m_StartStop = 0;
    private static System.Timers.Timer? m_Timer;
    public MainWindow()
    {
        InitializeComponent();
        SetTimer();
    }

   private static void SetTimer()
   {
        m_Timer = new System.Timers.Timer(1000);
        m_Timer.Elapsed += OnTimedEvent;
        m_Timer.AutoReset = true;
        m_Timer.Enabled = true;
    }


    void OnClickStartStop(object sender, RoutedEventArgs e)
    {
        m_OutTime = this.txtOutTime.Text;
        m_OutFolder = this.txtOutFolder.Text;
        m_Output = this.cbOutput.IsChecked != null ? (bool)this.cbOutput.IsChecked : false;
        if ( m_StartStop == 1 || m_StartStop == 0 )
        {
            m_StartStop = 2;
            this.btnStartStop.Content = "ストッパー実行中";
        }
        else if ( m_StartStop == 2 )
        {
            m_StartStop = 1;
            this.btnStartStop.Content = "ストッパー開始";
        }
    }

    private static void OnTimedEvent(Object? source, ElapsedEventArgs e)
    {
        [DllImport("User32.dll",EntryPoint = "mouse_event", CallingConvention = CallingConvention.Winapi)]
        static extern void Mouse_Event(int dwFlags,int dx,int dy,int dwData,int dwExtraInfo);
        if ( m_StartStop == 2 )
        {
            Mouse_Event(0x0001,0,0,0,0);
            if (m_Output)
            {
                m_TimeCount++;
                int count = Int32.Parse(m_OutTime);
                if (m_TimeCount > count)
                {
                    m_TimeCount = 0;
                    SaveScreenshot();
                }
            }
        }
    }

    private static void SaveScreenshot()
    {
        if (!Directory.Exists(m_OutFolder))
        {
            Directory.CreateDirectory(m_OutFolder);
        }
        string filename = m_OutFolder +"\\SS.bmp";
        unsafe //unsafeブロックの宣言
        {
            const int SRCCOPY = 0xcc0020;
            const int DIB_RGB_COLORS = 0;
            [DllImport("User32.dll", EntryPoint = "GetDC")]
            static extern IntPtr GetDC(IntPtr hwnd);
            [DllImport("User32.dll", EntryPoint = "ReleaseDC")]
            static extern void ReleaseDC(IntPtr hwnd, IntPtr dc);
            [DllImport("gdi32.dll", EntryPoint = "CreateCompatibleDC")]
            static extern IntPtr CreateCompatibleDC(IntPtr hdc);
            [DllImport("gdi32.dll", EntryPoint = "CreateCompatibleBitmap")]
            static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);
            [DllImport("gdi32.dll", EntryPoint = "SelectObject")]
            static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobjBmp);
            [DllImport("gdi32.dll", EntryPoint = "BitBlt")]
            static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);
            [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
            static extern IntPtr DeleteObject(IntPtr hObject);
            [DllImport("gdi32.dll")]
            static extern int GetDIBits(IntPtr hdc, IntPtr hbmp, UInt32 uStartScan, UInt32 cScanLines, IntPtr lpvBits, void* lpbi, UInt32 uUsage);
            [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
            static extern IntPtr CreateFile(string lpFileName, DesiredAccess dwDesiredAccess, ShareMode dwShareMode, int lpSecurityAttributes, CreationDisposition dwCreationDisposition, FlagsAndAttributes dwFlagsAndAttributes, IntPtr hTemplateFile);
            [DllImport("kernel32.dll", BestFitMapping = true, CharSet = CharSet.Ansi)]
            static extern bool WriteFile(IntPtr hFile, void* lpBuffer, uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten, int lpOverlapped);
            [DllImport("kernel32.dll", SetLastError = true)]
            static extern bool CloseHandle(IntPtr hObject);

            IntPtr screenDC = GetDC((IntPtr)null);
            if (screenDC == IntPtr.Zero)
            {
                Console.WriteLine("GetDC error!");
                return;
            }
            IntPtr hbmScreen = CreateCompatibleBitmap(screenDC, (int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight);
            if (hbmScreen == IntPtr.Zero)
            {
                Console.WriteLine("CreateCompatibleBitmap error!");
                return;
            }
            IntPtr memDC = CreateCompatibleDC(screenDC);
            if (memDC == IntPtr.Zero)
            {
                Console.WriteLine("CreateCompatibleDC error!");
                return;
            }
            SelectObject(memDC, hbmScreen);
            if (!BitBlt(memDC, 0, 0, (int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight, screenDC, 0, 0, SRCCOPY))
            {
                Console.WriteLine("BitBlt error!");
                return;
            }

            BITMAPFILEHEADER   bmfHeader;
            BITMAPINFOHEADER   bi;
            bi.biSize = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADER));
            bi.biWidth = Convert.ToUInt32(SystemParameters.PrimaryScreenWidth);
            bi.biHeight = Convert.ToUInt32(SystemParameters.PrimaryScreenHeight);
            bi.biPlanes = 1;
            bi.biBitCount = 32;
            bi.biCompression = 0;//BI_RGB;
            bi.biSizeImage = 0;
            bi.biXPelsPerMeter = 0;
            bi.biYPelsPerMeter = 0;
            bi.biClrUsed = 0;
            bi.biClrImportant = 0;

            UInt32 dwBmpSize = (UInt32)((SystemParameters.PrimaryScreenWidth * bi.biBitCount + 31) / 32) * 4 * (uint)SystemParameters.PrimaryScreenHeight;
            UInt32 dwSizeofDIB = dwBmpSize + (UInt32)sizeof(BITMAPFILEHEADER) + (UInt32)sizeof(BITMAPINFOHEADER);
            bmfHeader.bfOffBits = (UInt32)sizeof(BITMAPFILEHEADER) + (UInt32)sizeof(BITMAPINFOHEADER);
            bmfHeader.bfSize = dwSizeofDIB;
            bmfHeader.bfType = 0x4D42; // BM.

            IntPtr prtBitmap = Marshal.AllocHGlobal((int)dwBmpSize);
            GetDIBits(screenDC, hbmScreen, 0, (uint)SystemParameters.PrimaryScreenHeight, prtBitmap, &bi, DIB_RGB_COLORS);
            byte[] bufBitmap = new byte[dwBmpSize];
            Marshal.Copy(prtBitmap, bufBitmap, 0, (int)dwBmpSize);

            uint writesize = 0;
            IntPtr hf = CreateFile(filename, DesiredAccess.GENERIC_WRITE, ShareMode.FILE_SHARE_WRITE, 0, CreationDisposition.CREATE_NEW, FlagsAndAttributes.FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);
            if (hf != IntPtr.Zero)
            {
                WriteFile(hf, &bmfHeader, (uint)Marshal.SizeOf(typeof(BITMAPFILEHEADER)), out writesize,0);
                WriteFile(hf, &bi, (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADER)), out writesize,0);
                fixed (void* px = bufBitmap)
                {
                    WriteFile(hf, px, dwBmpSize, out writesize,0);
                }
                CloseHandle(hf);
            }

            DateTime now = DateTime.Now;
            string pngPath = m_OutFolder + "\\SS_" + now.ToString("yyyyMMddHHmmss") + ".png";
            using (var streambmp = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var streampng = new FileStream(pngPath, FileMode.Create))
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Interlace = PngInterlaceOption.On;
                    encoder.Frames.Add(BitmapFrame.Create(streambmp));
                    encoder.Save(streampng);
                }
            }

            DeleteObject(prtBitmap);
            DeleteObject(hbmScreen);
            DeleteObject(memDC);
            ReleaseDC((IntPtr)null, screenDC);
        }
        File.Delete(filename);
   }
}
