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
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.PerformanceReport.Dto;
using HCMv3.PerformanceManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.PerformanceReport;

/// <summary>
/// PA Summary analysis grid report -- port of legacy <c>PerformanceReport/uscRptPASummary.ascx(.cs)</c>,
/// method <c>Table1()</c> (lines 522-699).
///
/// NOT PORTED: the drill-up/drill-down session stack, the ColumnChooser JSON persistence, the
/// callback-panel state machine, and the PDF export button.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_PASummary)]
public class PASummaryReportAppService : GridReportAppServiceBase, IPASummaryReportAppService
{
    private readonly IRepository<PAForm> _paFormRepository;
    private readonly IRepository<PATimeline> _paTimelineRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;

    public PASummaryReportAppService(
        IRepository<PAForm> paFormRepository,
        IRepository<PATimeline> paTimelineRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository)
    {
        _paFormRepository = paFormRepository;
        _paTimelineRepository = paTimelineRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
    }

    protected override string LegacyScreenName => "uscRptPASummary";

    protected override string ScreenCaption => "PA Summary";

    [HttpPost]
    public async Task<PagedResultDto<PASummaryReportRowDto>> GetPASummaryReport(GetPASummaryReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);
        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<PASummaryReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetPASummaryReportToExcel(GetPASummaryReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: legacy's ASPxGridViewExporter exported the entire filtered set.
        var rows = await query.OrderBy(input.Sorting).ToListAsync();

        return await ExportToFileAsync(
            "PASummary.xlsx",
            rows,
            GetExportColumns(input.Variant),
            BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export. Ports
    /// legacy <c>Table1()</c> (uscRptPASummary.ascx.cs:606-652): the PATimeline window overlap select,
    /// PAForm filters, then projection.
    /// </summary>
    private async Task<IQueryable<PASummaryReportRowDto>> BuildQueryAsync(GetPASummaryReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var periodName = input.PeriodName;
        var today = Today;

        // Legacy lines 608-613: periods whose timeline window overlaps [fromDate, toDate].
        var overlappingPeriodIds = _paTimelineRepository.GetAll()
            .Where(t => t.PAPeriod != null
                        && ((t.StartDate <= fromDate && fromDate <= t.EndDate)
                            || (t.StartDate <= toDate && toDate <= t.EndDate)
                            || (fromDate <= t.StartDate && t.StartDate <= toDate)
                            || (fromDate <= t.EndDate && t.EndDate <= toDate)))
            .Select(t => t.PAPeriodId);

        var formQuery = _paFormRepository.GetAll()
            .Where(f => overlappingPeriodIds.Contains(f.PAPeriodId))
            .WhereIf(!string.IsNullOrEmpty(periodName), f => f.PAPeriod.Name == periodName)
            .WhereIf(employeeCodes.Count > 0, f => employeeCodes.Contains(f.Employee.Code))
            .WhereIf(rlsEmployeeIds.Count > 0, f => rlsEmployeeIds.Contains(f.EmployeeId));

        var orgAss = _employeeOrgAssignmentRepository.GetAll()
            .Where(o => o.StartDate.Date <= today && o.EndDate.Date >= today);

        return formQuery.Select(f => new PASummaryReportRowDto
        {
            Id = f.Id,
            EmployeeCode = f.Employee.Code,
            EmployeeName = useArabicNames ? f.Employee.FullNameAr : f.Employee.FullNameEn,
            JobTitle = orgAss
                .Where(o => o.EmployeeId == f.EmployeeId && o.JobTitleId != null)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => useArabicNames ? o.JobTitle.NameAr : o.JobTitle.NameEn)
                .FirstOrDefault(),
            Department = orgAss
                .Where(o => o.EmployeeId == f.EmployeeId && o.OrganizationUnitId != null)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.OrganizationUnit.DisplayName)
                .FirstOrDefault(),
            WorkLocation = orgAss
                .Where(o => o.EmployeeId == f.EmployeeId && o.WorkLocationId != null)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => useArabicNames ? o.WorkLocation.NameAr : o.WorkLocation.NameEn)
                .FirstOrDefault(),
            OverallScore = f.OverallScore,
            ModeratedScore = f.ModeratedScore,
            AppraiseeStrength = f.AppraiseeStrength,
            AreasOfImprovement = f.AreasOfImprovement,
            NewlySetGoals = f.NewlySetGoals,
            OverallComments = f.OverallComments
        });
    }

    /// <summary>
    /// Legacy conditionally created only the columns for the selected <c>cmbReportType</c> variant
    /// (uscRptPASummary.ascx.cs:585-603). This export mirrors that by variant.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<PASummaryReportRowDto>> GetExportColumns(PASummaryReportVariant variant)
    {
        var columns = new List<GridReportColumn<PASummaryReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("JobTitle", r => r.JobTitle),
            new("Department", r => r.Department),
            new("Location", r => r.WorkLocation)
        };

        switch (variant)
        {
            case PASummaryReportVariant.OverallScore:
                columns.Add(new GridReportColumn<PASummaryReportRowDto>("OverallScore", r => r.OverallScore));
                columns.Add(new GridReportColumn<PASummaryReportRowDto>("ModeratedScore", r => r.ModeratedScore));
                break;
            case PASummaryReportVariant.FeedbackReport:
                columns.Add(new GridReportColumn<PASummaryReportRowDto>("AppraiseeStrength", r => r.AppraiseeStrength));
                columns.Add(new GridReportColumn<PASummaryReportRowDto>("AreasOfImprovement", r => r.AreasOfImprovement));
                break;
            case PASummaryReportVariant.GoalReport:
                columns.Add(new GridReportColumn<PASummaryReportRowDto>("NewlySetGoals", r => r.NewlySetGoals));
                break;
            case PASummaryReportVariant.OverallAssessment:
                columns.Add(new GridReportColumn<PASummaryReportRowDto>("OverallComments", r => r.OverallComments));
                break;
        }

        return columns;
    }
}
