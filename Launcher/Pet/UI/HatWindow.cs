using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Launcher.Pet.UI;

// Визуальное окно шляпы: drag и выбор заранее отрисованного угла, без физики.
internal sealed class HatWindow : TransparentOverlayWindow
{
    private const int DragIntervalMs = 16;
    private static readonly float[] FrameAngles = { -7f, -3.5f, 0f, 3.5f, 7f };

    private readonly System.Windows.Forms.Timer _dragTimer = new() { Interval = DragIntervalMs };
    private readonly Bitmap[] _angleFrames;
    private int _angleFrame = -1;
    private bool _interactionEnabled = true;

    internal event Action? DragStarted;
    internal event Action<Point>? Dropped;

    internal HatWindow(Bitmap sprite) : base(clickThrough: false)
    {
        _angleFrames = CreateAngleFrames(sprite);
        _dragTimer.Tick += DragTimer_Tick;
        SetAngle(0f);
    }

    internal IntPtr WindowHandle => Handle;

    internal void BeginDrag(Point cursorPosition)
    {
        if (!_interactionEnabled)
            return;
        SetAngle(0f);
        ShowAt(GetLocationAtCursor(cursorPosition));
        DragStarted?.Invoke();
        _dragTimer.Start();
    }

    internal void CancelDrag() => _dragTimer.Stop();

    internal void SetInteractionEnabled(bool enabled)
    {
        _interactionEnabled = enabled;
        if (!enabled)
            CancelDrag();
    }

    internal void MoveTo(Point location) => ShowAt(location);

    internal void SetAngle(float angle)
    {
        int frame = Enumerable.Range(0, FrameAngles.Length)
            .MinBy(index => Math.Abs(FrameAngles[index] - angle));
        if (frame == _angleFrame)
            return;
        _angleFrame = frame;
        SetImage(_angleFrames[frame]);
    }

    private static Bitmap[] CreateAngleFrames(Bitmap sprite)
    {
        var frames = new List<Bitmap>(FrameAngles.Length);
        try
        {
            foreach (float angle in FrameAngles)
            {
                if (angle == 0f)
                {
                    frames.Add(new Bitmap(sprite));
                    continue;
                }

                var frame = new Bitmap(sprite.Width, sprite.Height, PixelFormat.Format32bppPArgb);
                using Graphics graphics = Graphics.FromImage(frame);
                graphics.Clear(Color.Transparent);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.TranslateTransform(sprite.Width / 2f, sprite.Height / 2f);
                graphics.RotateTransform(angle);
                graphics.TranslateTransform(-sprite.Width / 2f, -sprite.Height / 2f);
                graphics.DrawImageUnscaled(sprite, 0, 0);
                frames.Add(frame);
            }
            return frames.ToArray();
        }
        catch
        {
            foreach (Bitmap frame in frames)
                frame.Dispose();
            throw;
        }
    }

    private Point GetLocationAtCursor(Point cursorPosition) =>
        new(cursorPosition.X - ClientSize.Width / 2, cursorPosition.Y - ClientSize.Height / 2);

    private void MoveToCursor(Point cursorPosition) => Location = GetLocationAtCursor(cursorPosition);

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
        if (_interactionEnabled && e.Button == MouseButtons.Left && !_dragTimer.Enabled)
            BeginDrag(Cursor.Position);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button == MouseButtons.Left)
            EndDrag();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _dragTimer.Stop();
            _dragTimer.Tick -= DragTimer_Tick;
            _dragTimer.Dispose();
            foreach (Bitmap frame in _angleFrames)
                frame.Dispose();
        }
        base.Dispose(disposing);
    }

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int virtualKey);
}
