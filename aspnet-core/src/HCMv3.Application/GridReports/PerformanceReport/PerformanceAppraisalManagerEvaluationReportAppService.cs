using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
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
/// Performance Appraisal Manager Evaluation analysis grid report -- port of legacy customer variant
/// <c>PerformanceReport/uscRptPerformanceAppraisal_Custom035.ascx(.cs)</c>, method <c>Table1()</c>.
///
/// LEGACY QUIRK reproduced as-is: the period-window select (<c>paPeriodList</c>) is
/// <c>PACycle.Where(r =&gt; r.NameEn != null)</c> -- every cycle with a name, i.e. the date-range filter on
/// this variant's <c>Table1()</c> is a no-op despite the <c>dtFrom</c>/<c>dtTo</c> filter fields existing on
/// the panel. Only the <c>cmbPACycle</c> selection (and employee code) actually narrows the result.
///
/// Only the current-manager's (non-peer) review counts -- rows for employees with no manager-authored
/// review are simply absent, exactly like legacy's inner join.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_PerformanceAppraisalManagerEvaluation)]
public class PerformanceAppraisalManagerEvaluationReportAppService
    : GridReportAppServiceBase, IPerformanceAppraisalManagerEvaluationReportAppService
{
    private readonly IRepository<PAEmployeeReviewer> _paEmployeeReviewerRepository;
    private readonly IRepository<PAEmployeeRating> _paEmployeeRatingRepository;
    private readonly IRepository<PARating> _paRatingRepository;
    private readonly IRepository<EmployeeManager> _employeeManagerRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public PerformanceAppraisalManagerEvaluationReportAppService(
        IRepository<PAEmployeeReviewer> paEmployeeReviewerRepository,
        IRepository<PAEmployeeRating> paEmployeeRatingRepository,
        IRepository<PARating> paRatingRepository,
        IRepository<EmployeeManager> employeeManagerRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _paEmployeeReviewerRepository = paEmployeeReviewerRepository;
        _paEmployeeRatingRepository = paEmployeeRatingRepository;
        _paRatingRepository = paRatingRepository;
        _employeeManagerRepository = employeeManagerRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptPerformanceAppraisal_Custom035";

    protected override string ScreenCaption => "Performance Appraisal Manager Evaluation";

    [HttpPost]
    public async Task<PagedResultDto<PerformanceAppraisalManagerEvaluationReportRowDto>> GetPerformanceAppraisalManagerEvaluationReport(
        GetPerformanceAppraisalManagerEvaluationReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var totalCount = rows.Count;

        var page = rows.AsQueryable().OrderBy(input.Sorting).PageBy(input).ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<PerformanceAppraisalManagerEvaluationReportRowDto>(totalCount, page);
    }

    [HttpPost]
    public async Task<FileDto> GetPerformanceAppraisalManagerEvaluationReportToExcel(
        GetPerformanceAppraisalManagerEvaluationReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var ordered = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync(
            "PerformanceAppraisalManagerEvaluation.xlsx",
            ordered,
            GetExportColumns(ordered),
            BuildParameterString(input));
    }

    private async Task<List<PerformanceAppraisalManagerEvaluationReportRowDto>> BuildRowsAsync(
        GetPerformanceAppraisalManagerEvaluationReportInput input)
    {
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var today = Today;
        var cycleName = input.CycleName;

        var reviewerQuery = _paEmployeeReviewerRepository.GetAll()
            .Where(r => r.PAEmployee != null && !r.IsPeer);

        if (!cycleName.IsNullOrWhiteSpace())
        {
            reviewerQuery = reviewerQuery.Where(r => r.PAEmployee.PACycle.NameEn == cycleName);
        }

        if (employeeCodes.Count > 0)
        {
            reviewerQuery = reviewerQuery.Where(r => employeeCodes.Contains(r.PAEmployee.Employee.Code));
        }

        if (rlsEmployeeIds.Count > 0)
        {
            reviewerQuery = reviewerQuery.Where(r => rlsEmployeeIds.Contains(r.PAEmployee.EmployeeId));
        }

        var reviewers = await reviewerQuery
            .Select(r => new
            {
                r.PAEmployeeId,
                OwnerEmployeeId = r.PAEmployee.EmployeeId,
                OwnerEmployeeCode = r.PAEmployee.Employee.Code,
                OwnerEmployeeName = useArabicNames ? r.PAEmployee.Employee.FullNameAr : r.PAEmployee.Employee.FullNameEn,
                r.PAEmployee.Employee.JoiningDate,
                ReviewerEmployeeId = r.EmployeeId,
                r.AverageScore
            })
            .ToListAsync();

        if (reviewers.Count == 0)
        {
            return new List<PerformanceAppraisalManagerEvaluationReportRowDto>();
        }

        var employeeIds = reviewers.Select(r => r.OwnerEmployeeId).Distinct().ToList();

        var managers = await _employeeManagerRepository.GetAll()
            .Where(m => employeeIds.Contains(m.EmployeeId) && m.StartDate.Date <= today && m.EndDate.Date >= today)
            .Select(m => new { m.EmployeeId, m.ManagerId })
            .ToListAsync();

        // Legacy inner-joins onto the current-manager review only.
        var managerReviews = reviewers
            .Where(r => managers.Any(m => m.EmployeeId == r.OwnerEmployeeId && m.ManagerId == r.ReviewerEmployeeId))
            .ToList();

        if (managerReviews.Count == 0)
        {
            return new List<PerformanceAppraisalManagerEvaluationReportRowDto>();
        }

        var orgAss = await _employeeOrgAssignmentRepository.GetAll()
            .Where(o => employeeIds.Contains(o.EmployeeId) && o.StartDate.Date <= today && o.EndDate.Date >= today)
            .Select(o => new
            {
                o.EmployeeId,
                o.OrganizationUnitId,
                o.StartDate,
                o.Id,
                WorkLocationName = o.WorkLocationId == null ? null : (useArabicNames ? o.WorkLocation.NameAr : o.WorkLocation.NameEn)
            })
            .ToListAsync();

        var ratingRanges = await _paRatingRepository.GetAll().ToListAsync();

        string ResolveRatingName(double score)
        {
            var match = ratingRanges.FirstOrDefault(r => score >= r.FromRange && score <= r.ToRange);
            return match == null ? "" : (useArabicNames ? match.NameAr : match.NameEn);
        }

        Dictionary<int, List<PerformanceAppraisalManagerEvaluationKpiDto>> kpisByPaEmployee = new();
        if (input.RunType == PerformanceAppraisalManagerEvaluationRunType.Detail)
        {
            var paEmployeeIds = managerReviews.Select(r => r.PAEmployeeId).Distinct().ToList();
            var kpiRatings = await _paEmployeeRatingRepository.GetAll()
                .Where(r => paEmployeeIds.Contains(r.PAEmployeeId)
                            && r.PAEmployeeDetail != null
                            && r.PAEmployeeDetail.KpiInfo != null
                            && r.PAEmployeeReviewer != null)
                .Select(r => new
                {
                    r.PAEmployeeId,
                    ReviewerEmployeeId = r.PAEmployeeReviewer.EmployeeId,
                    OwnerEmployeeId = r.PAEmployee.EmployeeId,
                    KpiName = useArabicNames ? r.PAEmployeeDetail.KpiInfo.NameAr : r.PAEmployeeDetail.KpiInfo.NameEn,
                    r.Marks
                })
                .ToListAsync();

            kpisByPaEmployee = kpiRatings
                .Where(r => managers.Any(m => m.EmployeeId == r.OwnerEmployeeId && m.ManagerId == r.ReviewerEmployeeId))
                .GroupBy(r => r.PAEmployeeId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => new PerformanceAppraisalManagerEvaluationKpiDto
                    {
                        KpiName = x.KpiName,
                        Marks = x.Marks
                    }).ToList());
        }

        var lookup = await _orgLevelNameResolver.BuildLookupAsync(useArabicNames);

        var result = new List<PerformanceAppraisalManagerEvaluationReportRowDto>();
        foreach (var rec in managerReviews)
        {
            var manager = managers.First(m => m.EmployeeId == rec.OwnerEmployeeId);

            var managerName = await _employeeManagerRepository.GetAll()
                .Where(m => m.EmployeeId == rec.OwnerEmployeeId && m.ManagerId == manager.ManagerId)
                .Select(m => useArabicNames
                    ? m.Manager.Code + " " + m.Manager.FullNameAr
                    : m.Manager.Code + " " + m.Manager.FullNameEn)
                .FirstOrDefaultAsync() ?? "";

            var org = orgAss.Where(o => o.EmployeeId == rec.OwnerEmployeeId)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .FirstOrDefault();

            var levels = lookup.GetLevelNames(org?.OrganizationUnitId);

            result.Add(new PerformanceAppraisalManagerEvaluationReportRowDto
            {
                Id = rec.PAEmployeeId,
                EmployeeCode = rec.OwnerEmployeeCode,
                EmployeeName = rec.OwnerEmployeeName,
                JoiningDate = rec.JoiningDate,
                JobTitle = "",
                OrganizationUnitId = org?.OrganizationUnitId,
                OrgLevel1 = levels[0],
                OrgLevel2 = levels[1],
                OrgLevel3 = levels[2],
                OrgLevel4 = levels[3],
                OrgLevel5 = levels[4],
                OrgLevel6 = levels[5],
                OrgLevel7 = levels[6],
                WorkLocation = org?.WorkLocationName ?? "",
                Manager = managerName,
                AverageMark = rec.AverageScore,
                OverallEvaluation = rec.AverageScore != 0 ? ResolveRatingName(rec.AverageScore) : "",
                Kpis = kpisByPaEmployee.TryGetValue(rec.PAEmployeeId, out var kpis) ? kpis : new List<PerformanceAppraisalManagerEvaluationKpiDto>()
            });
        }

        return result;
    }

    private static IReadOnlyList<GridReportColumn<PerformanceAppraisalManagerEvaluationReportRowDto>> GetExportColumns(
        List<PerformanceAppraisalManagerEvaluationReportRowDto> rows)
    {
        var columns = new List<GridReportColumn<PerformanceAppraisalManagerEvaluationReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("JoiningDate", r => r.JoiningDate),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("WorkLocation", r => r.WorkLocation),
            new("Manager", r => r.Manager),
            new("AverageMark", r => r.AverageMark),
            new("OverallEvaluation", r => r.OverallEvaluation)
        };

        // Legacy Detail mode's dynamic "KPI1"/"KPI1 - Mark".."KPIn"/"KPIn - Mark" pivot columns, sized to
        // the widest row -- literal headers because they are not fixed localization keys.
        var maxKpiCount = rows.Count == 0 ? 0 : rows.Max(r => r.Kpis.Count);
        for (var i = 0; i < maxKpiCount; i++)
        {
            var index = i;
            columns.Add(GridReportColumn<PerformanceAppraisalManagerEvaluationReportRowDto>.FromLiteralHeader(
                $"KPI{index + 1}", r => index < r.Kpis.Count ? r.Kpis[index].KpiName : ""));
            columns.Add(GridReportColumn<PerformanceAppraisalManagerEvaluationReportRowDto>.FromLiteralHeader(
                $"KPI{index + 1} - Mark", r => index < r.Kpis.Count ? r.Kpis[index].Marks : (object)null));
        }

        return columns;
    }
}
