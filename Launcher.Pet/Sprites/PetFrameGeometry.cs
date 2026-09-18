namespace Launcher.Pet.Sprites;

// Width/Height и OffsetX/Y — пиксели назначения. OffsetX добавляется после
// совмещения BodyAnchorX с центром логической зоны, OffsetY — от её верха.
// BodyAnchorX/HeadAnchorY заданы в координатах нормализованной ячейки атласа.
// Это авторские настройки, а не результат анализа прозрачности PNG.
internal readonly record struct PetFrameGeometry(
    int BodyAnchorX,
    int Width = 149,
    int Height = 200,
    int OffsetX = 0,
    int OffsetY = 3,
    int HeadAnchorY = 45);
