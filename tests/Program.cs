using System.Text.Json;

// Purely temporary-directory regression harness. Never touches a real Firefox
// registration, user profile, existing browser host, or operating-system registry.
static void Require(bool condition, string message)
{
    if (!condition) throw new Exception("Installer safety regression: " + message);
}
static void MustRefuse(Action operation, string message)
{
    try { operation(); }
    catch (InvalidOperationException) { return; }
    throw new Exception("Expected installer refusal: " + message);
}
// macOS /var is a system symlink; use an isolated disposable directory under the CI account home.
var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ramratio-safety-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
try
{
    var source = Path.Combine(root, "source.bin");
    var host = Path.Combine(root, "install", "RamRatio.Host");
    var manifest = Path.Combine(root, "native", InstallSafety.HostName + ".json");
    File.WriteAllText(source, "v1");
    InstallSafety.Install(host, manifest, source, false);
    Require(File.ReadAllText(host) == "v1", "first install did not install source");
    Require(InstallSafety.IsOwnedManifest(manifest, host), "manifest not owned");
    Require(JsonDocument.Parse(InstallSafety.Diagnose(host, manifest, true)).RootElement
        .GetProperty("status").GetString() == "installed", "status not installed");

    File.WriteAllText(source, "v2");
    InstallSafety.Install(host, manifest, source, true);
    Require(File.ReadAllText(host) == "v2", "update did not apply");

    var savedHost = File.ReadAllBytes(host);
    var savedManifest = File.ReadAllBytes(manifest);
    File.WriteAllText(source, "v3");
    var failed = false;
    try
    {
        InstallSafety.Install(host, manifest, source, true,
            () => throw new IOException("simulated registry failure"));
    }
    catch (IOException e) when (e.Message.Contains("simulated registry failure"))
    {
        failed = true;
    }
    Require(failed, "injected registration failure was not reported");
    Require(File.ReadAllBytes(host).SequenceEqual(savedHost), "host was not restored after failed registration");
    Require(File.ReadAllBytes(manifest).SequenceEqual(savedManifest), "manifest not restored after failed registration");
    Require(!Directory.EnumerateFiles(root, "*.stage-*", SearchOption.AllDirectories).Any(), "staged files remain");
    Require(!Directory.EnumerateFiles(root, "*.backup-*", SearchOption.AllDirectories).Any(), "backups remain after a successful rollback");

    var foreign = File.ReadAllText(manifest).Replace(InstallSafety.HostName, "com.foreign.example");
    File.WriteAllText(manifest, foreign);
    MustRefuse(() => InstallSafety.Install(host, manifest, source, true), "foreign manifest install");
    MustRefuse(() => InstallSafety.Remove(host, manifest, true), "foreign manifest remove");
    Require(File.ReadAllText(manifest) == foreign, "foreign manifest was changed");
    File.WriteAllBytes(manifest, savedManifest);

    // An existing binary without a manifest cannot be attributed to RamRatio,
    // even when a Windows registry key happens to point to the expected path.
    File.Delete(manifest);
    MustRefuse(() => InstallSafety.Install(host, manifest, source, true),
        "registry must not authorize overwriting an orphaned executable");
    MustRefuse(() => InstallSafety.Remove(host, manifest, true),
        "registry must not authorize deleting an orphaned executable");
    Require(File.ReadAllBytes(host).SequenceEqual(savedHost), "orphaned executable was changed");
    File.WriteAllBytes(manifest, savedManifest);

    // A failing registry unregistration must leave host and manifest intact.
    var unregisterFailed = false;
    try
    {
        InstallSafety.Remove(host, manifest, true,
            () => throw new IOException("simulated registry deletion failure"));
    }
    catch (IOException e) when (e.Message.Contains("simulated registry deletion failure"))
    {
        unregisterFailed = true;
    }
    Require(unregisterFailed, "unregister callback failure was swallowed");
    Require(File.ReadAllBytes(host).SequenceEqual(savedHost), "failed unregister removed host");
    Require(File.ReadAllBytes(manifest).SequenceEqual(savedManifest), "failed unregister removed manifest");
    Require(!Directory.EnumerateFiles(root, "*.remove-*", SearchOption.AllDirectories).Any(),
        "failed unregister left staged removal files behind");

    // Simulate a missing recovery backup: rollback must attempt other restores
    // and return a distinct incomplete-recovery error rather than hide it.
    File.WriteAllText(source, "v4");
    bool incomplete = false;
    try
    {
        InstallSafety.Install(host, manifest, source, true, () =>
        {
            var backups = Directory.GetFiles(Path.GetDirectoryName(host)!, "*.backup-*");
            Require(backups.Length == 1, "expected staged host recovery file");
            File.Delete(backups[0]);
            throw new IOException("simulated backup loss");
        });
    }
    catch (IOException e) when (e.Message.Contains("rollback was incomplete"))
    {
        incomplete = true;
    }
    Require(incomplete, "incomplete rollback was not explicitly reported");
    Require(File.ReadAllBytes(manifest).SequenceEqual(savedManifest), "remaining manifest backup was not restored");
    // Return the synthetic test state to an installable condition.
    File.WriteAllBytes(host, savedHost);
    File.WriteAllBytes(manifest, savedManifest);

    bool unregisterCalled = false;
    InstallSafety.Remove(host, manifest, true, () => unregisterCalled = true);
    Require(unregisterCalled, "registered host was not unregistered");
    Require(!File.Exists(host) && !File.Exists(manifest), "remove left owned files");
    Require(!Directory.EnumerateFiles(root, "*.remove-*", SearchOption.AllDirectories).Any(),
        "successful remove left staged files");

    // Symlink safety, including broken symlinks and linked parent directories.
    if (!OperatingSystem.IsWindows())
    {
        var protectedFile = Path.Combine(root, "do-not-touch.json");
        File.WriteAllText(protectedFile, "protected");
        File.CreateSymbolicLink(manifest, protectedFile);
        MustRefuse(() => InstallSafety.Install(host, manifest, source, false), "manifest symlink");
        Require(File.ReadAllText(protectedFile) == "protected", "symlink target changed");
        File.Delete(manifest);
        File.CreateSymbolicLink(manifest, Path.Combine(root, "missing-link-target"));
        MustRefuse(() => InstallSafety.Install(host, manifest, source, false), "dangling manifest symlink");
        File.Delete(manifest);
        var linkedDir = Path.Combine(root, "linked-native");
        Directory.CreateSymbolicLink(linkedDir, Path.Combine(root, "native"));
        var linkedManifest = Path.Combine(linkedDir, InstallSafety.HostName + ".json");
        MustRefuse(() => InstallSafety.Install(host, linkedManifest, source, false), "linked parent directory");
    }
    Console.WriteLine("PASS: install/update, registry-less host protection, failed-unregister rollback, incomplete-recovery detection, safe removal, and symlinks.");
}
finally
{
    Directory.Delete(root, recursive: true);
}
