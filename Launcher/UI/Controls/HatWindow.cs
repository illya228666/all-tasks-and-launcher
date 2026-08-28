using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Launcher.UI.Controls;

// Отдельное окно без владельца: шляпа остаётся на рабочем столе при сворачивании Main.
internal sealed class HatWindow : Form
{
    // Интервал обновления положения при переносе. Таймер работает только пока
    // удерживается ЛКМ; 16 мс даёт примерно 60 обновлений в секунду.
    private const int DragIntervalMs = 16;
    private const int WsExLayered = 0x00080000;
    private const int WsExNoActivate = 0x08000000;
    private const int WsExToolWindow = 0x00000080;

    // Изображение принадлежит Main и освобождается после закрытия этого окна.
    private readonly Bitmap _sprite;
    private readonly System.Windows.Forms.Timer _dragTimer = new() { Interval = DragIntervalMs };

    // Координаты курсора при отпускании; Main решает, попала ли шляпа на голову.
    internal event Action<Point>? Dropped;

    internal HatWindow(Bitmap sprite)
    {
        _sprite = sprite;
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        ClientSize = sprite.Size;
        TopMost = true;
        _dragTimer.Tick += DragTimer_Tick;
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams parameters = base.CreateParams;
            parameters.ExStyle |= WsExLayered | WsExNoActivate | WsExToolWindow;
            // WS_EX_TRANSPARENT не нужен: прозрачные пиксели пропускают клики
            // автоматически, а непрозрачную часть шляпы надо уметь брать снова.
            return parameters;
        }
    }

    internal void BeginDrag(Point cursorPosition)
    {
        MoveToCursor(cursorPosition);
        if (!Visible)
        {
            // Вызов вне оконного callback: ошибка Win32 попадёт в обработчик
            // снятия шляпы, и Main не переключит атлас при неудаче.
            UploadSprite();
            Show(); // Без Owner: сворачивание Launcher не скрывает шляпу.
        }
        _dragTimer.Start();
    }

    private void MoveToCursor(Point cursorPosition) =>
        Location = new Point(cursorPosition.X - _sprite.Width / 2, cursorPosition.Y - _sprite.Height / 2);

    private void EndDrag()
    {
        if (!_dragTimer.Enabled)
            return;

        _dragTimer.Stop();
        Point dropPosition = Cursor.Position;
        MoveToCursor(dropPosition);
        Dropped?.Invoke(dropPosition);
    }

    private void DragTimer_Tick(object? sender, EventArgs e)
    {
        if (!_dragTimer.Enabled)
            return;

        // Только физическое состояние ЛКМ во время переноса. Не требует фокуса,
        // захвата мыши или глобальных хуков; работает и за пределами Launcher.
        if ((GetAsyncKeyState(0x01) & 0x8000) == 0)
            EndDrag();
        else
            MoveToCursor(Cursor.Position);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left)
            BeginDrag(Cursor.Position);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button == MouseButtons.Left && _dragTimer.Enabled)
            EndDrag();
    }

    private void UploadSprite()
    {
        IntPtr memoryDc = CreateCompatibleDC(IntPtr.Zero);
        if (memoryDc == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        IntPtr bitmap = IntPtr.Zero;
        IntPtr previousBitmap = IntPtr.Zero;
        try
        {
            // UpdateLayeredWindow ожидает premultiplied alpha. Готовим копию
            // только при создании HWND; при переносе меняется лишь Location.
            using var premultiplied = new Bitmap(_sprite.Width, _sprite.Height, PixelFormat.Format32bppPArgb);
            using (Graphics graphics = Graphics.FromImage(premultiplied))
                graphics.DrawImageUnscaled(_sprite, 0, 0);
            bitmap = premultiplied.GetHbitmap(Color.FromArgb(0));
            previousBitmap = SelectObject(memoryDc, bitmap);
            if (previousBitmap == IntPtr.Zero || previousBitmap == new IntPtr(-1))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            Point destination = Location;
            Point source = Point.Empty;
            Size size = _sprite.Size;
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
            _dragTimer.Dispose();
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

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int virtualKey);

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
