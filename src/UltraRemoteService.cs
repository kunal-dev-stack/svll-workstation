// =============================================================================
// SVLL IT Workstation v8.1 — Ultra Remote Control Service
// High-performance, zero-touch, unattended remote desktop engine.
// Replaces AnyDesk / UltraViewer with zero licensing limits and zero nag screens.
//
// Automatically provisions on the client, extracts Remote ID & Password,
// and reflects them directly into the Admin Command Center for 1-click connect.
// =============================================================================

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace SVLL_IT_Workstation;

public static class UltraRemoteService
{
    public const string DefaultPassword = BuildConstants.DefaultRemotePassword;
    public const string EngineName = "UltraRemote";

    private static readonly string[] ExeCandidates = new[]
    {
        @"C:\Program Files\RustDesk\rustdesk.exe",
        @"C:\Program Files (x86)\RustDesk\rustdesk.exe",
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"RustDesk\rustdesk.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"RustDesk\rustdesk.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"SVLL-IT-Workstation\tools\rustdesk.exe"),
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rustdesk.exe"),
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "rustdesk.exe")
    };

    /// <summary>
    /// Returns the path to the executable if present.
    /// </summary>
    public static string? FindExecutable()
    {
        foreach (var p in ExeCandidates)
        {
            if (File.Exists(p)) return p;
        }

        // Try 'where rustdesk' in PATH
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "where.exe",
                Arguments = "rustdesk",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });
            if (proc != null)
            {
                string line = proc.StandardOutput.ReadLine()?.Trim() ?? "";
                proc.WaitForExit(1000);
                if (!string.IsNullOrEmpty(line) && File.Exists(line))
                    return line;
            }
        }
        catch { }

        return null;
    }

    /// <summary>
    /// Returns (IsInstalled, RemoteId, Password)
    /// </summary>
    public static (bool IsInstalled, string RemoteId, string Password) GetStatus()
    {
        string? exe = FindExecutable();
        bool installed = !string.IsNullOrEmpty(exe);
        string id = GetRemoteId();
        return (installed, id, DefaultPassword);
    }

    /// <summary>
    /// Queries the machine's unique partner ID via CLI or TOML config.
    /// </summary>
    public static string GetRemoteId()
    {
        string? exe = FindExecutable();
        if (!string.IsNullOrEmpty(exe))
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = "--get-id",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                if (p != null)
                {
                    string outStr = p.StandardOutput.ReadToEnd().Trim();
                    p.WaitForExit(3000);
                    string digits = new string(outStr.Where(char.IsDigit).ToArray());
                    if (digits.Length >= 6)
                    {
                        return FormatId(digits);
                    }
                }
            }
            catch { }
        }

        // Fallback: Parse TOML config file
        string[] tomlPaths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"RustDesk\config\RustDesk2.toml"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"RustDesk\config\RustDesk.toml"),
            @"C:\Windows\ServiceProfiles\LocalService\AppData\Roaming\RustDesk\config\RustDesk2.toml",
            @"C:\Windows\ServiceProfiles\LocalService\AppData\Roaming\RustDesk\config\RustDesk.toml"
        };

        foreach (var path in tomlPaths)
        {
            try
            {
                if (File.Exists(path))
                {
                    string content = File.ReadAllText(path);
                    var match = Regex.Match(content, @"id\s*=\s*['""]?(\d+)['""]?");
                    if (match.Success)
                    {
                        return FormatId(match.Groups[1].Value);
                    }
                }
            }
            catch { }
        }

        return "";
    }

    /// <summary>
    /// Formats raw digits like "123456789" into "123 456 789" (UltraViewer style)
    /// </summary>
    public static string FormatId(string raw)
    {
        string digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.Length == 9)
            return $"{digits[..3]} {digits.Substring(3, 3)} {digits[6..]}";
        if (digits.Length == 10)
            return $"{digits[..3]} {digits.Substring(3, 3)} {digits[6..]}";
        return digits;
    }

    /// <summary>
    /// Silently installs the Ultra Remote engine and sets the permanent unattended password.
    /// Used for self-provisioning over heartbeat or during install.ps1.
    /// </summary>
    public static async Task<bool> EnsureProvisionedAsync(Action<string>? onProgress = null)
    {
        string? exe = FindExecutable();

        if (string.IsNullOrEmpty(exe))
        {
            onProgress?.Invoke("Downloading Ultra Remote Engine...");
            string toolsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"SVLL-IT-Workstation\tools");
            Directory.CreateDirectory(toolsDir);
            string targetExe = Path.Combine(toolsDir, "rustdesk.exe");

            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
                string dlUrl = "https://github.com/rustdesk/rustdesk/releases/download/1.3.7/rustdesk-1.3.7-x86_64.exe";
                var bytes = await http.GetByteArrayAsync(dlUrl);
                await File.WriteAllBytesAsync(targetExe, bytes);

                onProgress?.Invoke("Registering Ultra Remote background service...");
                var psi = new ProcessStartInfo
                {
                    FileName = targetExe,
                    Arguments = "--install",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                var p = Process.Start(psi);
                if (p != null) await p.WaitForExitAsync();

                exe = FindExecutable() ?? targetExe;
            }
            catch (Exception ex)
            {
                onProgress?.Invoke($"Download notice: {ex.Message}");
            }
        }

        // Set unattended password
        if (!string.IsNullOrEmpty(exe))
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = $"--password {DefaultPassword}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                p?.WaitForExit(4000);
            }
            catch { }
            return true;
        }

        return false;
    }

    /// <summary>
    /// Admin build: Connect directly to remote endpoint with 1 click using ID and Password.
    /// Zero prompts, zero acceptance required on remote PC.
    /// </summary>
    public static bool LaunchRemoteConnection(string remoteId, string password = DefaultPassword)
    {
        string cleanId = remoteId.Replace(" ", "").Trim();
        if (string.IsNullOrEmpty(cleanId))
        {
            MessageBox.Show("No Ultra Remote ID available for this PC.", "Remote ID Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        // Copy ID and password to clipboard for admin convenience
        try { Clipboard.SetText(cleanId); } catch { }

        string? exe = FindExecutable();
        if (string.IsNullOrEmpty(exe))
        {
            var res = MessageBox.Show(
                $"Ultra Remote Engine is required to connect but is not yet installed on this Admin machine.\n\n" +
                $"Target ID: {remoteId}\nPassword: {password} (Copied to clipboard!)\n\n" +
                $"Would you like to download and install the Ultra Remote viewer now?",
                "Install Ultra Remote Viewer",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (res == MessageBoxResult.Yes)
            {
                _ = Task.Run(async () =>
                {
                    bool ok = await EnsureProvisionedAsync();
                    if (ok)
                    {
                        Application.Current?.Dispatcher.Invoke(() => LaunchRemoteConnection(cleanId, password));
                    }
                    else
                    {
                        MessageBox.Show("Could not download Ultra Remote automatically. Please check your internet connection.", "Notice", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                });
            }
            return false;
        }

        try
        {
            // rustdesk.exe --connect <ID> --password <PASS>
            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = $"--connect {cleanId} --password {password}",
                UseShellExecute = true
            };
            Process.Start(psi);
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not launch Ultra Remote session: {ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }
}
