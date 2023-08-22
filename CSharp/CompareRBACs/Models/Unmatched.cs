namespace CompareRBACs.Models;

public class Unmatched
{
    public string RoleAssignmentName { get; set; }
    public string RoleDefinitionName { get; set; }
    public string DisplayName { get; set; }
    public string SignInName { get; set; }
    public string Name { get; set; }
    public string OffendedAppInsName { get; set; }

    public string Offence =>
        $"No match for {RoleAssignmentName} ({RoleDefinitionName} for {DisplayName} ({SignInName})) from {Name} in {OffendedAppInsName}";
}