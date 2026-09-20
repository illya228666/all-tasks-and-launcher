using System.Drawing;
namespace Launcher.Pet.Sprites;

public static class PetSpriteCatalog
{
    public const int AtlasColumns = 8;
    public const int AtlasRows = 11;
    public const int AtlasCellWidth = 149;
    public const int AtlasCellHeight = 200;
    public const float RenderScale = 1f;
    private static readonly PetFrameGeometry[][] FramesByRow =
    {
        new PetFrameGeometry[] { new(33, 200, new(33, 45), new(0, 0, 109, 90)), new(33, 200, new(33, 45), new(0, 0, 108, 90)), new(33, 200, new(33, 45), new(0, 0, 109, 90)), new(32, 200, new(32, 45), new(0, 0, 107, 90)), new(33, 200, new(33, 45), new(0, 0, 109, 90)), new(33, 200, new(33, 45), new(0, 0, 109, 90)), new(33, 200, new(33, 45), new(0, 0, 109, 90)) },
        new PetFrameGeometry[] { new(76, 200, new(76, 51), new(0, 7, 146, 88)), new(79, 200, new(79, 56), new(0, 13, 144, 87)), new(76, 200, new(76, 63), new(0, 20, 144, 86)), new(70, 200, new(70, 50), new(0, 7, 136, 87)), new(78, 200, new(78, 57), new(0, 13, 146, 88)), new(73, 200, new(73, 56), new(0, 14, 135, 84)), new(77, 200, new(77, 57), new(0, 14, 144, 87)), new(73, 200, new(73, 54), new(0, 10, 145, 88)) },
        new PetFrameGeometry[] { new(70, 200, new(70, 47), new(0, 2, 146, 89)), new(66, 200, new(66, 61), new(0, 17, 146, 88)), new(60, 200, new(60, 47), new(0, 3, 143, 89)), new(67, 200, new(67, 51), new(0, 7, 145, 88)), new(61, 200, new(61, 53), new(0, 10, 139, 86)), new(61, 200, new(61, 55), new(0, 12, 141, 85)), new(64, 200, new(64, 57), new(0, 14, 141, 85)), new(69, 200, new(69, 61), new(0, 17, 149, 88)) },
        new PetFrameGeometry[] { new(31, 200, new(31, 45), new(0, 0, 104, 90)), new(33, 200, new(33, 45), new(0, 0, 107, 90)), new(44, 200, new(44, 49), new(0, 5, 112, 88)), new(29, 200, new(29, 45), new(0, 0, 104, 90)) },
        new PetFrameGeometry[] { new(36, 200, new(36, 105), new(0, 75, 100, 60)), new(39, 185, new(39, 58), new(0, 21, 106, 74)), new(46, 156, new(46, 35), new(0, 0, 114, 70)), new(41, 193, new(41, 64), new(0, 26, 110, 75)), new(39, 200, new(39, 102), new(0, 71, 102, 62)) },
        new PetFrameGeometry[] { new(26, 200, new(26, 46), new(0, 1, 106, 90)), new(30, 200, new(30, 55), new(0, 13, 107, 84)), new(33, 200, new(33, 69), new(0, 31, 113, 76)), new(55, 200, new(55, 96), new(0, 63, 125, 66)), new(47, 200, new(47, 100), new(0, 69, 111, 63)), new(34, 200, new(34, 67), new(0, 28, 106, 77)), new(30, 200, new(30, 50), new(0, 7, 101, 87)), new(32, 200, new(32, 50), new(0, 7, 101, 87)) },
        new PetFrameGeometry[] { new(18, 200, new(18, 45), new(0, 0, 94, 90)), new(23, 200, new(23, 46), new(0, 1, 96, 90)), new(25, 200, new(25, 45), new(0, 0, 98, 90)), new(26, 200, new(26, 45), new(0, 0, 100, 90)), new(30, 200, new(30, 50), new(0, 7, 106, 87)), new(25, 200, new(25, 46), new(0, 1, 98, 90)) },
        new PetFrameGeometry[] { new(31, 200, new(31, 47), new(0, 2, 106, 89)), new(28, 200, new(28, 47), new(0, 3, 106, 89)), new(31, 200, new(31, 48), new(0, 4, 103, 88)), new(32, 200, new(32, 47), new(0, 3, 106, 89)), new(30, 200, new(30, 51), new(0, 8, 102, 86)), new(34, 200, new(34, 48), new(0, 4, 104, 88)) },
        new PetFrameGeometry[] { new(38, 200, new(38, 46), new(0, 1, 111, 90)), new(37, 200, new(37, 49), new(0, 5, 111, 88)), new(35, 200, new(35, 47), new(0, 2, 107, 89)), new(36, 200, new(36, 47), new(0, 3, 109, 89)), new(35, 200, new(35, 47), new(0, 3, 109, 89)), new(35, 200, new(35, 45), new(0, 0, 107, 90)) },
        new PetFrameGeometry[] { new(33, 200, new(33, 51), new(0, 8, 105, 86)), new(48, 200, new(48, 47), new(0, 3, 108, 89)), new(45, 200, new(45, 47), new(0, 2, 105, 89)), new(43, 200, new(43, 45), new(0, 0, 105, 90)), new(43, 200, new(43, 46), new(0, 1, 104, 90)), new(47, 200, new(47, 48), new(0, 4, 111, 88)), new(53, 200, new(53, 50), new(0, 7, 114, 87)), new(52, 200, new(52, 54), new(0, 12, 112, 85)) },
        new PetFrameGeometry[] { new(22, 200, new(22, 45), new(0, 0, 100, 90)), new(33, 200, new(33, 51), new(0, 8, 111, 86)), new(20, 200, new(20, 55), new(0, 13, 95, 84)), new(22, 200, new(22, 54), new(0, 12, 100, 85)), new(18, 200, new(18, 54), new(0, 11, 95, 85)), new(11, 200, new(11, 54), new(0, 12, 86, 85)), new(10, 200, new(10, 54), new(0, 12, 85, 85)), new(9, 200, new(9, 54), new(0, 12, 89, 85)) },
    };
    public static PetFrameGeometry GetFrameGeometry(int row, int frame) => FramesByRow[row][frame];
    public static int GetFrameCount(int row) => FramesByRow[row].Length;
    public static Rectangle GetSourceRectangle(int row, int frame) =>
        new(frame * AtlasCellWidth, row * AtlasCellHeight, AtlasCellWidth, AtlasCellHeight);
    public static int RequiredRenderAreaHeight => Math.Max(PetLogicalGeometry.Height,
        FramesByRow.SelectMany(row => row).Max(frame => PetLogicalGeometry.Height +
            (int)Math.Ceiling((AtlasCellHeight - frame.GroundAnchorY) * RenderScale)));
}
