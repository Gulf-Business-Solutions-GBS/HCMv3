using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp;
using Abp.Notifications;
using Castle.MicroKernel.Registration;
using HCMv3.GeneralMasterInfo;
using HCMv3.Notifications;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.SystemSecurity.Services;
using HCMv3.SystemSettings;
using HCMv3.Tests.Workflow;
using HCMv3.TimeManagement;
using HCMv3.Workflow;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// The absence-violation job (legacy <c>NotificationService.SendNotification_AbsentViolation</c> and
/// <c>SendNotification_NotReturnViolation</c>). Every test fixes "today" and works on one employee of its own;
/// the seeded employees have no day rows, so they never produce anything.
/// </summary>
// ReSharper disable once InconsistentNaming
public class AbsenceViolationService_Tests : AppTestBase
{
    /// <summary>A Monday. The employee works every day of the week (see <see cref="EssLeaveTestDuty"/>).</summary>
    private static readonly DateTime AbsentDay = new DateTime(2026, 3, 2);

    private readonly IAppNotifier _appNotifier;
    private readonly IEmailService _emailService;
    private readonly IAbsenceViolationService _service;

    public AbsenceViolationService_Tests()
    {
        LoginAsDefaultTenantAdmin();

        _appNotifier = Substitute.For<IAppNotifier>();
        _emailService = Substitute.For<IEmailService>();
        LocalIocManager.IocContainer.Register(Component.For<IAppNotifier>().Instance(_appNotifier).IsDefault());
        LocalIocManager.IocContainer.Register(Component.For<IEmailService>().Instance(_emailService).IsDefault());

        _service = Resolve<IAbsenceViolationService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    #region Absent violation: escalation

    [Fact]
    public async Task Should_Take_Each_Step_On_The_First_Working_Day_Its_Interval_Allows_And_Then_Close()
    {
        var employeeId = CreateEmployee("AV01");
        var ruleId = CreateRuleInTimeSettings();
        AddStep(ruleId, 1, dayInterval: 1);
        AddStep(ruleId, 2, dayInterval: 3);
        var absentRegisterId = AddAbsentDayWithWorkedDaysAfter(employeeId, workedDaysAfter: 5);

        await _service.SendAbsentViolationNotificationsAsync(AbsentDay.AddDays(5));

        var record = RecordFor(ActionLogType.TimeEvent, absentRegisterId);
        record.LogStatus.ShouldBe(LogStatus.Closed);

        var logs = LogsFor(record.Id);
        logs.Select(l => l.Step).ShouldBe(new[] { 1, 2 });
        logs[0].ActionDate.ShouldBe(AbsentDay.AddDays(1));
        logs[1].ActionDate.ShouldBe(AbsentDay.AddDays(3));
        logs.ShouldAllBe(l => l.Action == AbsenceViolationAction.Notification && l.ObjectOId == 0);
    }

    [Fact]
    public async Task Should_Not_Take_A_Step_Before_Its_Day_Interval_Has_Passed()
    {
        var employeeId = CreateEmployee("AV02");
        var ruleId = CreateRuleInTimeSettings();
        AddStep(ruleId, 1, dayInterval: 1);
        AddStep(ruleId, 2, dayInterval: 3);
        var absentRegisterId = AddAbsentDayWithWorkedDaysAfter(employeeId, workedDaysAfter: 5);

        await _service.SendAbsentViolationNotificationsAsync(AbsentDay.AddDays(2));

        var record = RecordFor(ActionLogType.TimeEvent, absentRegisterId);
        record.LogStatus.ShouldBe(LogStatus.Open);
        LogsFor(record.Id).Select(l => l.Step).ShouldBe(new[] { 1 });
    }

    [Fact]
    public async Task Should_Resume_From_The_Last_Step_Taken_On_A_Later_Run()
    {
        var employeeId = CreateEmployee("AV03");
        var ruleId = CreateRuleInTimeSettings();
        AddStep(ruleId, 1, dayInterval: 1);
        AddStep(ruleId, 2, dayInterval: 3);
        var absentRegisterId = AddAbsentDayWithWorkedDaysAfter(employeeId, workedDaysAfter: 5);

        await _service.SendAbsentViolationNotificationsAsync(AbsentDay.AddDays(2));
        await _service.SendAbsentViolationNotificationsAsync(AbsentDay.AddDays(4));

        var record = RecordFor(ActionLogType.TimeEvent, absentRegisterId);
        record.LogStatus.ShouldBe(LogStatus.Closed);
        LogsFor(record.Id).Select(l => l.ActionDate).ShouldBe(new[] { AbsentDay.AddDays(1), AbsentDay.AddDays(3) });
    }

    [Fact]
    public async Task Should_Not_Take_A_Step_Twice_When_Run_Again_The_Same_Day()
    {
        var employeeId = CreateEmployee("AV04");
        var violationTypeId = CreateDisciplinaryType();
        var ruleId = CreateRuleInTimeSettings();
        AddStep(ruleId, 1, dayInterval: 1, action: AbsenceViolationAction.Violation, actionTypeId: violationTypeId);
        AddStep(ruleId, 2, dayInterval: 10);
        var absentRegisterId = AddAbsentDayWithWorkedDaysAfter(employeeId, workedDaysAfter: 3);

        await _service.SendAbsentViolationNotificationsAsync(AbsentDay.AddDays(3));
        await _service.SendAbsentViolationNotificationsAsync(AbsentDay.AddDays(3));

        var record = RecordFor(ActionLogType.TimeEvent, absentRegisterId);
        LogsFor(record.Id).Count.ShouldBe(1);
        DisciplinariesOf(employeeId).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Skip_Days_Off_When_Choosing_The_Day_A_Step_Is_Taken()
    {
        var employeeId = CreateEmployee("AV05");
        var ruleId = CreateRuleInTimeSettings();
        AddStep(ruleId, 1, dayInterval: 1);
        AddStep(ruleId, 2, dayInterval: 2);
        var absentRegisterId = AddDay(employeeId, AbsentDay, punched: false);
        AddDay(employeeId, AbsentDay.AddDays(1), punched: true, weeklyOff: true);
        AddDay(employeeId, AbsentDay.AddDays(2), punched: true);
        AddDay(employeeId, AbsentDay.AddDays(3), punched: true);

        await _service.SendAbsentViolationNotificationsAsync(AbsentDay.AddDays(3));

        var record = RecordFor(ActionLogType.TimeEvent, absentRegisterId);
        LogsFor(record.Id).Select(l => l.ActionDate).ShouldBe(new[] { AbsentDay.AddDays(2), AbsentDay.AddDays(3) });
    }

    [Fact]
    public async Task Should_Take_Nothing_When_The_Rule_Has_No_First_Step()
    {
        var employeeId = CreateEmployee("AV06");
        var ruleId = CreateRuleInTimeSettings();
        AddStep(ruleId, 2, dayInterval: 1);
        var absentRegisterId = AddAbsentDayWithWorkedDaysAfter(employeeId, workedDaysAfter: 3);

        await _service.SendAbsentViolationNotificationsAsync(AbsentDay.AddDays(3));

        ShouldHaveNoRecord(ActionLogType.TimeEvent, absentRegisterId);
    }

    #endregion

    #region Absent violation: HoldTillPunchFound

    [Fact]
    public async Task Should_Hold_A_Step_Until_A_Following_Working_Day_Has_A_Punch()
    {
        var employeeId = CreateEmployee("AV10");
        var ruleId = CreateRuleInTimeSettings();
        AddStep(ruleId, 1, dayInterval: 1, holdTillPunchFound: true);
        var absentRegisterId = AddAbsentDayWithWorkedDaysAfter(employeeId, workedDaysAfter: 3);
        AddTimeEvent(employeeId, AbsentDay.AddDays(2), workflowStatus: null);

        await _service.SendAbsentViolationNotificationsAsync(AbsentDay.AddDays(3));

        var record = RecordFor(ActionLogType.TimeEvent, absentRegisterId);
        LogsFor(record.Id).Single().ActionDate.ShouldBe(AbsentDay.AddDays(2));
        record.LogStatus.ShouldBe(LogStatus.Closed);
    }

    [Fact]
    public async Task Should_Take_No_Held_Step_While_No_Punch_Is_Found()
    {
        var employeeId = CreateEmployee("AV11");
        var ruleId = CreateRuleInTimeSettings();
        AddStep(ruleId, 1, dayInterval: 1, holdTillPunchFound: true);
        var absentRegisterId = AddAbsentDayWithWorkedDaysAfter(employeeId, workedDaysAfter: 3);

        // A missing-punch request still waiting for approval is not a punch the Time Engine counts.
        AddTimeEvent(employeeId, AbsentDay.AddDays(2), workflowStatus: RequestStatus.Open);

        await _service.SendAbsentViolationNotificationsAsync(AbsentDay.AddDays(3));

        ShouldHaveNoRecord(ActionLogType.TimeEvent, absentRegisterId);
    }

    #endregion

    #region Absent violation: excuses

    [Theory]
    [InlineData(null, true)]
    [InlineData(RequestStatus.Approved, true)]
    [InlineData(RequestStatus.Open, true)]
    [InlineData(RequestStatus.Draft, true)]
    [InlineData(RequestStatus.Rejected, false)]
    public async Task Should_Excuse_A_Day_With_A_Punch_Or_Missing_Punch_Request_Unless_Rejected(RequestStatus? status, bool excused)
    {
        var employeeId = CreateEmployee("AV20", AbpSession.UserId);
        var configId = CreateNotificationConfig(sendNotification: true, sendEmail: false);
        var ruleId = CreateRuleInTimeSettings();
        AddStep(ruleId, 1, dayInterval: 1, notificationConfigId: configId);
        var absentRegisterId = AddAbsentDayWithWorkedDaysAfter(employeeId, workedDaysAfter: 2);
        AddTimeEvent(employeeId, AbsentDay, status);

        await _service.SendAbsentViolationNotificationsAsync(AbsentDay.AddDays(2));

        ShouldBeExcused(absentRegisterId, excused);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(RequestStatus.Approved, true)]
    [InlineData(RequestStatus.Open, true)]
    [InlineData(RequestStatus.Draft, true)]
    [InlineData(RequestStatus.Rejected, false)]
    public async Task Should_Excuse_A_Day_Covered_By_Leave_Unless_Rejected(RequestStatus? status, bool excused)
    {
        var employeeId = CreateEmployee("AV21", AbpSession.UserId);
        var absenceTypeId = CreateAbsenceType();
        var configId = CreateNotificationConfig(sendNotification: true, sendEmail: false);
        var ruleId = CreateRuleInTimeSettings();
        AddStep(ruleId, 1, dayInterval: 1, notificationConfigId: configId);
        var absentRegisterId = AddAbsentDayWithWorkedDaysAfter(employeeId, workedDaysAfter: 2);
        AddAbsence(employeeId, absenceTypeId, AbsentDay.AddDays(-1), AbsentDay, status);

        await _service.SendAbsentViolationNotificationsAsync(AbsentDay.AddDays(2));

        ShouldBeExcused(absentRegisterId, excused);
    }

    [Fact]
    public async Task Should_Close_An_Open_Record_When_The_Day_Is_Excused_Later()
    {
        var employeeId = CreateEmployee("AV22");
        var ruleId = CreateRuleInTimeSettings();
        AddStep(ruleId, 1, dayInterval: 1);
        AddStep(ruleId, 2, dayInterval: 5);
        var absentRegisterId = AddAbsentDayWithWorkedDaysAfter(employeeId, workedDaysAfter: 6);

        await _service.SendAbsentViolationNotificationsAsync(AbsentDay.AddDays(1));
        RecordFor(ActionLogType.TimeEvent, absentRegisterId).LogStatus.ShouldBe(LogStatus.Open);

        // The employee asks for the missing punch afterwards.
        AddTimeEvent(employeeId, AbsentDay, RequestStatus.Open);
        await _service.SendAbsentViolationNotificationsAsync(AbsentDay.AddDays(6));

        var record = RecordFor(ActionLogType.TimeEvent, absentRegisterId);
        record.LogStatus.ShouldBe(LogStatus.Closed);
        LogsFor(record.Id).Count.ShouldBe(1);
    }

    #endregion

    #region Absent violation: who and which days

    [Fact]
    public async Task Should_Skip_An_Employee_With_A_Rule_Exception_Covering_Today()
    {
        var employeeId = CreateEmployee("AV30");
        var ruleId = CreateRuleInTimeSettings();
        AddStep(ruleId, 1, dayInterval: 1);
        var absentRegisterId = AddAbsentDayWithWorkedDaysAfter(employeeId, workedDaysAfter: 2);
        AddRuleException(ruleId, employeeId, AbsentDay.AddDays(1), AbsentDay.AddDays(10));

        await _service.SendAbsentViolationNotificationsAsync(AbsentDay.AddDays(2));

        ShouldHaveNoRecord(ActionLogType.TimeEvent, absentRegisterId);
    }

    [Fact]
    public async Task Should_Process_An_Employee_Whose_Rule_Exception_Covers_Only_The_Absent_Day()
    {
        // Legacy tests the exception against today, not against the day that was missed.
        var employeeId = CreateEmployee("AV31");
        var ruleId = CreateRuleInTimeSettings();
        AddStep(ruleId, 1, dayInterval: 1);
        var absentRegisterId = AddAbsentDayWithWorkedDaysAfter(employeeId, workedDaysAfter: 2);
        AddRuleException(ruleId, employeeId, AbsentDay, AbsentDay);

        await _service.SendAbsentViolationNotificationsAsync(AbsentDay.AddDays(2));

        LogsFor(RecordFor(ActionLogType.TimeEvent, absentRegisterId).Id).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Ignore_Days_Before_The_Effective_Date()
    {
        var employeeId = CreateEmployee("AV32");
        var ruleId = CreateRuleInTimeSettings(effectiveDate: AbsentDay.AddDays(1));
        AddStep(ruleId, 1, dayInterval: 1);
        var absentRegisterId = AddAbsentDayWithWorkedDaysAfter(employeeId, workedDaysAfter: 2);

        await _service.SendAbsentViolationNotificationsAsync(AbsentDay.AddDays(2));

        ShouldHaveNoRecord(ActionLogType.TimeEvent, absentRegisterId);
    }

    [Fact]
    public async Task Should_Process_Days_On_The_Effective_Date()
    {
        var employeeId = CreateEmployee("AV33");
        var ruleId = CreateRuleInTimeSettings(effectiveDate: AbsentDay);
        AddStep(ruleId, 1, dayInterval: 1);
        var absentRegisterId = AddAbsentDayWithWorkedDaysAfter(employeeId, workedDaysAfter: 2);

        await _service.SendAbsentViolationNotificationsAsync(AbsentDay.AddDays(2));

        LogsFor(RecordFor(ActionLogType.TimeEvent, absentRegisterId).Id).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Treat_The_Legacy_No_Punch_Date_As_No_Punch()
    {
        var employeeId = CreateEmployee("AV34");
        var ruleId = CreateRuleInTimeSettings();
        AddStep(ruleId, 1, dayInterval: 1);
        var absentRegisterId = AddDay(employeeId, AbsentDay, punched: false, noPunchValue: new DateTime(1800, 1, 1));
        AddDay(employeeId, AbsentDay.AddDays(1), punched: true);

        await _service.SendAbsentViolationNotificationsAsync(AbsentDay.AddDays(1));

        LogsFor(RecordFor(ActionLogType.TimeEvent, absentRegisterId).Id).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Do_Nothing_When_Time_Settings_Name_No_Rule()
    {
        var employeeId = CreateEmployee("AV35");
        var ruleId = CreateRule();
        AddStep(ruleId, 1, dayInterval: 1);
        SetTimeSettings(ruleId: null, effectiveDate: default);
        var absentRegisterId = AddAbsentDayWithWorkedDaysAfter(employeeId, workedDaysAfter: 2);

        await _service.SendAbsentViolationNotificationsAsync(AbsentDay.AddDays(2));

        ShouldHaveNoRecord(ActionLogType.TimeEvent, absentRegisterId);
    }

    #endregion

    #region Absent violation: actions

    [Fact]
    public async Task Should_File_A_Disciplinary_Dated_The_Step_Day_For_A_Violation_Step()
    {
        var employeeId = CreateEmployee("AV40");
        var violationTypeId = CreateDisciplinaryType();
        var ruleId = CreateRuleInTimeSettings();
        AddStep(ruleId, 1, dayInterval: 2, action: AbsenceViolationAction.Violation, actionTypeId: violationTypeId);
        var absentRegisterId = AddAbsentDayWithWorkedDaysAfter(employeeId, workedDaysAfter: 3);

        await _service.SendAbsentViolationNotificationsAsync(AbsentDay.AddDays(3));

        var disciplinary = DisciplinariesOf(employeeId).Single();
        disciplinary.DisciplinaryTypeId.ShouldBe(violationTypeId);
        disciplinary.DisciplinaryDate.ShouldBe(AbsentDay.AddDays(2));
        disciplinary.ReportedById.ShouldBe(employeeId);
        disciplinary.Severity.ShouldBe(Severity.Low);
        disciplinary.WorkflowStatus.ShouldBeNull();
        disciplinary.Comments.ShouldContain("02/03/2026");

        var log = LogsFor(RecordFor(ActionLogType.TimeEvent, absentRegisterId).Id).Single();
        log.Action.ShouldBe(AbsenceViolationAction.Violation);
        log.ObjectOId.ShouldBe(disciplinary.Id);
    }

    [Fact]
    public async Task Should_Book_A_One_Day_Leave_On_The_Absent_Day_For_A_Leave_Step_And_Charge_The_Quota()
    {
        var employeeId = CreateEmployee("AV41");
        var absenceTypeId = CreateAbsenceType();
        CreateQuota(employeeId, absenceTypeId, accrued: 10);
        var ruleId = CreateRuleInTimeSettings();
        AddStep(ruleId, 1, dayInterval: 1, action: AbsenceViolationAction.Leave, actionTypeId: absenceTypeId);
        var absentRegisterId = AddAbsentDayWithWorkedDaysAfter(employeeId, workedDaysAfter: 2);

        await _service.SendAbsentViolationNotificationsAsync(AbsentDay.AddDays(2));

        var leave = LeavesOf(employeeId).Single();
        leave.AbsenceTypeId.ShouldBe(absenceTypeId);
        leave.StartDate.ShouldBe(AbsentDay);
        leave.EndDate.ShouldBe(AbsentDay);
        leave.LeavePaymentType.ShouldBe(LeavePaymentType.None);
        leave.WorkflowStatus.ShouldBeNull();
        leave.TotalLeaveDays.ShouldBe(1);

        QuotaUsed(employeeId, absenceTypeId).ShouldBe(1);

        var log = LogsFor(RecordFor(ActionLogType.TimeEvent, absentRegisterId).Id).Single();
        log.ObjectOId.ShouldBe(leave.Id);
    }

    [Fact]
    public async Task Should_Log_The_Step_With_Nothing_Created_When_Its_Type_Does_Not_Exist()
    {
        var employeeId = CreateEmployee("AV42");
        var ruleId = CreateRuleInTimeSettings();
        AddStep(ruleId, 1, dayInterval: 1, action: AbsenceViolationAction.Violation, actionTypeId: 987654);
        var absentRegisterId = AddAbsentDayWithWorkedDaysAfter(employeeId, workedDaysAfter: 2);

        await _service.SendAbsentViolationNotificationsAsync(AbsentDay.AddDays(2));

        DisciplinariesOf(employeeId).ShouldBeEmpty();
        LogsFor(RecordFor(ActionLogType.TimeEvent, absentRegisterId).Id).Single().ObjectOId.ShouldBe(0);
    }

    #endregion

    #region Notifications

    [Fact]
    public async Task Should_Notify_The_Resolved_Recipients_In_App_And_By_Email()
    {
        var employeeId = CreateEmployee("AV50", AbpSession.UserId);
        var templateId = CreateEmailTemplate();
        var configId = CreateNotificationConfig(sendNotification: true, sendEmail: true, emailTemplateId: templateId);
        var ruleId = CreateRuleInTimeSettings();
        AddStep(ruleId, 1, dayInterval: 1, notificationConfigId: configId);
        var absentRegisterId = AddAbsentDayWithWorkedDaysAfter(employeeId, workedDaysAfter: 2);

        await _service.SendAbsentViolationNotificationsAsync(AbsentDay.AddDays(2));

        await _appNotifier.Received(1).SendMessageAsync(
            Arg.Is<UserIdentifier>(u => u.UserId == AbpSession.UserId && u.TenantId == TenantId),
            Arg.Is<string>(m => m.Contains("AV50")),
            NotificationSeverity.Warn);

        await _emailService.Received(1).SendEmailForNotificationConfigAsync(
            Arg.Is<NotificationConfig>(c => c.Id == configId),
            Arg.Is<EmailTemplate>(t => t.Id == templateId),
            Arg.Is<object>(o => o is TimeRegister && ((TimeRegister)o).Id == absentRegisterId),
            Arg.Any<IDictionary<string, object>>());
    }

    [Fact]
    public async Task Should_Not_Notify_When_The_Step_Has_No_Notification_Config()
    {
        var employeeId = CreateEmployee("AV51", AbpSession.UserId);
        var ruleId = CreateRuleInTimeSettings();
        AddStep(ruleId, 1, dayInterval: 1);
        AddAbsentDayWithWorkedDaysAfter(employeeId, workedDaysAfter: 2);

        await _service.SendAbsentViolationNotificationsAsync(AbsentDay.AddDays(2));

        _appNotifier.ReceivedCalls().ShouldBeEmpty();
        _emailService.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Send_Nothing_For_An_Excused_Day()
    {
        var employeeId = CreateEmployee("AV52", AbpSession.UserId);
        var templateId = CreateEmailTemplate();
        var configId = CreateNotificationConfig(sendNotification: true, sendEmail: true, emailTemplateId: templateId);
        var ruleId = CreateRuleInTimeSettings();
        AddStep(ruleId, 1, dayInterval: 1, notificationConfigId: configId);
        AddAbsentDayWithWorkedDaysAfter(employeeId, workedDaysAfter: 2);
        AddTimeEvent(employeeId, AbsentDay, workflowStatus: null);

        await _service.SendAbsentViolationNotificationsAsync(AbsentDay.AddDays(2));

        _appNotifier.ReceivedCalls().ShouldBeEmpty();
        _emailService.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Still_Log_The_Step_When_Notifying_Fails()
    {
        var employeeId = CreateEmployee("AV53", AbpSession.UserId);
        var templateId = CreateEmailTemplate();
        var configId = CreateNotificationConfig(sendNotification: false, sendEmail: true, emailTemplateId: templateId);
        var ruleId = CreateRuleInTimeSettings();
        AddStep(ruleId, 1, dayInterval: 1, notificationConfigId: configId);
        var absentRegisterId = AddAbsentDayWithWorkedDaysAfter(employeeId, workedDaysAfter: 2);

        _emailService
            .SendEmailForNotificationConfigAsync(default, default, default, default(IDictionary<string, object>))
            .ReturnsForAnyArgs(Task.FromException(new InvalidOperationException("SMTP down")));

        await _service.SendAbsentViolationNotificationsAsync(AbsentDay.AddDays(2));

        LogsFor(RecordFor(ActionLogType.TimeEvent, absentRegisterId).Id).Count.ShouldBe(1);
    }

    #endregion

    #region Not-returned violation

    [Fact]
    public async Task Should_File_A_Disciplinary_Dated_Today_For_A_Leave_Not_Returned_From()
    {
        var employeeId = CreateEmployee("NR01", AbpSession.UserId);
        var violationTypeId = CreateDisciplinaryType();
        var templateId = CreateEmailTemplate();
        var configId = CreateNotificationConfig(sendNotification: true, sendEmail: true, emailTemplateId: templateId);
        var ruleId = CreateRule();
        AddStep(ruleId, 1, dayInterval: 2, action: AbsenceViolationAction.Violation, actionTypeId: violationTypeId,
            notificationConfigId: configId);
        var absenceTypeId = CreateAbsenceType(ruleId);
        var leaveEnd = new DateTime(2026, 3, 5);
        var leaveId = AddAbsence(employeeId, absenceTypeId, leaveEnd.AddDays(-4), leaveEnd, null);
        var today = leaveEnd.AddDays(2);

        await _service.SendNotReturnViolationNotificationsAsync(today);

        var disciplinary = DisciplinariesOf(employeeId).Single();
        disciplinary.DisciplinaryDate.ShouldBe(today);
        disciplinary.DisciplinaryTypeId.ShouldBe(violationTypeId);
        disciplinary.Comments.ShouldContain("not return from leave");

        var record = RecordFor(ActionLogType.Leave, leaveId);
        record.LogStatus.ShouldBe(LogStatus.Closed);
        var log = LogsFor(record.Id).Single();
        log.Step.ShouldBe(1);
        log.ObjectOId.ShouldBe(disciplinary.Id);

        await _appNotifier.Received(1).SendMessageAsync(
            Arg.Is<UserIdentifier>(u => u.UserId == AbpSession.UserId),
            Arg.Any<string>(),
            NotificationSeverity.Warn);

        await _emailService.Received(1).SendEmailForNotificationConfigAsync(
            Arg.Is<NotificationConfig>(c => c.Id == configId),
            Arg.Any<EmailTemplate>(),
            Arg.Is<object>(o => o is EmployeeAbsence && ((EmployeeAbsence)o).Id == leaveId),
            Arg.Is<IDictionary<string, object>>(m => (string)m["EmployeeName"] == "NR01"));
    }

    [Fact]
    public async Task Should_Wait_For_The_Day_Interval_After_The_Leave_Ended()
    {
        var employeeId = CreateEmployee("NR02");
        var ruleId = CreateRule();
        AddStep(ruleId, 1, dayInterval: 3);
        var absenceTypeId = CreateAbsenceType(ruleId);
        var leaveEnd = new DateTime(2026, 3, 5);
        var leaveId = AddAbsence(employeeId, absenceTypeId, leaveEnd.AddDays(-4), leaveEnd, null);

        await _service.SendNotReturnViolationNotificationsAsync(leaveEnd.AddDays(2));

        ShouldHaveNoRecord(ActionLogType.Leave, leaveId);
    }

    [Fact]
    public async Task Should_Take_One_Step_Per_Run_And_Close_After_The_Last()
    {
        var employeeId = CreateEmployee("NR03");
        var ruleId = CreateRule();
        AddStep(ruleId, 1, dayInterval: 1);
        AddStep(ruleId, 2, dayInterval: 2);
        var absenceTypeId = CreateAbsenceType(ruleId);
        var leaveEnd = new DateTime(2026, 3, 5);
        var leaveId = AddAbsence(employeeId, absenceTypeId, leaveEnd.AddDays(-4), leaveEnd, null);

        await _service.SendNotReturnViolationNotificationsAsync(leaveEnd.AddDays(5));
        var record = RecordFor(ActionLogType.Leave, leaveId);
        record.LogStatus.ShouldBe(LogStatus.Open);
        LogsFor(record.Id).Select(l => l.Step).ShouldBe(new[] { 1 });

        await _service.SendNotReturnViolationNotificationsAsync(leaveEnd.AddDays(5));
        record = RecordFor(ActionLogType.Leave, leaveId);
        record.LogStatus.ShouldBe(LogStatus.Closed);
        LogsFor(record.Id).Select(l => l.Step).ShouldBe(new[] { 1, 2 });

        await _service.SendNotReturnViolationNotificationsAsync(leaveEnd.AddDays(6));
        LogsFor(record.Id).Count.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Book_Leave_From_The_Day_After_The_Leave_Ended_Until_Today_Unless_It_Overlaps()
    {
        var employeeId = CreateEmployee("NR04");
        var unpaidTypeId = CreateAbsenceType();
        var ruleId = CreateRule();
        AddStep(ruleId, 1, dayInterval: 1, action: AbsenceViolationAction.Leave, actionTypeId: unpaidTypeId);
        AddStep(ruleId, 2, dayInterval: 2, action: AbsenceViolationAction.Leave, actionTypeId: unpaidTypeId);
        var absenceTypeId = CreateAbsenceType(ruleId);
        var leaveEnd = new DateTime(2026, 3, 5);
        var leaveId = AddAbsence(employeeId, absenceTypeId, leaveEnd.AddDays(-4), leaveEnd, null);

        await _service.SendNotReturnViolationNotificationsAsync(leaveEnd.AddDays(1));

        var booked = LeavesOf(employeeId).Single(l => l.Id != leaveId);
        booked.AbsenceTypeId.ShouldBe(unpaidTypeId);
        booked.StartDate.ShouldBe(leaveEnd.AddDays(1));
        booked.EndDate.ShouldBe(leaveEnd.AddDays(1));

        // The leave this job booked is not "a later leave" that stops the escalation, but step 2's leave
        // would overlap it, so step 2 is logged with nothing booked.
        await _service.SendNotReturnViolationNotificationsAsync(leaveEnd.AddDays(2));

        LeavesOf(employeeId).Count.ShouldBe(2);
        var record = RecordFor(ActionLogType.Leave, leaveId);
        record.LogStatus.ShouldBe(LogStatus.Closed);
        LogsFor(record.Id).Select(l => l.ObjectOId).ShouldBe(new[] { booked.Id, 0 });
    }

    [Theory]
    [InlineData(RequestStatus.Open, true)]
    [InlineData(null, true)]
    [InlineData(RequestStatus.Rejected, false)]
    public async Task Should_Skip_A_Leave_Followed_By_Another_Leave_Unless_Rejected(RequestStatus? laterStatus, bool skipped)
    {
        var employeeId = CreateEmployee("NR05");
        var ruleId = CreateRule();
        AddStep(ruleId, 1, dayInterval: 1);
        var absenceTypeId = CreateAbsenceType(ruleId);
        var otherTypeId = CreateAbsenceType();
        var leaveEnd = new DateTime(2026, 3, 5);
        var leaveId = AddAbsence(employeeId, absenceTypeId, leaveEnd.AddDays(-4), leaveEnd, null);
        AddAbsence(employeeId, otherTypeId, leaveEnd.AddDays(3), leaveEnd.AddDays(5), laterStatus);

        await _service.SendNotReturnViolationNotificationsAsync(leaveEnd.AddDays(2));

        if (skipped)
        {
            ShouldHaveNoRecord(ActionLogType.Leave, leaveId);
        }
        else
        {
            LogsFor(RecordFor(ActionLogType.Leave, leaveId).Id).Count.ShouldBe(1);
        }
    }

    [Fact]
    public async Task Should_Ignore_A_Leave_That_Was_Returned_From()
    {
        var employeeId = CreateEmployee("NR06");
        var ruleId = CreateRule();
        AddStep(ruleId, 1, dayInterval: 1);
        var absenceTypeId = CreateAbsenceType(ruleId);
        var leaveEnd = new DateTime(2026, 3, 5);
        var leaveId = AddAbsence(employeeId, absenceTypeId, leaveEnd.AddDays(-4), leaveEnd, null, returnDate: leaveEnd.AddDays(1));

        await _service.SendNotReturnViolationNotificationsAsync(leaveEnd.AddDays(2));

        ShouldHaveNoRecord(ActionLogType.Leave, leaveId);
    }

    [Fact]
    public async Task Should_Ignore_A_Leave_Still_Awaiting_Approval()
    {
        var employeeId = CreateEmployee("NR07");
        var ruleId = CreateRule();
        AddStep(ruleId, 1, dayInterval: 1);
        var absenceTypeId = CreateAbsenceType(ruleId);
        var leaveEnd = new DateTime(2026, 3, 5);
        var leaveId = AddAbsence(employeeId, absenceTypeId, leaveEnd.AddDays(-4), leaveEnd, RequestStatus.Open);

        await _service.SendNotReturnViolationNotificationsAsync(leaveEnd.AddDays(2));

        ShouldHaveNoRecord(ActionLogType.Leave, leaveId);
    }

    [Fact]
    public async Task Should_Skip_An_Employee_With_A_Rule_Exception_Covering_Today_For_Not_Returned()
    {
        var employeeId = CreateEmployee("NR08");
        var ruleId = CreateRule();
        AddStep(ruleId, 1, dayInterval: 1);
        var absenceTypeId = CreateAbsenceType(ruleId);
        var leaveEnd = new DateTime(2026, 3, 5);
        var leaveId = AddAbsence(employeeId, absenceTypeId, leaveEnd.AddDays(-4), leaveEnd, null);
        AddRuleException(ruleId, employeeId, leaveEnd, leaveEnd.AddDays(5));

        await _service.SendNotReturnViolationNotificationsAsync(leaveEnd.AddDays(2));

        ShouldHaveNoRecord(ActionLogType.Leave, leaveId);
    }

    [Fact]
    public async Task Should_Hold_A_Not_Returned_Step_Until_There_Is_A_Punch_Today()
    {
        var employeeId = CreateEmployee("NR09");
        var ruleId = CreateRule();
        AddStep(ruleId, 1, dayInterval: 1, holdTillPunchFound: true);
        var absenceTypeId = CreateAbsenceType(ruleId);
        var leaveEnd = new DateTime(2026, 3, 5);
        var leaveId = AddAbsence(employeeId, absenceTypeId, leaveEnd.AddDays(-4), leaveEnd, null);

        await _service.SendNotReturnViolationNotificationsAsync(leaveEnd.AddDays(2));
        ShouldHaveNoRecord(ActionLogType.Leave, leaveId);

        AddTimeEvent(employeeId, leaveEnd.AddDays(3), workflowStatus: null);
        await _service.SendNotReturnViolationNotificationsAsync(leaveEnd.AddDays(3));

        LogsFor(RecordFor(ActionLogType.Leave, leaveId).Id).Count.ShouldBe(1);
    }

    #endregion

    #region Arrange helpers

    private int CreateEmployee(string code, long? userId = null)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Set<Nationality>().Add(new Nationality
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
        });

        Employee employee = null;
        UsingDbContext(context =>
        {
            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId,
                Code = code,
                FirstNameEn = code,
                LastNameEn = "T",
                FirstNameAr = "ت",
                LastNameAr = "ت",
                FullNameEn = code + " T",
                FullNameAr = "ت ت",
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                Status = EmployeeStatus.Active,
                UserId = userId,
                NationalityId = nationality.Id
            }).Entity;
        });

        UsingDbContext(context => EssLeaveTestDuty.AssignAllWorkingWeek(context, TenantId, employee.Id));

        return employee.Id;
    }

    private int CreateRule()
    {
        AbsenceViolationRule rule = null;
        UsingDbContext(context =>
        {
            rule = context.Set<AbsenceViolationRule>().Add(new AbsenceViolationRule
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "No-show rule",
                NameAr = "No-show rule"
            }).Entity;
        });

        return rule.Id;
    }

    private int CreateRuleInTimeSettings(DateTime effectiveDate = default)
    {
        var ruleId = CreateRule();
        SetTimeSettings(ruleId, effectiveDate);
        return ruleId;
    }

    private void SetTimeSettings(int? ruleId, DateTime effectiveDate)
    {
        UsingDbContext(context =>
        {
            var settings = context.Set<ManageTimeSettings>().FirstOrDefault(s => s.TenantId == TenantId)
                           ?? context.Set<ManageTimeSettings>().Add(new ManageTimeSettings { TenantId = TenantId }).Entity;

            settings.AbsenceViolationRuleId = ruleId;
            settings.AbsenceViolationEffectiveDate = effectiveDate;
        });
    }

    private void AddStep(
        int ruleId,
        int step,
        int dayInterval,
        AbsenceViolationAction action = AbsenceViolationAction.Notification,
        int? actionTypeId = null,
        bool holdTillPunchFound = false,
        int? notificationConfigId = null)
    {
        UsingDbContext(context => context.Set<AbsenceViolationRuleDetail>().Add(new AbsenceViolationRuleDetail
        {
            TenantId = TenantId,
            AbsenceViolationRuleId = ruleId,
            Step = step,
            DayInterval = dayInterval,
            Action = action,
            ActionTypeId = actionTypeId,
            HoldTillPunchFound = holdTillPunchFound,
            NotificationConfigId = notificationConfigId
        }));
    }

    private void AddRuleException(int ruleId, int employeeId, DateTime startDate, DateTime endDate)
    {
        UsingDbContext(context => context.Set<AbsenceViolationRuleException>().Add(new AbsenceViolationRuleException
        {
            TenantId = TenantId,
            AbsenceViolationRuleId = ruleId,
            EmployeeId = employeeId,
            StartDate = startDate,
            EndDate = endDate
        }));
    }

    /// <summary>
    /// The absent day with no punch, then <paramref name="workedDaysAfter"/> worked (punched) days -- punched so
    /// that they are not absent days of their own.
    /// </summary>
    private int AddAbsentDayWithWorkedDaysAfter(int employeeId, int workedDaysAfter)
    {
        var absentRegisterId = AddDay(employeeId, AbsentDay, punched: false);

        for (var i = 1; i <= workedDaysAfter; i++)
        {
            AddDay(employeeId, AbsentDay.AddDays(i), punched: true);
        }

        return absentRegisterId;
    }

    private int AddDay(int employeeId, DateTime day, bool punched, bool weeklyOff = false, DateTime noPunchValue = default)
    {
        TimeRegister register = null;
        UsingDbContext(context =>
        {
            register = context.Set<TimeRegister>().Add(new TimeRegister
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                DayDate = day,
                ActualIn = punched ? day.AddHours(8) : noPunchValue,
                ActualOut = punched ? day.AddHours(16) : noPunchValue,
                WeeklyOff = weeklyOff ? 1 : 0
            }).Entity;
        });

        return register.Id;
    }

    private void AddTimeEvent(int employeeId, DateTime day, RequestStatus? workflowStatus)
    {
        UsingDbContext(context =>
        {
            var transType = context.Set<TransType>().Add(new TransType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = "In",
                NameAr = "In"
            }).Entity;

            context.SaveChanges();

            context.Set<TimeEvent>().Add(new TimeEvent
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                TransTypeId = transType.Id,
                TransDate = day,
                TransTime = day.AddHours(8),
                RecordSource = day.Ticks,
                WorkflowStatus = workflowStatus
            });
        });
    }

    private int CreateAbsenceType(int? violationRuleId = null)
    {
        AbsenceType absenceType = null;
        UsingDbContext(context =>
        {
            absenceType = context.Set<AbsenceType>().Add(new AbsenceType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Annual",
                NameAr = "Annual",
                IncludeWeekend = true,
                IncludePublicHoliday = true,
                IncludeRestDay = true,
                CanApplyAgain = true,
                AllowDuringProbation = true,
                AbsenceViolationRuleId = violationRuleId
            }).Entity;
        });

        return absenceType.Id;
    }

    private int AddAbsence(
        int employeeId,
        int absenceTypeId,
        DateTime startDate,
        DateTime endDate,
        RequestStatus? workflowStatus,
        DateTime returnDate = default)
    {
        EmployeeAbsence absence = null;
        UsingDbContext(context =>
        {
            absence = context.Set<EmployeeAbsence>().Add(new EmployeeAbsence
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                AbsenceTypeId = absenceTypeId,
                StartDate = startDate,
                EndDate = endDate,
                InitialEndDate = endDate,
                CalendarDays = (endDate - startDate).TotalDays + 1,
                TotalLeaveDays = (endDate - startDate).TotalDays + 1,
                ReturnDate = returnDate,
                IsReturned = returnDate != default,
                WorkflowStatus = workflowStatus
            }).Entity;
        });

        return absence.Id;
    }

    private void CreateQuota(int employeeId, int absenceTypeId, double accrued)
    {
        UsingDbContext(context => context.Set<EmployeeAbsenceQuota>().Add(new EmployeeAbsenceQuota
        {
            TenantId = TenantId,
            EmployeeId = employeeId,
            AbsenceTypeId = absenceTypeId,
            EntitlementFrom = AbsentDay.AddYears(-1),
            EntitlementTo = AbsentDay.AddYears(1),
            DeductionFrom = AbsentDay.AddYears(-1),
            DeductionTo = AbsentDay.AddYears(1),
            QuotaAccrued = accrued,
            QuotaUsed = 0
        }));
    }

    private int CreateDisciplinaryType()
    {
        DisciplinaryType type = null;
        UsingDbContext(context =>
        {
            type = context.Set<DisciplinaryType>().Add(new DisciplinaryType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Absence without notice",
                NameAr = "Absence without notice"
            }).Entity;
        });

        return type.Id;
    }

    private int CreateEmailTemplate()
    {
        EmailTemplate template = null;
        UsingDbContext(context =>
        {
            template = context.Set<EmailTemplate>().Add(new EmailTemplate
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Absence violation",
                NameAr = "Absence violation",
                Subject = "Absence violation"
            }).Entity;
        });

        return template.Id;
    }

    private int CreateNotificationConfig(bool sendNotification, bool sendEmail, int? emailTemplateId = null)
    {
        NotificationConfig config = null;
        UsingDbContext(context =>
        {
            config = context.Set<NotificationConfig>().Add(new NotificationConfig
            {
                TenantId = TenantId,
                Name = "Cfg " + Guid.NewGuid(),
                SendNotification = sendNotification,
                SendEmail = sendEmail,
                ToEmployee = true,
                EmailTemplateId = emailTemplateId
            }).Entity;
        });

        return config.Id;
    }

    #endregion

    #region Assert helpers

    private AbsenceViolationActionRecord RecordFor(ActionLogType type, int entityObjectId)
    {
        return UsingDbContext(context => context.Set<AbsenceViolationActionRecord>()
            .Single(r => r.ActionLogType == type && r.EntityObjectId == entityObjectId));
    }

    private void ShouldHaveNoRecord(ActionLogType type, int entityObjectId)
    {
        UsingDbContext(context => context.Set<AbsenceViolationActionRecord>()
            .Any(r => r.ActionLogType == type && r.EntityObjectId == entityObjectId))
            .ShouldBeFalse();
    }

    private void ShouldBeExcused(int absentRegisterId, bool excused)
    {
        var record = RecordFor(ActionLogType.TimeEvent, absentRegisterId);
        var logCount = LogsFor(record.Id).Count;

        if (excused)
        {
            record.LogStatus.ShouldBe(LogStatus.Closed);
            logCount.ShouldBe(0);
            _appNotifier.ReceivedCalls().ShouldBeEmpty();
        }
        else
        {
            logCount.ShouldBe(1);
            _appNotifier.ReceivedCalls().ShouldNotBeEmpty();
        }
    }

    private List<AbsenceViolationActionLog> LogsFor(int recordId)
    {
        return UsingDbContext(context => context.Set<AbsenceViolationActionLog>()
            .Where(l => l.AbsenceViolationActionRecordId == recordId)
            .OrderBy(l => l.Step)
            .ToList());
    }

    private List<EmployeeDisciplinary> DisciplinariesOf(int employeeId)
    {
        return UsingDbContext(context => context.Set<EmployeeDisciplinary>()
            .IgnoreQueryFilters()
            .Where(d => d.EmployeeId == employeeId && !d.IsDeleted)
            .ToList());
    }

    private List<EmployeeAbsence> LeavesOf(int employeeId)
    {
        return UsingDbContext(context => context.Set<EmployeeAbsence>()
            .IgnoreQueryFilters()
            .Where(a => a.EmployeeId == employeeId && !a.IsDeleted)
            .OrderBy(a => a.StartDate)
            .ToList());
    }

    private double QuotaUsed(int employeeId, int absenceTypeId)
    {
        return UsingDbContext(context => context.Set<EmployeeAbsenceQuota>()
            .IgnoreQueryFilters()
            .Single(q => q.EmployeeId == employeeId && q.AbsenceTypeId == absenceTypeId)
            .QuotaUsed);
    }

    #endregion
}
