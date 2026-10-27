using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PayrollManagement;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// LoanTypeDetail (ported from legacy XAF <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.LoanTypeDetail</c>).
/// Pure detail record: no Code/NameEn/NameAr. Owned by a <see cref="LoanType"/>.
/// A <see cref="LoanDeductionType.Variable"/> loan type recovers <see cref="Percent"/> of the employee's
/// <see cref="WageTypeId"/> register line each month, summed over its detail rows (legacy
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 1628-1639).
/// </summary>
public class LoanTypeDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int? LoanTypeId { get; set; }

    public virtual LoanType LoanType { get; set; }

    /// <summary>
    /// Legacy <c>LoanTypeDetail.WageType</c>: the register line the percentage is taken of. Nullable only because
    /// rows saved before it was ported have none; the app service requires it, as legacy's validation did.
    /// </summary>
    public virtual int? WageTypeId { get; set; }

    public virtual WageType WageType { get; set; }

    public virtual double Percent { get; set; }
}
