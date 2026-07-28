namespace VideoLibrarySystemVlc.Services;

internal static class AppPaths
{
    public static string BaseFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VideoLibrarySystemVlc");

    public static string StateFile => Path.Combine(BaseFolder, "state.json");
    public static string TempFolder => Path.Combine(BaseFolder, "temp");
}
