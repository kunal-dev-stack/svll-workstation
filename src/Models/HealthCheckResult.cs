namespace SVLL_IT_Workstation;

public class HealthCheckResult
{
    public string Component { get; set; } = "";
    public string Status { get; set; } = "OK";
    public string Details { get; set; } = "";
    public string Recommendation { get; set; } = "";
    public bool IsWarning { get; set; }
}
