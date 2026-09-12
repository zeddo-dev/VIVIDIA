namespace Vividia.Storage;

public static class Paths
{
    public static string DataDirectory
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "VIVIDIA");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string IconDirectory
    {
        get
        {
            var dir = Path.Combine(DataDirectory, "icons");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string ConfigFile => Path.Combine(DataDirectory, "config.json");
    public static string StateFile => Path.Combine(DataDirectory, "state.json");
}
