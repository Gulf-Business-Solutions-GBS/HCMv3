using System;
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
/// PA Employee Scorecard analysis grid report -- port of legacy customer variant
/// <c>PerformanceReport/uscRptPAEmployee_Custom033.ascx(.cs)</c>, method <c>Table1()</c>.
///
/// This variant replaces the base report's reviewer-fan-out/manual-average columns with a weighted
/// Behavior/Technical/Overall percentage computed from <c>PAEmployeeRating</c> rows, grouped by KPI
/// category code "1" (Behavior) / "2" (Technical). Because the computation needs every matching rating row
/// materialized before it can be grouped and weighted in memory (legacy itself does the same with repeated
/// <c>.ToList()</c> calls), <see cref="BuildRowsAsync"/> returns an already-computed
/// <see cref="List{T}"/> rather than an <see cref="IQueryable{T}"/> -- both the paged read and the export
/// call it so the two cannot drift.
///
/// NOT PORTED: drill-up/down session stack, ColumnChooser JSON persistence, callback-panel state machine,
/// PDF export.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_PAEmployeeScorecard)]
public class PAEmployeeScorecardReportAppService : GridReportAppServiceBase, IPAEmployeeScorecardReportAppService
{
    private readonly IRepository<PAEmployee> _paEmployeeRepository;
    private readonly IRepository<PAEmployeeRating> _paEmployeeRatingRepository;
    private readonly IRepository<PARating> _paRatingRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly IRepository<EmployeeManager> _employeeManagerRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public PAEmployeeScorecardReportAppService(
        IRepository<PAEmployee> paEmployeeRepository,
        IRepository<PAEmployeeRating> paEmployeeRatingRepository,
        IRepository<PARating> paRatingRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        IRepository<EmployeeManager> employeeManagerRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _paEmployeeRepository = paEmployeeRepository;
        _paEmployeeRatingRepository = paEmployeeRatingRepository;
        _paRatingRepository = paRatingRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _employeeManagerRepository = employeeManagerRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptPAEmployee_Custom033";

    protected override string ScreenCaption => "PA Employee Scorecard";

    [HttpPost]
    public async Task<PagedResultDto<PAEmployeeScorecardReportRowDto>> GetPAEmployeeScorecardReport(
        GetPAEmployeeScorecardReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var totalCount = rows.Count;

        var page = rows.AsQueryable()
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<PAEmployeeScorecardReportRowDto>(totalCount, page);
    }

    [HttpPost]
    public async Task<FileDto> GetPAEmployeeScorecardReportToExcel(GetPAEmployeeScorecardReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var ordered = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync(
            "PAEmployeeScorecard.xlsx",
            ordered,
            GetExportColumns(),
            BuildParameterString(input));
    }

    /// <summary>
    /// Filtering, weighted scoring AND projection in one place, shared by the read and the export.
    /// </summary>
    private async Task<List<PAEmployeeScorecardReportRowDto>> BuildRowsAsync(GetPAEmployeeScorecardReportInput input)
    {
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var today = Today;

        var paQuery = _paEmployeeRepository.GetAll()
            .WhereIf(!string.IsNullOrEmpty(input.FormTypeCode), pa => pa.FormType.Code == input.FormTypeCode)
            .WhereIf(!string.IsNullOrEmpty(input.PeriodCode), pa => pa.PACycle.Code == input.PeriodCode)
            .WhereIf(employeeCodes.Count > 0, pa => employeeCodes.Contains(pa.Employee.Code))
            .WhereIf(rlsEmployeeIds.Count > 0, pa => rlsEmployeeIds.Contains(pa.EmployeeId));

        var paEmployees = await paQuery
            .Select(pa => new
            {
                pa.Id,
                pa.EmployeeId,
                EmployeeCode = pa.Employee.Code,
                EmployeeName = useArabicNames ? pa.Employee.FullNameAr : pa.Employee.FullNameEn
            })
            .ToListAsync();

        if (paEmployees.Count == 0)
        {
            return new List<PAEmployeeScorecardReportRowDto>();
        }

        var paEmployeeIds = paEmployees.Select(pa => pa.Id).ToList();

        var orgAss = _employeeOrgAssignmentRepository.GetAll()
            .Where(o => o.StartDate.Date <= today && o.EndDate.Date >= today);

        var managers = await _employeeManagerRepository.GetAll()
            .Where(m => m.StartDate.Date <= today && m.EndDate.Date >= today)
            .Select(m => new { m.EmployeeId, m.ManagerId })
            .ToListAsync();

        // Legacy lines: categoryScores -- every rating row with a KPI category, joined back to the
        // reviewer and the owning employee's current manager.
        var ratings = await _paEmployeeRatingRepository.GetAll()
            .Where(r => paEmployeeIds.Contains(r.PAEmployeeId)
                        && r.PAEmployeeDetail != null
                        && r.PAEmployeeDetail.KpiInfo != null
                        && r.Rating != null
                        && r.PAEmployeeReviewer != null)
            .Select(r => new
            {
                r.PAEmployeeId,
                r.PAEmployeeDetailId,
                CategoryCode = r.PAEmployeeDetail.KpiInfo.KpiCategory.Code,
                r.PAEmployeeDetail.Weight,
                RatingCode = r.Rating.Code,
                ReviewerEmployeeId = r.PAEmployeeReviewer.EmployeeId
            })
            .ToListAsync();

        var paEmployeeToEmployee = paEmployees.ToDictionary(pa => pa.Id, pa => pa.EmployeeId);

        var managerOnlyRatings = ratings
            .Where(r => paEmployeeToEmployee.TryGetValue(r.PAEmployeeId, out var ownerEmployeeId)
                        && managers.Any(m => m.EmployeeId == ownerEmployeeId && m.ManagerId == r.ReviewerEmployeeId))
            .ToList();

        // Legacy quirk (Custom033 Table1): the manager-only/all-reviewers switch is decided ONCE for the
        // whole batch -- if ANY manager rating exists anywhere in the filtered set, every row's score uses
        // manager ratings only; otherwise every row falls back to all reviewers' ratings.
        var effectiveRatings = managerOnlyRatings.Count > 0 ? managerOnlyRatings : ratings;

        var maxRatingCode = 0;
        foreach (var code in (await _paRatingRepository.GetAll().Select(r => r.Code).ToListAsync()))
        {
            if (int.TryParse(code, out var parsed) && parsed > maxRatingCode)
            {
                maxRatingCode = parsed;
            }
        }

        var categoryScoresPerKpi = effectiveRatings
            .GroupBy(r => new { r.PAEmployeeId, r.PAEmployeeDetailId, r.CategoryCode, r.Weight })
            .Select(g => new
            {
                g.Key.PAEmployeeId,
                g.Key.CategoryCode,
                g.Key.Weight,
                AvgRatingCode = g.Average(r => double.TryParse(r.RatingCode, out var v) ? v : 0)
            })
            .ToList();

        double WeightedPercentage(string categoryCode, int paEmployeeId)
        {
            var scores = categoryScoresPerKpi
                .Where(r => r.PAEmployeeId == paEmployeeId && r.CategoryCode == categoryCode)
                .ToList();

            if (scores.Count == 0 || maxRatingCode == 0)
            {
                return 0.0;
            }

            return Math.Round(scores.Sum(r => (r.Weight / maxRatingCode) * r.AvgRatingCode) / 100, 2);
        }

        var paRatingRanges = await _paRatingRepository.GetAll().ToListAsync();

        string ResolveRatingName(double score)
        {
            var match = paRatingRanges.FirstOrDefault(r => r.FromRange < score && score <= r.ToRange);
            if (match == null)
            {
                return "";
            }

            return useArabicNames ? match.NameAr : match.NameEn;
        }

        var lookup = await _orgLevelNameResolver.BuildLookupAsync(useArabicNames);

        var result = new List<PAEmployeeScorecardReportRowDto>();
        foreach (var pa in paEmployees)
        {
            var currentOrgAss = await orgAss
                .Where(o => o.EmployeeId == pa.EmployeeId)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => new
                {
                    JobTitle = o.JobTitleId != null ? (useArabicNames ? o.JobTitle.NameAr : o.JobTitle.NameEn) : "",
                    CostCenter = o.CostCenterId != null ? (useArabicNames ? o.CostCenter.NameAr : o.CostCenter.NameEn) : "",
                    o.OrganizationUnitId
                })
                .FirstOrDefaultAsync();

            var managerLink = managers.FirstOrDefault(m => m.EmployeeId == pa.EmployeeId);
            string managerName = "";
            string managerJobTitle = "";
            if (managerLink != null)
            {
                var mgr = await _employeeOrgAssignmentRepository.GetAll()
                    .Where(o => o.EmployeeId == managerLink.ManagerId && o.JobTitleId != null)
                    .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                    .Select(o => useArabicNames ? o.JobTitle.NameAr : o.JobTitle.NameEn)
                    .FirstOrDefaultAsync();
                managerJobTitle = mgr ?? "";

                var managerEmployee = await _employeeManagerRepository.GetAll()
                    .Where(m => m.EmployeeId == pa.EmployeeId && m.ManagerId == managerLink.ManagerId)
                    .Select(m => useArabicNames
                        ? m.Manager.Code + " " + m.Manager.FullNameAr
                        : m.Manager.Code + " " + m.Manager.FullNameEn)
                    .FirstOrDefaultAsync();
                managerName = managerEmployee ?? "";
            }

            var behaviorPercentage = WeightedPercentage("1", pa.Id) * 100;
            var technicalPercentage = WeightedPercentage("2", pa.Id) * 100;
            var behaviorScore = Math.Round(behaviorPercentage * 5.0 / 100, 2);
            var technicalScore = Math.Round(technicalPercentage * 5.0 / 100, 2);
            var overallPercentage = Math.Round(behaviorPercentage + technicalPercentage, 2);
            var overallScore = Math.Round(overallPercentage * 5.0 / 100, 2);

            var levels = lookup.GetLevelNames(currentOrgAss?.OrganizationUnitId);

            result.Add(new PAEmployeeScorecardReportRowDto
            {
                Id = pa.Id,
                EmployeeCode = pa.EmployeeCode,
                EmployeeName = pa.EmployeeName,
                JobTitle = currentOrgAss?.JobTitle ?? "",
                OrganizationUnitId = currentOrgAss?.OrganizationUnitId,
                OrgLevel1 = levels[0],
                OrgLevel2 = levels[1],
                OrgLevel3 = levels[2],
                OrgLevel4 = levels[3],
                OrgLevel5 = levels[4],
                OrgLevel6 = levels[5],
                OrgLevel7 = levels[6],
                CostCenter = currentOrgAss?.CostCenter ?? "",
                Manager = managerName,
                ManagerJobTitle = managerJobTitle,
                BehaviorPercentage = behaviorPercentage,
                BehaviorRating = ResolveRatingName(behaviorScore),
                TechnicalPercentage = technicalPercentage,
                TechnicalRating = ResolveRatingName(technicalScore),
                OverallAveragePercentage = overallPercentage,
                OverallAverageRating = ResolveRatingName(overallScore)
            });
        }

        return result;
    }

    private static IReadOnlyList<GridReportColumn<PAEmployeeScorecardReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<PAEmployeeScorecardReportRowDto>>
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
            new("CostCenter", r => r.CostCenter),
            new("Manager", r => r.Manager),
            new("ManagerJobTitle", r => r.ManagerJobTitle),
            new("BehaviorPercentage", r => r.BehaviorPercentage),
            new("BehaviorRating", r => r.BehaviorRating),
            new("TechnicalPercentage", r => r.TechnicalPercentage),
            new("TechnicalRating", r => r.TechnicalRating),
            new("OverallAveragePercentage", r => r.OverallAveragePercentage),
            new("OverallAverageRating", r => r.OverallAverageRating)
        };
    }
}
