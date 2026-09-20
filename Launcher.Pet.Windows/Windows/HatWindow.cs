using static Launcher.Pet.Windows.Windows.HatMouseApi;
using Launcher.Pet.Hat;
using Launcher.Pet.Windows.Drawing;
namespace Launcher.Pet.Windows.Windows;

internal sealed class HatWindow : TransparentOverlayWindow
{
    private readonly HatFrameCache _frames;
    private HatRenderKey? _lastPose;
    private bool _interactionEnabled = true;
    private bool _dragging;
    private float _scale = 1;
    private int ScaledPadding => (int)Math.Round(HatFrameCache.Padding * _scale);
    internal void SetScale(float scale)
    {
        if (_scale == scale) return;
        _scale = scale;
        _lastPose = null;
    }
    internal event Action? DragStarted;
    internal event Action<Point>? Dropped;
    internal event Action<Point>? DragMoved;
    internal HatWindow(HatFrameCache frames) : base(clickThrough: false)
    {
        _frames = frames;
        DisplayPose(default);
    }
    internal IntPtr WindowHandle => Handle;
    internal void BeginDrag(Point cursorPosition)
    {
        if (!_interactionEnabled || _dragging) return;
        DisplayPose(default);
        ShowAt(GetLocationAtCursor(cursorPosition));
        _dragging = true;
        DragStarted?.Invoke();
    }
    internal void UpdateDrag()
    {
        if (!_dragging) return;
        if ((GetAsyncKeyState(0x01) & 0x8000) == 0) EndDrag();
        else MoveToCursor(Cursor.Position);
    }
    internal void CancelDrag() => _dragging = false;
    internal void SetInteractionEnabled(bool enabled)
    {
        _interactionEnabled = enabled;
        if (!enabled) CancelDrag();
    }
    internal void MoveTo(Point location) => ShowAt(new(location.X - ScaledPadding, location.Y - ScaledPadding));
    internal void DisplayPose(HatVisualPose pose)
    {
        var key = HatFrameCache.GetKey(pose);
        if (_lastPose == key) return;
        _frames.Draw(pose, image =>
        {
            if (_scale == 1) { SetImage(image); return; }
            using var scaled = new Bitmap(Math.Max(1, (int)Math.Round(image.Width * _scale)), Math.Max(1, (int)Math.Round(image.Height * _scale)), System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            using (var graphics = Graphics.FromImage(scaled))
            {
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(image, new Rectangle(Point.Empty, scaled.Size));
            }
            SetImage(scaled);
        });
        _lastPose = key;
    }
    private Point GetLocationAtCursor(Point cursorPosition) => new(cursorPosition.X - (int)Math.Round(HatGeometry.Width * _scale / 2) - ScaledPadding, cursorPosition.Y - (int)Math.Round(HatGeometry.Height * _scale / 2) - ScaledPadding);

    private void MoveToCursor(Point cursorPosition)
    {
        Location = GetLocationAtCursor(cursorPosition);
        DragMoved?.Invoke(cursorPosition);
    }

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

}
