using Launcher.Pet.Animation;

namespace Launcher.Pet.Windows.Drawing;
internal sealed class PetImages : IDisposable
{
    internal Bitmap WithHat { get; private set; } = null!;
    internal Bitmap WithoutHat { get; private set; } = null!;
    internal Bitmap Hat { get; private set; } = null!;

    internal PetImages()
    {
        try
        {
            WithHat = Read("spritesheet_sumrak_hat.png", new(PetAnimationCatalog.AtlasColumns * PetAnimationCatalog.CellWidth, PetAnimationCatalog.AtlasRows * PetAnimationCatalog.CellHeight));
            WithoutHat = Read("spritesheet_sumrak_no_hat.png", WithHat.Size);
            Hat = Read("hat_small.png", new(109, 64));
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    private static Bitmap Read(string name, Size size)
    {
        using var original = new Bitmap(Path.Combine(AppContext.BaseDirectory, "Resources", name));
        if (original.Size != size)
            throw new InvalidDataException($"Unerwartete Bildgroesse ({name}): {original.Size}.");
        return new(original);
    }

    public void Dispose()
    {
        Hat?.Dispose();
        WithoutHat?.Dispose();
        WithHat?.Dispose();
    }
}
