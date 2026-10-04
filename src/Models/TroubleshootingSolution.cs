using System;

namespace SVLL_IT_Workstation;

public class TroubleshootingSolution
{
    public string Title { get; set; } = "";
    public string Category { get; set; } = "General";
    public string Symptoms { get; set; } = "";
    public string Explanation { get; set; } = "";
    public string CmdCommand { get; set; } = "";
    public string PowerShellCommand { get; set; } = "";
    public string ManualSteps { get; set; } = "";
    public string[] Tags { get; set; } = Array.Empty<string>();

    public override string ToString()
    {
        return $"[{Category}] {Title}";
    }
}
