using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.Windows.Forms;

namespace Launcher.UI.Controls;

internal sealed class SpeechBubbleWindow : TransparentOverlayWindow
{
    // Размеры в экранных пикселях; ширина относится к тексту, без отступов и хвоста.
    private const int MaxTextWidth = 260;
    private const int TextPadding = 12;
    private const int CornerRadius = 10;
    private const int TailWidth = 12;
    private const int TailHalfHeight = 6;
    // Расстояние от оси головы до кончика хвоста.
    private const int HeadOffset = 40;
    // Размер шрифта в пунктах. Разметка выполняется один раз по полной фразе.
    private const float TextSizePt = 10f;

    private readonly Font _textFont = new("Segoe UI", TextSizePt, FontStyle.Regular, GraphicsUnit.Point);
    private readonly StringFormat _textFormat = StringFormat.GenericTypographic;
    private readonly List<(int Start, int Length)> _lines = new();
    private string _phrase = "";
    private int[] _letterStarts = Array.Empty<int>();
    private Size _bubbleSize;
    private int _lineHeight;
    private int _paintedLetters = -1;
    private bool _onRight;
    private int _tailY;
    private Color _surface, _text, _border;

    internal int LetterCount => _letterStarts.Length;

    internal SpeechBubbleWindow() : base(clickThrough: true)
    {
        _textFormat.FormatFlags |= StringFormatFlags.NoWrap | StringFormatFlags.MeasureTrailingSpaces;
    }

    internal void SetPhrase(string phrase)
    {
        _phrase = phrase;
        _letterStarts = StringInfo.ParseCombiningCharacters(phrase);
        _paintedLetters = -1;
        _lines.Clear();

        using var bitmap = new Bitmap(1, 1);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        _lineHeight = (int)Math.Ceiling(_textFont.GetHeight(graphics));
        int lineStart = 0, wordStart = 0;
        string line = "";
        float textWidth = 0;
        foreach (string word in phrase.Split(' '))
        {
            string candidate = line.Length == 0 ? word : line + " " + word;
            if (line.Length > 0 && Measure(candidate) > MaxTextWidth)
            {
                _lines.Add((lineStart, line.Length));
                textWidth = Math.Max(textWidth, Measure(line));
                lineStart = wordStart;
                line = word;
            }
            else
                line = candidate;
            wordStart += word.Length + 1;
        }
        _lines.Add((lineStart, line.Length));
        textWidth = Math.Max(textWidth, Measure(line));
        _bubbleSize = new Size(
            (int)Math.Ceiling(textWidth) + TextPadding * 2 + TailWidth,
            _lineHeight * _lines.Count + TextPadding * 2);

        // Проверка редактируемого набора: отдельное слово должно помещаться в строку.
        Debug.Assert(textWidth <= MaxTextWidth);
        Debug.Assert(_lines.All(item => item.Start >= 0 && item.Start + item.Length <= phrase.Length));

        float Measure(string value) => graphics.MeasureString(value, _textFont, PointF.Empty, _textFormat).Width;
    }

    internal void Display(Point head, int visibleLetters, Color surface, Color text, Color border)
    {
        Rectangle workArea = Screen.FromPoint(head).WorkingArea;
        bool onRight = head.X + HeadOffset + _bubbleSize.Width <= workArea.Right;
        int x = onRight ? head.X + HeadOffset : head.X - HeadOffset - _bubbleSize.Width;
        int y = head.Y - _bubbleSize.Height / 2;
        var location = new Point(
            Math.Clamp(x, workArea.Left, Math.Max(workArea.Left, workArea.Right - _bubbleSize.Width)),
            Math.Clamp(y, workArea.Top, Math.Max(workArea.Top, workArea.Bottom - _bubbleSize.Height)));
        int tailMargin = CornerRadius + TailHalfHeight + 1;
        int tailY = Math.Clamp(head.Y - location.Y, tailMargin, _bubbleSize.Height - tailMargin);
        visibleLetters = Math.Clamp(visibleLetters, 0, LetterCount);
        bool redraw = _paintedLetters != visibleLetters || _onRight != onRight || _tailY != tailY
            || _surface != surface || _text != text || _border != border;
        if (redraw)
        {
            _paintedLetters = visibleLetters;
            _onRight = onRight;
            _tailY = tailY;
            _surface = surface;
            _text = text;
            _border = border;
            DrawBubble();
        }
        ShowAt(location);
    }

    private void DrawBubble()
    {
        using var bitmap = new Bitmap(_bubbleSize.Width, _bubbleSize.Height, PixelFormat.Format32bppPArgb);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        float left = (_onRight ? TailWidth : 0) + 0.5f;
        float right = _bubbleSize.Width - (_onRight ? 0 : TailWidth) - 0.5f;
        float top = 0.5f, bottom = _bubbleSize.Height - 0.5f;
        int diameter = CornerRadius * 2;
        using var path = new GraphicsPath();
        path.AddArc(left, top, diameter, diameter, 180, 90);
        path.AddArc(right - diameter, top, diameter, diameter, 270, 90);
        if (!_onRight)
        {
            path.AddLine(right, top + CornerRadius, right, _tailY - TailHalfHeight);
            path.AddLine(right, _tailY - TailHalfHeight, _bubbleSize.Width - 0.5f, _tailY);
            path.AddLine(_bubbleSize.Width - 0.5f, _tailY, right, _tailY + TailHalfHeight);
        }
        path.AddArc(right - diameter, bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(left, bottom - diameter, diameter, diameter, 90, 90);
        if (_onRight)
        {
            path.AddLine(left, bottom - CornerRadius, left, _tailY + TailHalfHeight);
            path.AddLine(left, _tailY + TailHalfHeight, 0.5f, _tailY);
            path.AddLine(0.5f, _tailY, left, _tailY - TailHalfHeight);
        }
        path.CloseFigure();
        using var background = new SolidBrush(_surface);
        using var outline = new Pen(_border);
        using var foreground = new SolidBrush(_text);
        graphics.FillPath(background, path);
        graphics.DrawPath(outline, path);

        int visibleChars = _paintedLetters == LetterCount ? _phrase.Length : _letterStarts[_paintedLetters];
        for (int index = 0; index < _lines.Count; index++)
        {
            (int start, int length) = _lines[index];
            int count = Math.Clamp(visibleChars - start, 0, length);
            if (count > 0)
                graphics.DrawString(_phrase.Substring(start, count), _textFont, foreground,
                    new PointF(left + TextPadding, TextPadding + index * _lineHeight), _textFormat);
        }
        SetImage(bitmap);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _textFont.Dispose();
            _textFormat.Dispose();
        }
        base.Dispose(disposing);
    }
}
