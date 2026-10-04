namespace SVLL_IT_Workstation;

public class WmsEndpointItem
{
    public string ServiceName { get; set; } = "WMS Database";
    public string Host { get; set; } = "192.168.1.10";
    public int Port { get; set; } = 1433;
    public long LatencyMs { get; set; } = -1;
    public string Status { get; set; } = "STANDBY";
    public string Protocol { get; set; } = "TCP";

    public string StatusFormatted => LatencyMs >= 0 ? $"Connected ({LatencyMs}ms)" : Status;
}
