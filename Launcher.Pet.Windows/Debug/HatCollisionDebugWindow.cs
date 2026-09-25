using static Launcher.Pet.Windows.Debug.DebugWindowApi;
using Launcher.Pet.Exploration;
using Launcher.Pet.Hat;
using Launcher.Pet.Windows.Desktop;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace Launcher.Pet.Windows.Debug;
internal sealed class HatCollisionDebugWindow : Form
{
    private static readonly Color TransparencyColor = Color.FromArgb(1, 0, 1);
    private static readonly Color WindowColor = Color.FromArgb(255, 80, 80);
    private static readonly Color IconColor = Color.FromArgb(40, 220, 255);
    private static readonly Color TaskbarColor = Color.FromArgb(255, 205, 60);
    private static readonly Color HatColor = Color.Lime;
    private static readonly Color PlatformColor = Color.FromArgb(238, 94, 255);
    private static readonly Color BridgeColor = Color.FromArgb(120, 245, 208);
    private DesktopSurface[] _surfaces = Array.Empty<DesktopSurface>();
    private HatCollisionSegment[] _hatSegments = Array.Empty<HatCollisionSegment>();
    private HatCollisionConnector[] _hatConnectors = Array.Empty<HatCollisionConnector>();
    private Point? _hatLocation;
    private Size _hatSize;
    private RuinScene? _ruins;
    private RuinLink[] _route = Array.Empty<RuinLink>();
    private Point _ruinOrigin;
    private Point? _routeStart, _routeTarget;
    internal HatCollisionDebugWindow()
    {
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        BackColor = TransparencyColor;
        TransparencyKey = TransparencyColor;
        DoubleBuffered = true;
        Bounds = SystemInformation.VirtualScreen;
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams parameters = base.CreateParams;
            // WS_EX_TRANSPARENT | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW.
            parameters.ExStyle |= 0x00000020 | 0x08000000 | 0x00000080;
            return parameters;
        }
    }

    internal void UpdateDebug(IReadOnlyList<DesktopSurface> surfaces, Point? hatLocation, Size hatSize,
        IReadOnlyList<HatCollisionSegment> hatSegments, IReadOnlyList<HatCollisionConnector> hatConnectors,
        RuinScene? ruins, Point ruinOrigin, IReadOnlyList<RuinLink> route, Point? routeStart, Point? routeTarget)
    {
        _surfaces = surfaces.ToArray();
        _hatLocation = hatLocation;
        _hatSize = hatSize;
        _hatSegments = hatSegments.ToArray();
        _hatConnectors = hatConnectors.ToArray();
        _ruins = ruins;
        _ruinOrigin = ruinOrigin;
        _route = route.ToArray();
        _routeStart = routeStart;
        _routeTarget = routeTarget;
        Rectangle virtualScreen = SystemInformation.VirtualScreen;
        if (Bounds != virtualScreen)
            Bounds = virtualScreen;
        if (!Visible)
            Show();
        // И HatWindow, и debug overlay являются TopMost. Принудительно поднимаем
        // debug-окно на вершину topmost-группы, чтобы профиль шляпы рисовался поверх неё.
        SetWindowPos(Handle, HwndTopMost, 0, 0, 0, 0, SwpNoSize | SwpNoMove | SwpNoActivate);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.None;
        using var windowPen = new Pen(WindowColor, 1f)
        {
            DashStyle = DashStyle.Dash
        };
        using var windowTopPen = new Pen(WindowColor, 3f);
        using var iconPen = new Pen(IconColor, 1f)
        {
            DashStyle = DashStyle.Dash
        };
        using var iconTopPen = new Pen(IconColor, 3f);
        using var taskbarPen = new Pen(TaskbarColor, 3f);
        using var hatPen = new Pen(HatColor, 3f);
        using var platformPen = new Pen(PlatformColor, 3f);
        using var bridgePen = new Pen(BridgeColor, 3f);
        foreach (DesktopSurface surface in _surfaces)
        {
            Rectangle rect = ToClient(surface.Bounds);
            switch (surface.Type)
            {
                case DesktopSurfaceType.Window:
                    DrawSurface(e.Graphics, rect, windowPen, windowTopPen);
                    break;
                case DesktopSurfaceType.DesktopIcon:
                    DrawSurface(e.Graphics, rect, iconPen, iconTopPen);
                    break;
                case DesktopSurfaceType.PetGround:
                case DesktopSurfaceType.Taskbar:
                    e.Graphics.DrawLine(taskbarPen, rect.Left, rect.Top, rect.Right, rect.Top);
                    break;
                case DesktopSurfaceType.Ruin:
                    Pen ruinPen = surface.Identity.ItemKey?.StartsWith("bridge:", StringComparison.Ordinal) == true ? bridgePen : platformPen;
                    e.Graphics.DrawLine(ruinPen, rect.Left, rect.Top, rect.Right, rect.Top);
                    e.Graphics.DrawLine(ruinPen, rect.Left, rect.Top - 4, rect.Left, rect.Top + 4);
                    e.Graphics.DrawLine(ruinPen, rect.Right, rect.Top - 4, rect.Right, rect.Top + 4);
                    break;
            }
        }

        DrawHatProfile(e.Graphics, hatPen);
        DrawRoute(e.Graphics);
    }

    private void DrawRoute(Graphics graphics)
    {
        if (_ruins is null || _routeStart is null || (_route.Length == 0 && _routeTarget is null)) return;
        const float routeLift = 8;
        var platforms = _ruins.Platforms.ToDictionary(p => p.Id);
        PointF Local(float x, float y) => new(_ruinOrigin.X + x - Bounds.Left, _ruinOrigin.Y + y - Bounds.Top - routeLift);
        PointF current = new(_routeStart.Value.X - Bounds.Left, _routeStart.Value.Y - Bounds.Top - routeLift);
        using var pen = new Pen(Color.FromArgb(255, 154, 64), 2.5f) { DashStyle = DashStyle.Dash, EndCap = LineCap.Round };
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        foreach (var link in _route)
        {
            if (!platforms.TryGetValue(link.From, out RuinPlatform? from) || !platforms.TryGetValue(link.To, out RuinPlatform? to)) continue;
            PointF approach = Local(link.X, from.Y);
            PointF landing = Local(link.EndX ?? to.Center, to.Y);
            graphics.DrawLine(pen, current, approach);
            if (link.Kind == RuinLinkKind.Climb)
            {
                PointF top = from.Y < to.Y ? new(link.X, from.Y) : new(link.EndX ?? link.X, to.Y);
                PointF bottom = from.Y < to.Y ? new(link.EndX ?? link.X, to.Y) : new(link.X, from.Y);
                PointF previous = approach;
                for (int step = 1; step <= 12; step++)
                {
                    float y = from.Y + (to.Y - from.Y) * step / 12f;
                    PointF point = Local(RuinMotion.ClimbX(top, bottom, y), y);
                    graphics.DrawLine(pen, previous, point);
                    previous = point;
                }
            }
            else graphics.DrawLine(pen, approach, landing);
            current = landing;
        }
        if (_routeTarget is Point target)
            graphics.DrawLine(pen, current, new PointF(target.X - Bounds.Left, target.Y - Bounds.Top - routeLift));
        using var marker = new SolidBrush(Color.FromArgb(255, 154, 64));
        PointF end = _routeTarget is Point goal ? new(goal.X - Bounds.Left, goal.Y - Bounds.Top - routeLift) : current;
        graphics.FillEllipse(marker, end.X - 4, end.Y - 4, 8, 8);
    }

    private void DrawHatProfile(Graphics graphics, Pen pen)
    {
        if (_hatLocation is null || _hatSize.IsEmpty || _hatSegments.Length == 0)
            return;
        float originX = _hatLocation.Value.X - Bounds.Left;
        float originY = _hatLocation.Value.Y - Bounds.Top;
        foreach (HatCollisionSegment segment in _hatSegments)
        {
            float y = originY + segment.ContactY;
            graphics.DrawLine(pen, originX + segment.Left, y, originX + segment.Right, y);
        }

        foreach (HatCollisionConnector connector in _hatConnectors)
        {
            float x = originX + connector.X;
            graphics.DrawLine(pen, x, originY + connector.Top, x, originY + connector.Bottom);
        }

        using var boundsPen = new Pen(HatColor, 1f)
        {
            DashStyle = DashStyle.Dot
        };
        graphics.DrawRectangle(boundsPen, originX, originY, Math.Max(1, _hatSize.Width - 1), Math.Max(1, _hatSize.Height - 1));
    }

    private static void DrawSurface(Graphics graphics, Rectangle rect, Pen outlinePen, Pen topPen)
    {
        if (rect.Width <= 0)
            return;
        if (rect.Height > 1)
            graphics.DrawRectangle(outlinePen, rect.Left, rect.Top, Math.Max(1, rect.Width - 1), Math.Max(1, rect.Height - 1));
        graphics.DrawLine(topPen, rect.Left, rect.Top, rect.Right, rect.Top);
    }

    private Rectangle ToClient(Rectangle screenRectangle) => new(screenRectangle.Left - Bounds.Left, screenRectangle.Top - Bounds.Top, screenRectangle.Width, screenRectangle.Height);
}
