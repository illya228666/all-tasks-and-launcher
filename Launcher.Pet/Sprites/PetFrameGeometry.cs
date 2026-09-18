namespace Launcher.Pet.Sprites;

// Размер кадра задаётся только uniform scale: aspect ratio нормализованной
// ячейки атласа не меняется. Offset'ы отвечают только за визуальное положение.
// BodyAnchorX/HeadAnchorY остаются в координатах нормализованной ячейки атласа.
internal readonly record struct PetFrameGeometry(
    int BodyAnchorX,
    float RenderScale,
    int RenderOffsetX,
    int RenderOffsetY,
    int HeadAnchorY)
{
    internal int RenderWidth => PetSpriteCatalog.GetRenderWidth(RenderScale);
    internal int RenderHeight => PetSpriteCatalog.GetRenderHeight(RenderScale);
}
