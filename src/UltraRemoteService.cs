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
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "rustdesk.exe"),
        @"C:\Program Files\RustDesk\rustdesk.exe",
        @"C:\Program Files (x86)\RustDesk\rustdesk.exe",
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"SVLL-IT-Workstation\tools\rustdesk.exe"),
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rustdesk.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"RustDesk\rustdesk.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"RustDesk\rustdesk.exe")
    };

    /// <summary>
    /// Synchronizes all bundled engine files (rustdesk.exe, Flutter plugin DLLs, data folder)
    /// to Program Files and ProgramData so that missing dependency DLLs (e.g. desktop_drop_plugin.dll)
    /// are never encountered.
    /// </summary>
    public static void SyncBundledTools()
    {
        try
        {
            string sourceDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools");
            if (!Directory.Exists(sourceDir)) return;

            string[] targets = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RustDesk"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"SVLL-IT-Workstation\tools")
            };

            foreach (var target in targets)
            {
                try
                {
                    if (!Directory.Exists(target)) Directory.CreateDirectory(target);
                    foreach (var file in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
                    {
                        string relative = Path.GetRelativePath(sourceDir, file);
                        string destFile = Path.Combine(target, relative);
                        string? destSubDir = Path.GetDirectoryName(destFile);
                        if (!string.IsNullOrEmpty(destSubDir) && !Directory.Exists(destSubDir))
                        {
                            Directory.CreateDirectory(destSubDir);
                        }
                        if (!File.Exists(destFile) || new FileInfo(file).Length != new FileInfo(destFile).Length)
                        {
                            File.Copy(file, destFile, true);
                        }
                    }
                }
                catch { }
            }
        }
        catch { }
    }

    /// <summary>
    /// Returns the path to the executable if present.
    /// </summary>
    public static string? FindExecutable()
    {
        SyncBundledTools();

        foreach (var p in ExeCandidates)
        {
            if (File.Exists(p))
            {
                string dir = Path.GetDirectoryName(p) ?? "";
                string dropDll = Path.Combine(dir, "desktop_drop_plugin.dll");
                if (File.Exists(dropDll) || p.Contains("ITWorkstation"))
                    return p;

                string bundledDrop = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "desktop_drop_plugin.dll");
                if (File.Exists(bundledDrop))
                {
                    try { File.Copy(bundledDrop, dropDll, true); } catch { }
                    if (File.Exists(dropDll)) return p;
                }
                else
                {
                    return p;
                }
            }
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
                    WorkingDirectory = Path.GetDirectoryName(exe) ?? "",
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
        SyncBundledTools();
        string? exe = FindExecutable();

        if (string.IsNullOrEmpty(exe))
        {
            string toolsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"SVLL-IT-Workstation\tools");
            Directory.CreateDirectory(toolsDir);
            string targetExe = Path.Combine(toolsDir, "rustdesk.exe");

            // 1. Check if bundled in local application directory
            string localBundled = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "rustdesk.exe");
            if (File.Exists(localBundled))
            {
                try
                {
                    SyncBundledTools();
                    exe = FindExecutable() ?? targetExe;
                }
                catch { }
            }

            // 2. Fallback to download if not bundled
            if (string.IsNullOrEmpty(exe) || !File.Exists(exe))
            {
                onProgress?.Invoke("Downloading Ultra Remote Engine...");
                try
                {
                    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
                    string dlUrl = "https://github.com/rustdesk/rustdesk/releases/download/1.3.7/rustdesk-1.3.7-x86_64.exe";
                    var bytes = await http.GetByteArrayAsync(dlUrl);
                    await File.WriteAllBytesAsync(targetExe, bytes);
                    exe = targetExe;
                }
                catch (Exception ex)
                {
                    onProgress?.Invoke($"Notice: {ex.Message}");
                }
            }

            // Register background service
            if (!string.IsNullOrEmpty(exe) && File.Exists(exe))
            {
                onProgress?.Invoke("Registering Ultra Remote background service...");
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = exe,
                        Arguments = "--silent-install",
                        WorkingDirectory = Path.GetDirectoryName(exe) ?? "",
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    var p = Process.Start(psi);
                    if (p != null) await p.WaitForExitAsync();
                }
                catch { }

                exe = FindExecutable() ?? targetExe;

                // Ensure the Windows service ImagePath is correctly set to --service
                try
                {
                    using var scKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\RustDesk", true);
                    if (scKey != null)
                    {
                        string currentPath = scKey.GetValue("ImagePath") as string ?? "";
                        if (!currentPath.Contains("--service"))
                        {
                            scKey.SetValue("ImagePath", $"\"{exe}\" --service");
                        }
                    }
                }
                catch { }
            }
        }

        // Configure permanent unattended password, direct IP listener, and start service
        if (!string.IsNullOrEmpty(exe) && File.Exists(exe))
        {
            try
            {
                // Set password
                var psi = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = $"--password {DefaultPassword}",
                    WorkingDirectory = Path.GetDirectoryName(exe) ?? "",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                p?.WaitForExit(4000);

                // Enable Direct IP peer-to-peer server listener in TOML config
                EnsureDirectServerConfigured();

                // Ensure service ImagePath is set to --service
                try
                {
                    using var scKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\RustDesk", true);
                    if (scKey != null)
                    {
                        string currentPath = scKey.GetValue("ImagePath") as string ?? "";
                        if (!currentPath.Contains("--service"))
                        {
                            scKey.SetValue("ImagePath", $"\"{exe}\" --service");
                        }
                    }
                }
                catch { }

                // Ensure service is running
                using var scProc = Process.Start(new ProcessStartInfo
                {
                    FileName = "net.exe",
                    Arguments = "start RustDesk",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                scProc?.WaitForExit(3000);

                // Open firewall for direct peer-to-peer LAN connection (both App and TCP/UDP port 21118)
                using var fwPort = Process.Start(new ProcessStartInfo
                {
                    FileName = "netsh.exe",
                    Arguments = "advfirewall firewall add rule name=\"UltraRemotePort21118\" dir=in action=allow protocol=TCP localport=21118 enable=yes",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                fwPort?.WaitForExit(2000);

                using var fwUdp = Process.Start(new ProcessStartInfo
                {
                    FileName = "netsh.exe",
                    Arguments = "advfirewall firewall add rule name=\"UltraRemoteUDP\" dir=in action=allow protocol=UDP localport=21118-21119 enable=yes",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                fwUdp?.WaitForExit(2000);

                using var fwApp = Process.Start(new ProcessStartInfo
                {
                    FileName = "netsh.exe",
                    Arguments = $"advfirewall firewall add rule name=\"UltraRemoteApp\" dir=in action=allow program=\"{exe}\" enable=yes",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                fwApp?.WaitForExit(2000);
            }
            catch { }
            return true;
        }

        return false;
    }

    /// <summary>
    /// Configures direct-server = 'Y' and direct-access-port = '21118' in RustDesk configuration.
    /// </summary>
    public static void EnsureDirectServerConfigured()
    {
        string[] tomlPaths = new[]
        {
            @"C:\Windows\ServiceProfiles\LocalService\AppData\Roaming\RustDesk\config\RustDesk2.toml",
            @"C:\Windows\ServiceProfiles\LocalService\AppData\Roaming\RustDesk\config\RustDesk.toml",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"RustDesk\config\RustDesk2.toml"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"RustDesk\config\RustDesk.toml"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"RustDesk\config\RustDesk2.toml")
        };

        bool changed = false;

        foreach (var p in tomlPaths)
        {
            try
            {
                string dir = Path.GetDirectoryName(p) ?? "";
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                string content = File.Exists(p) ? File.ReadAllText(p) : "";
                string original = content;

                if (string.IsNullOrWhiteSpace(content))
                {
                    content = "[options]\r\ndirect-server = 'Y'\r\ndirect-access-port = '21118'\r\n";
                }
                else
                {
                    if (Regex.IsMatch(content, @"direct-server\s*="))
                    {
                        content = Regex.Replace(content, @"direct-server\s*=\s*['""]?[^'""\r\n]+['""]?", "direct-server = 'Y'");
                    }
                    else if (content.Contains("[options]"))
                    {
                        content = content.Replace("[options]", "[options]\r\ndirect-server = 'Y'");
                    }
                    else
                    {
                        content += "\r\n[options]\r\ndirect-server = 'Y'\r\n";
                    }

                    if (Regex.IsMatch(content, @"direct-access-port\s*="))
                    {
                        content = Regex.Replace(content, @"direct-access-port\s*=\s*['""]?[^'""\r\n]+['""]?", "direct-access-port = '21118'");
                    }
                    else
                    {
                        content = content.Replace("direct-server = 'Y'", "direct-server = 'Y'\r\ndirect-access-port = '21118'");
                    }
                }

                if (content != original)
                {
                    File.WriteAllText(p, content);
                    changed = true;
                }
            }
            catch { }
        }

        if (changed)
        {
            try
            {
                using var pRestart = Process.Start(new ProcessStartInfo
                {
                    FileName = "net.exe",
                    Arguments = "stop RustDesk",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                pRestart?.WaitForExit(2000);

                using var pStart = Process.Start(new ProcessStartInfo
                {
                    FileName = "net.exe",
                    Arguments = "start RustDesk",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                pStart?.WaitForExit(2000);
            }
            catch { }
        }
    }

    /// <summary>
    /// Admin build: Connect directly to remote endpoint with 1 click using ID and Password.
    /// Can target either Remote Partner ID or Direct LAN IP address.
    /// </summary>
    public static bool LaunchRemoteConnection(string remoteId, string password = DefaultPassword, string? targetIp = null)
    {
        string cleanId = remoteId.Replace(" ", "").Trim();
        string target = !string.IsNullOrEmpty(cleanId) ? cleanId : (targetIp?.Trim() ?? "");

        if (string.IsNullOrEmpty(target))
        {
            MessageBox.Show("No Ultra Remote ID or IP address available for this PC.", "Remote Target Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        // Copy ID/target and password to clipboard for admin convenience
        try { Clipboard.SetText(target); } catch { }

        string? exe = FindExecutable();
        if (string.IsNullOrEmpty(exe))
        {
            var res = MessageBox.Show(
                $"Ultra Remote Engine is required to connect but is not yet installed on this Admin machine.\n\n" +
                $"Target: {target}\nPassword: {password} (Copied to clipboard!)\n\n" +
                $"Would you like to install the Ultra Remote viewer now?",
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
                        Application.Current?.Dispatcher.Invoke(() => LaunchRemoteConnection(cleanId, password, targetIp));
                    }
                    else
                    {
                        MessageBox.Show("Could not install Ultra Remote viewer. Please verify setup file.", "Notice", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                });
            }
            return false;
        }

        try
        {
            // rustdesk.exe --connect <ID_OR_IP> --password <PASS>
            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = $"--connect {target} --password {password}",
                WorkingDirectory = Path.GetDirectoryName(exe) ?? "",
                UseShellExecute = true
            };
            Process.Start(psi);
            LogRemoteConnectionAudit(target, target == targetIp ? "Direct LAN Peer-to-Peer" : "Ultra Remote Partner ID", true);
            return true;
        }
        catch (Exception ex)
        {
            LogRemoteConnectionAudit(target, target == targetIp ? "Direct LAN Peer-to-Peer" : "Ultra Remote Partner ID", false, ex.Message);
            MessageBox.Show($"Could not launch Ultra Remote session: {ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    /// <summary>
    /// Audit logging for all initiated remote access sessions (Phase 4 audit compliance).
    /// Logs timestamp, operator, target, and outcome without recording secrets.
    /// </summary>
    public static void LogRemoteConnectionAudit(string target, string mode, bool success, string? error = null)
    {
        try
        {
            string auditDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"SVLL-IT-Workstation\audit");
            if (!Directory.Exists(auditDir)) Directory.CreateDirectory(auditDir);
            string logFile = Path.Combine(auditDir, "remote_connections.log");
            string entry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] OPERATOR: {Environment.UserName} | TARGET: {target} | MODE: {mode} | SUCCESS: {success}" + (error != null ? $" | ERROR: {error}" : "") + Environment.NewLine;
            File.AppendAllText(logFile, entry);
        }
        catch { }
    }

    /// <summary>
    /// Connects directly via LAN IP address without relying on external relay servers.
    /// </summary>
    public static bool LaunchDirectIpConnection(string ipAddress, string password = DefaultPassword)
    {
        string cleanIp = ipAddress.Trim();
        if (string.IsNullOrEmpty(cleanIp))
        {
            MessageBox.Show("No IP address available for direct LAN connection.", "IP Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        return LaunchRemoteConnection(cleanIp, password, cleanIp);
    }
}

