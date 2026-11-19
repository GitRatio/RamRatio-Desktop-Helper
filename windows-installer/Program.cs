using System.Security.Cryptography;
using System.Text.Json;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.Versioning;

namespace RamRatioInstaller;

[SupportedOSPlatform("windows")]
internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        // Tests verify the embedded native-host executable without installing anything.
        if (args.Length == 1 && args[0] == "--verify-payload")
        {
            try { _ = Payload.ReadVerified(); return 0; }
            catch { return 2; }
        }
        if (args.Length != 0) return 2; // No hidden/silent installation entry points.
        ApplicationConfiguration.Initialize();
        Application.Run(new SetupWindow());
        return 0;
    }
}

internal static class Payload
{
    internal static byte[] ReadVerified()
    {
        using var resource = typeof(Payload).Assembly.GetManifestResourceStream("RamRatio.NativeHost.Payload")
            ?? throw new InvalidDataException("The native-host component is missing.");
        using var buffer = new MemoryStream();
        resource.CopyTo(buffer);
        var bytes = buffer.ToArray();
        var digest = Convert.ToHexString(SHA256.HashData(bytes));
        if (!string.Equals(digest, PayloadHash.Sha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The native-host component failed its integrity check.");
        return bytes;
    }

    internal static void WithTemporaryHost(Action<string> install)
    {
        byte[] bytes = ReadVerified();
        var directory = Path.Combine(Path.GetTempPath(), "RamRatio-Setup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var source = Path.Combine(directory, "RamRatio.Host.exe");
        try
        {
            File.WriteAllBytes(source, bytes);
            install(source);
        }
        finally
        {
            try { File.Delete(source); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
            try { Directory.Delete(directory); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        }
    }
}

[SupportedOSPlatform("windows")]
internal sealed class SetupWindow : Form
{
    private static readonly Color Background = Color.FromArgb(10, 23, 38);
    private static readonly Color Card = Color.FromArgb(18, 40, 61);
    private static readonly Color Blue = Color.FromArgb(108, 206, 250);
    private static readonly Color Muted = Color.FromArgb(172, 197, 216);
    private readonly Button installButton = MakeButton("Install", true);
    private readonly Button removeButton = MakeButton("Remove", false);
    private readonly Button checkButton = MakeButton("Check status", false);
    private readonly Label status = MakeLabel("Checking installation…", 12, Color.White);
    private readonly Label summary = MakeLabel("", 10, Muted);
    private readonly Label pathLabel = MakeLabel("", 9, Muted);
    private bool working;

    internal SetupWindow()
    {
        Text = "RamRatio Desktop Helper Setup";
        AutoScaleMode = AutoScaleMode.Dpi;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Background;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10);
        ClientSize = new Size(665, 515);
        MinimumSize = new Size(645, 510);
        MaximizeBox = false;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(28, 24, 28, 18), BackColor = Background };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var title = new Label { Text = "RamRatio", Font = new Font("Segoe UI", 27, FontStyle.Bold), ForeColor = Color.White,
            Dock = DockStyle.Top, Height = 54, AutoSize = false };
        var sub = MakeLabel("DESKTOP HELPER  /  WINDOWS SETUP", 9, Blue);
        var header = new Panel { Dock = DockStyle.Fill };
        header.Controls.Add(sub);
        header.Controls.Add(title);
        sub.Dock = DockStyle.Bottom;
        sub.Height = 23;
        root.Controls.Add(header, 0, 0);

        var intro = MakeLabel("Unlock optional system-memory statistics in your RamRatio Firefox extension.\nNo administrator rights, background service, or automatic startup.", 11, Color.White);
        intro.Dock = DockStyle.Fill;
        intro.Padding = new Padding(0, 12, 4, 0);
        root.Controls.Add(intro, 0, 1);

        var statePanel = new Panel { Dock = DockStyle.Fill, BackColor = Card, Padding = new Padding(16, 10, 16, 5) };
        status.Dock = DockStyle.Top;
        status.Height = 28;
        summary.Dock = DockStyle.Fill;
        statePanel.Controls.Add(summary);
        statePanel.Controls.Add(status);
        root.Controls.Add(statePanel, 0, 2);

        var buttonBar = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoSize = false, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 18, 0, 0) };
        installButton.Width = 190;
        removeButton.Width = 120;
        checkButton.Width = 145;
        buttonBar.Controls.Add(installButton);
        buttonBar.Controls.Add(removeButton);
        buttonBar.Controls.Add(checkButton);
        root.Controls.Add(buttonBar, 0, 3);

        var footer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 13, 0, 0) };
        var next = MakeLabel("After installing: RamRatio → Settings → Advanced System Stats → Check connection.", 9, Color.White);
        next.Dock = DockStyle.Top;
        next.Height = 36;
        var lower = MakeLabel("Stored only for your Windows account. Firefox asks separately for permission.", 9, Muted);
        lower.Dock = DockStyle.Top;
        lower.Height = 24;
        pathLabel.Dock = DockStyle.Top;
        pathLabel.Height = 27;
        var links = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 32, WrapContents = false };
        var addon = MakeLink("Firefox add-on", "https://addons.mozilla.org/en-US/firefox/addon/ramratio/");
        var help = MakeLink("Help / privacy / terms", "https://github.com/GitRatio/RamRatio-Desktop-Helper");
        links.Controls.Add(addon);
        links.Controls.Add(help);
        footer.Controls.Add(links);
        footer.Controls.Add(pathLabel);
        footer.Controls.Add(lower);
        footer.Controls.Add(next);
        root.Controls.Add(footer, 0, 4);

        installButton.Click += async (_, _) => await PerformAsync(true);
        removeButton.Click += async (_, _) => await PerformAsync(false);
        checkButton.Click += (_, _) => RefreshStatus();
        RefreshStatus();
    }

    private void RefreshStatus()
    {
        try
        {
            using var result = JsonDocument.Parse(WindowsHostManager.Diagnose());
            var state = result.RootElement.GetProperty("status").GetString();
            switch (state)
            {
                case "installed":
                    status.Text = "●  Installed and registered";
                    status.ForeColor = Blue;
                    summary.Text = "Local helper is registered. Use Check connection in Firefox to verify permission.";
                    installButton.Text = "Update / Repair";
                    break;
                case "not-installed":
                    status.Text = "○  Ready to install";
                    status.ForeColor = Color.White;
                    summary.Text = "The optional desktop helper is not installed for this account.";
                    installButton.Text = "Install helper";
                    break;
                case "foreign-manifest":
                case "unsafe-path":
                    status.Text = "●  Attention required";
                    status.ForeColor = Color.FromArgb(255, 186, 121);
                    summary.Text = "Unsafe or unrecognized registration. Setup will not overwrite unrelated files.";
                    installButton.Text = "Install helper";
                    break;
                default:
                    status.Text = "●  Repair may be needed";
                    status.ForeColor = Color.FromArgb(255, 186, 121);
                    summary.Text = "Existing host or manifest is incomplete. Safety checks will prevent unsafe changes.";
                    installButton.Text = "Repair helper";
                    break;
            }
            removeButton.Enabled = state != "not-installed" && !working;
        }
        catch (Exception e)
        {
            status.Text = "Status unavailable";
            status.ForeColor = Color.FromArgb(255, 186, 121);
            summary.Text = e.Message;
        }
        pathLabel.Text = "%LOCALAPPDATA%\\RamRatio\\DesktopHelper";
    }

    private async Task PerformAsync(bool installing)
    {
        if (working) return;
        var operation = installing ? "install or update" : "remove";
        if (!installing && MessageBox.Show(this, "Remove the optional RamRatio helper from this Windows account?\nFirefox add-on settings will not be deleted.",
                "Confirm removal", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;
        working = true;
        installButton.Enabled = removeButton.Enabled = checkButton.Enabled = false;
        status.Text = installing ? "Installing local helper…" : "Removing local helper…";
        summary.Text = "Working only with the RamRatio native-host files and registration.";
        UseWaitCursor = true;
        string? error = null;
        try
        {
            await Task.Run(() =>
            {
                if (installing) Payload.WithTemporaryHost(WindowsHostManager.Install);
                else WindowsHostManager.Remove();
            });
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException
                                   or InvalidOperationException or System.Security.SecurityException
                                   or InvalidDataException)
        {
            error = e.Message;
        }
        finally
        {
            working = false;
            UseWaitCursor = false;
            installButton.Enabled = checkButton.Enabled = true;
            RefreshStatus();
        }
        if (error != null)
            MessageBox.Show(this, "Could not " + operation + " the helper:\n\n" + error +
                "\n\nYour existing files have not been intentionally overwritten if their ownership cannot be verified.",
                "RamRatio Setup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        else
            MessageBox.Show(this, installing
                ? "Helper installed. In Firefox, enable Advanced System Stats and select Check connection to verify it is working."
                : "Helper removed from this Windows account. Your RamRatio browser extension and settings remain.",
                "RamRatio Setup", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static Label MakeLabel(string text, float size, Color color) =>
        new() { Text = text, ForeColor = color, Font = new Font("Segoe UI", size),
                AutoSize = false, TextAlign = ContentAlignment.MiddleLeft };

    private static Button MakeButton(string text, bool primary)
    {
        var b = new Button { Text = text, Height = 46, Width = 140, Margin = new Padding(0, 0, 10, 0),
            FlatStyle = FlatStyle.Flat, BackColor = primary ? Blue : Card,
            ForeColor = primary ? Color.FromArgb(7, 24, 37) : Color.White, Cursor = Cursors.Hand,
            AccessibleName = text, TabStop = true };
        b.FlatAppearance.BorderSize = primary ? 0 : 1;
        b.FlatAppearance.BorderColor = Color.FromArgb(72, 111, 136);
        return b;
    }

    private static LinkLabel MakeLink(string text, string url)
    {
        var l = new LinkLabel { Text = text, AutoSize = true, LinkColor = Blue, ActiveLinkColor = Color.White,
            Margin = new Padding(0, 4, 20, 0), VisitedLinkColor = Blue, TabStop = true };
        l.LinkClicked += (_, _) =>
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException)
            { MessageBox.Show("Unable to open link: " + e.Message, "RamRatio Setup"); }
        };
        return l;
    }
}
