using Launcher.Pet.Animation;
using Launcher.Pet.Data;
using Launcher.Pet.Hat;

namespace Launcher.Pet.Windows.Windows;
public sealed class PetArea : FlowLayoutPanel
{
    public int GroundLocalY { get; private set; }
    public IReadOnlyList<Rectangle> ObstaclesLocal { get; private set; } = Array.Empty<Rectangle>();

    public PetArea()
    {
        DoubleBuffered = true;
        WrapContents = true;
    }

    public void SetGeometry(int groundLocalY, IEnumerable<Rectangle> obstaclesLocal)
    {
        GroundLocalY = groundLocalY;
        ObstaclesLocal = Array.AsReadOnly(obstaclesLocal.ToArray());
        Invalidate();
    }

    internal Rectangle VisibleScreenBounds(Form window)
    {
        if (!IsHandleCreated || !window.Visible || window.WindowState == FormWindowState.Minimized)
            return Rectangle.Empty;
        Rectangle bounds = RectangleToScreen(ClientRectangle);
        for (Control? control = this; control is not null; control = control.Parent)
        {
            if (!control.Visible || !control.IsHandleCreated)
                return Rectangle.Empty;
            bounds = Rectangle.Intersect(bounds, control.RectangleToScreen(control.ClientRectangle));
        }

        return bounds;
    }

    internal Rectangle? GroundScreenBounds(Form window)
    {
        if (!IsHandleCreated)
            return null;
        Rectangle ground = RectangleToScreen(new Rectangle(0, GroundLocalY + PetAnimationCatalog.FrameHeight - 1, ClientSize.Width, 1));
        ground = Rectangle.Intersect(ground, VisibleScreenBounds(window));
        return ground.Width > 0 && ground.Height > 0 ? ground : null;
    }

    internal PetEnvironment ReadEnvironment(Form window, Point cursor, IReadOnlyList<HatSurface> surfaces) => new(IsHandleCreated ? PointToScreen(Point.Empty) : Point.Empty, ClientSize.Width, GroundLocalY, window.ClientSize.Width, VisibleScreenBounds(window), window.Visible && window.WindowState != FormWindowState.Minimized && window.RectangleToScreen(window.ClientRectangle).Contains(cursor), cursor, ObstaclesLocal, surfaces);
}
