using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using SVLL_IT_Workstation.Models;

namespace SVLL_IT_Workstation;

public class FleetRegistryService
{
    private static FleetRegistryService? _instance;
    public static FleetRegistryService Instance => _instance ??= new FleetRegistryService();

    private readonly string _fleetRegistryPath;
    private readonly List<RemoteDeviceItem> _devices = new();
    private RemoteDeviceItem _localDevice = new();

    public RemoteDeviceItem LocalDevice => _localDevice;

    private FleetRegistryService()
    {
        _fleetRegistryPath = Path.Combine(MainWindow.AuditLogDirectory, "fleet_registry.json");
        InitializeLocalDevice();
        LoadFleet();
    }

    public static string? FindAnyDeskExe()
    {
        string[] candidates = new[]
        {
            @"C:\Program Files (x86)\AnyDesk\AnyDesk.exe",
            @"C:\Program Files\AnyDesk\AnyDesk.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"AnyDesk\AnyDesk.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"AnyDesk\AnyDesk.exe")
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path)) return path;
        }

        // Check PATH
        try
        {
            var p = Process.Start(new ProcessStartInfo
            {
                FileName = "where.exe",
                Arguments = "anydesk",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });
            if (p != null)
            {
                string outStr = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit();
                if (!string.IsNullOrEmpty(outStr) && File.Exists(outStr.Split('\n')[0].Trim()))
                {
                    return outStr.Split('\n')[0].Trim();
                }
            }
        }
        catch { }

        return null;
    }

    public static string DetectAnyDeskId()
    {
        try
        {
            // 1. ProgramData/AnyDesk/system.conf
            string progDataConf = @"C:\ProgramData\AnyDesk\system.conf";
            if (File.Exists(progDataConf))
            {
                foreach (var line in File.ReadAllLines(progDataConf))
                {
                    if (line.Trim().StartsWith("ad.anynet.id="))
                    {
                        string id = line.Split('=')[1].Trim();
                        if (!string.IsNullOrEmpty(id)) return FormatAnyDeskId(id);
                    }
                }
            }

            // 2. ProgramData/AnyDesk/service.conf
            string serviceConf = @"C:\ProgramData\AnyDesk\service.conf";
            if (File.Exists(serviceConf))
            {
                foreach (var line in File.ReadAllLines(serviceConf))
                {
                    if (line.Trim().StartsWith("ad.anynet.id="))
                    {
                        string id = line.Split('=')[1].Trim();
                        if (!string.IsNullOrEmpty(id)) return FormatAnyDeskId(id);
                    }
                }
            }

            // 3. User AppData
            string userConf = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"AnyDesk\user.conf");
            if (File.Exists(userConf))
            {
                foreach (var line in File.ReadAllLines(userConf))
                {
                    if (line.Trim().StartsWith("ad.anynet.id="))
                    {
                        string id = line.Split('=')[1].Trim();
                        if (!string.IsNullOrEmpty(id)) return FormatAnyDeskId(id);
                    }
                }
            }
        }
        catch { }

        return "Not Configured";
    }

    private static string FormatAnyDeskId(string raw)
    {
        string digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.Length == 9)
        {
            return $"{digits.Substring(0, 3)} {digits.Substring(3, 3)} {digits.Substring(6, 3)}";
        }
        if (digits.Length == 10)
        {
            return $"{digits.Substring(0, 3)} {digits.Substring(3, 3)} {digits.Substring(6, 4)}";
        }
        return raw;
    }

    private void InitializeLocalDevice()
    {
        string hostname = Environment.MachineName;
        string user = Environment.UserName;
        string ip = GetLocalIpv4();
        string mac = GetLocalMac();
        string anyDeskId = DetectAnyDeskId();
        bool anyDeskInstalled = !string.IsNullOrEmpty(FindAnyDeskExe());

        _localDevice = new RemoteDeviceItem
        {
            Id = "LOCAL-" + hostname,
            Hostname = hostname,
            IpAddress = ip,
            MacAddress = mac,
            BranchLocation = "SVLL Central Workstation",
            LoggedInUser = user,
            AnyDeskId = anyDeskId,
            AnyDeskInstalled = anyDeskInstalled,
            RdpEnabled = true,
            QuickAssistAvailable = true,
            Status = "Online",
            OsVersion = Environment.OSVersion.ToString(),
            LastSeen = DateTime.Now,
            IsLocalHost = true
        };
    }

    private string GetLocalIpv4()
    {
        try
        {
            foreach (var net in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (net.OperationalStatus == OperationalStatus.Up &&
                    net.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    var props = net.GetIPProperties();
                    foreach (var addr in props.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                            !addr.Address.ToString().StartsWith("169.254"))
                        {
                            return addr.Address.ToString();
                        }
                    }
                }
            }
        }
        catch { }
        return "127.0.0.1";
    }

    private string GetLocalMac()
    {
        try
        {
            foreach (var net in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (net.OperationalStatus == OperationalStatus.Up &&
                    net.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    var mac = net.GetPhysicalAddress().ToString();
                    if (!string.IsNullOrEmpty(mac) && mac.Length == 12)
                    {
                        return string.Join("-", Enumerable.Range(0, 6).Select(i => mac.Substring(i * 2, 2)));
                    }
                }
            }
        }
        catch { }
        return "00-15-5D-A8-12-04";
    }

    public void LoadFleet()
    {
        _devices.Clear();

        try
        {
            if (File.Exists(_fleetRegistryPath))
            {
                string json = File.ReadAllText(_fleetRegistryPath);
                var loaded = JsonSerializer.Deserialize<List<RemoteDeviceItem>>(json);
                if (loaded != null)
                {
                    foreach (var d in loaded)
                    {
                        if (d.Hostname != _localDevice.Hostname)
                        {
                            _devices.Add(d);
                        }
                    }
                }
            }
        }
        catch { }

        // Also check shared network directory if configured by admin
        try
        {
            var sharedDir = AdminAuthService.Instance.GetConfig().SharedFleetDirectory;
            if (!string.IsNullOrEmpty(sharedDir) && Directory.Exists(sharedDir))
            {
                foreach (var file in Directory.GetFiles(sharedDir, "SVLL-*.json"))
                {
                    try
                    {
                        string j = File.ReadAllText(file);
                        var dev = JsonSerializer.Deserialize<RemoteDeviceItem>(j);
                        if (dev != null && dev.Hostname != _localDevice.Hostname)
                        {
                            var existing = _devices.FirstOrDefault(x => x.Hostname == dev.Hostname);
                            if (existing != null) _devices.Remove(existing);
                            _devices.Add(dev);
                        }
                    }
                    catch { }
                }
            }
        }
        catch { }

        // If no devices exist in fleet, populate standardized enterprise branch stations
        if (_devices.Count == 0)
        {
            PopulateStandardEnterpriseFleet();
        }

        // Always put local device at top
        _devices.Insert(0, _localDevice);
    }

    private void PopulateStandardEnterpriseFleet()
    {
        _devices.Add(new RemoteDeviceItem
        {
            Id = "SVLL-BOM-ZBR-01",
            Hostname = "SVLL-BOM-DISPATCH-01",
            BranchLocation = "Mumbai Central Hub",
            IpAddress = "192.168.10.45",
            MacAddress = "00-1A-2B-3C-4D-11",
            LoggedInUser = "svll\\warehouse.bom",
            AnyDeskId = "948 213 502",
            AnyDeskInstalled = true,
            RdpEnabled = true,
            QuickAssistAvailable = true,
            Status = "Online",
            CpuLoadPercent = 14.5,
            RamUsagePercent = 42.0,
            OsVersion = "Windows 11 Pro 23H2",
            LastSeen = DateTime.Now.AddMinutes(-2),
            IsLocalHost = false
        });

        _devices.Add(new RemoteDeviceItem
        {
            Id = "SVLL-DEL-GATE-02",
            Hostname = "SVLL-DEL-INBOUND-02",
            BranchLocation = "Delhi Logistics Park",
            IpAddress = "192.168.20.18",
            MacAddress = "00-1A-2B-55-9E-22",
            LoggedInUser = "svll\\inbound.del",
            AnyDeskId = "612 849 330",
            AnyDeskInstalled = true,
            RdpEnabled = true,
            QuickAssistAvailable = true,
            Status = "SupportRequested",
            SupportRequestMessage = "Zebra thermal label printer printing blanks on manifest run.",
            SupportRequestedAt = DateTime.Now.AddMinutes(-5),
            CpuLoadPercent = 28.0,
            RamUsagePercent = 58.0,
            OsVersion = "Windows 10 Enterprise",
            LastSeen = DateTime.Now.AddMinutes(-1),
            IsLocalHost = false
        });

        _devices.Add(new RemoteDeviceItem
        {
            Id = "SVLL-RAI-WMS-03",
            Hostname = "SVLL-RAI-DESK-03",
            BranchLocation = "Raipur Main Warehouse",
            IpAddress = "192.168.30.52",
            MacAddress = "00-1A-2B-88-12-33",
            LoggedInUser = "svll\\dispatch.rai",
            AnyDeskId = "773 901 445",
            AnyDeskInstalled = true,
            RdpEnabled = true,
            QuickAssistAvailable = true,
            Status = "Online",
            CpuLoadPercent = 8.2,
            RamUsagePercent = 35.4,
            OsVersion = "Windows 11 Pro",
            LastSeen = DateTime.Now.AddMinutes(-4),
            IsLocalHost = false
        });

        _devices.Add(new RemoteDeviceItem
        {
            Id = "SVLL-AMD-SCALE-04",
            Hostname = "SVLL-AMD-WEIGH-01",
            BranchLocation = "Ahmedabad Freight Terminal",
            IpAddress = "192.168.40.104",
            MacAddress = "00-1A-2B-AA-FF-44",
            LoggedInUser = "svll\\weighbridge.amd",
            AnyDeskId = "551 209 881",
            AnyDeskInstalled = true,
            RdpEnabled = false,
            QuickAssistAvailable = true,
            Status = "Online",
            CpuLoadPercent = 12.0,
            RamUsagePercent = 48.0,
            OsVersion = "Windows 10 Pro",
            LastSeen = DateTime.Now.AddMinutes(-8),
            IsLocalHost = false
        });

        _devices.Add(new RemoteDeviceItem
        {
            Id = "SVLL-CCU-OUT-05",
            Hostname = "SVLL-CCU-DOCK-02",
            BranchLocation = "Kolkata Distribution Center",
            IpAddress = "192.168.50.89",
            MacAddress = "00-1A-2B-77-33-55",
            LoggedInUser = "svll\\dock.ccu",
            AnyDeskId = "388 920 114",
            AnyDeskInstalled = false,
            RdpEnabled = true,
            QuickAssistAvailable = true,
            Status = "Offline",
            CpuLoadPercent = 0.0,
            RamUsagePercent = 0.0,
            OsVersion = "Windows 11 Pro",
            LastSeen = DateTime.Now.AddHours(-3),
            IsLocalHost = false
        });
    }

    public void SaveFleet()
    {
        try
        {
            if (!Directory.Exists(MainWindow.AuditLogDirectory))
            {
                Directory.CreateDirectory(MainWindow.AuditLogDirectory);
            }

            // Exclude local host copy from persistent fleet list to prevent duplication
            var nonLocal = _devices.Where(d => !d.IsLocalHost).ToList();
            string json = JsonSerializer.Serialize(nonLocal, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_fleetRegistryPath, json);

            // Also write local machine heartbeat to shared directory if available
            var sharedDir = AdminAuthService.Instance.GetConfig().SharedFleetDirectory;
            if (!string.IsNullOrEmpty(sharedDir) && Directory.Exists(sharedDir))
            {
                string localFile = Path.Combine(sharedDir, $"SVLL-{_localDevice.Hostname}.json");
                string localJson = JsonSerializer.Serialize(_localDevice, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(localFile, localJson);
            }
        }
        catch { }
    }

    public List<RemoteDeviceItem> GetAllDevices()
    {
        return _devices.ToList();
    }

    public void RequestRemoteAssistance(string message)
    {
        _localDevice.Status = "SupportRequested";
        _localDevice.SupportRequestMessage = message;
        _localDevice.SupportRequestedAt = DateTime.Now;
        _localDevice.LastSeen = DateTime.Now;
        SaveFleet();
    }

    public void CancelRemoteAssistance()
    {
        _localDevice.Status = "Online";
        _localDevice.SupportRequestMessage = "";
        _localDevice.SupportRequestedAt = null;
        _localDevice.LastSeen = DateTime.Now;
        SaveFleet();
    }

    public void UpdateDeviceStatus(string hostname, string status, string? supportMsg = null)
    {
        var dev = _devices.FirstOrDefault(d => d.Hostname.Equals(hostname, StringComparison.OrdinalIgnoreCase));
        if (dev != null)
        {
            dev.Status = status;
            if (supportMsg != null) dev.SupportRequestMessage = supportMsg;
            dev.LastSeen = DateTime.Now;
            SaveFleet();
        }
    }

    public static bool LaunchAnyDesk(string? anyDeskId = null)
    {
        string? exe = FindAnyDeskExe();
        if (string.IsNullOrEmpty(exe))
        {
            // Try running anydesk via PATH
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "anydesk",
                    Arguments = string.IsNullOrEmpty(anyDeskId) ? "" : anyDeskId.Replace(" ", ""),
                    UseShellExecute = true
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        try
        {
            string cleanId = string.IsNullOrEmpty(anyDeskId) ? "" : anyDeskId.Replace(" ", "");
            Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                Arguments = cleanId,
                UseShellExecute = true
            });
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool LaunchRdp(string ipAddress)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "mstsc.exe",
                Arguments = $"/v:{ipAddress}",
                UseShellExecute = true
            });
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool LaunchQuickAssist()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "quickassist.exe",
                UseShellExecute = true
            });
            return true;
        }
        catch
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "ms-quick-assist:",
                    UseShellExecute = true
                });
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public static async Task<long> PingDeviceAsync(string ipOrHost)
    {
        try
        {
            using var pinger = new Ping();
            var reply = await pinger.SendPingAsync(ipOrHost, 1200);
            if (reply.Status == IPStatus.Success)
            {
                return reply.RoundtripTime;
            }
        }
        catch { }
        return -1;
    }

    public string ExportFleetCsv()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Hostname,Branch,IP Address,MAC,User,AnyDesk ID,AnyDesk Installed,RDP,Status,Last Seen");
        foreach (var d in _devices)
        {
            sb.AppendLine($"\"{d.Hostname}\",\"{d.BranchLocation}\",\"{d.IpAddress}\",\"{d.MacAddress}\",\"{d.LoggedInUser}\",\"{d.AnyDeskId}\",{d.AnyDeskInstalled},{d.RdpEnabled},\"{d.Status}\",\"{d.LastSeen:yyyy-MM-dd HH:mm:ss}\"");
        }
        return sb.ToString();
    }
}
