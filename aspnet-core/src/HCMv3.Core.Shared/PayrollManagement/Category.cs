namespace HCMv3.PayrollManagement;

/// <summary>
/// Wage type category (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.Category</c>). <see cref="Technical"/> wage types are
/// reserved for internal/system use — <c>WageTypesAppService</c> blocks deleting a <c>WageType</c> whose
/// Category is Technical (legacy <c>OnDeleting</c> parity).
/// </summary>
public enum Category
{
    Custom = 0,
    Technical = 1
}
