using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp;
using Abp.Domain.Repositories;
using Abp.Notifications;
using HCMv3.GeneralMasterInfo;
using HCMv3.HcmNotifications;
using HCMv3.Notifications;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.SystemSecurity.Services;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.HcmNotifications;

/// <summary>
/// The one delivery path the fixed notices and the rules share: recipients from the Notification Config,
/// the bell through Zero's notifier under the HCM definition (only to users subscribed to it), and the email
/// through the templated email service.
/// </summary>
// ReSharper disable once InconsistentNaming
public class NotificationDeliveryService_Tests : HcmNotificationTestBase
{
    private const long SubscribedUserId = 992001;
    private const long UnsubscribedUserId = 992002;

    private readonly IAppNotifier _appNotifier = Substitute.For<IAppNotifier>();
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly IHcmNotificationSubscriptionGate _gate = Substitute.For<IHcmNotificationSubscriptionGate>();
    private readonly List<(string Name, string Message, UserIdentifier[] Users)> _bell = new();
    private readonly NotificationDeliveryService _service;

    public NotificationDeliveryService_Tests()
    {
        _appNotifier
            .SendMessageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<UserIdentifier[]>(), Arg.Any<NotificationSeverity>())
            .Returns(call =>
            {
                _bell.Add((call.ArgAt<string>(0), call.ArgAt<string>(1), call.ArgAt<UserIdentifier[]>(2)));
                return Task.CompletedTask;
            });

        // Stands in for Zero's subscriptions: one user switched this notice off.
        _gate.FilterSubscribedAsync(Arg.Any<string>(), Arg.Any<IEnumerable<UserIdentifier>>())
            .Returns(call => Task.FromResult(call.ArgAt<IEnumerable<UserIdentifier>>(1).Where(u => u.UserId != UnsubscribedUserId).ToList()));

        _emailService
            .ParseTemplateAsync(Arg.Any<string>(), Arg.Any<EmailTemplate>(), Arg.Any<object>(), Arg.Any<IDictionary<string, object>>())
            .Returns(call => Task.FromResult("Parsed: " + call.ArgAt<string>(0)));

        _service = new NotificationDeliveryService(
            Resolve<INotificationRecipientResolver>(),
            _appNotifier,
            _emailService,
            _gate)
        {
            UnitOfWorkManager = UnitOfWorkManager,
            LocalizationManager = LocalizationManager
        };
    }

    private NotificationConfig Config(bool sendNotification, bool sendEmail)
    {
        var id = CreateNotificationConfig(sendNotification, sendEmail);
        return UsingDbContext(context => context.Set<NotificationConfig>().Single(c => c.Id == id));
    }

    private Employee EmployeeRow(int id) => UsingDbContext(context => context.Set<Employee>().Single(e => e.Id == id));

    private Task<NotificationDeliveryResult> DeliverAsync(NotificationDelivery delivery) =>
        InUnitOfWorkAsync(() => _service.DeliverAsync(delivery));

    [Fact]
    public async Task Should_Ring_The_Bell_Under_The_Hcm_Definition_With_The_Template_Subject()
    {
        var employee = EmployeeRow(CreateEmployee("DEL-BELL", SubscribedUserId));
        var template = new EmailTemplate { Id = 1, Subject = "Your $#Doc#$ expires" };

        var result = await DeliverAsync(new NotificationDelivery
        {
            NotificationName = HcmNotificationNames.ProbationEnd,
            NotificationConfig = Config(sendNotification: true, sendEmail: false),
            Subject = employee,
            EmailTemplate = template,
            FallbackMessage = "fallback"
        });

        result.RecipientCount.ShouldBe(1);
        result.InAppCount.ShouldBe(1);
        var bell = _bell.Single();
        bell.Name.ShouldBe(HcmNotificationNames.ProbationEnd);
        bell.Message.ShouldBe("Parsed: Your $#Doc#$ expires");
        bell.Users.Single().UserId.ShouldBe(SubscribedUserId);
        bell.Users.Single().TenantId.ShouldBe(TenantId);
    }

    [Fact]
    public async Task Should_Not_Ring_For_A_User_Who_Switched_The_Notice_Off()
    {
        var employee = EmployeeRow(CreateEmployee("DEL-OFF", UnsubscribedUserId));

        var result = await DeliverAsync(new NotificationDelivery
        {
            NotificationName = HcmNotificationNames.ProbationEnd,
            NotificationConfig = Config(sendNotification: true, sendEmail: false),
            Subject = employee,
            FallbackMessage = "fallback"
        });

        result.RecipientCount.ShouldBe(1);
        result.InAppCount.ShouldBe(0);
        _bell.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Skip_The_Bell_For_An_Employee_With_No_Login_But_Still_Email()
    {
        var employee = EmployeeRow(CreateEmployee("DEL-NOLOGIN"));
        var template = new EmailTemplate { Id = 1, Subject = "S" };
        var config = Config(sendNotification: true, sendEmail: true);

        var result = await DeliverAsync(new NotificationDelivery
        {
            NotificationName = HcmNotificationNames.ProbationEnd,
            NotificationConfig = config,
            Subject = employee,
            EmailTemplate = template
        });

        _bell.ShouldBeEmpty();
        result.EmailSent.ShouldBeTrue();
        await _emailService.Received(1).SendEmailForNotificationConfigAsync(config, template, employee, Arg.Any<IDictionary<string, object>>());
    }

    [Fact]
    public async Task Should_Use_The_Fallback_Message_When_There_Is_No_Template_And_Not_Email()
    {
        var employee = EmployeeRow(CreateEmployee("DEL-FALLBACK", SubscribedUserId));

        var result = await DeliverAsync(new NotificationDelivery
        {
            NotificationName = HcmNotificationNames.News,
            NotificationConfig = Config(sendNotification: true, sendEmail: true),
            Subject = employee,
            FallbackMessage = "Probation ends soon"
        });

        _bell.Single().Message.ShouldBe("Probation ends soon");

        // Legacy sent the email leg only with a template to send.
        result.EmailSent.ShouldBeFalse();
        await _emailService.DidNotReceiveWithAnyArgs().SendEmailForNotificationConfigAsync(default, default, default, (IDictionary<string, object>)default);
    }

    [Fact]
    public async Task Should_Send_Nothing_When_The_Configuration_Switches_Both_Channels_Off()
    {
        var employee = EmployeeRow(CreateEmployee("DEL-NONE", SubscribedUserId));

        var result = await DeliverAsync(new NotificationDelivery
        {
            NotificationName = HcmNotificationNames.News,
            NotificationConfig = Config(sendNotification: false, sendEmail: false),
            Subject = employee,
            EmailTemplate = new EmailTemplate { Id = 1, Subject = "S" },
            FallbackMessage = "x"
        });

        result.RecipientCount.ShouldBe(0);
        _bell.ShouldBeEmpty();
        await _emailService.DidNotReceiveWithAnyArgs().SendEmailForNotificationConfigAsync(default, default, default, (IDictionary<string, object>)default);
    }

    [Fact]
    public async Task Should_Send_Nothing_Without_A_Configuration()
    {
        var result = await DeliverAsync(new NotificationDelivery { NotificationName = HcmNotificationNames.News });

        result.RecipientCount.ShouldBe(0);
        _bell.ShouldBeEmpty();
    }

    [Fact]
    public async Task Direct_In_App_Delivery_Should_Honour_Subscriptions_And_Ignore_A_Blank_Message()
    {
        var reached = await InUnitOfWorkAsync(() => _service.DeliverInAppAsync(
            HcmNotificationNames.News, "Hello", new[] { SubscribedUserId, UnsubscribedUserId, SubscribedUserId }));
        var blank = await InUnitOfWorkAsync(() => _service.DeliverInAppAsync(
            HcmNotificationNames.News, " ", new[] { SubscribedUserId }));

        reached.ShouldBe(1);
        blank.ShouldBe(0);
        _bell.Single().Users.Select(u => u.UserId).ShouldBe(new[] { SubscribedUserId });
    }
}
