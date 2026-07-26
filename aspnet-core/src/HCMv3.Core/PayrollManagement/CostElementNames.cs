namespace HCMv3.PayrollManagement;

/// <summary>
/// Names of the 7 fixed "cost element slot" properties shared by <see cref="WageAccGroupDetail"/>,
/// <see cref="CostRegisterDetail"/> and <see cref="EmployeeWageAccount"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Services.CostService</c>). Pure constants, no DB/session dependency.
/// </summary>
public static class CostElementNames
{
    public const string CostElement1 = "CostElement1";
    public const string CostElement2 = "CostElement2";
    public const string CostElement3 = "CostElement3";
    public const string CostElement4 = "CostElement4";
    public const string CostElement5 = "CostElement5";
    public const string CostElement6 = "CostElement6";
    public const string CostElement7 = "CostElement7";
}
