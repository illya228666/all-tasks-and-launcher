using Launcher.Pet.Animation;
using Launcher.Pet.Data;
using Launcher.Pet.Windows.Windows;

namespace Launcher.Pet.Windows.Drawing;
internal sealed class PetDrawing : IDisposable
{
    private readonly PetArea _area;
    private readonly PetImages _images;
    private PetScene? _scene;
    private PetColors _colors = new(Color.White, Color.White, Color.Black, Color.Gray);
    internal PetDrawing(PetArea area, PetImages images)
    {
        _area = area;
        _images = images;
        _area.Paint += Paint;
    }

    internal void Display(PetScene scene)
    {
        bool changed = _scene is null || _scene.Row != scene.Row || _scene.Frame != scene.Frame || _scene.LocalBounds != scene.LocalBounds || _scene.HatAttached != scene.HatAttached;
        _scene = scene;
        if (changed)
            _area.Invalidate();
    }

    internal void SetColors(PetColors colors)
    {
        _colors = colors;
        _area.BackColor = colors.Background;
        _area.Invalidate();
    }

    private void Paint(object? sender, PaintEventArgs args)
    {
        using var pen = new Pen(_colors.Border);
        args.Graphics.DrawRectangle(pen, 0, _area.GroundLocalY, Math.Max(1, _area.ClientSize.Width - 1), PetAnimationCatalog.FrameHeight - 1);
        if (_scene is null)
            return;
        args.Graphics.DrawImage(_scene.HatAttached ? _images.WithHat : _images.WithoutHat, _scene.LocalBounds, PetAnimationCatalog.GetSourceRectangle(_scene.Row, _scene.Frame), GraphicsUnit.Pixel);
    }

    internal bool IsPetAtScreen(Point screenPoint, Rectangle visibleScreenBounds) =>
        TryGetPetPixel(screenPoint, visibleScreenBounds, out _, out _, out _, out _);

    internal bool IsHeadAtScreen(Point screenPoint, Rectangle visibleScreenBounds)
    {
        if (!TryGetPetPixel(screenPoint, visibleScreenBounds, out _, out int y, out Rectangle source, out Bitmap atlas))
            return false;
        for (int row = 0; row <= y; row++)
            for (int column = 0; column < PetAnimationCatalog.CellWidth; column++)
                if (atlas.GetPixel(source.X + column, source.Y + row).A != 0)
                    return y - row < PetAnimationCatalog.HeadHitHeight;
        return false;
    }

    private bool TryGetPetPixel(Point screenPoint, Rectangle visibleScreenBounds, out int x, out int y, out Rectangle source, out Bitmap atlas)
    {
        x = y = 0;
        source = Rectangle.Empty;
        atlas = null!;
        if (_scene is null || !visibleScreenBounds.Contains(screenPoint))
            return false;
        Point local = _area.PointToClient(screenPoint);
        if (_area.GetChildAtPoint(local, GetChildAtPointSkip.Invisible) is not null || !_scene.LocalBounds.Contains(local))
            return false;
        x = local.X - _scene.LocalBounds.X;
        y = local.Y - _scene.LocalBounds.Y;
        source = PetAnimationCatalog.GetSourceRectangle(_scene.Row, _scene.Frame);
        atlas = _scene.HatAttached ? _images.WithHat : _images.WithoutHat;
        return atlas.GetPixel(source.X + x, source.Y + y).A != 0;
    }

    public void Dispose()
    {
        _area.Paint -= Paint;
    }
}
