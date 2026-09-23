using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.Ess.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// The time-settings toggles that shape leave requests, as legacy applied them:
/// <c>EnableBookingQuotaInLeaveRequest</c> (LeaveRequest.PerformValidation and the "Get Booked Quota" block of
/// EmployeeAbsenceController), <c>EnablePayFullWithPayroll</c> (WorkFlowServices.LeavePaymentTypeList), and the
/// settings the leave form reads.
/// </summary>
// ReSharper disable once InconsistentNaming
public class LeaveSettingsToggles_Tests : EssLeaveWorkflowTestBase
{
    private int _meId;

    private int GivenAnEmployee(string code)
    {
        _meId = CreateCurrentUserEmployee(code);
        EnableLeaveWorkflowWithApprover(code + "APP");
        return _meId;
    }

    private void SetTimeSettings(Action<ManageTimeSettings> configure)
    {
        UsingDbContext(context =>
        {
            var settings = context.Set<ManageTimeSettings>().FirstOrDefault(s => s.TenantId == TenantId);
            if (settings == null)
            {
                settings = context.Set<ManageTimeSettings>().Add(new ManageTimeSettings
                {
                    TenantId = TenantId,
                    AllowFutureDatedReturnFromLeave = true
                }).Entity;
            }

            configure(settings);
        });
    }

    private Task<ApplyForLeaveResultDto> ApplyAsync(int typeId, DateTime start, int days = 3,
        LeavePaymentType paymentType = LeavePaymentType.PayWithPayroll) =>
        _myLeaveAppService.ApplyForLeave(new ApplyForLeaveInput
        {
            AbsenceTypeId = typeId,
            StartDate = start,
            EndDate = start.AddDays(days - 1),
            LeavePaymentType = paymentType
        });

    private static DateTime Soon => DateTime.Today.AddDays(30);

    // ------------------------------------------------------------------ EnableBookingQuotaInLeaveRequest

    [Fact]
    public async Task With_Booking_Quota_On_A_Pending_Request_Reduces_The_Bookable_Balance()
    {
        GivenAnEmployee("BQ100");
        SetTimeSettings(s => s.EnableBookingQuotaInLeaveRequest = true);
        var typeId = CreateAbsenceType(t => { t.IncludeWeekend = true; t.IncludePublicHoliday = true; t.IncludeRestDay = true; });
        CreateQuota(_meId, typeId, accrued: 30);

        await ApplyAsync(typeId, Soon, days: 3);

        var balance = (await _myLeaveAppService.GetMyBalances()).Single(b => b.AbsenceTypeId == typeId);
        balance.Bookable.ShouldBe(27);
    }

    [Fact]
    public async Task With_Booking_Quota_Off_A_Pending_Request_Does_Not_Reduce_The_Bookable_Balance()
    {
        GivenAnEmployee("BQ110");
        SetTimeSettings(s => s.EnableBookingQuotaInLeaveRequest = false);
        var typeId = CreateAbsenceType(t => { t.IncludeWeekend = true; t.IncludePublicHoliday = true; t.IncludeRestDay = true; });
        CreateQuota(_meId, typeId, accrued: 30);

        await ApplyAsync(typeId, Soon, days: 3);

        var balance = (await _myLeaveAppService.GetMyBalances()).Single(b => b.AbsenceTypeId == typeId);
        balance.Bookable.ShouldBe(30);
    }

    /// <summary>Legacy off-branch: a pending request of the same type blocks a new one whatever its dates.</summary>
    [Fact]
    public async Task With_Booking_Quota_Off_A_Second_Request_Of_The_Same_Type_Is_Refused_While_The_First_Is_Pending()
    {
        GivenAnEmployee("BQ120");
        SetTimeSettings(s => s.EnableBookingQuotaInLeaveRequest = false);
        var typeId = CreateAbsenceType(t => { t.IncludeWeekend = true; t.IncludePublicHoliday = true; t.IncludeRestDay = true; t.CanApplyAgain = true; });
        CreateQuota(_meId, typeId, accrued: 30);

        await ApplyAsync(typeId, Soon, days: 2);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => ApplyAsync(typeId, Soon.AddDays(20), days: 2));
        exception.Message.ShouldContain("pending");
    }

    /// <summary>Legacy on-branch: only an OVERLAPPING pending request blocks; other dates are fine.</summary>
    [Fact]
    public async Task With_Booking_Quota_On_A_Second_Request_Of_The_Same_Type_On_Other_Dates_Is_Allowed()
    {
        GivenAnEmployee("BQ130");
        SetTimeSettings(s => s.EnableBookingQuotaInLeaveRequest = true);
        var typeId = CreateAbsenceType(t => { t.IncludeWeekend = true; t.IncludePublicHoliday = true; t.IncludeRestDay = true; t.CanApplyAgain = true; });
        CreateQuota(_meId, typeId, accrued: 30);

        await ApplyAsync(typeId, Soon, days: 2);
        var second = await ApplyAsync(typeId, Soon.AddDays(20), days: 2);

        second.EmployeeAbsenceId.ShouldBeGreaterThan(0);

        await Assert.ThrowsAsync<UserFriendlyException>(() => ApplyAsync(typeId, Soon.AddDays(1), days: 2));
    }

    // ------------------------------------------------------------------ EnablePayFullWithPayroll

    [Fact]
    public async Task Pay_Full_With_Payroll_Is_Refused_Unless_Time_Settings_Enable_It()
    {
        GivenAnEmployee("PF100");
        var typeId = CreateAbsenceType(t => { t.IncludeWeekend = true; t.IncludePublicHoliday = true; t.IncludeRestDay = true; });
        CreateQuota(_meId, typeId, accrued: 30);

        SetTimeSettings(s => s.EnablePayFullWithPayroll = false);
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() =>
            ApplyAsync(typeId, Soon, paymentType: LeavePaymentType.PayFullWithPayroll));
        exception.Message.ShouldContain("Pay Full With Payroll");

        SetTimeSettings(s => s.EnablePayFullWithPayroll = true);
        var result = await ApplyAsync(typeId, Soon, paymentType: LeavePaymentType.PayFullWithPayroll);
        result.EmployeeAbsenceId.ShouldBeGreaterThan(0);
    }

    // ------------------------------------------------------------------ the leave form settings

    [Fact]
    public async Task GetLeaveFormSettings_Should_Reflect_The_Tenant_Settings()
    {
        GivenAnEmployee("LF100");
        SetTimeSettings(s =>
        {
            s.EnableAutoHalfDayLeave = true;
            s.EnablePayFullWithPayroll = true;
            s.EnableIsTravelling = true;
            s.EnableBookingQuotaInLeaveRequest = true;
        });
        UsingDbContext(context =>
        {
            var payroll = context.Set<ManagePayrollSettings>().FirstOrDefault(s => s.TenantId == TenantId)
                          ?? context.Set<ManagePayrollSettings>().Add(new ManagePayrollSettings { TenantId = TenantId }).Entity;
            payroll.DefaultLeavePaymentType = LeavePaymentType.PayInAdvance;

            var workflow = context.Set<ManageWorkflowSettings>().FirstOrDefault(s => s.TenantId == TenantId)
                           ?? context.Set<ManageWorkflowSettings>().Add(new ManageWorkflowSettings { TenantId = TenantId }).Entity;
            workflow.HideLeavePayDetailsInWorkflow = true;
        });

        var settings = await _myLeaveAppService.GetLeaveFormSettings();

        settings.EnableAutoHalfDayLeave.ShouldBeTrue();
        settings.EnablePayFullWithPayroll.ShouldBeTrue();
        settings.EnableIsTravelling.ShouldBeTrue();
        settings.EnableBookingQuotaInLeaveRequest.ShouldBeTrue();
        settings.DefaultLeavePaymentType.ShouldBe(LeavePaymentType.PayInAdvance);
        settings.HideLeavePayDetailsInWorkflow.ShouldBeTrue();
    }
}
