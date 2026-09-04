using System.Drawing;
using Launcher.Pet.Animation;
using Launcher.UI.Controls;

namespace Launcher.Pet.Rendering;

internal sealed class PetRenderer : IDisposable
{
    private readonly Form _window;
    private readonly PetState _state;
    private readonly Bitmap _atlas;
    private readonly Bitmap _atlasWithoutHat;
    private Panel? _panel;
    private PetTheme _theme;
    private int _groundY;
    private bool _hatAttached = true;
    private bool _disposed;

    internal PetRenderer(Form window, PetState state)
    {
        _window = window;
        _state = state;

        try
        {
            _atlas = LoadAtlas("spritesheet_sumrak_hat.png");
            _atlasWithoutHat = LoadAtlas("spritesheet_sumrak_no_hat.png");
        }
        catch
        {
            Dispose();
            throw;
        }

        PetAnimationCatalog.Validate(_atlas);
    }

    internal event Action? HatRemovalRequested;

    internal int RequiredHostWidth => PetAnimationCatalog.FrameWidth;
    internal int RequiredHostHeight => PetAnimationCatalog.FrameHeight;
    internal int ClientWidth => _panel?.ClientSize.Width ?? 0;
    internal bool IsReady => _panel is { IsDisposed: false, IsHandleCreated: true };

    internal void SetHatAttached(bool attached)
    {
        _hatAttached = attached;
        _panel?.Invalidate();
    }

    internal void ApplyTheme(PetTheme theme)
    {
        _theme = theme;
        _panel?.Invalidate();
    }

    internal void AttachHost(Panel panel, int groundY)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(PetRenderer));

        if (_panel is not null)
        {
            _panel.Paint -= Panel_Paint;
            _panel.MouseDown -= Panel_MouseDown;
        }

        _panel = panel;
        _groundY = groundY;
        _panel.Paint += Panel_Paint;
        _panel.MouseDown += Panel_MouseDown;
        EnableDoubleBuffer(_panel);
        ClampPosition();
    }

    internal void Invalidate() => _panel?.Invalidate();

    internal void ClampPosition()
    {
        if (_panel is null)
            return;

        float minX = PetAnimationCatalog.EdgePadding;
        float maxX = Math.Max(minX,
            _panel.ClientSize.Width - PetAnimationCatalog.FrameWidth - PetAnimationCatalog.EdgePadding);
        _state.X = float.IsNaN(_state.X)
            ? Math.Clamp((_panel.ClientSize.Width - PetAnimationCatalog.FrameWidth) / 2f, minX, maxX)
            : Math.Clamp(_state.X, minX, maxX);
    }

    internal Point? GetCenterScreen()
    {
        if (!IsReady)
            return null;

        ClampPosition();
        return _panel!.PointToScreen(new Point(
            (int)Math.Round(_state.X) + PetAnimationCatalog.FrameWidth / 2,
            _groundY + PetAnimationCatalog.FrameHeight / 2));
    }

    internal Point? GetSpeechHead()
    {
        if (!_window.Visible || _window.WindowState == FormWindowState.Minimized || !IsReady || _panel!.Parent is null)
            return null;

        Rectangle destination = GetDestinationRectangle();
        Point head = _panel.PointToScreen(new Point(
            destination.X + PetAnimationCatalog.BodyAnchorXByRow[_state.Row][_state.Frame],
            destination.Y + PetAnimationCatalog.HeadHitHeight / 2));
        for (Control? control = _panel; control is not null; control = control.Parent)
            if (!control.Visible || !control.ClientRectangle.Contains(control.PointToClient(head)))
                return null;
        return head;
    }

    internal bool IsHeadAtScreenPoint(Point screenPoint)
    {
        if (_window.WindowState == FormWindowState.Minimized || !IsReady)
            return false;
        for (Control? control = _panel; control is not null; control = control.Parent)
            if (!control.Visible || !control.RectangleToScreen(control.ClientRectangle).Contains(screenPoint))
                return false;

        Point panelPoint = _panel!.PointToClient(screenPoint);
        return _panel.GetChildAtPoint(panelPoint, GetChildAtPointSkip.Invisible) is null
            && IsHeadAt(panelPoint);
    }

    internal bool IsBelowBottomCardRow()
    {
        if (_panel is null)
            return false;

        List<AppCardControl> cards = _panel.Controls.OfType<AppCardControl>().ToList();
        if (cards.Count == 0)
            return false;

        int bottomRowTop = cards.Max(card => card.Top);
        List<AppCardControl> bottomRow = cards.Where(card => card.Top == bottomRowTop).ToList();
        int left = bottomRow.Min(card => card.Left);
        int right = bottomRow.Max(card => card.Right);
        return PetAnimationCatalog.IsInsideCardSpan(
            _state.X + PetAnimationCatalog.CardProbeOffset, left, right);
    }

    private static Bitmap LoadAtlas(string fileName)
    {
        using var source = new Bitmap(Path.Combine(AppContext.BaseDirectory, "Resources", fileName));
        if (source.Width != PetAnimationCatalog.AtlasColumns * PetAnimationCatalog.CellWidth
            || source.Height != PetAnimationCatalog.AtlasRows * PetAnimationCatalog.CellHeight)
            throw new InvalidDataException(
                $"Unerwartete Sumrak-Atlasgroesse ({fileName}): {source.Width}x{source.Height}.");
        return new Bitmap(source);
    }

    private Rectangle GetDestinationRectangle()
    {
        ClampPosition();
        float jumpLift = _state.JumpSequence is not null
            ? _state.JumpSequence[_state.JumpIndex].Lift * _state.JumpPeak
            : 0f;
        Point offset = PetAnimationCatalog.GetFrameOffset(_state.Row, _state.Frame);
        return new Rectangle(
            (int)Math.Round(_state.X) + offset.X,
            _groundY + offset.Y - (int)Math.Round(jumpLift),
            PetAnimationCatalog.CellWidth,
            PetAnimationCatalog.CellHeight);
    }

    private bool IsHeadAt(Point point)
    {
        Rectangle destination = GetDestinationRectangle();
        Bitmap atlas = _hatAttached ? _atlas : _atlasWithoutHat;
        if (!destination.Contains(point))
            return false;

        Rectangle source = PetAnimationCatalog.GetSourceRectangle(_state.Row, _state.Frame);
        int localX = point.X - destination.X;
        int localY = point.Y - destination.Y;
        if (atlas.GetPixel(source.X + localX, source.Y + localY).A == 0)
            return false;

        // Верх фигуры ищется в текущем кадре, поэтому приседание и прыжок
        // не оставляют область захвата на прежней высоте.
        for (int y = 0; y <= localY; y++)
        for (int x = 0; x < PetAnimationCatalog.CellWidth; x++)
            if (atlas.GetPixel(source.X + x, source.Y + y).A != 0)
                return localY - y < PetAnimationCatalog.HeadHitHeight;
        return false;
    }

    private void Panel_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || !_hatAttached || !IsHeadAt(e.Location))
            return;
        HatRemovalRequested?.Invoke();
    }

    private void Panel_Paint(object? sender, PaintEventArgs e)
    {
        e.Graphics.Clear(_theme.Background);
        using (var pen = new Pen(_theme.Border, 1f))
        {
            var zone = new Rectangle(0, _groundY, _panel!.ClientSize.Width - 1,
                PetAnimationCatalog.FrameHeight - 1);
            e.Graphics.DrawRectangle(pen, zone);
        }

        Rectangle destination = GetDestinationRectangle();
        Rectangle source = PetAnimationCatalog.GetSourceRectangle(_state.Row, _state.Frame);
        e.Graphics.DrawImage(_hatAttached ? _atlas : _atlasWithoutHat,
            destination, source, GraphicsUnit.Pixel);
    }

    private static void EnableDoubleBuffer(Control control)
    {
        typeof(Control)
            .GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.SetValue(control, true, null);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        if (_panel is not null)
        {
            _panel.Paint -= Panel_Paint;
            _panel.MouseDown -= Panel_MouseDown;
        }
        _atlasWithoutHat?.Dispose();
        _atlas?.Dispose();
        HatRemovalRequested = null;
    }
}
