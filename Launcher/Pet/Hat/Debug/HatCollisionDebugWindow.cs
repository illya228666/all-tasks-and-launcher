using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace Launcher.Pet.Hat.Debug;

internal sealed class HatCollisionDebugWindow : Form
{
    private static readonly Color TransparencyColor = Color.FromArgb(1, 0, 1);
    private static readonly Color WindowColor = Color.FromArgb(255, 80, 80);
    private static readonly Color IconColor = Color.FromArgb(40, 220, 255);
    private static readonly Color TaskbarColor = Color.FromArgb(255, 205, 60);
    private static readonly Color HatColor = Color.Lime;

    private DesktopSurface[] _surfaces = Array.Empty<DesktopSurface>();
    private HatCollisionSegment[] _hatSegments = Array.Empty<HatCollisionSegment>();
    private HatCollisionConnector[] _hatConnectors = Array.Empty<HatCollisionConnector>();
    private Point? _hatLocation;
    private Size _hatSize;

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

    internal void UpdateDebug(
        IReadOnlyList<DesktopSurface> surfaces,
        Point? hatLocation,
        Size hatSize,
        IReadOnlyList<HatCollisionSegment> hatSegments,
        IReadOnlyList<HatCollisionConnector> hatConnectors)
    {
        _surfaces = surfaces.ToArray();
        _hatLocation = hatLocation;
        _hatSize = hatSize;
        _hatSegments = hatSegments.ToArray();
        _hatConnectors = hatConnectors.ToArray();

        Rectangle virtualScreen = SystemInformation.VirtualScreen;
        if (Bounds != virtualScreen)
            Bounds = virtualScreen;

        if (!Visible)
            Show();

        // И HatWindow, и debug overlay являются TopMost. Принудительно поднимаем
        // debug-окно на вершину topmost-группы, чтобы профиль шляпы рисовался поверх неё.
        SetWindowPos(
            Handle,
            HwndTopMost,
            0,
            0,
            0,
            0,
            SwpNoSize | SwpNoMove | SwpNoActivate);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.None;

        using var windowPen = new Pen(WindowColor, 1f) { DashStyle = DashStyle.Dash };
        using var windowTopPen = new Pen(WindowColor, 3f);
        using var iconPen = new Pen(IconColor, 1f) { DashStyle = DashStyle.Dash };
        using var iconTopPen = new Pen(IconColor, 3f);
        using var taskbarPen = new Pen(TaskbarColor, 3f);
        using var hatPen = new Pen(HatColor, 3f);

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
                case DesktopSurfaceType.Taskbar:
                    e.Graphics.DrawLine(taskbarPen, rect.Left, rect.Top, rect.Right, rect.Top);
                    break;
            }
        }

        DrawHatProfile(e.Graphics, hatPen);
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
            graphics.DrawLine(
                pen,
                originX + segment.Left,
                y,
                originX + segment.Right,
                y);
        }

        foreach (HatCollisionConnector connector in _hatConnectors)
        {
            float x = originX + connector.X;
            graphics.DrawLine(
                pen,
                x,
                originY + connector.Top,
                x,
                originY + connector.Bottom);
        }

        using var boundsPen = new Pen(HatColor, 1f) { DashStyle = DashStyle.Dot };
        graphics.DrawRectangle(
            boundsPen,
            originX,
            originY,
            Math.Max(1, _hatSize.Width - 1),
            Math.Max(1, _hatSize.Height - 1));
    }

    private static void DrawSurface(
        Graphics graphics,
        Rectangle rect,
        Pen outlinePen,
        Pen topPen)
    {
        if (rect.Width <= 0)
            return;

        if (rect.Height > 1)
            graphics.DrawRectangle(
                outlinePen,
                rect.Left,
                rect.Top,
                Math.Max(1, rect.Width - 1),
                Math.Max(1, rect.Height - 1));

        graphics.DrawLine(topPen, rect.Left, rect.Top, rect.Right, rect.Top);
    }

    private Rectangle ToClient(Rectangle screenRectangle) =>
        new(
            screenRectangle.Left - Bounds.Left,
            screenRectangle.Top - Bounds.Top,
            screenRectangle.Width,
            screenRectangle.Height);

    private static readonly IntPtr HwndTopMost = new(-1);
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoActivate = 0x0010;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        IntPtr window,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);
}
