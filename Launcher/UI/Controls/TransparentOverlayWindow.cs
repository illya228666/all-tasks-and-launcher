using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Launcher.UI.Controls;

// Общая прозрачная поверхность. Окна независимы и не имеют владельца Launcher.
internal class TransparentOverlayWindow : Form
{
    private readonly bool _clickThrough;
    private Bitmap? _image;

    protected TransparentOverlayWindow(bool clickThrough)
    {
        _clickThrough = clickThrough;
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams parameters = base.CreateParams;
            // WS_EX_LAYERED | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW.
            parameters.ExStyle |= 0x00080000 | 0x08000000 | 0x00000080;
            // Шляпа принимает клики по непрозрачным пикселям; речь пропускает все.
            if (_clickThrough)
                parameters.ExStyle |= 0x00000020; // WS_EX_TRANSPARENT
            return parameters;
        }
    }

    protected void SetImage(Bitmap image)
    {
        // Только эта копия принадлежит окну. Исходное изображение не освобождаем.
        var copy = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppPArgb);
        try
        {
            using Graphics graphics = Graphics.FromImage(copy);
            graphics.DrawImageUnscaled(image, 0, 0);
        }
        catch
        {
            copy.Dispose();
            throw;
        }

        _image?.Dispose();
        _image = copy;
        ClientSize = copy.Size;
        if (Visible)
            UploadImage();
    }

    protected void ShowAt(Point location)
    {
        Location = location;
        if (Visible)
            return;

        // Вне оконного callback: ошибку Win32 сможет обработать вызывающий код.
        UploadImage();
        Show(); // Без Owner: сворачивание Main само по себе не скрывает оверлей.
    }

    private void UploadImage()
    {
        if (_image is null)
            throw new InvalidOperationException("Overlay image is not set.");

        IntPtr memoryDc = CreateCompatibleDC(IntPtr.Zero);
        if (memoryDc == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        IntPtr bitmap = IntPtr.Zero;
        IntPtr previousBitmap = IntPtr.Zero;
        try
        {
            bitmap = _image.GetHbitmap(Color.FromArgb(0));
            previousBitmap = SelectObject(memoryDc, bitmap);
            if (previousBitmap == IntPtr.Zero || previousBitmap == new IntPtr(-1))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            Point destination = Location;
            Point source = Point.Empty;
            Size size = _image.Size;
            var blend = new BlendFunction { SourceConstantAlpha = 255, AlphaFormat = 1 }; // AC_SRC_ALPHA
            if (!UpdateLayeredWindow(Handle, IntPtr.Zero, ref destination, ref size,
                    memoryDc, ref source, 0, ref blend, 2)) // ULW_ALPHA
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        finally
        {
            if (previousBitmap != IntPtr.Zero && previousBitmap != new IntPtr(-1))
                SelectObject(memoryDc, previousBitmap);
            if (bitmap != IntPtr.Zero)
                DeleteObject(bitmap);
            DeleteDC(memoryDc);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _image?.Dispose();
            _image = null;
        }
        base.Dispose(disposing);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct BlendFunction
    {
        public byte BlendOp;
        public byte BlendFlags;
        public byte SourceConstantAlpha;
        public byte AlphaFormat;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UpdateLayeredWindow(IntPtr window, IntPtr destinationDc,
        ref Point destination, ref Size size, IntPtr sourceDc, ref Point source,
        uint colorKey, ref BlendFunction blend, uint flags);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateCompatibleDC(IntPtr dc);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr SelectObject(IntPtr dc, IntPtr item);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr item);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteDC(IntPtr dc);
}
