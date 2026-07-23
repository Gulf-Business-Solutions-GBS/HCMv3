using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.TimeManagement;

/// <summary>
/// Per-date Duty override for an Employee (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TimeManagement.EmployeeSubstituteDuty</c>, Wave 3).
///
/// DELIBERATE, SIGNIFICANT SIMPLIFICATION: the legacy <c>OnSaving</c>/<c>OnDeleting</c> contain a large
/// <c>RecalculateTotalLeaveDays()</c>/<c>CheckQuotaBalance()</c> cascade into <c>EmployeeAbsence</c>/
/// <c>EmployeeAbsenceQuota</c>/<c>EmployeeSubGroupAbsence</c>/<c>TimeTransfer</c> sick-leave-opening-balance
/// logic. None of those leave-quota entities exist in this port yet (<c>EmployeeAbsence</c>/
/// <c>EmployeeAbsenceQuota</c> were never ported -- deferred as TimeManagement-adjacent when
/// PersonnelAdministration was scoped). Only the entity shape and a basic duplicate
/// (Employee, SubstituteDate) validation are ported here (in
/// <c>EmployeeSubstituteDutiesAppService</c>) -- none of the quota-recalculation cascade is ported. This
/// is the same category of deferral as the <c>HolidayPeriod</c> cascade from Wave 1.
///
/// The legacy <c>IExcelImport</c> marker is an XAF UI concept and is not ported.
/// </summary>
public class EmployeeSubstituteDuty : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int DutyId { get; set; }

    public virtual Duty Duty { get; set; }

    public virtual DateTime SubstituteDate { get; set; }
}
