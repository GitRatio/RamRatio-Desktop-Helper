using System.Text.Json;

// Only the known per-user native-host registration may be changed. No broad directory deletions.
static class InstallSafety
{
    internal const string HostName = "com.ratioworks.ramratio";
    internal const string ExtensionId = "memory-optimizer@local";
    internal const string Version = "0.3.0";

    internal static string ManifestJson(string host, bool windows)
        => JsonSerializer.Serialize(new
        {
            name = HostName,
            description = windows ? "RamRatio local Windows memory statistics" : "RamRatio local memory statistics",
            path = Path.GetFullPath(host),
            type = "stdio",
            allowed_extensions = new[] { ExtensionId }
        });

    internal static bool IsOwnedManifest(string manifest, string host)
    {
        try
        {
            using var json = JsonDocument.Parse(File.ReadAllText(manifest));
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                root.GetProperty("name").GetString() != HostName ||
                root.GetProperty("type").GetString() != "stdio" ||
                !PathEquals(root.GetProperty("path").GetString(), host))
                return false;
            var allowed = root.GetProperty("allowed_extensions");
            return allowed.ValueKind == JsonValueKind.Array &&
                   allowed.GetArrayLength() == 1 &&
                   allowed[0].GetString() == ExtensionId;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException
                                       or JsonException or InvalidOperationException
                                       or KeyNotFoundException or ArgumentException)
        {
            return false;
        }
    }

    internal static bool PathEquals(string? a, string? b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return false;
        try
        {
            var comparer = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), comparer);
        }
        catch (Exception e) when (e is ArgumentException or NotSupportedException or PathTooLongException) { return false; }
    }

    internal static void RejectLinkPath(params string[] paths)
    {
        foreach (var path in paths)
        {
            for (string? current = Path.GetFullPath(path); !string.IsNullOrEmpty(current);)
            {
                try
                {
                    if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                        throw new InvalidOperationException("Refusing a symbolic link or reparse point in the install path.");
                }
                catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException) { }
                var parent = Path.GetDirectoryName(current);
                if (parent == current) break;
                current = parent;
            }
        }
    }

    internal static string Diagnose(string host, string manifest, bool registrationMatches)
    {
        bool hostExists = File.Exists(host), manifestExists = File.Exists(manifest);
        bool manifestOwned = manifestExists && IsOwnedManifest(manifest, host);
        string state = !hostExists && !manifestExists && !registrationMatches ? "not-installed"
                     : hostExists && manifestOwned && registrationMatches ? "installed"
                     : manifestExists && !manifestOwned ? "foreign-manifest"
                     : "repair-needed";
        try { RejectLinkPath(host, manifest); }
        catch (InvalidOperationException) { state = "unsafe-path"; }
        return JsonSerializer.Serialize(new {
            helper_version = Version,
            status = state,
            host_present = hostExists,
            manifest_valid = manifestOwned,
            registration_valid = registrationMatches,
            extension_id = ExtensionId,
            native_host = HostName
        });
    }

    internal static void Install(string host, string manifest, string source, bool hasRegistration, Action? register = null)
    {
        RejectLinkPath(host, manifest);
        if (File.Exists(manifest) && !IsOwnedManifest(manifest, host))
            throw new InvalidOperationException("An unexpected native messaging manifest exists. Nothing was changed.");
        if (File.Exists(host) && !File.Exists(manifest) && !hasRegistration)
            throw new InvalidOperationException("An unregistered executable already exists at the helper path. Nothing was changed.");

        Directory.CreateDirectory(Path.GetDirectoryName(host)!);
        Directory.CreateDirectory(Path.GetDirectoryName(manifest)!);
        RejectLinkPath(host, manifest);
        var token = Guid.NewGuid().ToString("N");
        var stagedHost = host + ".stage-" + token, stagedManifest = manifest + ".stage-" + token;
        var backupHost = host + ".backup-" + token, backupManifest = manifest + ".backup-" + token;
        bool oldHost = File.Exists(host), oldManifest = File.Exists(manifest);
        bool hostReplaced = false, manifestReplaced = false;
        try
        {
            File.Copy(source, stagedHost);
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(stagedHost, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            File.WriteAllText(stagedManifest, ManifestJson(host, OperatingSystem.IsWindows()));
            if (oldHost) File.Copy(host, backupHost);
            if (oldManifest) File.Copy(manifest, backupManifest);
            File.Move(stagedHost, host, overwrite: true);
            hostReplaced = true;
            File.Move(stagedManifest, manifest, overwrite: true);
            manifestReplaced = true;
            register?.Invoke();
        }
        catch
        {
            // A failure after the executable is replaced restores the earlier state.
            if (manifestReplaced)
            {
                if (oldManifest) File.Move(backupManifest, manifest, overwrite: true);
                else File.Delete(manifest);
            }
            if (hostReplaced)
            {
                if (oldHost) File.Move(backupHost, host, overwrite: true);
                else File.Delete(host);
            }
            throw;
        }
        finally
        {
            foreach (var temporary in new[] { stagedHost, stagedManifest, backupHost, backupManifest })
                try { File.Delete(temporary); } catch (IOException) { }
        }
    }

    internal static void Remove(string host, string manifest, bool hasRegistration)
    {
        RejectLinkPath(host, manifest);
        if (File.Exists(manifest) && !IsOwnedManifest(manifest, host))
            throw new InvalidOperationException("Manifest does not belong to RamRatio. Refusing removal.");
        if (File.Exists(host) && !File.Exists(manifest) && !hasRegistration)
            throw new InvalidOperationException("Unregistered host cannot be verified. Refusing removal.");
        // No recursive delete: never touch unrelated files or folders.
        File.Delete(host);
        File.Delete(manifest);
    }
}
