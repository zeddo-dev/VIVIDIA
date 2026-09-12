using System.Globalization;
using Vividia.Core;
using Vividia.Storage;
using Vividia.Ui;

namespace Vividia;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(true, @"Global\Vividia.SingleInstance", out bool isFirstInstance);
        if (!isFirstInstance)
            return;

        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

        ApplicationConfiguration.Initialize();

        var config = ConfigStore.Load();
        Theme.SetCurrent(config.Theme);

        var engine = new ProfileEngine(config);

        AppDomain.CurrentDomain.ProcessExit += (_, _) => engine.Dispose();

        engine.Start();
        Application.Run(new TrayContext(config, engine));
    }
}
