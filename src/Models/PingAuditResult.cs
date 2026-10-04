using System;

namespace SVLL_IT_Workstation;

public class PingAuditResult
{
    public string Target { get; set; } = "";
    public string Status { get; set; } = "Unknown";
    public int Sent { get; set; } = 4;
    public int Received { get; set; }
    public double LossPct => Sent <= 0 ? 0.0 : Math.Round((double)(Sent - Received) / Sent * 100.0, 1);
    public long MinLatency { get; set; }
    public long AvgLatency { get; set; }
    public long MaxLatency { get; set; }
}
