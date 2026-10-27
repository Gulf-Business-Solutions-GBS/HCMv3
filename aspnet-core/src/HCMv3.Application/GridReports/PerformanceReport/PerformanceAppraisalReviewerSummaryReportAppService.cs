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
/// Performance Appraisal Reviewer Summary analysis grid report -- port of legacy customer variant
/// <c>PerformanceReport/uscRptPerformanceAppraisal_Custom012.ascx(.cs)</c>, method <c>Table1()</c>.
///
/// Legacy queries <c>PAEmployeeReviewer</c> (not <c>PAEmployee</c> directly), groups by the owning
/// employee, and picks the current-manager reviewer's row (falling back to the first reviewer) as the
/// "headline" row per employee. RLS is applied at the reviewer level, same as legacy's
/// <c>ApplyRowLevelSecurity(IEnumerable&lt;PAEmployeeReviewer&gt;)</c> override.
///
/// See <see cref="PerformanceAppraisalReviewerSummaryReportRowDto"/> remarks for the workflow-status
/// columns intentionally not reproduced.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_PerformanceAppraisalReviewerSummary)]
public class PerformanceAppraisalReviewerSummaryReportAppService
    : GridReportAppServiceBase, IPerformanceAppraisalReviewerSummaryReportAppService
{
    private readonly IRepository<PAEmployeeReviewer> _paEmployeeReviewerRepository;
    private readonly IRepository<EmployeeManager> _employeeManagerRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public PerformanceAppraisalReviewerSummaryReportAppService(
        IRepository<PAEmployeeReviewer> paEmployeeReviewerRepository,
        IRepository<EmployeeManager> employeeManagerRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _paEmployeeReviewerRepository = paEmployeeReviewerRepository;
        _employeeManagerRepository = employeeManagerRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptPerformanceAppraisal_Custom012";

    protected override string ScreenCaption => "Performance Appraisal Reviewer Summary";

    [HttpPost]
    public async Task<PagedResultDto<PerformanceAppraisalReviewerSummaryReportRowDto>> GetPerformanceAppraisalReviewerSummaryReport(
        GetPerformanceAppraisalReviewerSummaryReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var totalCount = rows.Count;

        var page = rows.AsQueryable().OrderBy(input.Sorting).PageBy(input).ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<PerformanceAppraisalReviewerSummaryReportRowDto>(totalCount, page);
    }

    [HttpPost]
    public async Task<FileDto> GetPerformanceAppraisalReviewerSummaryReportToExcel(
        GetPerformanceAppraisalReviewerSummaryReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var ordered = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync(
            "PerformanceAppraisalReviewerSummary.xlsx",
            ordered,
            GetExportColumns(),
            BuildParameterString(input));
    }

    private async Task<List<PerformanceAppraisalReviewerSummaryReportRowDto>> BuildRowsAsync(
        GetPerformanceAppraisalReviewerSummaryReportInput input)
    {
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var today = Today;
        var cycleName = input.CycleName;

        var reviewerQuery = _paEmployeeReviewerRepository.GetAll()
            .Where(r => r.PAEmployee != null);

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
                r.IsPeer,
                r.AverageScore,
                AverageRatingName = r.AverageRating == null
                    ? null
                    : (useArabicNames ? r.AverageRating.NameAr : r.AverageRating.NameEn),
                r.CustomTextField1,
                r.CustomTextField2,
                r.CustomTextField3
            })
            .ToListAsync();

        if (reviewers.Count == 0)
        {
            return new List<PerformanceAppraisalReviewerSummaryReportRowDto>();
        }

        var employeeIds = reviewers.Select(r => r.OwnerEmployeeId).Distinct().ToList();

        var managers = await _employeeManagerRepository.GetAll()
            .Where(m => employeeIds.Contains(m.EmployeeId) && m.StartDate.Date <= today && m.EndDate.Date >= today)
            .Select(m => new { m.EmployeeId, m.ManagerId })
            .ToListAsync();

        var orgAss = await _employeeOrgAssignmentRepository.GetAll()
            .Where(o => employeeIds.Contains(o.EmployeeId) && o.StartDate.Date <= today && o.EndDate.Date >= today)
            .Select(o => new
            {
                o.EmployeeId,
                o.OrganizationUnitId,
                o.StartDate,
                o.Id,
                WorkLocationName = o.WorkLocationId == null ? null : (useArabicNames ? o.WorkLocation.NameAr : o.WorkLocation.NameEn),
                CountryName = o.PayrollAreaId != null && o.PayrollArea.CountryId != null
                    ? (useArabicNames ? o.PayrollArea.Country.NameAr : o.PayrollArea.Country.NameEn)
                    : null
            })
            .ToListAsync();

        var lookup = await _orgLevelNameResolver.BuildLookupAsync(useArabicNames);

        var result = new List<PerformanceAppraisalReviewerSummaryReportRowDto>();

        foreach (var group in reviewers.GroupBy(r => r.OwnerEmployeeId))
        {
            var manager = managers.FirstOrDefault(m => m.EmployeeId == group.Key);

            var managerRec = manager == null
                ? null
                : group.FirstOrDefault(r => !r.IsPeer && r.ReviewerEmployeeId == manager.ManagerId);
            var firstRec = group.First();
            var headline = managerRec ?? firstRec;

            var selfRec = group.FirstOrDefault(r => r.ReviewerEmployeeId == r.OwnerEmployeeId);

            var org = orgAss.Where(o => o.EmployeeId == group.Key)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .FirstOrDefault();

            var managerName = "";
            if (manager != null)
            {
                managerName = await _employeeManagerRepository.GetAll()
                    .Where(m => m.EmployeeId == group.Key && m.ManagerId == manager.ManagerId)
                    .Select(m => useArabicNames
                        ? m.Manager.Code + " " + m.Manager.FullNameAr
                        : m.Manager.Code + " " + m.Manager.FullNameEn)
                    .FirstOrDefaultAsync() ?? "";
            }

            var levels = lookup.GetLevelNames(org?.OrganizationUnitId);

            result.Add(new PerformanceAppraisalReviewerSummaryReportRowDto
            {
                Id = headline.PAEmployeeId,
                EmployeeCode = firstRec.OwnerEmployeeCode,
                EmployeeName = firstRec.OwnerEmployeeName,
                JoiningDate = firstRec.JoiningDate,
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
                Country = org?.CountryName ?? "",
                Manager = managerName,
                SelfAssessmentScore = selfRec?.AverageScore ?? 0,
                FinalScore = manager != null ? (managerRec?.AverageScore ?? 0) : 0,
                FinalRating = manager != null ? (managerRec?.AverageRatingName ?? "") : "",
                AppraiseeStrengthAreas = managerRec?.CustomTextField1,
                AppraiseeAreasOfImprovements = managerRec?.CustomTextField2,
                OverallEvaluationSummaryAndTargetedTrainingNeeds = managerRec?.CustomTextField3
            });
        }

        return result;
    }

    private static IReadOnlyList<GridReportColumn<PerformanceAppraisalReviewerSummaryReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<PerformanceAppraisalReviewerSummaryReportRowDto>>
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
            new("Country", r => r.Country),
            new("Manager", r => r.Manager),
            new("SelfAssessmentScore", r => r.SelfAssessmentScore),
            new("FinalScore", r => r.FinalScore),
            new("FinalRating", r => r.FinalRating),
            new("AppraiseeStrengthAreas", r => r.AppraiseeStrengthAreas),
            new("AppraiseeAreasOfImprovements", r => r.AppraiseeAreasOfImprovements),
            new("OverallEvaluationSummaryAndTargetedTrainingNeeds", r => r.OverallEvaluationSummaryAndTargetedTrainingNeeds)
        };
    }
}
