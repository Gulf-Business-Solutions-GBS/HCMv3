using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// LoanTypeDetail (ported from legacy XAF <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.LoanTypeDetail</c>).
/// Pure detail record: no Code/NameEn/NameAr. Owned by a <see cref="LoanType"/>.
/// The legacy <c>WageType</c> FK is not ported (Payroll module out of scope).
/// </summary>
public class LoanTypeDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int? LoanTypeId { get; set; }

    public virtual LoanType LoanType { get; set; }

    public virtual double Percent { get; set; }
}
