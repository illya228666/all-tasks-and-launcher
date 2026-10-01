using System.Drawing.Imaging;
using Launcher.Pet.Life;
using Launcher.Pet.Windows.Drawing;

namespace Launcher.Pet.Windows.Windows;

internal sealed class LifeItemWindow : TransparentOverlayWindow
{
    private readonly long _id;
    internal bool Interactive { get; }
    internal event Func<long, Point, bool>? DragStarted;
    internal LifeItemWindow(long id, bool interactive) : base(clickThrough: !interactive)
    {
        _id = id;
        Interactive = interactive;
        Text = "Sumrak world item";
    }
    internal void Display(LifeItemView item, Point origin, double seconds, float diameter = 18, float decomposition = 0)
    {
        int size = Math.Max(28, (int)Math.Ceiling(diameter * 1.7f));
        using var bitmap = new Bitmap(size, size, PixelFormat.Format32bppPArgb);
        using (var graphics = Graphics.FromImage(bitmap)) LifeDrawing.DrawItem(graphics, item.Kind, new(size / 2f, size / 2f), item.Mass, seconds + item.Id * 0.3, diameter, decomposition);
        SetImage(bitmap);
        float lift = item.Holder == LifeHolder.World ? item.Kind == LifeItemKind.Spore ? diameter * (.8f + (float)Math.Sin(seconds + item.Id) * .15f) : diameter * .22f : 0;
        ShowAt(new(origin.X + (int)item.Position.X - size / 2, origin.Y + (int)(item.Position.Y - lift) - size / 2));
        if (item.Holder != LifeHolder.User && Capture) Capture = false;
        RaiseWithoutActivation();
    }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (Interactive && e.Button == MouseButtons.Left && DragStarted?.Invoke(_id, Cursor.Position) == true) Capture = true;
    }
}
