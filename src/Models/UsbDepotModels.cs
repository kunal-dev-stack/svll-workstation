using System;

namespace SVLL_IT_Workstation;

public enum UsbItemCategory
{
    Software,
    Driver,
    Script,
    Archive
}

public class UsbDepotItem
{
    public bool IsSelected { get; set; } = true;
    public string Name { get; set; } = "";
    public string RelativePath { get; set; } = "";
    public string FullPath { get; set; } = "";
    public string Extension { get; set; } = "";
    public long FileSizeBytes { get; set; }
    public string FileSizeFormatted { get; set; } = "";
    public UsbItemCategory Category { get; set; } = UsbItemCategory.Software;
    public string CategoryBadge { get; set; } = "[EXE]";
    public string CategoryColor { get; set; } = "#2563EB";
    public string SilentCommand { get; set; } = "";
    public string Status { get; set; } = "Ready";
    public string StatusColor { get; set; } = "#64748B"; // Slate
}

public class MissingHardwareDeviceItem
{
    public string DeviceName { get; set; } = "";
    public string DeviceId { get; set; } = "";
    public string HardwareId { get; set; } = "";
    public string ClassName { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public int StatusCode { get; set; }
    public string ProblemDescription { get; set; } = "";
    public string Status { get; set; } = "Warning";
}

public class WhqlDriverPackageItem
{
    public bool IsSelected { get; set; } = true;
    public string Title { get; set; } = "";
    public string DriverModel { get; set; } = "";
    public string DriverClass { get; set; } = "";
    public string DriverProvider { get; set; } = "";
    public string DriverDate { get; set; } = "";
    public string DriverVersion { get; set; } = "";
    public string Status { get; set; } = "Available (WHQL Certified)";
    public string StatusColor { get; set; } = "#16A34A";
}
