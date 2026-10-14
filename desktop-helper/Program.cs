using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Win32;

const string hostName = "com.ratioworks.ramratio";
const string registryPath = @"Software\Mozilla\NativeMessagingHosts\com.ratioworks.ramratio";
if (args.Contains("--host") || string.Equals(Path.GetFileNameWithoutExtension(Environment.ProcessPath), "RamRatio.Host", StringComparison.OrdinalIgnoreCase))
{
    using var input = Console.OpenStandardInput();
    using var output = Console.OpenStandardOutput();
    try
    {
        while (true)
        {
            var header = new byte[4];
            if (input.Read(header, 0, 1) == 0) break;
            input.ReadExactly(header.AsSpan(1));
            int size = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(header);
            if (size <= 0 || size > 65536) return 2;
            var body = new byte[size];
            input.ReadExactly(body);
            using var request = JsonDocument.Parse(body);
            object reply = request.RootElement.ValueKind == JsonValueKind.Object &&
                request.RootElement.TryGetProperty("type", out var type) && type.GetString() == "stats"
                ? Stats.Read() : new { ok = false, reason = "unsupported-request" };
            var bytes = JsonSerializer.SerializeToUtf8Bytes(reply);
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(header, bytes.Length);
            output.Write(header); output.Write(bytes); output.Flush();
        }
        return 0;
    }
    catch (Exception e) when (e is JsonException or EndOfStreamException or InvalidOperationException)
    { Console.Error.WriteLine("Invalid native message."); return 2; }
}

if (!OperatingSystem.IsWindows()) return UnixSetup.Run();
Console.WriteLine("RamRatio Desktop Helper setup\nReads system memory and Firefox process memory on request. No network access, remote control, service, startup task, or administrator permission.");
Console.WriteLine("Install writes only this user's LocalAppData/RamRatio/DesktopHelper folder and Firefox native-host registry key. It does not read browsing content, passwords, or documents. See the bundled TERMS.md and PRIVACY.md.\nPress I to install/update, U to remove, or any other key to cancel.");
var choice = Console.ReadKey(true).Key;
try
{
    if (choice == ConsoleKey.U)
    {
        Registry.CurrentUser.DeleteSubKey(registryPath, false);
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RamRatio", "DesktopHelper");
        foreach (var name in new[] { "RamRatio.Host.exe", hostName + ".json" })
            File.Delete(Path.Combine(folder, name));
        Console.WriteLine("Helper registration and installed host removed. Downloaded ZIP/Setup and extension settings remain yours to remove separately.");
    }
    else if (choice == ConsoleKey.I)
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RamRatio", "DesktopHelper");
        Directory.CreateDirectory(folder);
        var host = Path.Combine(folder, "RamRatio.Host.exe");
        File.Copy(Environment.ProcessPath!, host, true);
        var manifest = Path.Combine(folder, hostName + ".json");
        File.WriteAllText(manifest, JsonSerializer.Serialize(new {
            name = hostName, description = "RamRatio local Windows memory statistics", path = host,
            type = "stdio", allowed_extensions = new[] { "memory-optimizer@local" }
        }));
        using var key = Registry.CurrentUser.CreateSubKey(registryPath);
        key.SetValue("", manifest);
        Console.WriteLine("Installed. Open RamRatio settings, enable Advanced System Stats, then Check connection.");
    }
    else return 0;
}
catch (Exception e) { Console.WriteLine("Setup failed: " + e.Message); return 1; }
Console.WriteLine("Press any key to close."); Console.ReadKey(true); return 0;

static class Stats
{
    [StructLayout(LayoutKind.Sequential)]
    struct MemoryStatus
    {
        public uint length, load;
        public ulong totalPhysical, availablePhysical, totalPageFile, availablePageFile, totalVirtual, availableVirtual, extended;
    }
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GlobalMemoryStatusEx(ref MemoryStatus status);
    public static object Read()
    {
        if (!OperatingSystem.IsWindows()) return UnixStats.Read();
        var memory = new MemoryStatus { length = (uint)Marshal.SizeOf<MemoryStatus>() };
        if (!GlobalMemoryStatusEx(ref memory)) return new { ok = false, reason = "memory-unavailable" };
        long rss = 0; int count = 0, skipped = 0;
        foreach (var process in Process.GetProcessesByName("firefox"))
        {
            using (process)
            {
                try { rss += process.WorkingSet64; count++; }
                catch (Exception e) when (e is InvalidOperationException or System.ComponentModel.Win32Exception) { skipped++; }
            }
        }
        return new { ok = true, platform = "Windows", helper_version = "0.2.0", memory = new {
            total_bytes = memory.totalPhysical, available_bytes = memory.availablePhysical,
            used_bytes = memory.totalPhysical - memory.availablePhysical
        }, firefox = new { rss_bytes = rss, process_count = count, skipped_process_count = skipped,
            measurement = "sum-of-working-sets" } };
    }
}
