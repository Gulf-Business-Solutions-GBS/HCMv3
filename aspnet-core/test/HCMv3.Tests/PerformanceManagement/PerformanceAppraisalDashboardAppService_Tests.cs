using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Localization;
using Abp.UI;
using HCMv3.PerformanceManagement;
using HCMv3.PerformanceManagement.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.SystemSecurity.Services;
using NSubstitute;
using Shouldly;
using Xunit;

// See ProjectCostEngineFakes for why a plain List<T>.AsQueryable() is a complete stand-in here.
using Fakes = HCMv3.Tests.ProjectManagement.ProjectCostEngineFakes;

namespace HCMv3.Tests.PerformanceManagement;

/// <summary>
/// Unit tests for <see cref="PerformanceAppraisalDashboardAppService"/> -- the port of legacy
/// <c>uscPerformanceAppraisal.BindChart</c>.
/// </summary>
public class PerformanceAppraisalDashboardAppService_Tests
{
    private const int PeriodId = 2;

    private readonly List<PAPeriod> _periods = new List<PAPeriod>();
    private readonly List<PAForm> _forms = new List<PAForm>();
    private readonly List<Employee> _employees = new List<Employee>();
    private readonly IEmployeeSessionService _employeeSessionService = Substitute.For<IEmployeeSessionService>();
    private readonly IRlsService _rlsService = Substitute.For<IRlsService>();

    public PerformanceAppraisalDashboardAppService_Tests()
    {
        _periods.Add(new PAPeriod { Id = PeriodId, TenantId = 1, Name = "2024" });

        for (var i = 1; i <= 4; i++)
        {
            _employees.Add(new Employee
            {
                Id = i, TenantId = 1, Code = "E" + i, FullNameEn = "E" + i,
                Status = EmployeeStatus.Active
            });
        }

        // No session employee -> legacy's "every active employee" arm (lines 110-112).
        _employeeSessionService.GetCurrentEmployeeAsync().Returns(Task.FromResult<Employee>(null));
    }

    private PerformanceAppraisalDashboardAppService CreateService()
    {
        var service = new PerformanceAppraisalDashboardAppService(
            Fakes.RepoOver(_periods),
            Fakes.RepoOver(_forms),
            Fakes.RepoOver(_employees),
            _employeeSessionService,
            _rlsService);

        ((ApplicationService)service).AbpSession = Fakes.Session();
        ((ApplicationService)service).LocalizationManager = Fakes.LocalizationManager();
        return service;
    }

    /// <summary>
    /// Legacy lines 120-131. 4 active employees; for PLAN: 1 completed, 1 pending-by-appraisee, so
    /// pending-by-appraiser = 4 - 1 - 1 = 2 (a subtraction from headcount, NOT a form count).
    /// </summary>
    [Fact]
    public async Task Should_Count_Each_Stage_The_Way_Legacy_Does()
    {
        _forms.Add(new PAForm
        {
            Id = 1, TenantId = 1, PAPeriodId = PeriodId, EmployeeId = 1,
            PlanSubmitByAppraiser = true, PlanSubmitByAppraisee = true,
            MRSubmitByAppraiser = true, MRSubmitByAppraisee = false
        });
        _forms.Add(new PAForm
        {
            Id = 2, TenantId = 1, PAPeriodId = PeriodId, EmployeeId = 2,
            PlanSubmitByAppraiser = true, PlanSubmitByAppraisee = false
        });

        var service = CreateService();

        var result = await service.GetPerformanceAppraisalDashboard(
            new GetPerformanceAppraisalDashboardInput { PAPeriodId = PeriodId });

        result.EmployeeCount.ShouldBe(4);
        result.PAPeriodName.ShouldBe("2024");

        result.Plan.Stage.ShouldBe("PLAN");
        result.Plan.Completed.ShouldBe(1);
        result.Plan.PendingByAppraisee.ShouldBe(1);
        result.Plan.PendingByAppraiser.ShouldBe(2);

        result.MidYearReview.Completed.ShouldBe(0);
        result.MidYearReview.PendingByAppraisee.ShouldBe(1);
        result.MidYearReview.PendingByAppraiser.ShouldBe(3);

        result.EndYearReview.Completed.ShouldBe(0);
        result.EndYearReview.PendingByAppraisee.ShouldBe(0);
        result.EndYearReview.PendingByAppraiser.ShouldBe(4);
    }

    /// <summary>Inactive employees are excluded from the headcount (legacy line 111).</summary>
    [Fact]
    public async Task Should_Only_Count_Active_Employees()
    {
        _employees[3].Status = EmployeeStatus.Inactive;

        var service = CreateService();

        var result = await service.GetPerformanceAppraisalDashboard(
            new GetPerformanceAppraisalDashboardInput { PAPeriodId = PeriodId });

        result.EmployeeCount.ShouldBe(3);
    }

    /// <summary>Legacy lines 101-108: with a session employee, the RLS-visible set is used instead.</summary>
    [Fact]
    public async Task Should_Restrict_To_The_Rls_Visible_Employees_For_A_Logged_In_Employee()
    {
        _employeeSessionService.GetCurrentEmployeeAsync().Returns(Task.FromResult(_employees[0]));
        _rlsService.GetRlsEmployeeIdsAsync().Returns(Task.FromResult(new List<int> { 1, 2 }));

        var service = CreateService();

        var result = await service.GetPerformanceAppraisalDashboard(
            new GetPerformanceAppraisalDashboardInput { PAPeriodId = PeriodId });

        result.EmployeeCount.ShouldBe(2);
    }

    /// <summary>
    /// Legacy's arithmetic can go negative when more forms exist than active employees. Asserted so a later
    /// "tidy-up" that clamps it is caught.
    /// </summary>
    [Fact]
    public async Task PendingByAppraiser_Should_Be_Allowed_To_Go_Negative_Like_Legacy()
    {
        _employees.Clear();
        _employees.Add(new Employee { Id = 1, TenantId = 1, Code = "E1", Status = EmployeeStatus.Active });

        for (var i = 1; i <= 3; i++)
        {
            _forms.Add(new PAForm
            {
                Id = i, TenantId = 1, PAPeriodId = PeriodId, EmployeeId = 1,
                PlanSubmitByAppraiser = true, PlanSubmitByAppraisee = true
            });
        }

        var service = CreateService();

        var result = await service.GetPerformanceAppraisalDashboard(
            new GetPerformanceAppraisalDashboardInput { PAPeriodId = PeriodId });

        result.Plan.Completed.ShouldBe(3);
        result.Plan.PendingByAppraiser.ShouldBe(-2);
    }

    [Fact]
    public async Task Should_Reject_An_Unknown_Period()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<UserFriendlyException>(() => service.GetPerformanceAppraisalDashboard(
            new GetPerformanceAppraisalDashboardInput { PAPeriodId = 999 }));
    }
}
