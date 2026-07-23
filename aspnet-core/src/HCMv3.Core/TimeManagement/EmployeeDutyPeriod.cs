using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.TimeManagement;

/// <summary>
/// Date-effective assignment of an Employee to a <see cref="DutyPeriod"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TimeManagement.EmployeeDutyPeriod</c>, Wave 3).
///
/// The EndDate auto-chaining (legacy <c>SetEndDate</c>) and no-duplicate-StartDate-per-employee
/// validation are ported in <c>EmployeeDutyPeriodsAppService</c>, following the same pattern already
/// established by <c>EmployeeOrgAssignmentsAppService</c> (PersonnelAdministration Wave B).
///
/// DEFERRED (not ported): the legacy <c>StartPoint</c> field and the commented-out <c>Weekoff</c>
/// calculated property it fed (dead code in the legacy source); the delete-guard that blocked deletion
/// when a <c>SalaryRegister</c>/<c>TimeRegister</c> exists in the record's date window
/// (<c>SalaryRegister</c> doesn't exist in this port; the <c>TimeRegister</c> cross-check is deferred as
/// not worth the coupling this wave); the static <c>GetDuty(session, dayDate, empOid, throwException)</c>
/// lookup helper (a calculation-engine integration point depending on the not-yet-ported
/// <c>EnableRosterDuty</c> global settings + <c>RosterService</c> + <see cref="EmployeeSubstituteDuty"/>
/// resolution -- not appropriate to port as a side-effect-free static helper without the engine it feeds).
/// The legacy <c>IExcelImport</c> marker is an XAF UI concept and is not ported.
/// </summary>
public class EmployeeDutyPeriod : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int DutyPeriodId { get; set; }

    public virtual DutyPeriod DutyPeriod { get; set; }

    /// <summary>
    /// "Effective Date" in the legacy UI.
    /// </summary>
    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }
}
