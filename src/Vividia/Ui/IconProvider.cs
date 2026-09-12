using Vividia.Models;
using Vividia.Storage;
using Vividia.Watch;

namespace Vividia.Ui;

public static class IconProvider
{
    private static readonly Dictionary<string, Image> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static Image? GetForProfile(GameProfile profile)
    {
        string cacheFile = Path.Combine(Paths.IconDirectory, profile.Id + ".png");

        if (Cache.TryGetValue(cacheFile, out var cached))
            return cached;

        var image = ExtractFresh(profile, cacheFile) ?? LoadFromCache(cacheFile);
        if (image != null)
            Cache[cacheFile] = image;

        return image;
    }

    public static void Forget(GameProfile profile)
    {
        string cacheFile = Path.Combine(Paths.IconDirectory, profile.Id + ".png");
        Cache.Remove(cacheFile);

        try
        {
            if (File.Exists(cacheFile))
                File.Delete(cacheFile);
        }
        catch
        {
        }
    }

    public static string? ResolveExecutable(GameProfile profile)
    {
        if (!string.IsNullOrWhiteSpace(profile.ExecutablePath) && File.Exists(profile.ExecutablePath))
            return profile.ExecutablePath;

        if (string.IsNullOrWhiteSpace(profile.ProcessName))
            return null;

        return ProcessPath.TryGetByName(GameProfile.NormalizeProcessName(profile.ProcessName));
    }

    private static Image? ExtractFresh(GameProfile profile, string cacheFile)
    {
        var executable = ResolveExecutable(profile);
        if (executable == null)
            return null;

        try
        {
            using var icon = Icon.ExtractAssociatedIcon(executable);
            if (icon == null)
                return null;

            var bitmap = icon.ToBitmap();
            TrySave(bitmap, cacheFile);
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    private static Image? LoadFromCache(string cacheFile)
    {
        try
        {
            if (!File.Exists(cacheFile))
                return null;

            using var stream = new FileStream(cacheFile, FileMode.Open, FileAccess.Read);
            using var temp = Image.FromStream(stream);
            return new Bitmap(temp);
        }
        catch
        {
            return null;
        }
    }

    private static void TrySave(Image image, string cacheFile)
    {
        try
        {
            image.Save(cacheFile, System.Drawing.Imaging.ImageFormat.Png);
        }
        catch
        {
        }
    }
}
