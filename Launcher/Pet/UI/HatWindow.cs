using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Launcher.Pet.Hat;

namespace Launcher.Pet.UI;

// Визуальное окно шляпы: drag/input и выбор заранее отрисованного угла, без физики и scheduler.
internal sealed class HatWindow : TransparentOverlayWindow
{
    private readonly Bitmap[] _angleFrames;
    private int _angleFrame = -1;
    private bool _interactionEnabled = true;
    private bool _dragging;

    internal event Action? DragStarted;
    internal event Action<Point>? Dropped;

    internal HatWindow(Bitmap sprite) : base(clickThrough: false)
    {
        _angleFrames = CreateAngleFrames(sprite);
        SetAngle(0f);
    }

    internal IntPtr WindowHandle => Handle;

    internal void BeginDrag(Point cursorPosition)
    {
        if (!_interactionEnabled || _dragging)
            return;

        SetAngle(0f);
        ShowAt(GetLocationAtCursor(cursorPosition));
        _dragging = true;
        DragStarted?.Invoke();
    }

    internal void UpdateDrag()
    {
        if (!_dragging)
            return;

        // Физическое состояние ЛКМ не требует фокуса, захвата мыши или хуков.
        if ((GetAsyncKeyState(0x01) & 0x8000) == 0)
            EndDrag();
        else
            MoveToCursor(Cursor.Position);
    }

    internal void CancelDrag() => _dragging = false;

    internal void SetInteractionEnabled(bool enabled)
    {
        _interactionEnabled = enabled;
        if (!enabled)
            CancelDrag();
    }

    internal void MoveTo(Point location) => ShowAt(location);

    internal void SetAngle(float angle)
    {
        int frame = HatRotationProfile.GetNearestFrameIndex(angle);
        if (frame == _angleFrame)
            return;

        _angleFrame = frame;
        SetImage(_angleFrames[frame]);
    }

    private static Bitmap[] CreateAngleFrames(Bitmap sprite)
    {
        var frames = new List<Bitmap>(HatRotationProfile.FrameCount);
        try
        {
            for (int frameIndex = 0; frameIndex < HatRotationProfile.FrameCount; frameIndex++)
            {
                float angle = HatRotationProfile.GetFrameAngle(frameIndex);
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
        if (!_dragging)
            return;

        _dragging = false;
        Point dropPosition = Cursor.Position;
        MoveToCursor(dropPosition);
        Dropped?.Invoke(dropPosition);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (_interactionEnabled && e.Button == MouseButtons.Left && !_dragging)
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
            _dragging = false;
            foreach (Bitmap frame in _angleFrames)
                frame.Dispose();
        }

        base.Dispose(disposing);
    }

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int virtualKey);
}
