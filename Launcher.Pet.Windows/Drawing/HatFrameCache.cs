using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Launcher.Pet.Hat;
namespace Launcher.Pet.Windows.Drawing;

internal sealed class HatFrameCache : IDisposable
{
    internal const int Padding = 10;
    private const int BlendSteps = 16;
    private readonly Bitmap[][] _frames;
    internal HatFrameCache(Bitmap neutral, Bitmap[] falling)
    {
        if (falling.Length != HatAnimation.FallingFrameCount) throw new InvalidDataException("Hat frame count does not match animation.");
        _frames = new Bitmap[falling.Length + 1][];
        try
        {
            for (int pose = 0; pose < _frames.Length; pose++)
            {
                _frames[pose] = new Bitmap[HatRotationProfile.FrameCount];
                for (int angle = 0; angle < _frames[pose].Length; angle++)
                {
                    var bitmap = new Bitmap(HatGeometry.Width + 2 * Padding, HatGeometry.Height + 2 * Padding, PixelFormat.Format32bppPArgb);
                    _frames[pose][angle] = bitmap;
                    using var graphics = Graphics.FromImage(bitmap);
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.TranslateTransform(bitmap.Width / 2f, bitmap.Height / 2f);
                    graphics.RotateTransform(HatRotationProfile.GetFrameAngle(angle));
                    graphics.TranslateTransform(-HatGeometry.Width / 2f, -HatGeometry.Height / 2f);
                    graphics.DrawImageUnscaled(pose == 0 ? neutral : falling[pose - 1], 0, 0);
                }
            }
        }
        catch { Dispose(); throw; }
    }
    internal void Draw(HatVisualPose pose, Action<Bitmap> display)
    {
        var key = GetKey(pose);
        if (key.Frame == 0 || key.BlendStep == 0) { display(_frames[key.Frame][key.Angle]); return; }
        using var blended = Blend(_frames[key.Frame][key.Angle], _frames[0][key.Angle], (float)key.BlendStep / BlendSteps);
        display(blended);
    }
    internal static HatRenderKey GetKey(HatVisualPose pose)
    {
        int blend = (int)MathF.Round(Math.Clamp(pose.NeutralBlend, 0, 1) * BlendSteps);
        int frame = blend == BlendSteps ? 0 : pose.Frame;
        return new(frame, HatRotationProfile.GetNearestFrameIndex(pose.AngleDegrees), frame == 0 ? 0 : blend);
    }
    internal static Bitmap Blend(Bitmap from, Bitmap to, float progress)
    {
        if (ReferenceEquals(from, to)) return from.Clone(new Rectangle(Point.Empty, from.Size), PixelFormat.Format32bppPArgb);
        var result = new Bitmap(from.Width, from.Height, PixelFormat.Format32bppPArgb);
        try
        {
            BlendInto(from, to, result, Math.Clamp(progress, 0, 1));
            return result;
        }
        catch { result.Dispose(); throw; }
    }
    private static void BlendInto(Bitmap from, Bitmap to, Bitmap result, float progress)
    {
        var bounds = new Rectangle(Point.Empty, from.Size);
        BitmapData? a = null, b = null, target = null;
        try
        {
            a = from.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
            b = to.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
            target = result.LockBits(bounds, ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
            var left = new byte[from.Width * 4];
            var right = new byte[left.Length];
            for (int y = 0; y < from.Height; y++)
            {
                Marshal.Copy(IntPtr.Add(a.Scan0, y * a.Stride), left, 0, left.Length);
                Marshal.Copy(IntPtr.Add(b.Scan0, y * b.Stride), right, 0, right.Length);
                for (int i = 0; i < left.Length; i++) left[i] = (byte)MathF.Round(left[i] * (1 - progress) + right[i] * progress);
                Marshal.Copy(left, 0, IntPtr.Add(target.Scan0, y * target.Stride), left.Length);
            }
        }
        finally
        {
            if (target is not null) result.UnlockBits(target);
            if (b is not null) to.UnlockBits(b);
            if (a is not null) from.UnlockBits(a);
        }
    }
    public void Dispose()
    {
        foreach (var pose in _frames)
            if (pose is not null) foreach (var image in pose) image?.Dispose();
    }
}
