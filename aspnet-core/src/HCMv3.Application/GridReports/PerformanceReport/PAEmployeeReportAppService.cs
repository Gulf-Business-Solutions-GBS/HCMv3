using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.Dto;
using HCMv3.GridReports.Employees;
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.PerformanceReport.Dto;
using HCMv3.PerformanceManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.PerformanceReport;

/// <summary>
/// PA Employee analysis grid report -- port of legacy
/// <c>PerformanceReport/uscRptPAEmployee.ascx(.cs)</c>, method <c>Table1()</c> (lines 525-776).
///
/// NOT PORTED: the drill-up/drill-down session stack (only "Table1" ever runs), the ColumnChooser JSON
/// persistence, the callback-panel state machine, the PDF export button, and the dead <c>Table1_Old()</c>
/// (lines 777-1000, an earlier formulation reading Employee.OrgLevelN/CurrentManager directly instead of
/// the org-assignment/EmployeeManager snapshot tables -- superseded by <c>Table1()</c>).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_PAEmployee)]
public class PAEmployeeReportAppService : GridReportAppServiceBase, IPAEmployeeReportAppService
{
    private readonly IRepository<PAEmployee> _paEmployeeRepository;
    private readonly IRepository<PAEmployeeReviewer> _paEmployeeReviewerRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly IRepository<EmployeeManager> _employeeManagerRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public PAEmployeeReportAppService(
        IRepository<PAEmployee> paEmployeeRepository,
        IRepository<PAEmployeeReviewer> paEmployeeReviewerRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        IRepository<EmployeeManager> employeeManagerRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _paEmployeeRepository = paEmployeeRepository;
        _paEmployeeReviewerRepository = paEmployeeReviewerRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _employeeManagerRepository = employeeManagerRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptPAEmployee";

    protected override string ScreenCaption => "PA Employee";

    [HttpPost]
    public async Task<PagedResultDto<PAEmployeeReportRowDto>> GetPAEmployeeReport(GetPAEmployeeReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<PAEmployeeReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetPAEmployeeReportToExcel(GetPAEmployeeReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: legacy's ASPxGridViewExporter exported the entire filtered set.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync(
            "PAEmployee.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c> (uscRptPAEmployee.ascx.cs:635-716): PAEmployee LEFT-JOINed to
    /// PAEmployeeReviewer (fan-out -- see the row DTO remarks), org-assignment/manager snapshots resolved
    /// "as of today" exactly like legacy's <c>DateTime.Now.Date</c> windows.
    /// </summary>
    private async Task<IQueryable<PAEmployeeReportRowDto>> BuildQueryAsync(GetPAEmployeeReportInput input)
    {
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var today = Today;
        var formTypeCode = input.FormTypeCode;
        var periodCode = input.PeriodCode;

        var paQuery = _paEmployeeRepository.GetAll()
            // Legacy line 636: r.Employee != null -- always true here (EmployeeId is required).
            // Legacy lines 639-641: FormType.Code == cmbFormType, only when a form type was picked.
            .WhereIf(!string.IsNullOrEmpty(formTypeCode), pa => pa.FormType.Code == formTypeCode)
            // Legacy lines 643-645: Period.Code == cmbPeriod (legacy's "Period" is this port's PACycle).
            .WhereIf(!string.IsNullOrEmpty(periodCode), pa => pa.PACycle.Code == periodCode)
            // Legacy lines 647-652: employees.Contains(Employee.Code) over the comma-split text box.
            .WhereIf(employeeCodes.Count > 0, pa => employeeCodes.Contains(pa.Employee.Code))
            // Legacy line 654 ApplyRowLevelSecurity: an empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, pa => rlsEmployeeIds.Contains(pa.EmployeeId));

        var orgAss = _employeeOrgAssignmentRepository.GetAll()
            .Where(o => o.StartDate.Date <= today && o.EndDate.Date >= today);

        var managers = _employeeManagerRepository.GetAll()
            .Where(m => m.StartDate.Date <= today && m.EndDate.Date >= today);

        // Legacy lines 685-689: LEFT JOIN PAEmployeeReviewer onto PAEmployee -- one output row per
        // reviewer, and one row (with blank reviewer columns) when there are none.
        var query =
            from pa in paQuery
            join reviewer in _paEmployeeReviewerRepository.GetAll()
                on pa.Id equals reviewer.PAEmployeeId into reviewerGroup
            from reviewer in reviewerGroup.DefaultIfEmpty()
            select new PAEmployeeReportRowDto
            {
                Id = pa.Id,
                EmployeeCode = pa.Employee.Code,
                EmployeeName = useArabicNames ? pa.Employee.FullNameAr : pa.Employee.FullNameEn,

                JobTitle = orgAss
                    .Where(o => o.EmployeeId == pa.EmployeeId && o.JobTitleId != null)
                    .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                    .Select(o => useArabicNames ? o.JobTitle.NameAr : o.JobTitle.NameEn)
                    .FirstOrDefault(),
                OrganizationUnitId = orgAss
                    .Where(o => o.EmployeeId == pa.EmployeeId && o.OrganizationUnitId != null)
                    .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                    .Select(o => o.OrganizationUnitId)
                    .FirstOrDefault(),

                Period = pa.PACycle == null ? "" : (useArabicNames ? pa.PACycle.NameAr : pa.PACycle.NameEn),
                FormType = pa.FormType == null ? "" : (useArabicNames ? pa.FormType.NameAr : pa.FormType.NameEn),

                Manager = managers
                    .Where(m => m.EmployeeId == pa.EmployeeId)
                    .OrderBy(m => m.StartDate).ThenBy(m => m.Id)
                    .Select(m => useArabicNames
                        ? m.Manager.Code + " " + m.Manager.FullNameAr
                        : m.Manager.Code + " " + m.Manager.FullNameEn)
                    .FirstOrDefault(),
                ManagerJobTitle = managers
                    .Where(m => m.EmployeeId == pa.EmployeeId)
                    .OrderBy(m => m.StartDate).ThenBy(m => m.Id)
                    .SelectMany(m => orgAss
                        .Where(o => o.EmployeeId == m.ManagerId && o.JobTitleId != null)
                        .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                        .Select(o => useArabicNames ? o.JobTitle.NameAr : o.JobTitle.NameEn))
                    .FirstOrDefault(),

                OverallAverageRating = pa.OverallAverageRating == null
                    ? ""
                    : (useArabicNames ? pa.OverallAverageRating.NameAr : pa.OverallAverageRating.NameEn),
                OverallAverageScore = pa.OverallAverageScore,
                PeerAverageRating = pa.PeerAverageRating == null
                    ? ""
                    : (useArabicNames ? pa.PeerAverageRating.NameAr : pa.PeerAverageRating.NameEn),
                PeerAverageScore = pa.PeerAverageScore,
                EmployeeAverageRating = pa.EmployeeAverageRating == null
                    ? ""
                    : (useArabicNames ? pa.EmployeeAverageRating.NameAr : pa.EmployeeAverageRating.NameEn),
                EmployeeAverageScore = pa.EmployeeAverageScore,

                ProposedIncrement = pa.ProposedIncrement,
                ProposedJobTitle = pa.ProposedJobTitle,
                AreaOfImprovement = pa.AreaOfImprovement,

                Person = reviewer == null
                    ? ""
                    : (useArabicNames
                        ? reviewer.Employee.Code + " " + reviewer.Employee.FullNameAr
                        : reviewer.Employee.Code + " " + reviewer.Employee.FullNameEn),
                ReviewerAverageRating = reviewer == null || reviewer.AverageRating == null
                    ? ""
                    : (useArabicNames ? reviewer.AverageRating.NameAr : reviewer.AverageRating.NameEn),
                ReviewerAverageScore = reviewer == null ? 0 : reviewer.AverageScore
            };

        return query;
    }

    private static IReadOnlyList<GridReportColumn<PAEmployeeReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<PAEmployeeReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("JobTitle", r => r.JobTitle),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("Period", r => r.Period),
            new("FormType", r => r.FormType),
            new("Manager", r => r.Manager),
            new("ManagerJobTitle", r => r.ManagerJobTitle),
            new("OverallAverageRating", r => r.OverallAverageRating),
            new("OverallAverageScore", r => r.OverallAverageScore),
            new("PeerAverageRating", r => r.PeerAverageRating),
            new("PeerAverageScore", r => r.PeerAverageScore),
            new("EmployeeAverageRating", r => r.EmployeeAverageRating),
            new("EmployeeAverageScore", r => r.EmployeeAverageScore),
            new("ProposedIncrement", r => r.ProposedIncrement),
            new("ProposedJobTitle", r => r.ProposedJobTitle),
            new("AreaOfImprovement", r => r.AreaOfImprovement),
            new("Person", r => r.Person),
            new("AverageRating", r => r.ReviewerAverageRating),
            new("AverageScore", r => r.ReviewerAverageScore)
        };
    }

    private async Task StampOrgLevelsAsync(List<PAEmployeeReportRowDto> rows)
    {
        if (rows.Count == 0)
        {
            return;
        }

        var lookup = await _orgLevelNameResolver.BuildLookupAsync(UseArabicNames);

        foreach (var row in rows)
        {
            var levels = lookup.GetLevelNames(row.OrganizationUnitId);
            row.OrgLevel1 = levels[0];
            row.OrgLevel2 = levels[1];
            row.OrgLevel3 = levels[2];
            row.OrgLevel4 = levels[3];
            row.OrgLevel5 = levels[4];
            row.OrgLevel6 = levels[5];
            row.OrgLevel7 = levels[6];
        }
    }
}
