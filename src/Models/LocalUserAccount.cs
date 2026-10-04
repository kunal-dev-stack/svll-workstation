namespace SVLL_IT_Workstation;

public class LocalUserAccount
{
    public string Username { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public bool IsAdmin { get; set; } = false;
    public bool PasswordRequired { get; set; } = true;
    public string StatusText => IsActive ? "Active" : "Disabled";
    public string RoleText => IsAdmin ? "Administrator" : "Standard User";
}
