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
/// Performance Appraisal analysis grid report -- port of legacy
/// <c>PerformanceReport/uscRptPerformanceAppraisal.ascx(.cs)</c>, method <c>Table1()</c> (lines 494-683).
///
/// NOT PORTED: drill-up/drill-down session stack, ColumnChooser JSON persistence, callback-panel state
/// machine, PDF export, and the dead <c>Table1_Old()</c> (lines 684+, an earlier formulation reading
/// Employee.OrgLevelN/CurrentPayrollArea directly -- superseded by <c>Table1()</c>'s org-assignment snapshot).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_PerformanceAppraisal)]
public class PerformanceAppraisalReportAppService : GridReportAppServiceBase, IPerformanceAppraisalReportAppService
{
    private readonly IRepository<PAForm> _paFormRepository;
    private readonly IRepository<PATimeline> _paTimelineRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public PerformanceAppraisalReportAppService(
        IRepository<PAForm> paFormRepository,
        IRepository<PATimeline> paTimelineRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _paFormRepository = paFormRepository;
        _paTimelineRepository = paTimelineRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptPerformanceAppraisal";

    protected override string ScreenCaption => "Performance Appraisal";

    [HttpPost]
    public async Task<PagedResultDto<PerformanceAppraisalReportRowDto>> GetPerformanceAppraisalReport(
        GetPerformanceAppraisalReportInput input)
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

        return new PagedResultDto<PerformanceAppraisalReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetPerformanceAppraisalReportToExcel(GetPerformanceAppraisalReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var rows = await query.OrderBy(input.Sorting).ToListAsync();

        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync(
            "PerformanceAppraisal.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input));
    }

    private async Task<IQueryable<PerformanceAppraisalReportRowDto>> BuildQueryAsync(GetPerformanceAppraisalReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var periodName = input.PeriodName;
        var today = Today;

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

        return formQuery.Select(f => new PerformanceAppraisalReportRowDto
        {
            Id = f.Id,
            EmployeeCode = f.Employee.Code,
            EmployeeName = useArabicNames ? f.Employee.FullNameAr : f.Employee.FullNameEn,
            JobTitle = orgAss
                .Where(o => o.EmployeeId == f.EmployeeId && o.JobTitleId != null)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => useArabicNames ? o.JobTitle.NameAr : o.JobTitle.NameEn)
                .FirstOrDefault(),
            OrganizationUnitId = orgAss
                .Where(o => o.EmployeeId == f.EmployeeId && o.OrganizationUnitId != null)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.OrganizationUnitId)
                .FirstOrDefault(),
            PayrollArea = orgAss
                .Where(o => o.EmployeeId == f.EmployeeId && o.PayrollAreaId != null)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.PayrollArea.Name)
                .FirstOrDefault(),
            Period = f.PAPeriod.Name,
            PlanSubmittedByAppraisee = f.PlanSubmitByAppraisee,
            PlanSubmittedByAppraiser = f.PlanSubmitByAppraiser,
            AnnualReviewSubmittedByAppraisee = f.ERSubmitByAppraisee,
            AnnualReviewSubmittedByAppraiser = f.ERSubmitByAppraiser,
            OverallScore = f.OverallScore,
            ModeratedScore = f.ModeratedScore
        });
    }

    private static IReadOnlyList<GridReportColumn<PerformanceAppraisalReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<PerformanceAppraisalReportRowDto>>
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
            new("PayrollArea", r => r.PayrollArea),
            new("Period", r => r.Period),
            new("PlanSubmittedByAppraisee", r => r.PlanSubmittedByAppraisee),
            new("PlanSubmittedByAppraiser", r => r.PlanSubmittedByAppraiser),
            new("AnnualReviewSubmittedByAppraisee", r => r.AnnualReviewSubmittedByAppraisee),
            new("AnnualReviewSubmittedByAppraiser", r => r.AnnualReviewSubmittedByAppraiser),
            new("OverallScore", r => r.OverallScore),
            new("ModeratedScore", r => r.ModeratedScore)
        };
    }

    private async Task StampOrgLevelsAsync(List<PerformanceAppraisalReportRowDto> rows)
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
