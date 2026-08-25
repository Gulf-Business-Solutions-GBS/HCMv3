using System;
using System.Collections.Generic;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.Recruitment;

namespace HCMv3.GridReports.Recruitment.Dto;

/// <summary>
/// Shared filter input for the Vacancy Status and Candidate Onboarding Init analysis grid reports (legacy
/// <c>uscRptVacancyStatus.ascx</c> and <c>uscRptCandidateOnbInit.ascx</c> -- see
/// <see cref="VacancyStatusReportRowDto"/> remarks for why the two legacy screens are near-duplicates).
///
/// Legacy has no employee-code/status/date-range controls on either screen, so the three inherited filters
/// are unused, same convention as <c>GetEmployeeSalaryChangeReportInput</c>.
/// </summary>
public class GetVacancyStatusReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>cmbVacancy</c> (session key "JobTitle", despite filtering by Vacancy.Oid).</summary>
    public int? VacancyId { get; set; }

    /// <summary>Legacy <c>cmbVacancyStatus</c>.</summary>
    public AppStatus? VacancyStatus { get; set; }

    protected override string DefaultSorting => "VacancyId ASC";
}

/// <summary>
/// One grid row: one vacancy plus its per-<see cref="AppStatus"/> application count.
///
/// Ports legacy <c>Table1()</c>, which is BYTE-FOR-BYTE IDENTICAL between <c>uscRptVacancyStatus.ascx.cs</c>
/// (lines 571-682) and <c>uscRptCandidateOnbInit.ascx.cs</c> (lines 570-681) -- same session keys, same
/// query, same columns. The two legacy screens differ only in filter-panel labels/UI plumbing, so both
/// reports in this port share this DTO and the query logic in <c>VacancyStatusReportQueryHelper</c>, with
/// one app service class each (matching the task's 1:1 legacy-screen-to-app-service mapping).
/// </summary>
public class VacancyStatusReportRowDto : EntityDto<int>
{
    public string Code { get; set; }

    public int VacancyId { get; set; }

    public string VacancyName { get; set; }

    /// <summary>
    /// Legacy <c>GetCurrentCompany</c>: walks the OrganizationUnit ancestor chain to the unit classified as
    /// "Company". MODEL ADAPTATION: HCMv3's <c>OrganizationUnitType</c> has no "Company" level constant (it
    /// only has the generic Level1..Level7 codes used elsewhere in this port), so Company here is the
    /// ancestor-or-self classified <see cref="OrganizationManagement.OrganizationUnitType.Level1Code"/> --
    /// the top of the hierarchy, which is what legacy's Company level meant in practice.
    /// </summary>
    public string Company { get; set; }

    /// <summary>Legacy "Department": the vacancy's own OrganizationUnit's OrgUnitType name (NOT an ancestor walk).</summary>
    public string Department { get; set; }

    public DateTime PublishOn { get; set; }

    public DateTime ClosedOn { get; set; }

    /// <summary>
    /// Legacy's one dynamic-looking column per <see cref="AppStatus"/> value (lines 615-618/680-682) --
    /// FIXED at compile time because <see cref="AppStatus"/> is a fixed enum, unlike the true data-driven
    /// pivots elsewhere in this wave (e.g. wage-type columns).
    /// </summary>
    public List<VacancyStatusCountDto> StatusCounts { get; set; } = new();
}

/// <summary>One (AppStatus, count-of-applications) cell of <see cref="VacancyStatusReportRowDto.StatusCounts"/>.</summary>
public class VacancyStatusCountDto
{
    public AppStatus Status { get; set; }

    public int Count { get; set; }
}
