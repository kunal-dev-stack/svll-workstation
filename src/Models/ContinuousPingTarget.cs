namespace SVLL_IT_Workstation;

public class ContinuousPingTarget
{
    public string Host { get; set; } = "";
    public string Description { get; set; } = "";
    public long CurrentLatency { get; set; } = -1L;
    public string Status { get; set; } = "STANDBY";
    public int ConsecutiveDrops { get; set; }
    public string LastAuditTime { get; set; } = "-";
}
