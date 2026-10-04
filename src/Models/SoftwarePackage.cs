namespace SVLL_IT_Workstation;

public class SoftwarePackage
{
    public string Name { get; set; } = "";
    public string WingetId { get; set; } = "";
    public string Category { get; set; } = "Utility";
    public bool IsSelected { get; set; }
}
