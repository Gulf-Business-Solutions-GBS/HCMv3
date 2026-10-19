using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Localization;
using Abp.UI;
using HCMv3.PerformanceManagement;
using HCMv3.PerformanceManagement.Dto;
using HCMv3.PerformanceManagement.Review;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Services;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PerformanceManagement;

/// <summary>
/// The goal form's "may tick" rules as the save enforces them (<see cref="PAFormsAppService"/> with
/// <see cref="PAFormTickPolicy"/>): order, side, window and personal extension, against the stored form.
/// </summary>
// ReSharper disable once InconsistentNaming
public class PAFormsAppService_TickRules_Tests : PerformanceManagementAppServiceTestBase
{
    private int? _me;

    private readonly ManageAppraisalSettings _appraisalSettings = new() { AppraisalCycle = AppraisalCycle.Quarterly };

    private PAFormsAppService CreateAppService()
    {
        var settings = Substitute.For<ISysSettingsAccessor>();
        settings.GetAppraisalSettingsAsync().Returns(_ => Task.FromResult(_appraisalSettings));

        var scope = Substitute.For<IEmployeeScopeProvider>();
        scope.GetCurrentEmployeeIdAsync().Returns(_ => Task.FromResult(_me));
        scope.GetRequiredCurrentEmployeeIdAsync().Returns(_ => Task.FromResult(_me ?? 0));

        var policy = new PAFormTickPolicy(
            FakeRepository.For<PATimeline>(Context),
            FakeRepository.For<PATimelineException>(Context),
            FakeRepository.For<PAPeerNominee>(Context),
            settings)
        {
            LocalizationManager = LocalIocManager.Resolve<ILocalizationManager>()
        };

        return CreateService(() => new PAFormsAppService(
            FakeRepository.For<PAForm>(Context),
            FakeRepository.For<PAPeerNominee>(Context),
            settings,
            policy,
            scope));
    }

    private void Timeline(int periodId, ActivityType activity, DateTime start, DateTime end)
    {
        Context.PATimelines.Add(new PATimeline { TenantId = AbpSession.TenantId ?? 1, PAPeriodId = periodId, ActivityType = activity, StartDate = start, EndDate = end });
        Context.SaveChanges();
    }

    private void OpenNow(int periodId, ActivityType activity) => Timeline(periodId, activity, DateTime.Today.AddDays(-5), DateTime.Today.AddDays(5));

    private void ClosedNow(int periodId, ActivityType activity) => Timeline(periodId, activity, DateTime.Today.AddDays(-30), DateTime.Today.AddDays(-10));

    private CreateOrEditPAFormDto Edit(int formId)
    {
        var form = Context.PAForms.Find(formId);
        return new CreateOrEditPAFormDto
        {
            Id = form.Id,
            PAPeriodId = form.PAPeriodId,
            EmployeeId = form.EmployeeId,
            PlanSubmitByAppraisee = form.PlanSubmitByAppraisee,
            PlanSubmitByAppraiser = form.PlanSubmitByAppraiser,
            ERSubmitByAppraisee = form.ERSubmitByAppraisee,
            ERSubmitByAppraiser = form.ERSubmitByAppraiser,
            AppraiseeSignedOff = form.AppraiseeSignedOff,
            AppraiserSignedOff = form.AppraiserSignedOff
        };
    }

    private (int PeriodId, int Employee, int Manager, int FormId) Setup()
    {
        var periodId = CreatePAPeriod("2026");
        var employee = CreateEmployee("E", "1");
        var manager = CreateEmployee("M", "1");
        var formId = CreatePAForm(periodId, employee);
        OpenNow(periodId, ActivityType.Plan);
        ClosedNow(periodId, ActivityType.AnnualReview);
        return (periodId, employee, manager, formId);
    }

    [Fact]
    public async Task The_Employee_Should_Tick_Their_Own_Plan_Box_In_The_Window()
    {
        var s = Setup();
        _me = s.Employee;
        var input = Edit(s.FormId);
        input.PlanSubmitByAppraisee = true;

        await CreateAppService().CreateOrEdit(input);

        Context.PAForms.Find(s.FormId).PlanSubmitByAppraisee.ShouldBeTrue();
    }

    [Fact]
    public async Task The_Employee_Should_Not_Tick_The_Appraisers_Box()
    {
        var s = Setup();
        _me = s.Employee;
        var input = Edit(s.FormId);
        input.PlanSubmitByAppraiser = true;

        await Should.ThrowAsync<UserFriendlyException>(() => CreateAppService().CreateOrEdit(input));
    }

    [Fact]
    public async Task A_Later_Stage_Should_Wait_For_The_Plan()
    {
        var s = Setup();
        OpenNow(s.PeriodId, ActivityType.AnnualReview);
        _me = s.Manager;
        var input = Edit(s.FormId);
        input.ERSubmitByAppraiser = true;

        await Should.ThrowAsync<UserFriendlyException>(() => CreateAppService().CreateOrEdit(input));
    }

    [Fact]
    public async Task Nothing_Should_Be_Ticked_Outside_The_Window()
    {
        var periodId = CreatePAPeriod("2026");
        var employee = CreateEmployee("E", "1");
        var formId = CreatePAForm(periodId, employee);
        ClosedNow(periodId, ActivityType.Plan);
        _me = employee;
        var input = Edit(formId);
        input.PlanSubmitByAppraisee = true;

        await Should.ThrowAsync<UserFriendlyException>(() => CreateAppService().CreateOrEdit(input));
    }

    [Fact]
    public async Task A_Personal_Extension_Should_Reopen_The_Window_For_That_Person()
    {
        var periodId = CreatePAPeriod("2026");
        var employee = CreateEmployee("E", "1");
        var formId = CreatePAForm(periodId, employee);
        ClosedNow(periodId, ActivityType.Plan);
        Context.PATimelineExceptions.Add(new PATimelineException
        {
            TenantId = AbpSession.TenantId ?? 1,
            PAPeriodId = periodId,
            EmployeeId = employee,
            ActivityType = ActivityType.Plan,
            ExtensionDate = DateTime.Today.AddDays(3)
        });
        Context.SaveChanges();
        _me = employee;
        var input = Edit(formId);
        input.PlanSubmitByAppraisee = true;

        await CreateAppService().CreateOrEdit(input);

        Context.PAForms.Find(formId).PlanSubmitByAppraisee.ShouldBeTrue();
    }

    /// <summary>
    /// LEGACY ODDITY, kept: a closed period locks nothing. Legacy disabled every box for it, then re-enabled
    /// the open stage's boxes a few lines later in the same pass.
    /// </summary>
    [Fact]
    public async Task A_Closed_Period_Should_Not_Lock_The_Open_Stage()
    {
        var s = Setup();
        var period = Context.PAPeriods.Find(s.PeriodId);
        period.Closed = true;
        Context.SaveChanges();
        _me = s.Employee;
        var input = Edit(s.FormId);
        input.PlanSubmitByAppraisee = true;

        await CreateAppService().CreateOrEdit(input);

        Context.PAForms.Find(s.FormId).PlanSubmitByAppraisee.ShouldBeTrue();
    }

    [Fact]
    public async Task The_Unlinked_Admin_Should_Not_Be_Restricted()
    {
        var s = Setup();
        _me = null;
        var input = Edit(s.FormId);
        input.ERSubmitByAppraiser = true;
        input.AppraiseeSignedOff = true;

        await CreateAppService().CreateOrEdit(input);

        Context.PAForms.Find(s.FormId).AppraiseeSignedOff.ShouldBeTrue();
    }

    [Fact]
    public async Task An_Employee_Should_Not_Create_A_Form_With_A_Box_Already_Ticked()
    {
        var periodId = CreatePAPeriod("2026");
        var employee = CreateEmployee("E", "1");
        OpenNow(periodId, ActivityType.Plan);
        _me = employee;

        await Should.ThrowAsync<UserFriendlyException>(() => CreateAppService().CreateOrEdit(new CreateOrEditPAFormDto
        {
            PAPeriodId = periodId,
            EmployeeId = employee,
            PlanSubmitByAppraisee = true
        }));
    }

    [Fact]
    public async Task The_Rules_Endpoint_Should_Give_The_Same_Answer_The_Save_Enforces()
    {
        var s = Setup();
        _me = s.Employee;

        var rules = await CreateAppService().GetPAFormTickRules(new NullableIdDto(s.FormId));

        rules.Phase.ShouldBe("Plan");
        rules.Boxes.Where(b => b.Enabled).Select(b => b.Box).ShouldBe(new[] { "PlanSubmitByAppraisee" });
        rules.Boxes.Single(b => b.Box == "Quarter1SubmitByAppraisee").Visible.ShouldBeFalse();
    }

    [Fact]
    public async Task The_Rules_Endpoint_Should_Name_The_List_Boxes_The_Cycle_Hides()
    {
        _appraisalSettings.AppraisalCycle = AppraisalCycle.Annually;
        _me = null;

        var rules = await CreateAppService().GetPAFormTickRules(new NullableIdDto(null));

        rules.HiddenListBoxes.ShouldContain("MRSubmitByAppraiser");
        rules.HiddenListBoxes.ShouldNotContain("PlanSubmitByAppraiser");
    }
}
