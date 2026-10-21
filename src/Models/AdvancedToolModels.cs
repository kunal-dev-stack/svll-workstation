using System;

namespace SVLL_IT_Workstation.Models;

public class ProvisioningStepItem
{
    public int StepNumber { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsChecked { get; set; } = true;
    public string Status { get; set; } = "Ready"; // Ready, Running, Success, Failed, Skipped
}

public class DiskSmartInfo
{
    public string DeviceId { get; set; } = "";
    public string Model { get; set; } = "";
    public string SerialNumber { get; set; } = "";
    public string InterfaceType { get; set; } = ""; // NVMe, SATA, USB
    public string SizeGb { get; set; } = "";
    public string HealthStatus { get; set; } = "Healthy (OK)";
    public int WearPercentage { get; set; } = 1; // 1% used -> 99% remaining
    public string PowerOnHours { get; set; } = "0 hrs";
    public string EstimatedLifeRemaining { get; set; } = "99%";
    public string Temperature { get; set; } = "34°C";
    public int ReallocatedSectors { get; set; } = 0;
    public string TerabytesWritten { get; set; } = "3.2 TBW";
    public string AlertLevel { get; set; } = "Nominal"; // Nominal, Warning, Critical
}

public class TcpConnectionItem
{
    public string LocalAddress { get; set; } = "";
    public int LocalPort { get; set; }
    public string RemoteAddress { get; set; } = "";
    public int RemotePort { get; set; }
    public string State { get; set; } = "";
    public int ProcessId { get; set; }
    public string ProcessName { get; set; } = "";
    public string BandwidthHint { get; set; } = "Normal";
}

public class PaletteCommandItem
{
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public string Category { get; set; } = "";
    public string Icon { get; set; } = "⚡";
    public string TargetTag { get; set; } = "";
    public Action? CustomAction { get; set; }
}
