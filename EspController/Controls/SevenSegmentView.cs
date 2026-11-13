using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace EspController.Controls;

// Only the physical RIGHT digit is connected. Click its A–G bars to toggle
// individual segments; the left outline is deliberately dark.
public sealed class SevenSegmentView : FrameworkElement
{
    public static readonly DependencyProperty NumberProperty = DependencyProperty.Register(
        nameof(Number), typeof(int), typeof(SevenSegmentView),
        new FrameworkPropertyMetadata(4, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty ManualModeProperty = DependencyProperty.Register(
        nameof(ManualMode), typeof(bool), typeof(SevenSegmentView),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty SegmentMaskProperty = DependencyProperty.Register(
        nameof(SegmentMask), typeof(int), typeof(SevenSegmentView),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty SegmentCommandProperty = DependencyProperty.Register(
        nameof(SegmentCommand), typeof(ICommand), typeof(SevenSegmentView));

    public int Number
    {
        get => (int)GetValue(NumberProperty);
        set => SetValue(NumberProperty, value);
    }
    public bool ManualMode
    {
        get => (bool)GetValue(ManualModeProperty);
        set => SetValue(ManualModeProperty, value);
    }
    public int SegmentMask
    {
        get => (int)GetValue(SegmentMaskProperty);
        set => SetValue(SegmentMaskProperty, value);
    }
    public ICommand? SegmentCommand
    {
        get => (ICommand?)GetValue(SegmentCommandProperty);
        set => SetValue(SegmentCommandProperty, value);
    }

    private static readonly byte[] Digits = {
        0b00111111, 0b00000110, 0b01011011, 0b01001111, 0b01100110,
        0b01101101, 0b01111101, 0b00000111, 0b01111111, 0b01101111
    };
    private static readonly Rect[] Bars = {
        new(14, 5, 36, 8),   // A
        new(51, 14, 8, 31),  // B
        new(51, 55, 8, 31),  // C
        new(14, 87, 36, 8),  // D
        new(5, 55, 8, 31),   // E
        new(5, 14, 8, 31),   // F
        new(14, 46, 36, 8)  // G
    };
    private static readonly Brush Off = MakeBrush("#45242B");
    private static readonly Brush Glow = MakeBrush("#56341F");
    private static readonly Brush On = MakeBrush("#FF634F");

    private static Brush MakeBrush(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }

    protected override Size MeasureOverride(Size availableSize) => new(280, 172);

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        double width = Math.Max(1, RenderSize.Width), height = Math.Max(1, RenderSize.Height);
        dc.PushTransform(new ScaleTransform(width / 170, height / 116));
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(14, 17, 25)), null,
            new Rect(0, 0, 170, 116), 13, 13);
        DrawDigit(dc, 0, 17); // left digit unconnected
        int mask = ManualMode ? Math.Clamp(SegmentMask, 0, 127) : Digits[Math.Clamp(Number, 0, 9)];
        DrawDigit(dc, mask, 93);
        dc.Pop();
    }

    private static void DrawDigit(DrawingContext dc, int mask, double left)
    {
        dc.PushTransform(new TranslateTransform(left, 8));
        for (int i = 0; i < Bars.Length; i++)
        {
            bool active = (mask & (1 << i)) != 0;
            Rect rectangle = Bars[i];
            dc.DrawRoundedRectangle(active ? Glow : Off, null,
                new Rect(rectangle.X - 1.4, rectangle.Y - 1.4, rectangle.Width + 2.8, rectangle.Height + 2.8), 3.5, 3.5);
            dc.DrawRoundedRectangle(active ? On : Off, null, rectangle, 2.7, 2.7);
        }
        dc.Pop();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (ActualWidth <= 0 || ActualHeight <= 0) return;
        Point where = e.GetPosition(this);
        // Invert the scale and the RIGHT-digit translation from OnRender.
        Point relative = new(where.X * 170 / ActualWidth - 93,
                             where.Y * 116 / ActualHeight - 8);
        for (int i = 0; i < Bars.Length; ++i)
        {
            // Slightly enlarge the hit target for easier debugging.
            Rect hit = Bars[i];
            hit.Inflate(2, 2);
            if (!hit.Contains(relative)) continue;
            if (SegmentCommand?.CanExecute(i) == true)
            {
                SegmentCommand.Execute(i);
                e.Handled = true;
            }
            return;
        }
    }
}
