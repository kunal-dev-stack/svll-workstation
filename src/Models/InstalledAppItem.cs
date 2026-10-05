using System;

namespace SVLL_IT_Workstation;

public class InstalledAppItem
{
    public bool IsSelected { get; set; }
    public string DisplayName { get; set; } = "";
    public string DisplayVersion { get; set; } = "";
    public string Publisher { get; set; } = "";
    public string InstallDate { get; set; } = "";
    public string InstallLocation { get; set; } = "";
    public string UninstallString { get; set; } = "";
    public string QuietUninstallString { get; set; } = "";
    public bool IsMsi { get; set; }
    public string RegistryKeyName { get; set; } = "";
    public string RegistryKeyPath { get; set; } = "";
    public string RegistryHive { get; set; } = "HKLM (64-bit)";
    public string ArchitectureBadge { get; set; } = "[64-Bit]";
    public string ArchitectureColor { get; set; } = "#2563EB";
    public string EstimatedSizeFormatted { get; set; } = "";
    public string Status { get; set; } = "Installed";
    public string StatusColor { get; set; } = "#64748B";
}
