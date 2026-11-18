using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Win32;

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

if (!OperatingSystem.IsWindows()) return UnixSetup.Run(args);
return WindowsSetup.Run(args);

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
        return new { ok = true, platform = "Windows", helper_version = InstallSafety.Version, memory = new {
            total_bytes = memory.totalPhysical, available_bytes = memory.availablePhysical,
            used_bytes = memory.totalPhysical - memory.availablePhysical
        }, firefox = new { rss_bytes = rss, process_count = count, skipped_process_count = skipped,
            measurement = "sum-of-working-sets" } };
    }
}