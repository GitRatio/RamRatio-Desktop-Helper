using Microsoft.Win32;

static class WindowsSetup
{
    private const string RegistryPath = @"Software\Mozilla\NativeMessagingHosts\com.ratioworks.ramratio";
    private static string Folder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RamRatio", "DesktopHelper");
    private static string Host => Path.Combine(Folder, "RamRatio.Host.exe");
    private static string Manifest => Path.Combine(Folder, InstallSafety.HostName + ".json");

    private static (bool exists, bool valid) Registration()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryPath);
        if (key == null) return (false, false);
        return (true, InstallSafety.PathEquals(key.GetValue("")?.ToString(), Manifest));
    }

    public static int Run(string[] args)
    {
        if (args.Contains("--version"))
        {
            Console.WriteLine(InstallSafety.Version);
            return 0;
        }
        if (args.Contains("--diagnose"))
        {
            var state = Registration();
            Console.WriteLine(InstallSafety.Diagnose(Host, Manifest, state.valid));
            return 0;
        }

        Console.WriteLine("RamRatio Desktop Helper " + InstallSafety.Version);
        Console.WriteLine("Optional system/Firefox memory readings only. No network client, remote control, startup service, or administrator requirement.");
        Console.WriteLine("Install: per-user LocalAppData/RamRatio/DesktopHelper and Firefox NativeMessagingHosts registration.");
        Console.WriteLine("See TERMS.md and PRIVACY.md. Press I to install/repair/update, U to uninstall, or anything else to cancel.");

        var choice = Console.IsInputRedirected
            ? Console.ReadLine()?.Trim().ToUpperInvariant()
            : Console.ReadKey(true).KeyChar.ToString().ToUpperInvariant();
        try
        {
            if (choice == "I")
            {
                var previous = Registration();
                if (previous.exists && !previous.valid)
                    throw new InvalidOperationException("A different native messaging registration is present. Refusing to overwrite.");
                try
                {
                    InstallSafety.Install(Host, Manifest, Environment.ProcessPath!, previous.valid, () =>
                    {
                        using var key = Registry.CurrentUser.CreateSubKey(RegistryPath);
                        if (key == null) throw new IOException("Cannot open user native messaging registration.");
                        key.SetValue("", Manifest, RegistryValueKind.String);
                    });
                }
                catch
                {
                    // Only undo a newly created key, not a pre-existing valid registration.
                    if (!previous.exists && Registration().exists)
                        Registry.CurrentUser.DeleteSubKey(RegistryPath, throwOnMissingSubKey: false);
                    throw;
                }
                Console.WriteLine("Installed or repaired. Enable Advanced System Stats in RamRatio settings, then Check connection.");
            }
            else if (choice == "U")
            {
                var previous = Registration();
                if (previous.exists && !previous.valid)
                    throw new InvalidOperationException("A different native messaging registration is present. Refusing removal.");
                InstallSafety.Remove(Host, Manifest, previous.valid);
                if (previous.exists)
                    Registry.CurrentUser.DeleteSubKey(RegistryPath, throwOnMissingSubKey: false);
                Console.WriteLine("Helper unregistered and its installed files removed. Downloaded files and other user data remain.");
            }
            else return 0;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException
                                        or System.Security.SecurityException)
        {
            Console.Error.WriteLine("Setup failed: " + e.Message);
            Console.Error.WriteLine("Use --diagnose to inspect the installation. Nothing outside the RamRatio paths was intentionally modified.");
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
