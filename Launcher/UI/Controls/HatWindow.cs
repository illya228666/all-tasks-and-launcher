using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Launcher.UI.Controls;

// Отдельное окно без владельца: шляпа остаётся на рабочем столе при сворачивании Main.
internal sealed class HatWindow : TransparentOverlayWindow
{
    // Интервал переноса в миллисекундах: примерно 60 обновлений в секунду.
    private const int DragIntervalMs = 16;
    private readonly System.Windows.Forms.Timer _dragTimer = new() { Interval = DragIntervalMs };

    // Координаты курсора при отпускании; Main решает, попала ли шляпа на голову.
    internal event Action<Point>? Dropped;

    internal HatWindow(Bitmap sprite) : base(clickThrough: false)
    {
        SetImage(sprite);
        _dragTimer.Tick += DragTimer_Tick;
    }

    internal void BeginDrag(Point cursorPosition)
    {
        ShowAt(GetLocationAtCursor(cursorPosition));
        _dragTimer.Start();
    }

    private Point GetLocationAtCursor(Point cursorPosition) =>
        new(cursorPosition.X - ClientSize.Width / 2, cursorPosition.Y - ClientSize.Height / 2);

    private void MoveToCursor(Point cursorPosition) =>
        Location = GetLocationAtCursor(cursorPosition);

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

        // Физическое состояние ЛКМ не требует фокуса, захвата мыши или хуков.
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _dragTimer.Dispose();
        base.Dispose(disposing);
    }

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int virtualKey);
}
