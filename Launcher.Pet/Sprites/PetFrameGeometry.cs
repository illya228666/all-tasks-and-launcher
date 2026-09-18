namespace Launcher.Pet.Sprites;

// RenderWidth/RenderHeight и RenderOffsetX/Y описывают только отрисовку кадра.
// BodyAnchorX/HeadAnchorY заданы в координатах нормализованной ячейки атласа.
// Это авторские настройки кода и никогда не вычисляются из прозрачных полей PNG.
internal readonly record struct PetFrameGeometry(
    int BodyAnchorX,
    int RenderWidth,
    int RenderHeight,
    int RenderOffsetX,
    int RenderOffsetY,
    int HeadAnchorY);
