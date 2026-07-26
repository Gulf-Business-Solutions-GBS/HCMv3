using System;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Abp.Net.Mail;
using Castle.MicroKernel.Registration;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.SystemSecurity.Services;
using HCMv3.SystemSettings;
using HCMv3.TimeManagement;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSecurity;

// ReSharper disable once InconsistentNaming
public class NotificationDispatchServiceTests : AppTestBase
{
    private readonly IEmailSender _fakeEmailSender;
    private readonly INotificationDispatchService _notificationDispatchService;

    public NotificationDispatchServiceTests()
    {
        LoginAsDefaultTenantAdmin();

        _fakeEmailSender = Substitute.For<IEmailSender>();
        _fakeEmailSender.SendAsync(Arg.Any<MailMessage>()).Returns(Task.CompletedTask);
        LocalIocManager.IocContainer.Register(Component.For<IEmailSender>().Instance(_fakeEmailSender).IsDefault());

        _notificationDispatchService = Resolve<INotificationDispatchService>();
    }

    private int CreateNationality()
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Set<Nationality>().Add(new Nationality
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
        });
        return nationality.Id;
    }

    private Employee CreateEmployee(string code, string email, DateTime birthDate, DateTime joiningDate)
    {
        var nationalityId = CreateNationality();
        Employee employee = null;

        UsingDbContext(context =>
        {
            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                FirstNameEn = "First_" + code,
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "Full " + code,
                FullNameAr = "ف ل",
                BirthDate = birthDate,
                JoiningDate = joiningDate,
                Email = email,
                Status = EmployeeStatus.Active,
                NationalityId = nationalityId
            }).Entity;
        });

        return employee;
    }

    private int CreateNotificationConfig(bool sendEmail)
    {
        NotificationConfig config = null;
        UsingDbContext(context =>
        {
            config = context.Set<NotificationConfig>().Add(new NotificationConfig
            {
                TenantId = AbpSession.TenantId ?? 1,
                Name = "Cfg " + Guid.NewGuid(),
                SendEmail = sendEmail,
                ToEmployee = true
            }).Entity;
        });
        return config.Id;
    }

    private void CreateSystemNotification(string name, int notificationConfigId, bool isActive)
    {
        UsingDbContext(context =>
        {
            context.Set<SystemNotification>().Add(new SystemNotification
            {
                TenantId = AbpSession.TenantId ?? 1,
                NotificationName = name,
                NotificationConfigId = notificationConfigId,
                IsActive = isActive
            });
        });
    }

    private int CreateAbsenceType(bool isPaid, bool isReturnDateRequired)
    {
        AbsenceType absenceType = null;
        UsingDbContext(context =>
        {
            absenceType = context.Set<AbsenceType>().Add(new AbsenceType
            {
                TenantId = AbpSession.TenantId ?? 1,
                NameEn = "AT " + Guid.NewGuid(),
                NameAr = "AT",
                IsActive = true,
                IsPaid = isPaid,
                IsReturnDateRequired = isReturnDateRequired
            }).Entity;
        });
        return absenceType.Id;
    }

    private void CreateEmployeeAbsence(
        int employeeId,
        int absenceTypeId,
        DateTime startDate,
        DateTime endDate,
        bool isReturned = false,
        DateTime? returnDate = null,
        LeavePaymentType leavePaymentType = LeavePaymentType.None,
        DateTime? paymentDate = null,
        bool advanceLeavePaid = false)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeAbsence>().Add(new EmployeeAbsence
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeId = employeeId,
                AbsenceTypeId = absenceTypeId,
                StartDate = startDate,
                EndDate = endDate,
                IsReturned = isReturned,
                ReturnDate = returnDate ?? default,
                LeavePaymentType = leavePaymentType,
                PaymentDate = paymentDate ?? default,
                AdvanceLeavePaid = advanceLeavePaid
            });
        });
    }

    private void CreateManageNotificationSettings(AdvancePaymentReminderDateType reminderDateType)
    {
        UsingDbContext(context =>
        {
            context.Set<ManageNotificationSettings>().Add(new ManageNotificationSettings
            {
                TenantId = AbpSession.TenantId ?? 1,
                AdvancePaymentReminderDateType = reminderDateType
            });
        });
    }

    private void CreateSystemNotificationWithReminderDays(string name, int notificationConfigId, bool isActive, int reminderDays)
    {
        UsingDbContext(context =>
        {
            context.Set<SystemNotification>().Add(new SystemNotification
            {
                TenantId = AbpSession.TenantId ?? 1,
                NotificationName = name,
                NotificationConfigId = notificationConfigId,
                IsActive = isActive,
                ReminderDays = reminderDays
            });
        });
    }

    private int CreateTransType()
    {
        TransType transType = null;
        UsingDbContext(context =>
        {
            transType = context.Set<TransType>().Add(new TransType
            {
                TenantId = AbpSession.TenantId ?? 1,
                NameEn = "TT " + Guid.NewGuid(),
                NameAr = "TT"
            }).Entity;
        });
        return transType.Id;
    }

    private void CreateTimeEvent(int employeeId, DateTime transDate)
    {
        var transTypeId = CreateTransType();
        UsingDbContext(context =>
        {
            context.Set<TimeEvent>().Add(new TimeEvent
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeId = employeeId,
                TransTypeId = transTypeId,
                TransDate = transDate,
                TransTime = transDate
            });
        });
    }

    private void CreateEmailTemplate(string code)
    {
        UsingDbContext(context =>
        {
            context.Set<EmailTemplate>().Add(new EmailTemplate
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code,
                Subject = "Subject for " + code,
                EmailBodyHtml = "<p>Body $#EmployeeName#$</p>"
            });
        });
    }

    [Fact]
    public async Task Should_Send_Birthday_Email_To_Employee_With_Matching_BirthDate()
    {
        var today = DateTime.Now.Date;
        var employee = CreateEmployee("BD100", "bd100@example.com",
            new DateTime(1990, today.Month, today.Day), new DateTime(2015, 1, 1));

        var configId = CreateNotificationConfig(sendEmail: true);
        CreateSystemNotification(NotificationDispatchService.BirthdayNotificationName, configId, isActive: true);
        CreateEmailTemplate(NotificationDispatchService.BirthdayNotificationName);

        await _notificationDispatchService.SendBirthdayNotificationsAsync();

        await _fakeEmailSender.Received(1).SendAsync(Arg.Is<MailMessage>(m => m.To[0].Address == employee.Email));
    }

    [Fact]
    public async Task Should_Not_Send_Birthday_Email_When_SystemNotification_Inactive()
    {
        var today = DateTime.Now.Date;
        CreateEmployee("BD101", "bd101@example.com",
            new DateTime(1990, today.Month, today.Day), new DateTime(2015, 1, 1));

        var configId = CreateNotificationConfig(sendEmail: true);
        CreateSystemNotification(NotificationDispatchService.BirthdayNotificationName, configId, isActive: false);
        CreateEmailTemplate(NotificationDispatchService.BirthdayNotificationName);

        await _notificationDispatchService.SendBirthdayNotificationsAsync();

        await _fakeEmailSender.DidNotReceive().SendAsync(Arg.Any<MailMessage>());
    }

    [Fact]
    public async Task Should_Send_Anniversary_Email_On_Matching_Anniversary_Date()
    {
        var today = DateTime.Now.Date;
        var joiningDate = new DateTime(today.Year - 3, today.Month, today.Day);
        var employee = CreateEmployee("AN100", "an100@example.com", new DateTime(1990, 1, 1), joiningDate);

        var configId = CreateNotificationConfig(sendEmail: true);
        CreateSystemNotification(NotificationDispatchService.AnniversaryNotificationName, configId, isActive: true);
        CreateEmailTemplate(NotificationDispatchService.AnniversaryNotificationName);

        await _notificationDispatchService.SendAnniversaryNotificationsAsync();

        await _fakeEmailSender.Received(1).SendAsync(Arg.Is<MailMessage>(m => m.To[0].Address == employee.Email));
    }

    [Fact]
    public async Task Should_Not_Send_Anniversary_Email_When_Date_Does_Not_Match()
    {
        var today = DateTime.Now.Date;
        var joiningDate = new DateTime(today.Year - 3, today.Month == 12 ? 1 : today.Month + 1, 1);
        CreateEmployee("AN101", "an101@example.com", new DateTime(1990, 1, 1), joiningDate);

        var configId = CreateNotificationConfig(sendEmail: true);
        CreateSystemNotification(NotificationDispatchService.AnniversaryNotificationName, configId, isActive: true);
        CreateEmailTemplate(NotificationDispatchService.AnniversaryNotificationName);

        await _notificationDispatchService.SendAnniversaryNotificationsAsync();

        await _fakeEmailSender.DidNotReceive().SendAsync(Arg.Any<MailMessage>());
    }

    [Fact]
    public async Task Should_Not_Send_When_No_Matching_EmailTemplate_Configured()
    {
        var today = DateTime.Now.Date;
        CreateEmployee("BD102", "bd102@example.com",
            new DateTime(1990, today.Month, today.Day), new DateTime(2015, 1, 1));

        var configId = CreateNotificationConfig(sendEmail: true);
        CreateSystemNotification(NotificationDispatchService.BirthdayNotificationName, configId, isActive: true);
        // no EmailTemplate created

        await _notificationDispatchService.SendBirthdayNotificationsAsync();

        await _fakeEmailSender.DidNotReceive().SendAsync(Arg.Any<MailMessage>());
    }

    [Fact]
    public async Task Should_Send_AdvancePayment_Email_When_StartDate_Reminder_Matches()
    {
        var today = DateTime.Now.Date;
        var employee = CreateEmployee("AP100", "ap100@example.com", new DateTime(1990, 1, 1), new DateTime(2015, 1, 1));
        var absenceTypeId = CreateAbsenceType(isPaid: true, isReturnDateRequired: false);

        CreateManageNotificationSettings(AdvancePaymentReminderDateType.LeaveStartDate);
        CreateEmployeeAbsence(
            employee.Id,
            absenceTypeId,
            startDate: today.AddDays(3),
            endDate: today.AddDays(10),
            leavePaymentType: LeavePaymentType.PayInAdvance,
            advanceLeavePaid: false);

        var configId = CreateNotificationConfig(sendEmail: true);
        CreateSystemNotificationWithReminderDays(NotificationDispatchService.AdvancePaymentNotificationName, configId, isActive: true, reminderDays: 3);
        CreateEmailTemplate(NotificationDispatchService.AdvancePaymentNotificationName);

        await _notificationDispatchService.SendAdvancePaymentNotificationsAsync();

        await _fakeEmailSender.Received(1).SendAsync(Arg.Is<MailMessage>(m => m.To[0].Address == employee.Email));
    }

    [Fact]
    public async Task Should_Not_Send_AdvancePayment_Email_When_AdvanceLeavePaid_Already_True()
    {
        var today = DateTime.Now.Date;
        var employee = CreateEmployee("AP101", "ap101@example.com", new DateTime(1990, 1, 1), new DateTime(2015, 1, 1));
        var absenceTypeId = CreateAbsenceType(isPaid: true, isReturnDateRequired: false);

        CreateManageNotificationSettings(AdvancePaymentReminderDateType.LeaveStartDate);
        CreateEmployeeAbsence(
            employee.Id,
            absenceTypeId,
            startDate: today.AddDays(3),
            endDate: today.AddDays(10),
            leavePaymentType: LeavePaymentType.PayInAdvance,
            advanceLeavePaid: true);

        var configId = CreateNotificationConfig(sendEmail: true);
        CreateSystemNotificationWithReminderDays(NotificationDispatchService.AdvancePaymentNotificationName, configId, isActive: true, reminderDays: 3);
        CreateEmailTemplate(NotificationDispatchService.AdvancePaymentNotificationName);

        await _notificationDispatchService.SendAdvancePaymentNotificationsAsync();

        await _fakeEmailSender.DidNotReceive().SendAsync(Arg.Any<MailMessage>());
    }

    [Fact]
    public async Task Should_Send_LeaveResumption_Email_For_NotReturned_Employee_Past_EndDate()
    {
        var today = DateTime.Now.Date;
        var employee = CreateEmployee("LR100", "lr100@example.com", new DateTime(1990, 1, 1), new DateTime(2015, 1, 1));
        var absenceTypeId = CreateAbsenceType(isPaid: false, isReturnDateRequired: true);

        CreateEmployeeAbsence(
            employee.Id,
            absenceTypeId,
            startDate: today.AddDays(-10),
            endDate: today.AddDays(-1),
            isReturned: false);

        var configId = CreateNotificationConfig(sendEmail: true);
        CreateSystemNotification(NotificationDispatchService.LeaveResumptionNotificationName, configId, isActive: true);
        CreateEmailTemplate(NotificationDispatchService.LeaveResumptionNotificationName);

        await _notificationDispatchService.SendLeaveResumptionNotificationsAsync();

        await _fakeEmailSender.Received(1).SendAsync(Arg.Is<MailMessage>(m => m.To[0].Address == employee.Email));
    }

    [Fact]
    public async Task Should_Not_Send_LeaveResumption_Email_When_Employee_Already_Returned()
    {
        var today = DateTime.Now.Date;
        var employee = CreateEmployee("LR101", "lr101@example.com", new DateTime(1990, 1, 1), new DateTime(2015, 1, 1));
        var absenceTypeId = CreateAbsenceType(isPaid: false, isReturnDateRequired: true);

        CreateEmployeeAbsence(
            employee.Id,
            absenceTypeId,
            startDate: today.AddDays(-10),
            endDate: today.AddDays(-1),
            isReturned: true,
            returnDate: today.AddDays(-1));

        var configId = CreateNotificationConfig(sendEmail: true);
        CreateSystemNotification(NotificationDispatchService.LeaveResumptionNotificationName, configId, isActive: true);
        CreateEmailTemplate(NotificationDispatchService.LeaveResumptionNotificationName);

        await _notificationDispatchService.SendLeaveResumptionNotificationsAsync();

        await _fakeEmailSender.DidNotReceive().SendAsync(Arg.Any<MailMessage>());
    }

    [Fact]
    public async Task Should_Send_PunchDuringLeave_Email_When_TimeEvent_Exists_In_Leave_Range()
    {
        var today = DateTime.Now.Date;
        var employee = CreateEmployee("PD100", "pd100@example.com", new DateTime(1990, 1, 1), new DateTime(2015, 1, 1));
        var absenceTypeId = CreateAbsenceType(isPaid: false, isReturnDateRequired: true);

        CreateEmployeeAbsence(
            employee.Id,
            absenceTypeId,
            startDate: today.AddDays(-5),
            endDate: today.AddDays(5),
            isReturned: false);

        CreateTimeEvent(employee.Id, today);

        var configId = CreateNotificationConfig(sendEmail: true);
        CreateSystemNotification(NotificationDispatchService.PunchDuringLeaveNotificationName, configId, isActive: true);
        CreateEmailTemplate(NotificationDispatchService.PunchDuringLeaveNotificationName);

        await _notificationDispatchService.SendPunchDuringLeaveNotificationsAsync();

        await _fakeEmailSender.Received(1).SendAsync(Arg.Is<MailMessage>(m => m.To[0].Address == employee.Email));
    }

    [Fact]
    public async Task Should_Not_Send_PunchDuringLeave_Email_When_No_TimeEvent_In_Leave_Range()
    {
        var today = DateTime.Now.Date;
        var employee = CreateEmployee("PD101", "pd101@example.com", new DateTime(1990, 1, 1), new DateTime(2015, 1, 1));
        var absenceTypeId = CreateAbsenceType(isPaid: false, isReturnDateRequired: true);

        CreateEmployeeAbsence(
            employee.Id,
            absenceTypeId,
            startDate: today.AddDays(-5),
            endDate: today.AddDays(5),
            isReturned: false);

        // No TimeEvent created -- no punch recorded during leave.

        var configId = CreateNotificationConfig(sendEmail: true);
        CreateSystemNotification(NotificationDispatchService.PunchDuringLeaveNotificationName, configId, isActive: true);
        CreateEmailTemplate(NotificationDispatchService.PunchDuringLeaveNotificationName);

        await _notificationDispatchService.SendPunchDuringLeaveNotificationsAsync();

        await _fakeEmailSender.DidNotReceive().SendAsync(Arg.Any<MailMessage>());
    }
}
