using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using HCMv3.GeneralMasterInfo;
using HCMv3.HcmNotifications;
using HCMv3.HcmNotifications.Rules;
using HCMv3.HcmNotifications.Rules.Sources;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.HcmNotifications;

/// <summary>
/// The Notification Master rule runner: legacy <c>SendNotification_Generic</c> with the per-rule sent log in
/// place of the shared <c>NotificationSent</c> flag. The delivery service is a substitute, so each test reads
/// exactly which records were notified about.
/// </summary>
// ReSharper disable once InconsistentNaming
public class NotificationRuleRunner_Tests : HcmNotificationTestBase
{
    private const string DocumentSource = NotificationRuleSourceCodes.EmployeeDocument;

    private readonly NotificationRuleRunner _runner;

    public NotificationRuleRunner_Tests()
    {
        _runner = new NotificationRuleRunner(
            Resolve<IRepository<NotificationMaster>>(),
            Resolve<IRepository<NotificationConfig>>(),
            Resolve<IRepository<EmailTemplate>>(),
            Resolve<IRepository<NotificationSentLog, long>>(),
            Resolve<INotificationRuleSourceRegistry>(),
            Resolve<INotificationCriterionEvaluator>(),
            Delivery)
        {
            UnitOfWorkManager = UnitOfWorkManager,
            LocalizationManager = LocalizationManager
        };
    }

    private Task<NotificationRuleRunSummary> RunAsync() => InUnitOfWorkAsync(() => _runner.RunAsync(Today));

    private (int TypeId, int SoonId, int LaterId) TwoDocuments(string code)
    {
        var employeeId = CreateEmployee(code);
        var typeId = CreateDocumentType(code + "-T");
        var soon = CreateEmployeeDocument(employeeId, typeId, Today.AddDays(10));
        var later = CreateEmployeeDocument(employeeId, typeId, Today.AddDays(100));
        return (typeId, soon, later);
    }

    [Fact]
    public async Task Should_Notify_Once_About_Each_Matching_Record()
    {
        var (typeId, soon, later) = TwoDocuments("RUN-ONCE");
        var configId = CreateNotificationConfig();
        CreateRule(DocumentSource, configId, DueWithin(EmployeeDocumentNotificationRuleSource.Parameter, "ExpiryDate", 30), typeId);

        var summary = await RunAsync();

        summary.RulesRun.ShouldBe(1);
        Deliveries.Count.ShouldBe(1);
        ((EmployeeDocument)Deliveries[0].Subject).Id.ShouldBe(soon);
        Deliveries[0].NotificationName.ShouldBe(HcmNotificationNames.ForRuleSource(DocumentSource));
        Deliveries[0].NotificationConfig.Id.ShouldBe(configId);
        SentLog().Select(l => l.RecordId).ShouldBe(new long[] { soon });
        SentLog().ShouldNotContain(l => l.RecordId == later);
    }

    [Fact]
    public async Task Should_Not_Repeat_On_The_Next_Run()
    {
        var (typeId, _, _) = TwoDocuments("RUN-REPEAT");
        var configId = CreateNotificationConfig();
        CreateRule(DocumentSource, configId, DueWithin(EmployeeDocumentNotificationRuleSource.Parameter, "ExpiryDate", 30), typeId);

        await RunAsync();
        await RunAsync();
        await RunAsync();

        Deliveries.Count.ShouldBe(1);
        SentLog().Count.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Fire_Two_Rules_On_The_Same_Record()
    {
        // Legacy's single NotificationSent flag let the first rule silence the second. Not any more.
        var (typeId, soon, _) = TwoDocuments("RUN-TWO");
        var hrConfig = CreateNotificationConfig();
        var managerConfig = CreateNotificationConfig();
        var hrRule = CreateRule(DocumentSource, hrConfig, DueWithin(EmployeeDocumentNotificationRuleSource.Parameter, "ExpiryDate", 30), typeId);
        var managerRule = CreateRule(DocumentSource, managerConfig, DueWithin(EmployeeDocumentNotificationRuleSource.Parameter, "ExpiryDate", 15), typeId);

        await RunAsync();

        Deliveries.Count.ShouldBe(2);
        Deliveries.Select(d => d.NotificationConfig.Id).ShouldBe(new[] { hrConfig, managerConfig }, ignoreOrder: true);
        SentLog().Select(l => l.NotificationMasterId).ShouldBe(new int?[] { hrRule, managerRule }, ignoreOrder: true);
        SentLog().ShouldAllBe(l => l.RecordId == soon && l.IsFinal);
    }

    [Fact]
    public async Task Should_Fire_A_Rule_Added_Later_About_A_Record_Another_Rule_Already_Sent()
    {
        var (typeId, soon, _) = TwoDocuments("RUN-LATER");
        var configId = CreateNotificationConfig();
        CreateRule(DocumentSource, configId, DueWithin(EmployeeDocumentNotificationRuleSource.Parameter, "ExpiryDate", 30), typeId);
        await RunAsync();

        CreateRule(DocumentSource, CreateNotificationConfig(), null, typeId);
        await RunAsync();

        // The second rule has no criterion, so it fires about both documents; the first rule stays silent.
        Deliveries.Count.ShouldBe(3);
        SentLog().Count(l => l.RecordId == soon).ShouldBe(2);
    }

    [Fact]
    public async Task Should_Honour_The_Sub_Type()
    {
        var employeeId = CreateEmployee("RUN-SUB");
        var passport = CreateDocumentType("RUN-SUB-P");
        var visa = CreateDocumentType("RUN-SUB-V");
        var passportDoc = CreateEmployeeDocument(employeeId, passport, Today.AddDays(5));
        CreateEmployeeDocument(employeeId, visa, Today.AddDays(5));
        CreateRule(DocumentSource, CreateNotificationConfig(), DueWithin(EmployeeDocumentNotificationRuleSource.Parameter, "ExpiryDate", 30), passport);

        await RunAsync();

        Deliveries.Count.ShouldBe(1);
        ((EmployeeDocument)Deliveries[0].Subject).Id.ShouldBe(passportDoc);
    }

    [Fact]
    public async Task Should_Skip_A_Disabled_Rule()
    {
        var (typeId, _, _) = TwoDocuments("RUN-OFF");
        CreateRule(DocumentSource, CreateNotificationConfig(), null, typeId, isEnabled: false);

        var summary = await RunAsync();

        summary.RulesRun.ShouldBe(0);
        Deliveries.ShouldBeEmpty();
        SentLog().ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Skip_A_Rule_With_An_Unsupported_Type_And_Still_Run_The_Others()
    {
        var (typeId, soon, _) = TwoDocuments("RUN-OLD");
        CreateRule("AbsenceRequest", CreateNotificationConfig());
        CreateRule(DocumentSource, CreateNotificationConfig(), DueWithin(EmployeeDocumentNotificationRuleSource.Parameter, "ExpiryDate", 30), typeId);

        var summary = await RunAsync();

        summary.RulesSkipped.ShouldBe(1);
        summary.RulesRun.ShouldBe(1);
        ((EmployeeDocument)Deliveries.Single().Subject).Id.ShouldBe(soon);
    }

    [Fact]
    public async Task Should_Fail_Only_The_Rule_Whose_Criterion_Cannot_Parse()
    {
        var (typeId, soon, _) = TwoDocuments("RUN-BROKEN");
        CreateRule(DocumentSource, CreateNotificationConfig(), "[ExpiryDate] < LocalDateTimeToday()", typeId);
        CreateRule(DocumentSource, CreateNotificationConfig(), DueWithin(EmployeeDocumentNotificationRuleSource.Parameter, "ExpiryDate", 30), typeId);

        var summary = await RunAsync();

        summary.RulesFailed.ShouldBe(1);
        summary.RulesRun.ShouldBe(1);
        ((EmployeeDocument)Deliveries.Single().Subject).Id.ShouldBe(soon);
    }

    [Fact]
    public async Task Should_Log_A_Match_As_Done_Even_When_Nobody_Was_Addressed()
    {
        // Legacy set its flag after the send whatever the send found; otherwise a record about somebody with no
        // manager would be re-"sent" to nobody every four hours.
        Delivery.DeliverAsync(Arg.Any<NotificationDelivery>())
            .Returns(Task.FromResult(new NotificationDeliveryResult()));

        var (typeId, soon, _) = TwoDocuments("RUN-NOBODY");
        CreateRule(DocumentSource, CreateNotificationConfig(), DueWithin(EmployeeDocumentNotificationRuleSource.Parameter, "ExpiryDate", 30), typeId);

        var summary = await RunAsync();
        await RunAsync();

        summary.NotificationsSent.ShouldBe(0);
        var log = SentLog().Single();
        log.RecordId.ShouldBe(soon);
        log.WasSent.ShouldBeFalse();
        log.IsFinal.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Mark_Existing_Matches_As_Sent_Without_Sending()
    {
        var (typeId, soon, _) = TwoDocuments("RUN-MARK");
        var ruleId = CreateRule(DocumentSource, CreateNotificationConfig(), DueWithin(EmployeeDocumentNotificationRuleSource.Parameter, "ExpiryDate", 30), typeId);

        var marked = await InUnitOfWorkAsync(() => _runner.MarkExistingAsSentAsync(ruleId, Today));
        await RunAsync();

        marked.ShouldBe(1);
        Deliveries.ShouldBeEmpty();
        var log = SentLog().Single();
        log.RecordId.ShouldBe(soon);
        log.WasSent.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Preview_Matches_And_What_Is_Not_Yet_Sent()
    {
        var employeeId = CreateEmployee("RUN-PREVIEW");
        var typeId = CreateDocumentType("RUN-PREVIEW-T");
        CreateEmployeeDocument(employeeId, typeId, Today.AddDays(1));
        CreateEmployeeDocument(employeeId, typeId, Today.AddDays(2));
        CreateEmployeeDocument(employeeId, typeId, Today.AddDays(200));
        var criterion = DueWithin(EmployeeDocumentNotificationRuleSource.Parameter, "ExpiryDate", 30);
        var ruleId = CreateRule(DocumentSource, CreateNotificationConfig(), criterion, typeId);

        var before = await InUnitOfWorkAsync(() => _runner.PreviewAsync(DocumentSource, typeId, criterion, ruleId, Today));
        await RunAsync();
        var after = await InUnitOfWorkAsync(() => _runner.PreviewAsync(DocumentSource, typeId, criterion, ruleId, Today));

        before.Errors.ShouldBeEmpty();
        before.MatchCount.ShouldBe(2);
        before.NotYetSentCount.ShouldBe(2);
        before.Samples.Count.ShouldBe(2);
        before.Samples.ShouldAllBe(s => s.Record == null && s.Label.Contains("RUN-PREVIEW"));
        after.MatchCount.ShouldBe(2);
        after.NotYetSentCount.ShouldBe(0);
        Deliveries.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Report_A_Criterion_Error_In_The_Preview_Instead_Of_Counting()
    {
        var preview = await InUnitOfWorkAsync(() => _runner.PreviewAsync(DocumentSource, null, "not json", null, Today));

        preview.Errors.ShouldNotBeEmpty();
        preview.MatchCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Send_The_Configurations_Template_And_A_Fallback_Message()
    {
        var templateId = CreateEmailTemplate("RUN-TPL");
        var (typeId, _, _) = TwoDocuments("RUN-TPL");
        CreateRule(DocumentSource, CreateNotificationConfig(emailTemplateId: templateId), DueWithin(EmployeeDocumentNotificationRuleSource.Parameter, "ExpiryDate", 30), typeId);

        await RunAsync();

        Deliveries.Single().EmailTemplate.Id.ShouldBe(templateId);
        Deliveries.Single().FallbackMessage.ShouldNotBeNullOrWhiteSpace();
    }
}
