namespace SVLL_IT_Workstation;

public class ThermalPrinterItem
{
    public string Name { get; set; } = "";
    public string PortName { get; set; } = "USB001";
    public string DriverName { get; set; } = "";
    public string Status { get; set; } = "Ready";
    public int JobsCount { get; set; } = 0;
    public bool IsDefault { get; set; } = false;
}
