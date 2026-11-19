using System.Runtime.Versioning;

[SupportedOSPlatform("windows")]
static class WindowsSetup
{
    public static int Run(string[] args)
    {
        if (args.Contains("--version"))
        {
            Console.WriteLine(InstallSafety.Version);
            return 0;
        }
        if (args.Contains("--diagnose"))
        {
            Console.WriteLine(WindowsHostManager.Diagnose());
            return 0;
        }

        Console.WriteLine("RamRatio Desktop Helper " + InstallSafety.Version);
        Console.WriteLine("Optional local memory statistics. No remote control or background service.");
        Console.WriteLine("This is the advanced console installer. Windows users can use the single-EXE graphical installer.");
        Console.WriteLine("Press I to install or repair, U to remove, or anything else to cancel.");
        var choice = Console.IsInputRedirected
            ? Console.ReadLine()?.Trim().ToUpperInvariant()
            : Console.ReadKey(true).KeyChar.ToString().ToUpperInvariant();
        try
        {
            switch (choice)
            {
                case "I":
                    WindowsHostManager.Install(Environment.ProcessPath!);
                    Console.WriteLine("Installed. Open RamRatio options to enable Advanced System Stats and check connection.");
                    break;
                case "U":
                    WindowsHostManager.Remove();
                    Console.WriteLine("Removed RamRatio's local helper registration and installed files.");
                    break;
                default:
                    return 0;
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException
                                    or InvalidOperationException or System.Security.SecurityException)
        {
            Console.Error.WriteLine("Setup failed: " + e.Message);
            Console.Error.WriteLine("Use --diagnose to inspect ownership and registration.");
            return 1;
        }
        if (!Console.IsInputRedirected)
        {
            Console.WriteLine("Press any key to close.");
            Console.ReadKey(true);
        }
        return 0;
    }
}
