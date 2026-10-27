using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee contract record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeContract</c>, Wave B). The legacy
/// EndDate auto-chaining (<c>SetEndDate</c>) is entirely commented-out dead code in the legacy source and
/// is NOT ported. Only the active validations (required fields, no-duplicate-StartDate, no-overlapping
/// date range for the same employee) are ported -- see <c>EmployeeContractsAppService</c>.
/// </summary>
public class EmployeeContract : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int ContractTypeId { get; set; }

    public virtual ContractType ContractType { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    /// <summary>
    /// Optional link to the <see cref="EmployeeAction"/> that produced this record, so
    /// <c>EmployeeActionsAppService</c> can cascade-delete it when the action is deleted.
    /// </summary>
    public virtual int? EmployeeActionId { get; set; }

    public virtual EmployeeAction EmployeeAction { get; set; }
}
