using Launcher.Pet.Sprites;
using Launcher.Pet.Data;
using Launcher.Pet.Windows.Windows;

namespace Launcher.Pet.Windows.Drawing;
internal sealed class PetDrawing : IDisposable
{
    private readonly PetArea _area;
    private readonly PetImages _images;
    private PetScene? _scene;
    internal bool Enabled { get; set; } = true;
    private PetColors _colors = new(Color.White, Color.White, Color.Black, Color.Gray);
    internal PetDrawing(PetArea area, PetImages images)
    {
        _area = area;
        _images = images;
        _area.Paint += Paint;
    }

    internal void Display(PetScene scene)
    {
        bool changed = _scene is null || _scene.Row != scene.Row || _scene.Frame != scene.Frame || _scene.SpriteBounds != scene.SpriteBounds || _scene.HatAttached != scene.HatAttached;
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
        args.Graphics.DrawRectangle(pen, 0, _area.PetZoneTopY, Math.Max(1, _area.ClientSize.Width - 1), PetLogicalGeometry.Height - 1);
        if (!Enabled || _scene is null)
            return;
        args.Graphics.DrawImage(_images.GetFrame(_scene.Row, _scene.Frame), _scene.SpriteBounds);
    }

    internal bool IsPetAtScreen(Point screenPoint, Rectangle visibleScreenBounds) =>
        TryGetPetPixel(screenPoint, visibleScreenBounds, out _, out _);

    internal bool IsHeadAtScreen(Point screenPoint, Rectangle visibleScreenBounds)
    {
        return TryGetPetPixel(screenPoint, visibleScreenBounds, out int x, out int y)
            && PetSpriteCatalog.GetFrameGeometry(_scene!.Row, _scene.Frame).HeadBounds.Contains(x, y);
    }

    private bool TryGetPetPixel(Point screenPoint, Rectangle visibleScreenBounds, out int x, out int y)
    {
        x = y = 0;
        if (_scene is null || !visibleScreenBounds.Contains(screenPoint))
            return false;
        Point local = _area.PointToClient(screenPoint);
        if (_area.GetChildAtPoint(local, GetChildAtPointSkip.Invisible) is not null || !_scene.SpriteBounds.Contains(local))
            return false;
        Point cell = PetSpriteLayout.MapDestinationToAtlasCell(local, _scene.SpriteBounds);
        x = cell.X;
        y = cell.Y;
        return _images.GetMask(_scene.Row, _scene.Frame).Contains(x, y);
    }

    public void Dispose()
    {
        _area.Paint -= Paint;
    }
}
