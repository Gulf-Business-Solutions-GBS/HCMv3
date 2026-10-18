using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp;
using Abp.Localization;
using Abp.Notifications;
using HCMv3.GeneralMasterInfo;
using HCMv3.Notifications;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.SystemSecurity.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// Legacy <c>PayrollControlRecordController</c> Release / Exit / Release-for-correction notifications. Several
/// of these tests pin legacy quirks on purpose (no group means no send; one send per group); see
/// <see cref="PayrollPeriodNotificationService"/> remarks.
/// </summary>
// ReSharper disable once InconsistentNaming
public class PayrollPeriodNotificationService_Tests : PayrollManagementAppServiceTestBase
{
    private const long RecipientUserId = 42;

    private readonly INotificationRecipientResolver _resolver = Substitute.For<INotificationRecipientResolver>();
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly IAppNotifier _appNotifier = Substitute.For<IAppNotifier>();

    public PayrollPeriodNotificationService_Tests()
    {
        _resolver.Resolve(Arg.Any<NotificationConfig>(), Arg.Any<object>(), Arg.Any<string>())
            .Returns(new List<Employee> { new Employee { Id = 7, UserId = RecipientUserId } });
    }

    private PayrollPeriodNotificationService CreateService()
    {
        var service = new PayrollPeriodNotificationService(
            FakeRepository.For<PayrollArea>(Context),
            FakeRepository.For<PayrollControlRecord>(Context),
            FakeRepository.For<NotificationConfig>(Context),
            FakeRepository.For<NotificationGroup>(Context),
            FakeRepository.For<EmailTemplate>(Context),
            _resolver,
            _emailService,
            _appNotifier);
        service.LocalizationManager = LocalIocManager.Resolve<ILocalizationManager>();
        return service;
    }

    private int CreateTemplate()
    {
        var template = new EmailTemplate { TenantId = AbpSession.TenantId ?? 1, NameEn = "T", NameAr = "T", Subject = "Payroll locked" };
        Context.Set<EmailTemplate>().Add(template);
        Context.SaveChanges();
        return template.Id;
    }

    private int CreateConfig(bool sendEmail, bool sendNotification, int? templateId, int groups)
    {
        var config = new NotificationConfig
        {
            TenantId = AbpSession.TenantId ?? 1,
            Name = "Payroll",
            SendEmail = sendEmail,
            SendNotification = sendNotification,
            EmailTemplateId = templateId
        };
        Context.Set<NotificationConfig>().Add(config);
        Context.SaveChanges();

        for (var i = 0; i < groups; i++)
        {
            Context.Set<NotificationGroup>().Add(new NotificationGroup
            {
                TenantId = AbpSession.TenantId ?? 1,
                NotificationConfigId = config.Id
            });
        }

        Context.SaveChanges();
        return config.Id;
    }

    /// <summary>A payroll area (with the three configurations given) and its control record.</summary>
    private PayrollControlRecord CreateControlRecord(int? releaseConfigId, int? exitConfigId = null, int? correctionConfigId = null)
    {
        var tenantId = AbpSession.TenantId ?? 1;
        var period = new PayrollPeriod { TenantId = tenantId, Code = "Jan-Dec", NameEn = "Jan-Dec", NameAr = "Jan-Dec" };
        Context.PayrollPeriods.Add(period);
        var currency = new Currency { TenantId = tenantId, Code = "KWD", NameEn = "KWD", NameAr = "KWD" };
        Context.Currencies.Add(currency);
        Context.SaveChanges();

        var area = new PayrollArea
        {
            TenantId = tenantId,
            Code = "PA1",
            Name = "Monthly",
            PayrollPeriodId = period.Id,
            CurrencyId = currency.Id,
            PAReleaseNotificationId = releaseConfigId,
            PAExitNotificationId = exitConfigId,
            PACorrectionNotificationId = correctionConfigId
        };
        Context.PayrollAreas.Add(area);
        Context.SaveChanges();

        var record = new PayrollControlRecord
        {
            TenantId = tenantId,
            PayrollAreaId = area.Id,
            PayrollMonth = 3,
            PayrollYear = 2026,
            CalendarMonthStart = new DateTime(2026, 3, 1),
            CalendarMonthEnd = new DateTime(2026, 3, 31)
        };
        Context.PayrollControlRecords.Add(record);
        Context.SaveChanges();
        return record;
    }

    [Fact]
    public async Task Should_Send_Email_And_InApp_With_The_Release_Configuration_When_Released_For_Payroll()
    {
        var templateId = CreateTemplate();
        var configId = CreateConfig(sendEmail: true, sendNotification: true, templateId, groups: 1);
        var record = CreateControlRecord(releaseConfigId: configId);
        _emailService.ParseTemplateAsync(Arg.Any<string>(), Arg.Any<EmailTemplate>(), Arg.Any<object>(), Arg.Any<IDictionary<string, object>>())
            .Returns("Payroll locked");

        await CreateService().NotifyStateChangedAsync(record, PayrollPeriodState.ReleasedForPayroll);

        await _emailService.Received(1).SendEmailForNotificationConfigAsync(
            Arg.Is<NotificationConfig>(c => c.Id == configId),
            Arg.Is<EmailTemplate>(t => t.Id == templateId),
            Arg.Is<object>(o => o is PayrollControlRecord && ((PayrollControlRecord)o).Id == record.Id),
            Arg.Is<IDictionary<string, object>>(m => m.ContainsKey("EmployeeName") && (string)m["EmployeeName"] == string.Empty));

        await _appNotifier.Received(1).SendMessageAsync(
            Arg.Is<UserIdentifier>(u => u.UserId == RecipientUserId && u.TenantId == record.TenantId),
            Arg.Is<string>(m => m == "Payroll locked"),
            Arg.Any<NotificationSeverity>());
    }

    [Theory]
    [InlineData(PayrollPeriodState.Closed, 2)]
    [InlineData(PayrollPeriodState.ReleasedForCorrection, 3)]
    [InlineData(PayrollPeriodState.ReleasedForPayroll, 1)]
    public async Task Should_Use_The_Configuration_That_Belongs_To_The_State(PayrollPeriodState state, int expectedConfig)
    {
        var templateId = CreateTemplate();
        var releaseId = CreateConfig(true, false, templateId, 1);
        var exitId = CreateConfig(true, false, templateId, 1);
        var correctionId = CreateConfig(true, false, templateId, 1);
        var record = CreateControlRecord(releaseId, exitId, correctionId);
        var expectedId = expectedConfig == 1 ? releaseId : expectedConfig == 2 ? exitId : correctionId;

        await CreateService().NotifyStateChangedAsync(record, state);

        await _emailService.Received(1).SendEmailForNotificationConfigAsync(
            Arg.Is<NotificationConfig>(c => c.Id == expectedId),
            Arg.Any<EmailTemplate>(),
            Arg.Any<object>(),
            Arg.Any<IDictionary<string, object>>());
    }

    [Fact]
    public async Task Should_Send_Nothing_When_The_Area_Has_No_Configuration_For_The_State()
    {
        var templateId = CreateTemplate();
        var releaseId = CreateConfig(true, true, templateId, 1);
        var record = CreateControlRecord(releaseConfigId: releaseId);

        await CreateService().NotifyStateChangedAsync(record, PayrollPeriodState.Closed);

        _emailService.ReceivedCalls().ShouldBeEmpty();
        _appNotifier.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Send_Nothing_When_Reopened_To_Open()
    {
        var templateId = CreateTemplate();
        var id = CreateConfig(true, true, templateId, 1);
        var record = CreateControlRecord(id, id, id);

        await CreateService().NotifyStateChangedAsync(record, PayrollPeriodState.Open);

        _emailService.ReceivedCalls().ShouldBeEmpty();
        _appNotifier.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Send_Nothing_When_The_Configuration_Has_No_Notification_Group()
    {
        // Pinned legacy behaviour: the send is inside "foreach notifyGroup", so no group means no message.
        var templateId = CreateTemplate();
        var configId = CreateConfig(sendEmail: true, sendNotification: true, templateId, groups: 0);
        var record = CreateControlRecord(releaseConfigId: configId);

        await CreateService().NotifyStateChangedAsync(record, PayrollPeriodState.ReleasedForPayroll);

        _emailService.ReceivedCalls().ShouldBeEmpty();
        _appNotifier.ReceivedCalls().ShouldBeEmpty();
        _resolver.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Send_Once_Per_Notification_Group()
    {
        // Pinned legacy behaviour: the group loop variable is never used, so two groups send everything twice.
        var templateId = CreateTemplate();
        var configId = CreateConfig(sendEmail: true, sendNotification: true, templateId, groups: 2);
        var record = CreateControlRecord(releaseConfigId: configId);

        await CreateService().NotifyStateChangedAsync(record, PayrollPeriodState.ReleasedForPayroll);

        await _emailService.Received(2).SendEmailForNotificationConfigAsync(
            Arg.Any<NotificationConfig>(), Arg.Any<EmailTemplate>(), Arg.Any<object>(), Arg.Any<IDictionary<string, object>>());
        await _appNotifier.Received(2).SendMessageAsync(
            Arg.Any<UserIdentifier>(), Arg.Any<string>(), Arg.Any<NotificationSeverity>());
    }

    [Fact]
    public async Task Should_Not_Email_When_SendEmail_Is_Off()
    {
        var templateId = CreateTemplate();
        var configId = CreateConfig(sendEmail: false, sendNotification: true, templateId, groups: 1);
        var record = CreateControlRecord(releaseConfigId: configId);

        await CreateService().NotifyStateChangedAsync(record, PayrollPeriodState.ReleasedForPayroll);

        await _emailService.DidNotReceive().SendEmailForNotificationConfigAsync(
            Arg.Any<NotificationConfig>(), Arg.Any<EmailTemplate>(), Arg.Any<object>(), Arg.Any<IDictionary<string, object>>());
        await _appNotifier.Received(1).SendMessageAsync(
            Arg.Any<UserIdentifier>(), Arg.Any<string>(), Arg.Any<NotificationSeverity>());
    }

    [Fact]
    public async Task Should_Not_Send_InApp_When_SendNotification_Is_Off()
    {
        var templateId = CreateTemplate();
        var configId = CreateConfig(sendEmail: true, sendNotification: false, templateId, groups: 1);
        var record = CreateControlRecord(releaseConfigId: configId);

        await CreateService().NotifyStateChangedAsync(record, PayrollPeriodState.ReleasedForPayroll);

        _appNotifier.ReceivedCalls().ShouldBeEmpty();
        await _emailService.Received(1).SendEmailForNotificationConfigAsync(
            Arg.Any<NotificationConfig>(), Arg.Any<EmailTemplate>(), Arg.Any<object>(), Arg.Any<IDictionary<string, object>>());
    }

    [Fact]
    public async Task Should_Send_A_Plain_InApp_Message_And_No_Email_When_The_Configuration_Has_No_Template()
    {
        var configId = CreateConfig(sendEmail: true, sendNotification: true, templateId: null, groups: 1);
        var record = CreateControlRecord(releaseConfigId: configId);

        await CreateService().NotifyStateChangedAsync(record, PayrollPeriodState.ReleasedForPayroll);

        await _emailService.DidNotReceive().SendEmailForNotificationConfigAsync(
            Arg.Any<NotificationConfig>(), Arg.Any<EmailTemplate>(), Arg.Any<object>(), Arg.Any<IDictionary<string, object>>());
        await _appNotifier.Received(1).SendMessageAsync(
            Arg.Any<UserIdentifier>(),
            Arg.Is<string>(m => !string.IsNullOrWhiteSpace(m)),
            Arg.Any<NotificationSeverity>());
    }

    [Fact]
    public async Task Should_Not_Throw_When_Sending_Fails()
    {
        var templateId = CreateTemplate();
        var configId = CreateConfig(sendEmail: true, sendNotification: false, templateId, groups: 1);
        var record = CreateControlRecord(releaseConfigId: configId);
        _emailService.SendEmailForNotificationConfigAsync(
                Arg.Any<NotificationConfig>(), Arg.Any<EmailTemplate>(), Arg.Any<object>(), Arg.Any<IDictionary<string, object>>())
            .ThrowsAsync(new InvalidOperationException("SMTP down"));

        await Should.NotThrowAsync(() => CreateService().NotifyStateChangedAsync(record, PayrollPeriodState.ReleasedForPayroll));
    }
}
