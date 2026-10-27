using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Pay-band deduction step for an <see cref="EmployeeSubGroup"/> fund (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.EmployeeSubGroupFund</c>), listed separately and linked
/// back via <see cref="EmployeeSubGroupId"/>.
/// </summary>
public class EmployeeSubGroupFund : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeSubGroupId { get; set; }

    public virtual EmployeeSubGroup EmployeeSubGroup { get; set; }

    public virtual double FromPay { get; set; }

    public virtual double ToPay { get; set; }

    public virtual FundDedType FundDedType { get; set; }

    public virtual double Percentage { get; set; }

    public virtual double MinAmount { get; set; }

    public virtual double MaxAmount { get; set; }

    public virtual double FixAmount { get; set; }
}
