using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee basic pay detail (wage type) row (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeBasicPayDetail</c>, Wave 2 deferred
/// bucket).
///
/// DELIBERATE SIMPLIFICATIONS / DROPPED FIELDS:
/// <list type="bullet">
/// <item><description>The legacy <c>EmpWageType</c> filtered picklist (active Earning wage types flagged
/// IncludeInBasicPay, further filtered by the employee's EmployeeSubGroup wage-type whitelist) is an
/// Angular-side/lookup-endpoint concern, not ported as an entity member.</description></item>
/// <item><description>The legacy cascade that recomputes the parent <c>EmployeeBasicPay.Amount</c> total on
/// save/delete of a detail row lives in <c>EmployeeBasicPayDetailsAppService</c>, not on the entity.</description></item>
/// </list>
/// </summary>
public class EmployeeBasicPayDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeBasicPayId { get; set; }

    public virtual EmployeeBasicPay EmployeeBasicPay { get; set; }

    public virtual int WageTypeId { get; set; }

    public virtual WageType WageType { get; set; }

    public virtual double Amount { get; set; }

    public virtual int CurrencyId { get; set; }

    public virtual Currency Currency { get; set; }

    public virtual double DependantNumber { get; set; }

    public virtual DependantUnit DependantUnit { get; set; }

    public virtual int? DependantWageTypeId { get; set; }

    public virtual WageType DependantWageType { get; set; }
}
