using Launcher.Pet.Exploration;
using Launcher.Pet.Windows.Drawing;
namespace Launcher.Pet.Windows.Windows;

internal sealed class RuinsWindow : TransparentOverlayWindow
{
    private readonly RuinRenderer _renderer = new();
    private long _nextFrame;
    private RuinScene? _scene;
    internal RuinsWindow() : base(clickThrough: true) => Text = "Sumrak ruins";
    internal bool Display(RuinScene scene, Point origin, Point awakening, float seconds, long now)
    {
        if (now < _nextFrame && ReferenceEquals(scene, _scene)) return false;
        _nextFrame = now + 34;
        _scene = scene;
        SetImage(_renderer.Render(scene, awakening, seconds));
        ShowAt(origin);
        return true;
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) _renderer.Dispose();
        base.Dispose(disposing);
    }
}
