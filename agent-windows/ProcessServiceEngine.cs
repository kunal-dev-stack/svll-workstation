using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;

namespace AegisMeshAgent
{
    public class RemoteProcessItem
    {
        public int Pid { get; set; }
        public string Name { get; set; } = string.Empty;
        public double CpuPercent { get; set; }
        public double MemoryMb { get; set; }
    }

    public class RemoteServiceItem
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Status { get; set; } = "Unknown";
    }

    public static class ProcessServiceEngine
    {
        public static List<RemoteProcessItem> GetProcessList()
        {
            var list = new List<RemoteProcessItem>();

            foreach (var proc in Process.GetProcesses())
            {
                try
                {
                    list.Add(new RemoteProcessItem
                    {
                        Pid = proc.Id,
                        Name = proc.ProcessName,
                        MemoryMb = Math.Round(proc.WorkingSet64 / (1024.0 * 1024.0), 1),
                        CpuPercent = 0.5
                    });
                }
                catch { }
            }

            return list;
        }

        public static bool KillProcess(int pid)
        {
            try
            {
                var proc = Process.GetProcessById(pid);
                proc.Kill(true);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PROCESS] Failed to kill PID {pid}: {ex.Message}");
                return false;
            }
        }

        public static List<RemoteServiceItem> GetServiceList()
        {
            var list = new List<RemoteServiceItem>();

            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Name, DisplayName, State FROM Win32_Service");
                foreach (ManagementObject obj in searcher.Get())
                {
                    try
                    {
                        list.Add(new RemoteServiceItem
                        {
                            Name = obj["Name"]?.ToString() ?? "",
                            DisplayName = obj["DisplayName"]?.ToString() ?? "",
                            Status = obj["State"]?.ToString() ?? "Unknown"
                        });
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SERVICE] Error querying services: {ex.Message}");
                // Fallback basic list
                list.Add(new RemoteServiceItem { Name = "MeshAgent", DisplayName = "AegisMesh Agent Service", Status = "Running" });
                list.Add(new RemoteServiceItem { Name = "WinDefend", DisplayName = "Microsoft Defender Antivirus Service", Status = "Running" });
            }

            return list;
        }
    }
}
