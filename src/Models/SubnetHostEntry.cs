namespace SVLL_IT_Workstation;

public class SubnetHostEntry
{
    public string IpAddress { get; set; } = "";
    public string Hostname { get; set; } = "-";
    public string Status { get; set; } = "Free"; // "Occupied" or "Free"
    public long LatencyMs { get; set; } = -1;
    public bool IsOccupied => Status == "Occupied";
    public string DisplayStatus => IsOccupied ? $"Occupied ({LatencyMs}ms)" : "Free (Unassigned)";
}
