namespace Launcher.Apps.Data;
public sealed record AppSourceResult(IReadOnlyList<AppInfo> Apps, string RootFolder, IReadOnlyList<string> Warnings);
