using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace Launcher.Pet.Windows.Drawing;

internal sealed class SpritePixelMask
{
    private readonly bool[] _opaque;
    private readonly int _width;
    internal SpritePixelMask(Bitmap image)
    {
        _width = image.Width;
        _opaque = new bool[image.Width * image.Height];
        var data = image.LockBits(new(Point.Empty, image.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            var row = new byte[image.Width * 4];
            for (int y = 0; y < image.Height; y++)
            {
                Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), row, 0, row.Length);
                for (int x = 0; x < image.Width; x++) _opaque[y * _width + x] = row[x * 4 + 3] != 0;
            }
        }
        finally { image.UnlockBits(data); }
    }
    internal bool Contains(int x, int y) => _opaque[y * _width + x];
}
