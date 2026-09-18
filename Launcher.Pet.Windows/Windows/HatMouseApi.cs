using System.Runtime.InteropServices;

namespace Launcher.Pet.Windows.Windows;
internal static class HatMouseApi
{
    [DllImport("user32.dll")]
    internal static extern short GetAsyncKeyState(int virtualKey);
}
