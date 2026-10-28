// =============================================================================
// SVLL IT Workstation v8.4 — AegisMesh Enterprise Remote Service
// Pure AegisMesh zero-touch remote management and remote desktop engine.
// Replaces third-party tools with zero licensing limits and zero nag screens.
// =============================================================================

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SVLL_IT_Workstation;

public static class UltraRemoteService
{
    public const string DefaultPassword = BuildConstants.DefaultRemotePassword;
    public const string EngineName = "AegisMesh";

    public static void SyncBundledTools()
    {
        // Pure AegisMesh architecture - no third-party legacy dependencies needed
    }

    public static void EnsureDirectServerConfigured()
    {
        // AegisMesh uses zero-configuration direct WebSockets & HTTP
    }

    public static string? FindExecutable()
    {
        string[] candidates = new[]
        {
            @"C:\Program Files\AegisMesh\AegisMeshAgent.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"AegisMesh\AegisMeshAgent.exe"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AegisMeshAgent.exe")
        };

        foreach (var p in candidates)
        {
            if (File.Exists(p)) return p;
        }
        return null;
    }

    public static (bool IsInstalled, string RemoteId, string Password) GetStatus()
    {
        string id = GetRemoteId();
        bool installed = !string.IsNullOrEmpty(FindExecutable()) || !string.IsNullOrEmpty(id);
        return (installed, id, DefaultPassword);
    }

    public static string GetRemoteId()
    {
        try
        {
            string idFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"AegisMesh\device.id");
            if (File.Exists(idFile))
            {
                string id = File.ReadAllText(idFile).Trim();
                if (!string.IsNullOrEmpty(id)) return id;
            }
        }
        catch { }

        return $"win-{Environment.MachineName.ToLower()}";
    }

    public static Task<bool> EnsureProvisionedAsync(Action<string>? onProgress = null)
    {
        return Task.FromResult(true);
    }

    public static bool LaunchRemoteConnection(string remoteId, string password = DefaultPassword, string? targetIp = null)
    {
        string relayUrl = AdminAuthService.Instance.GetRelayServerUrl();
        if (string.IsNullOrEmpty(relayUrl)) relayUrl = BuildConstants.DefaultRelayUrl;
        relayUrl = relayUrl.TrimEnd('/');

        string target = !string.IsNullOrEmpty(remoteId) ? remoteId : (targetIp?.Trim() ?? "");
        if (string.IsNullOrEmpty(target))
        {
            MessageBox.Show("No AegisMesh device ID or IP address available for this PC.", "Remote Target Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        try
        {
            // Open AegisMesh Web Console for the device in the default browser
            string url = $"{relayUrl}/?device={Uri.EscapeDataString(target)}";
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });

            LogRemoteConnectionAudit(target, "AegisMesh Zero-Touch Remote Desktop", true);
            return true;
        }
        catch (Exception ex)
        {
            LogRemoteConnectionAudit(target, "AegisMesh Zero-Touch Remote Desktop", false, ex.Message);
            MessageBox.Show($"Could not open AegisMesh Remote Console: {ex.Message}\nGateway: {relayUrl}", "Remote Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

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

    public static bool LaunchDirectIpConnection(string ipAddress, string password = DefaultPassword)
    {
        return LaunchRemoteConnection(ipAddress, password, ipAddress);
    }
}
