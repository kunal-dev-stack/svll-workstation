using System;
using System.Collections.Generic;

namespace SVLL_IT_Workstation;

public class WorkstationConfig
{
    public string PresetName { get; set; } = "SVLL Standard Baseline";
    public string Description { get; set; } = "Standard corporate rollout configuration.";
    public string CreatedDate { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");
    public bool CreateRestorePoint { get; set; } = true;
    public bool DisableTelemetry { get; set; } = true;
    public bool RemoveUwpBloat { get; set; } = true;
    public bool EnableExplorerExtensions { get; set; } = true;
    public bool EnableEndTaskOnTaskbar { get; set; } = true;
    public bool DisableBingSearch { get; set; } = true;
    public bool DisableHibernation { get; set; } = true;
    public bool CleanTempsAndCaches { get; set; } = true;
    public bool OptimizePowerPlan { get; set; } = true;
    public bool FlushDnsAndNetworkCaches { get; set; } = true;
    public bool OptimizeBackgroundServices { get; set; } = true;
    public List<string> SelectedSoftwareIds { get; set; } = new List<string>();
    public List<string> SelectedFeatures { get; set; } = new List<string>();
    public string WindowsUpdateProfile { get; set; } = "Recommended";
    public string DnsPreset { get; set; } = "Google (8.8.8.8)";
}
