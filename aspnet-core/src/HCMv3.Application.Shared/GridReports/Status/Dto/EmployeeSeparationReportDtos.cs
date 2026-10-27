using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Employees.Dto;
using HCMv3.PersonnelAdministration;

namespace HCMv3.GridReports.Status.Dto;

/// <summary>
/// Filter input for the Employee Separation analysis grid report (legacy
/// <c>uscRptEmployeeSeparation.ascx</c>), filtered on <c>EmployeeSeparation.LastDayInService</c>.
///
/// DEVIATION: legacy's <c>cmbEmpStatus</c> box is labelled "Employee Status" but is bound to
/// <see cref="PersonnelAdministration.SeparationType"/> (line 67/600-601), not <c>Employee.Status</c>; the
/// correctly-typed <see cref="SeparationType"/> filter is added here instead of the shared base's
/// (wrong-type) <c>EmployeeStatus</c>. Legacy also reads an ACTIVE/INACTIVE toggle from
/// <c>Session[SeparationStatusKey]</c> (lines 609-621) with no corresponding markup control in the
/// reviewed <c>.ascx</c> (session-only, never set by this screen in practice) -- ported here as an
/// explicit <see cref="IsRevoked"/> filter so the capability is not silently dropped.
/// </summary>
public class GetEmployeeSeparationReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>cmbEmpStatus</c> (mislabelled -- see class remarks): actually a separation-type filter.</summary>
    public SeparationType? SeparationType { get; set; }

    /// <summary>
    /// Legacy <c>Session[SeparationStatusKey]</c> "Active"/"Inactive" toggle: <c>true</c> = revoked
    /// (<c>RevokeDate != null</c>, legacy "Inactive"), <c>false</c> = active (<c>RevokeDate == null</c>,
    /// legacy "Active"), <c>null</c> = both (legacy default, no filter applied).
    /// </summary>
    public bool? IsRevoked { get; set; }

    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row. Mirrors the legacy <c>DataTable</c> built in
/// <c>uscRptEmployeeSeparation.ascx.cs:485-789</c> (<c>Table1()</c>): OID, Code, Name, JobTitle, CivilId,
/// Salary, JoiningDate, SeparationDate, NoticePeriodDays, ActualLastWorkingDay, NoticePeriodEnd,
/// PlannedLastDayInService, OrgLevel1..7, WorkLocation, SeparationType, SeparationReason, ResidencyType,
/// ResidencyExpiry, PHBalance, PayStatus, AbscondingStatus, Country, RevokeDate, RevokeComments,
/// CostCenter, CostCenterGroup.
///
/// LEGACY DEFECT FLAGGED FOR REVIEW / DROPPED COLUMNS: legacy's "AbscondingStatus" column is actually
/// <c>EmployeeSeparation.Comments</c> (line 718/775 -- not an absconding-specific field, legacy's own
/// naming), and "RevokeComments" is <c>EmployeeSeparation.RevokeComments</c>. HCMv3's
/// <c>EmployeeSeparation</c> (ported in an earlier wave, deliberately minimal "record-keeping only" per
/// its own class remarks) does not carry either field. Both columns are OMITTED here -- an earlier-wave
/// entity gap, not something introduced or silently patched by this report -- rather than fabricated;
/// every other legacy column is present.
/// </summary>
public class EmployeeSeparationReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public string JobTitleName { get; set; }

    public long CivilId { get; set; }

    /// <summary>Legacy "Salary" column (line 645-651/701/740): the MOST RECENT basic-pay amount by StartDate, no date-window.</summary>
    public double Salary { get; set; }

    public DateTime JoiningDate { get; set; }

    /// <summary>Legacy "SeparationDate" column -- actually <c>EmployeeSeparation.SubmissionDate</c>.</summary>
    public DateTime SeparationDate { get; set; }

    public int NoticePeriodDays { get; set; }

    /// <summary>Legacy "ActualLastWorkingDay" column -- actually <c>EmployeeSeparation.LastDayInService</c> (legacy's own column-name/field mismatch).</summary>
    public DateTime LastDayInService { get; set; }

    /// <summary>
    /// Legacy "NoticePeriodEnd" column (line 697/737): <c>SubmissionDate.AddDays(89)</c> -- a fixed
    /// 89-day offset, unrelated to <see cref="NoticePeriodDays"/>. Reproduced verbatim as a pinned
    /// legacy quirk (see the matching test), not "corrected" to use the actual notice period.
    /// </summary>
    public DateTime NoticePeriodEndQuirk { get; set; }

    public DateTime PlannedLastDayInService { get; set; }

    public string OrgLevel1 { get; set; }

    public string OrgLevel2 { get; set; }

    public string OrgLevel3 { get; set; }

    public string OrgLevel4 { get; set; }

    public string OrgLevel5 { get; set; }

    public string OrgLevel6 { get; set; }

    public string OrgLevel7 { get; set; }

    public long? OrganizationUnitId { get; set; }

    public string WorkLocationName { get; set; }

    public SeparationType SeparationType { get; set; }

    public string SeparationReasonName { get; set; }

    /// <summary>Legacy lines 707-709: the sponsor record whose window covers the SubmissionDate.</summary>
    public ResidencyType? ResidencyType { get; set; }

    /// <summary>Legacy line 711: the employee's most recent CID/Civil Id document expiry (no date-window).</summary>
    public DateTime? ResidencyExpiry { get; set; }

    public double PHBalance { get; set; }

    public double PayStatus { get; set; }

    public string CountryName { get; set; }

    public DateTime? RevokeDate { get; set; }

    public string CostCenterName { get; set; }

    public string CostCenterGroupName { get; set; }
}
