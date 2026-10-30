using System.Windows;
using System.Windows.Media;

namespace EspController.Controls;

// Renders a real 00-99 seven-segment face without image assets.
public sealed class SevenSegmentView : FrameworkElement
{
    public static readonly DependencyProperty NumberProperty = DependencyProperty.Register(
        nameof(Number), typeof(int), typeof(SevenSegmentView),
        new FrameworkPropertyMetadata(42, FrameworkPropertyMetadataOptions.AffectsRender));
    public int Number
    {
        get => (int)GetValue(NumberProperty);
        set => SetValue(NumberProperty, value);
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
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(14, 17, 25)), null, new Rect(0, 0, 170, 116), 13, 13);
        int number = Math.Clamp(Number, 0, 99);
        DrawDigit(dc, number / 10, 17);
        DrawDigit(dc, number % 10, 93);
        dc.Pop();
    }

    private static void DrawDigit(DrawingContext dc, int number, double left)
    {
        dc.PushTransform(new TranslateTransform(left, 8));
        byte mask = Digits[number];
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
}
