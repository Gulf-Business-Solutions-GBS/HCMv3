namespace HCMv3.ProjectManagement;

/// <summary>
/// Costing method for an <c>EmployeeProject</c> assignment (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.ProjectManagement.ProjectCostingMethod</c>).
/// </summary>
public enum ProjectCostingMethod
{
    ActualHours = 0,
    ActualDays = 1,
    Percentage = 2
}
