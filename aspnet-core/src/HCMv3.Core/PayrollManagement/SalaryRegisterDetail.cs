using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Wage-type calculation line of a <see cref="SalaryRegister"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.SalaryRegisterDetail</c>), linked back via
/// <see cref="SalaryRegisterId"/>. Its child is <see cref="SalaryRegisterDaysDetail"/>, linked back via
/// <see cref="SalaryRegisterDaysDetail.SalaryRegisterDetailId"/>.
///
/// LOCK CASCADE PARITY (enforced in <c>SalaryRegisterDetailsAppService</c>, not here): create/update/delete
/// are blocked when the parent <see cref="SalaryRegister"/> is <see cref="SalaryRegister.Locked"/>.
/// </summary>
public class SalaryRegisterDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int SalaryRegisterId { get; set; }

    public virtual SalaryRegister SalaryRegister { get; set; }

    public virtual int WageTypeId { get; set; }

    public virtual WageType WageType { get; set; }

    public virtual double Rate { get; set; }

    public virtual double Number { get; set; }

    public virtual double Amount { get; set; }
}
