using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PerformanceManagement.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.SystemSecurity.Services;
using Microsoft.AspNetCore.Mvc;

namespace HCMv3.PerformanceManagement;

/// <inheritdoc cref="IPerformanceAppraisalDashboardAppService"/>
/// <remarks>
/// <para><b>NOT ported (WebForms/DevExpress presentation only):</b> the <c>WebChartControl</c> series,
/// <c>FullStackedBarSeriesView.BarWidth</c>, <c>XYDiagram</c> axis titles/scrolling and
/// <c>ScaleType.Qualitative</c> plumbing (legacy lines 134-191). The figures those series carried ARE
/// ported; the chart itself is Angular's job.</para>
///
/// <para><b>NOT ported (dead code):</b> <c>OpenPAdashboard</c> (legacy lines 220-264) is 40 lines of which
/// everything except a single <c>PAPeriod</c> lookup is commented out; it built a non-persistent
/// <c>PADashboardsList</c> XAF ListView that never shipped. <c>cmbDepartment_SelectedIndexChanged</c>
/// (lines 194-198) and <c>btnShow_Chart_Click</c> (lines 266-269) are empty bodies.</para>
/// </remarks>
public class PerformanceAppraisalDashboardAppService : HCMv3AppServiceBase, IPerformanceAppraisalDashboardAppService
{
    private readonly IRepository<PAPeriod> _paPeriodRepository;
    private readonly IRepository<PAForm> _paFormRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IEmployeeSessionService _employeeSessionService;
    private readonly IRlsService _rlsService;

    public PerformanceAppraisalDashboardAppService(
        IRepository<PAPeriod> paPeriodRepository,
        IRepository<PAForm> paFormRepository,
        IRepository<Employee> employeeRepository,
        IEmployeeSessionService employeeSessionService,
        IRlsService rlsService)
    {
        _paPeriodRepository = paPeriodRepository;
        _paFormRepository = paFormRepository;
        _employeeRepository = employeeRepository;
        _employeeSessionService = employeeSessionService;
        _rlsService = rlsService;
    }

    /// <summary>
    /// Legacy <c>BindChart</c> (lines 93-192).
    ///
    /// <para><b>Counting rules, preserved exactly.</b> For each stage: Completed = both flags true;
    /// PendingByAppraisee = appraiser true AND appraisee false; PendingByAppraiser = <c>employeeCount</c>
    /// minus the other two. Note the third is a SUBTRACTION from the employee headcount, not a count of
    /// forms -- so employees with no <c>PAForm</c> at all land in "Pending by Appraiser", and the figure can
    /// go NEGATIVE if more forms exist than active employees. That is legacy's arithmetic (lines 122, 126,
    /// 131) and is not "fixed" here.</para>
    ///
    /// <para><c>[HttpPost]</c> per this repo's NSwag rule for a custom input DTO.</para>
    /// </summary>
    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAForms)]
    public async Task<PerformanceAppraisalDashboardDto> GetPerformanceAppraisalDashboard(
        GetPerformanceAppraisalDashboardInput input)
    {
        var period = await _paPeriodRepository.FirstOrDefaultAsync(input.PAPeriodId);
        if (period == null)
        {
            throw new UserFriendlyException(L("PAPeriodNotFound"));
        }

        // Legacy lines 99-112: when the caller maps to an employee, restrict to the RLS-visible set;
        // otherwise every active employee. Legacy compared employee CODES
        // (RLSService.GetRLSEmployeeCodeList); this port uses ids, which is the same set.
        var loggedInEmployee = await _employeeSessionService.GetCurrentEmployeeAsync();

        List<int> employeeIds;
        if (loggedInEmployee != null)
        {
            var rlsEmployeeIds = await _rlsService.GetRlsEmployeeIdsAsync();

            employeeIds = _employeeRepository.GetAll()
                .Where(x => x.Status == EmployeeStatus.Active && rlsEmployeeIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToList();
        }
        else
        {
            employeeIds = _employeeRepository.GetAll()
                .Where(x => x.Status == EmployeeStatus.Active)
                .Select(x => x.Id)
                .ToList();
        }

        // Legacy lines 114-116.
        var forms = _paFormRepository.GetAll()
            .Where(x => x.PAPeriodId == period.Id && employeeIds.Contains(x.EmployeeId))
            .Select(x => new
            {
                x.PlanSubmitByAppraiser,
                x.PlanSubmitByAppraisee,
                x.MRSubmitByAppraiser,
                x.MRSubmitByAppraisee,
                x.ERSubmitByAppraiser,
                x.ERSubmitByAppraisee
            })
            .ToList();

        var count = employeeIds.Count;

        return new PerformanceAppraisalDashboardDto
        {
            PAPeriodId = period.Id,
            PAPeriodName = period.Name,
            EmployeeCount = count,
            Plan = BuildStage(
                "PLAN",
                count,
                forms.Count(r => r.PlanSubmitByAppraiser && r.PlanSubmitByAppraisee),
                forms.Count(r => r.PlanSubmitByAppraiser && !r.PlanSubmitByAppraisee)),
            MidYearReview = BuildStage(
                "MID-YEAR",
                count,
                forms.Count(r => r.MRSubmitByAppraiser && r.MRSubmitByAppraisee),
                forms.Count(r => r.MRSubmitByAppraiser && !r.MRSubmitByAppraisee)),
            EndYearReview = BuildStage(
                "END-YEAR",
                count,
                forms.Count(r => r.ERSubmitByAppraiser && r.ERSubmitByAppraisee),
                forms.Count(r => r.ERSubmitByAppraiser && !r.ERSubmitByAppraisee))
        };
    }

    private static PerformanceAppraisalStageStatusDto BuildStage(
        string stage, int employeeCount, int completed, int pendingByAppraisee)
    {
        return new PerformanceAppraisalStageStatusDto
        {
            Stage = stage,
            Completed = completed,
            PendingByAppraisee = pendingByAppraisee,
            // Legacy lines 122/126/131 -- a subtraction, deliberately not clamped at zero.
            PendingByAppraiser = employeeCount - completed - pendingByAppraisee
        };
    }
}
