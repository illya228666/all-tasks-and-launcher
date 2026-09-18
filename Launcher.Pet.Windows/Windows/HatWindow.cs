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
    private readonly Bitmap[][] _mirroredFallPoseFrames;
    private int _angleFrame = -1;
    private int _fallFrame = -1;
    private bool _fallMirrored;
    private bool _showingFall;
    private bool _interactionEnabled = true;
    private bool _dragging;
    internal event Action? DragStarted;
    internal event Action<Point>? Dropped;
    internal event Action<Point>? DragMoved;

    internal HatWindow(Bitmap sprite, Bitmap[] fallFrames) : base(clickThrough: false)
    {
        _angleFrames = CreateAngleFrames(sprite);
        Bitmap[][] normal = Array.Empty<Bitmap[]>();
        Bitmap[][] mirrored = Array.Empty<Bitmap[]>();
        try
        {
            normal = CreateFallPoseFrames(fallFrames, _angleFrames, mirrored: false);
            mirrored = CreateFallPoseFrames(fallFrames, _angleFrames, mirrored: true);
            _fallPoseFrames = normal;
            _mirroredFallPoseFrames = mirrored;
            SetAngle(0f);
        }
        catch
        {
            DisposeFallPoseFrames(normal);
            DisposeFallPoseFrames(mirrored);
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

    internal void SetPose(HatMode mode, float angle, float fallTimeSeconds, float settlementProgress)
    {
        if (mode != HatMode.Falling && mode != HatMode.Settling)
        {
            SetAngle(angle);
            return;
        }

        (int fallFrame, bool mirrored) = mode == HatMode.Settling
            ? GetSettlingPose(fallTimeSeconds, settlementProgress)
            : GetFallPose(fallTimeSeconds);
        int angleFrame = HatRotationProfile.GetNearestFrameIndex(angle);
        if (_showingFall && fallFrame == _fallFrame && angleFrame == _angleFrame && mirrored == _fallMirrored)
            return;

        _fallFrame = fallFrame;
        _angleFrame = angleFrame;
        _fallMirrored = mirrored;
        _showingFall = true;
        Bitmap[][] poses = mirrored ? _mirroredFallPoseFrames : _fallPoseFrames;
        SetImage(poses[fallFrame][angleFrame]);
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

    private (int Frame, bool Mirrored) GetFallPose(float fallTimeSeconds)
    {
        // Одна фаза наклона даёт две симметричные стороны:
        // 0 -> normal max -> 0 -> mirrored max -> 0.
        float phase = Math.Max(0f, fallTimeSeconds) * HatRotationProfile.TiltRadiansPerSecond;
        float tilt = MathF.Sin(phase);
        int frame = Math.Clamp((int)MathF.Round(MathF.Abs(tilt) * (_fallPoseFrames.Length - 1)), 0, _fallPoseFrames.Length - 1);
        return (frame, frame > 0 && tilt < 0f);
    }

    private (int Frame, bool Mirrored) GetSettlingPose(float fallTimeSeconds, float settlementProgress)
    {
        (int landingFrame, bool mirrored) = GetFallPose(fallTimeSeconds);
        float progress = Math.Clamp(settlementProgress, 0f, 1f);
        float smooth = progress * progress * (3f - 2f * progress);
        int frame = Math.Clamp((int)MathF.Round(landingFrame * (1f - smooth)), 0, _fallPoseFrames.Length - 1);
        return (frame, frame > 0 && mirrored);
    }

    private static Bitmap[][] CreateFallPoseFrames(Bitmap[] fallFrames, Bitmap[] neutralAngleFrames, bool mirrored)
    {
        // hat.png является общей нулевой 3D-позой; отражаются только ненулевые fall-позы.
        var poses = new Bitmap[fallFrames.Length + 1][];
        poses[0] = neutralAngleFrames;
        int created = 1;
        try
        {
            for (int index = 0; index < fallFrames.Length; index++)
            {
                if (mirrored)
                {
                    using Bitmap source = new(fallFrames[index]);
                    source.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    poses[index + 1] = CreateAngleFrames(source);
                }
                else
                {
                    poses[index + 1] = CreateAngleFrames(fallFrames[index]);
                }
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

    private static void DisposeFallPoseFrames(Bitmap[][] poses)
    {
        // Нулевая поза разделяет _angleFrames и здесь не освобождается.
        for (int pose = 1; pose < poses.Length; pose++)
        {
            foreach (Bitmap frame in poses[pose])
                frame.Dispose();
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
            DisposeFallPoseFrames(_fallPoseFrames);
            DisposeFallPoseFrames(_mirroredFallPoseFrames);
            foreach (Bitmap frame in _angleFrames)
                frame.Dispose();
        }

        base.Dispose(disposing);
    }
}
