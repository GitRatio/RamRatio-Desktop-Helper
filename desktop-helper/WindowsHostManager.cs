using Microsoft.Win32;
using System.Runtime.Versioning;

[SupportedOSPlatform("windows")]
internal static class WindowsHostManager
{
    private const string RegistryPath = @"Software\Mozilla\NativeMessagingHosts\com.ratioworks.ramratio";
    internal static string Folder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RamRatio", "DesktopHelper");
    internal static string Host => Path.Combine(Folder, "RamRatio.Host.exe");
    internal static string Manifest => Path.Combine(Folder, InstallSafety.HostName + ".json");

    private static (bool exists, bool valid) Registration()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryPath);
        if (key == null) return (false, false);
        return (true, InstallSafety.PathEquals(key.GetValue("")?.ToString(), Manifest));
    }

    internal static string Diagnose()
    {
        var registry = Registration();
        return InstallSafety.Diagnose(Host, Manifest, registry.valid);
    }

    internal static void Install(string source)
    {
        var previous = Registration();
        if (previous.exists && !previous.valid)
            throw new InvalidOperationException("Another native messaging registration already exists. No changes were made.");

        try
        {
            InstallSafety.Install(Host, Manifest, source, previous.valid, () =>
            {
                using var key = Registry.CurrentUser.CreateSubKey(RegistryPath);
                if (key == null) throw new IOException("Cannot create user native messaging registration.");
                key.SetValue("", Manifest, RegistryValueKind.String);
            });
        }
        catch
        {
            if (!previous.exists && Registration().valid)
                Registry.CurrentUser.DeleteSubKey(RegistryPath, throwOnMissingSubKey: false);
            throw;
        }
    }

    internal static void Remove()
    {
        var previous = Registration();
        if (previous.exists && !previous.valid)
            throw new InvalidOperationException("Another native messaging registration already exists. No changes were made.");

        InstallSafety.Remove(Host, Manifest, previous.valid, () =>
        {
            if (previous.exists)
                Registry.CurrentUser.DeleteSubKey(RegistryPath, throwOnMissingSubKey: false);
        });
    }
}
