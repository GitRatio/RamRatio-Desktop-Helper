using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

static class UnixSetup
{
    public static int Run()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        bool mac = OperatingSystem.IsMacOS();
        if (!mac && !OperatingSystem.IsLinux()) return 1;
        var folder = Path.Combine(home, mac ? "Library/Application Support/RamRatio/DesktopHelper" : ".local/share/ramratio/desktop-helper");
        var native = Path.Combine(home, mac ? "Library/Application Support/Mozilla/NativeMessagingHosts" : ".mozilla/native-messaging-hosts");
        var manifest = Path.Combine(native, "com.ratioworks.ramratio.json");
        Console.WriteLine("RamRatio Desktop Helper\nReads system memory and Firefox process memory on request. No network, remote control, sudo, background service, or startup task. Does not read passwords, documents, or browsing content.");
        Console.WriteLine($"Changes only:\n{folder}\n{manifest}\nSee bundled TERMS.md and PRIVACY.md.\nEnter I to install/update, U to remove, or anything else to cancel.");
        string? choice = Console.ReadLine()?.Trim().ToUpperInvariant();
        try
        {
            if (choice == "U")
            {
                File.Delete(manifest); File.Delete(Path.Combine(folder, "RamRatio.Host"));
                Console.WriteLine("Registration and installed host removed. Extension settings and downloaded files remain.");
            }
            else if (choice == "I")
            {
                Directory.CreateDirectory(folder); Directory.CreateDirectory(native);
                var host = Path.Combine(folder, "RamRatio.Host");
                File.Copy(Environment.ProcessPath!, host, true);
                if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(host, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                File.WriteAllText(manifest, JsonSerializer.Serialize(new {
                    name = "com.ratioworks.ramratio", description = "RamRatio local memory statistics", path = host,
                    type = "stdio", allowed_extensions = new[] { "memory-optimizer@local" }
                }));
                Console.WriteLine("Installed. Enable Advanced System Stats in RamRatio settings, allow Firefox permission, and Check connection.");
            }
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine("Setup failed: " + e.Message); return 1; }
    }
}

static class UnixStats
{
    static string Run(string exe, params string[] args)
    {
        var start = new ProcessStartInfo(exe) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        start.Environment["LC_ALL"] = "C";
        foreach (var arg in args) start.ArgumentList.Add(arg);
        using var p = Process.Start(start) ?? throw new IOException("Cannot start OS measurement tool");
        // Consume both pipes asynchronously; bound runtime, and never relay raw process output to Firefox.
        var stdout = p.StandardOutput.ReadToEndAsync(); var stderr = p.StandardError.ReadToEndAsync();
        if (!p.WaitForExit(3000)) { p.Kill(); throw new IOException("OS measurement timed out"); }
        if (p.ExitCode != 0) throw new IOException("OS measurement unavailable");
        return stdout.GetAwaiter().GetResult();
    }
    public static object Read()
    {
        try
        {
            ulong total, available; long rss = 0; int count = 0, skipped = 0;
            if (OperatingSystem.IsLinux())
            {
                var mem = File.ReadAllLines("/proc/meminfo").Select(line => line.Split(':', 2))
                    .ToDictionary(p => p[0], p => ulong.Parse(p[1].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0], CultureInfo.InvariantCulture) * 1024);
                total = mem["MemTotal"]; available = mem["MemAvailable"];
                foreach (var dir in Directory.EnumerateDirectories("/proc"))
                {
                    if (!int.TryParse(Path.GetFileName(dir), out _)) continue;
                    try
                    {
                        var exe = File.ResolveLinkTarget(Path.Combine(dir, "exe"), true);
                        if (exe == null || !new[] { "firefox", "firefox-bin" }.Contains(Path.GetFileName(exe.FullName))) continue;
                        var line = File.ReadLines(Path.Combine(dir, "status")).First(s => s.StartsWith("VmRSS:"));
                        rss += long.Parse(line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[1], CultureInfo.InvariantCulture) * 1024; count++;
                    }
                    catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException) { skipped++; }
                }
            }
            else
            {
                total = ulong.Parse(Run("/usr/sbin/sysctl", "-n", "hw.memsize").Trim(), CultureInfo.InvariantCulture);
                var vm = Run("/usr/bin/vm_stat");
                ulong pageSize = ulong.Parse(Regex.Match(vm, @"page size of (\d+) bytes").Groups[1].Value, CultureInfo.InvariantCulture);
                ulong pages = 0;
                foreach (var name in new[] { "Pages free", "Pages inactive", "Pages speculative" })
                    pages += ulong.Parse(Regex.Match(vm, Regex.Escape(name) + @":\s+(\d+)").Groups[1].Value, CultureInfo.InvariantCulture);
                available = Math.Min(total, pages * pageSize);
                foreach (var line in Run("/bin/ps", "-axo", "rss=,comm=").Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    var parts = line.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2 && parts[1].Contains("/Firefox.app/Contents/", StringComparison.OrdinalIgnoreCase))
                    { rss += long.Parse(parts[0], CultureInfo.InvariantCulture) * 1024; count++; }
                }
            }
            return new { ok = true, platform = OperatingSystem.IsMacOS() ? "macOS" : "Linux", helper_version = "0.2.1",
                memory = new { total_bytes = total, available_bytes = available, used_bytes = total - available,
                    measurement = OperatingSystem.IsMacOS() ? "free-inactive-speculative-estimate" : "kernel-MemAvailable" },
                firefox = new { rss_bytes = rss, process_count = count, skipped_process_count = skipped, measurement = "sum-of-working-sets" } };
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or FormatException or KeyNotFoundException)
        { return new { ok = false, reason = "os-measurement-unavailable" }; }
    }
}