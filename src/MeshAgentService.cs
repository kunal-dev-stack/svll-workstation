// =============================================================================
// SVLL IT Workstation v8.0 - MeshCentral Agent Service
// Detects if MeshAgent is installed and running, reads the node ID,
// and provides the Admin build with a direct launch URL.
//
// MeshCentral is a free, open-source remote control platform (like ScreenConnect).
// The User build installer silently installs MeshAgent as a Windows Service.
// Admin connects via the MeshCentral web UI — no password needed on remote end.
//
// MeshAgent install location: C:\Program Files\Mesh Agent\MeshAgent.exe
// MeshAgent service name:     Mesh Agent
// Node ID file:               C:\Program Files\Mesh Agent\.msh  (contains ServerMeshID)
// =============================================================================

using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace SVLL_IT_Workstation;

public static class MeshAgentService
{
    public const string ServiceName = "Mesh Agent";

    private static readonly string[] AgentExePaths = new[]
    {
        @"C:\Program Files\Mesh Agent\MeshAgent.exe",
        @"C:\Program Files (x86)\Mesh Agent\MeshAgent.exe",
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Mesh Agent\MeshAgent.exe"),
    };

    private static readonly string[] MshConfigPaths = new[]
    {
        @"C:\Program Files\Mesh Agent\meshagent.msh",
        @"C:\Program Files\Mesh Agent\.msh",
        @"C:\Program Files (x86)\Mesh Agent\meshagent.msh",
    };

    /// <summary>
    /// Returns (isRunning, nodeId). nodeId is empty string if agent not configured.
    /// </summary>
    public static (bool IsRunning, string NodeId) GetStatus()
    {
        bool isRunning = false;
        string nodeId = "";

        // Check Windows service
        try
        {
            using var svc = new ServiceController(ServiceName);
            isRunning = svc.Status == ServiceControllerStatus.Running;
        }
        catch { }

        // Read node ID from registry (MeshAgent stores it here)
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Open Source\MeshAgent");
            nodeId = key?.GetValue("NodeID")?.ToString() ?? "";
        }
        catch { }

        // Fallback: parse meshagent.msh config file
        if (string.IsNullOrEmpty(nodeId))
        {
            foreach (var path in MshConfigPaths)
            {
                try
                {
                    if (!File.Exists(path)) continue;
                    string content = File.ReadAllText(path);
                    var match = Regex.Match(content, @"MeshID=([a-fA-F0-9]+)");
                    if (match.Success)
                    {
                        nodeId = match.Groups[1].Value;
                        break;
                    }
                }
                catch { }
            }
        }

        return (isRunning, nodeId);
    }

    /// <summary>Returns true if MeshAgent.exe is installed on disk.</summary>
    public static bool IsInstalled()
    {
        foreach (var path in AgentExePaths)
        {
            if (File.Exists(path)) return true;
        }
        return false;
    }

    /// <summary>Returns path to MeshAgent.exe if found, null otherwise.</summary>
    public static string? FindAgentExe()
    {
        foreach (var path in AgentExePaths)
        {
            if (File.Exists(path)) return path;
        }
        return null;
    }

    /// <summary>
    /// Admin build only: open the MeshCentral web console to control a specific machine.
    /// meshServerUrl should be like http://192.168.1.10:8086
    /// nodeId is the MeshAgent node ID reported in the heartbeat.
    /// </summary>
    public static bool LaunchRemoteControl(string meshServerUrl, string nodeId)
    {
        if (string.IsNullOrEmpty(meshServerUrl) || string.IsNullOrEmpty(nodeId))
            return false;
        try
        {
            // MeshCentral remote desktop URL format
            string url = $"{meshServerUrl.TrimEnd('/')}/control.ashx?nodeid={Uri.EscapeDataString(nodeId)}";
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            return true;
        }
        catch { }
        return false;
    }

    /// <summary>
    /// Launch the MeshCentral main dashboard (Admin build).
    /// </summary>
    public static bool LaunchMeshDashboard(string meshServerUrl)
    {
        if (string.IsNullOrEmpty(meshServerUrl)) return false;
        try
        {
            Process.Start(new ProcessStartInfo { FileName = meshServerUrl, UseShellExecute = true });
            return true;
        }
        catch { }
        return false;
    }

    /// <summary>
    /// Attempt to start the MeshAgent Windows service (requires elevation).
    /// Used for self-healing in User build.
    /// </summary>
    public static void StartService()
    {
        try
        {
            using var svc = new ServiceController(ServiceName);
            if (svc.Status != ServiceControllerStatus.Running)
            {
                svc.Start();
                svc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
            }
        }
        catch { }
    }

    /// <summary>
    /// Silently installs MeshAgent service using local executable if available.
    /// </summary>
    public static bool TryInstallLocalAgent(string? sourceExe = null)
    {
        try
        {
            if (IsInstalled())
            {
                StartService();
                return true;
            }

            string candidate = sourceExe ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MeshAgent.exe");
            if (!File.Exists(candidate)) return false;

            var psi = new ProcessStartInfo
            {
                FileName = candidate,
                Arguments = "-install",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            var proc = Process.Start(psi);
            proc?.WaitForExit(10000);

            StartService();
            return IsInstalled();
        }
        catch { }
        return false;
    }
}

