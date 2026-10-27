using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.TimeManagement;

/// <summary>
/// Date-effective assignment of an Employee to a <see cref="TimePolicy"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TimeManagement.EmployeeTimePolicy</c>, Wave 3).
///
/// The EndDate auto-chaining (legacy <c>SetEndDate</c>) and no-duplicate-StartDate-per-employee
/// validation are ported in <c>EmployeeTimePoliciesAppService</c>, following the same pattern already
/// established by <c>EmployeeOrgAssignmentsAppService</c> (PersonnelAdministration Wave B) and
/// <see cref="EmployeeDutyPeriod"/> above.
///
/// DEFERRED (not ported): the delete-guard that blocked deletion when a <c>SalaryRegister</c>/
/// <c>TimeRegister</c> exists in the record's date window (<c>SalaryRegister</c> doesn't exist in this
/// port; the <c>TimeRegister</c> cross-check is deferred as not worth the coupling this wave). The legacy
/// <c>IExcelImport</c> marker is an XAF UI concept and is not ported.
/// </summary>
public class EmployeeTimePolicy : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int TimePolicyId { get; set; }

    public virtual TimePolicy TimePolicy { get; set; }

    /// <summary>
    /// "Effective Date" in the legacy UI.
    /// </summary>
    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }
}
