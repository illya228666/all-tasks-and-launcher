using static Launcher.Pet.Windows.Windows.HatMouseApi;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Launcher.Pet.Hat;

namespace Launcher.Pet.Windows.Windows;
// Визуальное окно шляпы: drag/input и выбор 3D-позы + программного Z-угла, без физики и scheduler.
internal sealed class HatWindow : TransparentOverlayWindow
{
    private readonly Bitmap[] _angleFrames;
    private readonly Bitmap[][] _fallPoseFrames;
    private int _angleFrame = -1;
    private int _fallFrame = -1;
    private bool _showingFall;
    private bool _interactionEnabled = true;
    private bool _dragging;
    internal event Action? DragStarted;
    internal event Action<Point>? Dropped;
    internal event Action<Point>? DragMoved;

    internal HatWindow(Bitmap sprite, Bitmap[] fallFrames) : base(clickThrough: false)
    {
        _angleFrames = CreateAngleFrames(sprite);
        try
        {
            _fallPoseFrames = CreateFallPoseFrames(fallFrames, _angleFrames);
            SetAngle(0f);
        }
        catch
        {
            foreach (Bitmap frame in _angleFrames)
                frame.Dispose();
            base.Dispose(true);
            throw;
        }
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

    internal void SetPose(HatMode mode, float angle, float fallTimeSeconds)
    {
        if (mode != HatMode.Falling)
        {
            SetAngle(angle);
            return;
        }

        int fallFrame = GetFallFrame(fallTimeSeconds);
        int angleFrame = HatRotationProfile.GetNearestFrameIndex(angle);
        if (_showingFall && fallFrame == _fallFrame && angleFrame == _angleFrame)
            return;

        _fallFrame = fallFrame;
        _angleFrame = angleFrame;
        _showingFall = true;
        SetImage(_fallPoseFrames[fallFrame][angleFrame]);
    }

    internal void SetAngle(float angle)
    {
        int frame = HatRotationProfile.GetNearestFrameIndex(angle);
        if (!_showingFall && frame == _angleFrame)
            return;
        _angleFrame = frame;
        _showingFall = false;
        SetImage(_angleFrames[frame]);
    }

    private int GetFallFrame(float fallTimeSeconds)
    {
        // Та же фаза, что используется HatPhysics для бокового скольжения и Angle:
        // 0 -> последний 3D-ракурс -> 0 за один полный цикл качания.
        float phase = Math.Max(0f, fallTimeSeconds) * HatRotationProfile.SwingRadiansPerSecond;
        float progress = (1f - MathF.Cos(phase)) * 0.5f;
        return Math.Clamp((int)MathF.Round(progress * (_fallPoseFrames.Length - 1)), 0, _fallPoseFrames.Length - 1);
    }

    private static Bitmap[][] CreateFallPoseFrames(Bitmap[] fallFrames, Bitmap[] neutralAngleFrames)
    {
        // hat.png является нулевой 3D-позой. Остальные позы приходят из hat_falling_*.png.
        var poses = new Bitmap[fallFrames.Length + 1][];
        poses[0] = neutralAngleFrames;
        int created = 1;
        try
        {
            for (int index = 0; index < fallFrames.Length; index++)
            {
                poses[index + 1] = CreateAngleFrames(fallFrames[index]);
                created++;
            }

            return poses;
        }
        catch
        {
            for (int pose = 1; pose < created; pose++)
            {
                foreach (Bitmap frame in poses[pose])
                    frame.Dispose();
            }

            throw;
        }
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
                frames.Add(frame);
                using Graphics graphics = Graphics.FromImage(frame);
                graphics.Clear(Color.Transparent);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.TranslateTransform(sprite.Width / 2f, sprite.Height / 2f);
                graphics.RotateTransform(angle);
                graphics.TranslateTransform(-sprite.Width / 2f, -sprite.Height / 2f);
                graphics.DrawImageUnscaled(sprite, 0, 0);
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

    private Point GetLocationAtCursor(Point cursorPosition) => new(cursorPosition.X - ClientSize.Width / 2, cursorPosition.Y - ClientSize.Height / 2);

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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _dragging = false;
            for (int pose = 1; pose < _fallPoseFrames.Length; pose++)
            {
                foreach (Bitmap frame in _fallPoseFrames[pose])
                    frame.Dispose();
            }

            foreach (Bitmap frame in _angleFrames)
                frame.Dispose();
        }

        base.Dispose(disposing);
    }
}
