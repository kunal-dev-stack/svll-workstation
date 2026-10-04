namespace SVLL_IT_Workstation;

public class NetworkProfile
{
    public string ProfileName { get; set; } = "Default Profile";
    public string BranchTag { get; set; } = "HQ";
    public bool IsDhcp { get; set; } = true;
    public string IpAddress { get; set; } = "";
    public string SubnetMask { get; set; } = "255.255.255.0";
    public string Gateway { get; set; } = "";
    public string PrimaryDns { get; set; } = "8.8.8.8";
    public string SecondaryDns { get; set; } = "8.8.4.4";
    public string Notes { get; set; } = "";

    public override string ToString()
    {
        return $"[{BranchTag}] {ProfileName}";
    }
}
