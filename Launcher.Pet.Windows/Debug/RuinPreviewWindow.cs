using System.Diagnostics;
using System.Drawing.Drawing2D;
using Launcher.Pet.Data;
using Launcher.Pet.Exploration;
using Launcher.Pet.Hat;
using Launcher.Pet.Sprites;
using Launcher.Pet.Windows.Drawing;

namespace Launcher.Pet.Windows.Debug;

// Interactive authoring surface. Uses production world, geometry and rendering; no test runner.
public sealed class RuinPreviewWindow : Form
{
    private readonly RuinRenderer _renderer = new();
    private readonly PetImages _images = new();
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 16 };
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private PetWorld _world = null!;
    private RuinScene _ruins = null!;
    private PetScene? _pet;
    private Bitmap? _layer;
    private Size _worldSize = new(1280, 760);
    private DesktopSeed[] _seed = Array.Empty<DesktopSeed>();
    private long _lastWall, _now, _awakeAt = -1;
    private int _variation = 731, _scenario;
    private bool _paused, _light, _geometry;
    private Point _cursor;
    private RectangleF _viewport;
    private string _lastError = "";
    public RuinPreviewWindow()
    {
        Text = "Sumrak — Ruins Studio";
        DoubleBuffered = true;
        KeyPreview = true;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new(Math.Min(1300, Screen.PrimaryScreen!.WorkingArea.Width - 100), Math.Min(850, Screen.PrimaryScreen.WorkingArea.Height - 80));
        ResetScene();
        _timer.Tick += Tick;
        _timer.Start();
    }
    private void ResetScene()
    {
        _worldSize = _scenario switch { 2 => new(800, 600), 3 => new(900, 1200), _ => new(1280, 760) };
        var seed = new List<DesktopSeed>();
        if (_scenario != 1)
        {
            for (int i = 0; i < (_scenario == 2 ? 16 : 8); i++)
            {
                int x = 25 + i / 7 * 78, y = 28 + i % 7 * 95;
                seed.Add(new(new(x, y, 40, 40), new(x - 10, y + 43, 62, 26), true));
            }
            if (_scenario == 0) seed.Add(new(new(360, 160, 620, 410), Rectangle.Empty, false));
        }
        _seed = seed.ToArray();
        _ruins = new RuinBuilder(_worldSize, _seed, _variation).Build(_worldSize, new(_worldSize.Width / 2, _worldSize.Height));
        _world = new(new Random(_variation));
        _world.Start(_now);
        _world.Update(_now, new(Point.Empty, _worldSize.Width, _worldSize.Height / 2, _worldSize.Width, new(Point.Empty, _worldSize), false, Point.Empty, Array.Empty<Rectangle>(), Array.Empty<HatSurface>()));
        _world.LeaveLauncher(_now);
        _awakeAt = -1; _pet = null; _lastError = "";
    }
    private void Tick(object? sender, EventArgs e)
    {
        long wall = _clock.ElapsedMilliseconds;
        long elapsed = Math.Min(40, wall - _lastWall);
        _lastWall = wall;
        if (_paused) return;
        _now += elapsed;
        try
        {
            float seconds = _awakeAt < 0 ? 0 : (_now - _awakeAt) / 1000f;
            var surfaces = _ruins.Platforms.Where(p => p.Id == "ruin:floor" || _world.HasLanded && seconds >= p.RevealAt + 0.85f)
                .Select(p => new HatSurface(p.Id, HatSurfaceKind.Ruin, new((int)p.Left, (int)p.Y, (int)(p.Right - p.Left), 1))).ToArray();
            surfaces = surfaces.Concat(_ruins.Bridges.Where(b => _world.HasLanded && seconds >= b.RevealAt + 0.85f)
                .Select(b => new HatSurface(b.Id, HatSurfaceKind.Ruin, new((int)b.Left, (int)b.Y, (int)(b.Right - b.Left), 1)))).ToArray();
            _pet = _world.Update(_now, new(Point.Empty, _worldSize.Width, _worldSize.Height - PetLogicalGeometry.Height, _worldSize.Width,
                new(Point.Empty, _worldSize), _world.IsHatDragging, _cursor, Array.Empty<Rectangle>(), surfaces, "ruin:floor", _world.RenderScale, _ruins, seconds));
            if (_world.HasLanded && _awakeAt < 0) _awakeAt = _now;
            _layer = _awakeAt < 0 ? null : _renderer.Render(_ruins, new(_worldSize.Width / 2, _worldSize.Height), seconds);
        }
        catch (Exception error) { _lastError = error.ToString(); _paused = true; }
        Invalidate();
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;
        g.Clear(Color.FromArgb(20, 26, 34));
        TextRenderer.DrawText(g, "R replay   V variation   C layout   B light/dark   D paths   H toss hat   E earthquake   Space pause", Font, new Point(16, 12), Color.Gainsboro);
        float scale = Math.Min((ClientSize.Width - 32f) / _worldSize.Width, (ClientSize.Height - 94f) / _worldSize.Height);
        _viewport = new((ClientSize.Width - _worldSize.Width * scale) / 2, 42, _worldSize.Width * scale, _worldSize.Height * scale);
        var saved = g.Save();
        g.TranslateTransform(_viewport.X, _viewport.Y); g.ScaleTransform(scale, scale);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        using (var background = new LinearGradientBrush(new Rectangle(Point.Empty, _worldSize), _light ? Color.FromArgb(231, 238, 242) : Color.FromArgb(12, 23, 36), _light ? Color.FromArgb(200, 211, 220) : Color.FromArgb(22, 43, 57), 90))
            g.FillRectangle(background, new Rectangle(Point.Empty, _worldSize));
        foreach (var seed in _seed)
        {
            using var surface = new SolidBrush(seed.IsIcon ? Color.FromArgb(90, 139, 163) : _light ? Color.WhiteSmoke : Color.FromArgb(34, 46, 60));
            g.FillRectangle(surface, seed.Bounds);
            if (seed.IsIcon) g.DrawString("Document", Font, _light ? Brushes.DimGray : Brushes.SlateGray, seed.LabelBounds);
            else { using var border = new Pen(Color.FromArgb(90, 120, 140)); g.DrawRectangle(border, seed.Bounds); g.DrawString("Open window", Font, Brushes.SlateGray, seed.Bounds.X + 18, seed.Bounds.Y + 18); }
        }
        if (_layer is not null) g.DrawImageUnscaled(_layer, 0, 0);
        if (_geometry)
        {
            using var edge = new Pen(Color.FromArgb(130, 138, 224, 205), 1);
            foreach (var link in _ruins.Links.Where(l => l.Kind == RuinLinkKind.Climb && string.CompareOrdinal(l.From, l.To) < 0))
            {
                var from = _ruins.Platforms.First(p => p.Id == link.From); var to = _ruins.Platforms.First(p => p.Id == link.To);
                g.DrawLine(edge, link.X, from.Y, link.X, to.Y);
            }
            foreach (var platform in _ruins.Platforms)
            {
                g.DrawLine(Pens.Orange, platform.Left, platform.Y, platform.Right, platform.Y);
                g.DrawString(platform.Id, Font, Brushes.Orange, platform.Left, platform.Y + 4);
            }
        }
        if (_pet is not null)
        {
            g.DrawImage(_pet.HatAttached ? _images.WithHat : _images.WithoutHat, _pet.SpriteBounds, PetSpriteCatalog.GetSourceRectangle(_pet.Row, _pet.Frame), GraphicsUnit.Pixel);
            if (!_pet.HatAttached) g.DrawImage(_images.Hat, new RectangleF(_pet.Hat.ScreenPosition.X, _pet.Hat.ScreenPosition.Y, HatGeometry.Width * _pet.Hat.Scale, HatGeometry.Height * _pet.Hat.Scale));
            if (_pet.Speech is not null && _pet.HeadScreenPosition is Point head)
                g.DrawString(_pet.Speech[..Math.Min(_pet.VisibleLetters, _pet.Speech.Length)], Font, _light ? Brushes.DarkSlateGray : Brushes.WhiteSmoke, head.X + 35, head.Y - 22);
        }
        g.Restore(saved);
        string status = $"layout {_scenario} / seed {_variation}   {_worldSize.Width}×{_worldSize.Height}   {_ruins.Platforms.Count - 1} islands   {_ruins.Bridges.Count} bridges   {_pet?.Mode}  {_pet?.Row}:{_pet?.Frame}   {(_paused ? "PAUSED" : "LIVE")}";
        TextRenderer.DrawText(g, _lastError.Length > 0 ? _lastError : status, Font, new Rectangle(16, ClientSize.Height - 38, ClientSize.Width - 32, 34), _lastError.Length > 0 ? Color.Salmon : Color.Silver);
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        switch (e.KeyCode)
        {
            case Keys.R: ResetScene(); break;
            case Keys.V: _variation++; ResetScene(); break;
            case Keys.C: _scenario = (_scenario + 1) % 4; ResetScene(); break;
            case Keys.B: _light = !_light; break;
            case Keys.D: _geometry = !_geometry; break;
            case Keys.Space: _paused = !_paused; break;
            case Keys.E: _world.TryStartEarthquake(_now); break;
            case Keys.H:
                var target = _ruins.Platforms.Skip(1).OrderBy(p => p.Y).FirstOrDefault();
                if (target is not null && _world.BeginHatDrag(new((int)target.Center, (int)target.Y - 70))) _world.DropHat(false);
                break;
            case Keys.Escape: Close(); break;
        }
        Invalidate();
    }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (!_viewport.Contains(e.Location)) return;
        _cursor = ToWorld(e.Location);
        if (_pet is not null && ((_pet.HatAttached && _pet.HeadScreenPosition is Point head && Math.Abs(head.X - _cursor.X) < 30 && Math.Abs(head.Y - _cursor.Y) < 35)
            || (!_pet.HatAttached && new Rectangle(_pet.Hat.ScreenPosition, new((int)(HatGeometry.Width * _pet.Scale), (int)(HatGeometry.Height * _pet.Scale))).Contains(_cursor))))
            Capture = _world.BeginHatDrag(_cursor);
    }
    protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); _cursor = ToWorld(e.Location); if (Capture) _world.MoveHat(_cursor); }
    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e); if (!Capture) return; Capture = false;
        _world.DropHat(_pet?.HeadScreenPosition is Point head && Math.Abs(head.X - _cursor.X) < 25 && Math.Abs(head.Y - _cursor.Y) < 30);
    }
    private Point ToWorld(Point point) => new((int)((point.X - _viewport.X) * _worldSize.Width / Math.Max(1, _viewport.Width)), (int)((point.Y - _viewport.Y) * _worldSize.Height / Math.Max(1, _viewport.Height)));
    protected override void Dispose(bool disposing)
    {
        if (disposing) { _timer.Stop(); _timer.Dispose(); _renderer.Dispose(); _images.Dispose(); }
        base.Dispose(disposing);
    }
}
