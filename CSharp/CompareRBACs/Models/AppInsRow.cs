namespace CompareRBACs.Models;

public class AppInsRow
{
    public string AppInsName { get; set; }
    public string ResourceGroup { get; set; }
    public Dictionary<string, RBAC>? RBAC { get; set; }
}

public class RBAC
{
    public string RoleAssignmentName { get; set; }
    public string RoleAssignmentId { get; set; }
    public string Scope { get; set; }
    public string DisplayName { get; set; }
    public string SignInName { get; set; }
    public string RoleDefinitionName { get; set; }
    public string RoleDefinitionId { get; set; }
    public string ObjectId { get; set; }
    public string ObjectType { get; set; }
    public bool CanDelegate { get; set; }
    public string? Description { get; set; }
    public string? ConditionVersion { get; set; }
    public string? Condition { get; set; }
}