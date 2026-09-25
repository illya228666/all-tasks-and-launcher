using System.Drawing.Imaging;
using Launcher.Pet.Data;
using Launcher.Pet.Sprites;
using Launcher.Pet.Windows.Drawing;

namespace Launcher.Pet.Windows.Windows;

internal sealed class PetWindow : TransparentOverlayWindow
{
    private readonly PetImages _images;
    internal PetWindow(PetImages images) : base(clickThrough: false) => _images = images;
    private PetScene? _scene;
    internal void Display(PetScene scene, Point origin)
    {
        if (_scene is null || _scene.Row != scene.Row || _scene.Frame != scene.Frame || _scene.HatAttached != scene.HatAttached || _scene.SpriteBounds.Size != scene.SpriteBounds.Size)
        {
            using var image = new Bitmap(scene.SpriteBounds.Width, scene.SpriteBounds.Height, PixelFormat.Format32bppPArgb);
            using (Graphics graphics = Graphics.FromImage(image))
                graphics.DrawImage(scene.HatAttached ? _images.WithHat : _images.WithoutHat, new Rectangle(Point.Empty, image.Size), PetSpriteCatalog.GetSourceRectangle(scene.Row, scene.Frame), GraphicsUnit.Pixel);
            SetImage(image);
        }
        _scene = scene;
        ShowAt(new Point(origin.X + scene.SpriteBounds.X, origin.Y + scene.SpriteBounds.Y));
    }

    internal bool IsHeadAtScreen(Point point) => TryGetOpaqueCell(point, out Point cell)
        && PetSpriteCatalog.GetFrameGeometry(_scene!.Row, _scene.Frame).HeadBounds.Contains(cell);

    internal bool IsBodyAtScreen(Point point) => TryGetOpaqueCell(point, out Point cell)
        && !PetSpriteCatalog.GetFrameGeometry(_scene!.Row, _scene.Frame).HeadBounds.Contains(cell);

    private bool TryGetOpaqueCell(Point point, out Point cell)
    {
        cell = Point.Empty;
        if (_scene is null || !Visible || !Bounds.Contains(point))
            return false;
        cell = PetSpriteLayout.MapDestinationToAtlasCell(point, Bounds);
        Rectangle source = PetSpriteCatalog.GetSourceRectangle(_scene.Row, _scene.Frame);
        return (_scene.HatAttached ? _images.WithHatMask : _images.WithoutHatMask).Contains(source.X + cell.X, source.Y + cell.Y);
    }
}
