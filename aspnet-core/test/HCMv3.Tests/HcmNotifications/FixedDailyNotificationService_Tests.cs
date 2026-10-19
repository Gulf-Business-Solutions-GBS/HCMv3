using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Organizations;
using HCMv3.GeneralMasterInfo;
using HCMv3.GovtRelationsManagement;
using HCMv3.HcmNotifications;
using HCMv3.HcmNotifications.Daily;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.SystemSecurity.Services;
using HCMv3.Workflow.Services;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.HcmNotifications;

/// <summary>
/// The six fixed daily notifications ported as-is from legacy <c>SendRealTimeNotification</c>. Each legacy
/// rule -- odd ones included -- is pinned by a test here, so a later "fix" has to change a test on purpose.
/// </summary>
// ReSharper disable once InconsistentNaming
public class FixedDailyNotificationService_Tests : HcmNotificationTestBase
{
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly List<(EmailTemplate Template, Employee Recipient)> _emails = new();
    private readonly FixedDailyNotificationService _service;

    public FixedDailyNotificationService_Tests()
    {
        _emailService
            .SendEmailAsync(Arg.Any<EmailTemplate>(), Arg.Any<Employee>(), Arg.Any<object>(), Arg.Any<IDictionary<string, object>>(), Arg.Any<System.Net.Mail.Attachment>())
            .Returns(call =>
            {
                _emails.Add((call.ArgAt<EmailTemplate>(0), call.ArgAt<Employee>(1)));
                return Task.FromResult(new EmailResponseViewModel { Status = true });
            });

        _service = new FixedDailyNotificationService(
            Resolve<IRepository<EmployeeDocument>>(),
            Resolve<IRepository<OrganizationDocument>>(),
            Resolve<IRepository<DocumentType>>(),
            Resolve<IRepository<NotificationConfig>>(),
            Resolve<IRepository<EmailTemplate>>(),
            Resolve<IRepository<SystemNotification>>(),
            Resolve<IRepository<Employee>>(),
            Resolve<IRepository<EmployeeSponsor>>(),
            Resolve<IRepository<Sponsor>>(),
            Resolve<IRepository<EmployeeSeparation>>(),
            Resolve<IRepository<EmployeeSubGroupAbsence>>(),
            Resolve<IRepository<EmployeeOrgAssignment>>(),
            Resolve<IRepository<EmployeeAbsenceQuota>>(),
            Resolve<IRepository<AbsenceType>>(),
            Resolve<IRepository<News>>(),
            Resolve<IRepository<OrganizationUnit, long>>(),
            Resolve<IRepository<OrgUnitProfile>>(),
            Resolve<IRepository<OrganizationUnitType>>(),
            Resolve<IRepository<NotificationSentLog, long>>(),
            Resolve<IWorkflowDelegationResolver>(),
            Delivery,
            _emailService)
        {
            UnitOfWorkManager = UnitOfWorkManager,
            LocalizationManager = LocalizationManager
        };
    }

    // ======================================================================================================
    // Employee document expiry
    // ======================================================================================================

    private Task<int> DocumentExpiryAsync(DateTime now) => InUnitOfWorkAsync(() => _service.SendEmployeeDocumentExpiryNotificationsAsync(now));

    [Fact]
    public async Task Document_Expiry_Should_Send_When_Expiry_Less_Period_Is_Reached()
    {
        var employeeId = CreateEmployee("DOC-DUE");
        var configId = CreateNotificationConfig();
        var typeId = CreateDocumentType("DOC-DUE-T", configId, notificationPeriod: 30);
        var due = CreateEmployeeDocument(employeeId, typeId, Today.AddDays(30));
        CreateEmployeeDocument(employeeId, CreateDocumentType("DOC-DUE-T2", configId, notificationPeriod: 30), Today.AddDays(31));

        var sent = await DocumentExpiryAsync(Now);

        sent.ShouldBe(1);
        var delivery = Deliveries.Single();
        delivery.NotificationName.ShouldBe(HcmNotificationNames.EmployeeDocumentExpiry);
        delivery.NotificationConfig.Id.ShouldBe(configId);
        ((EmployeeDocument)delivery.Subject).Id.ShouldBe(due);

        // Legacy's merge key, sent even when the employee has no sponsor on file.
        delivery.MergeValues["EmployeeSponsor"].ShouldBe(string.Empty);
    }

    [Fact]
    public async Task Document_Expiry_Should_Skip_An_Inactive_Employee()
    {
        var employeeId = CreateEmployee("DOC-LEAVER", status: EmployeeStatus.Inactive);
        var typeId = CreateDocumentType("DOC-LEAVER-T", CreateNotificationConfig());
        CreateEmployeeDocument(employeeId, typeId, Today.AddDays(1));

        (await DocumentExpiryAsync(Now)).ShouldBe(0);
        SentLog().ShouldBeEmpty();
    }

    [Fact]
    public async Task Document_Expiry_Once_Should_Send_Once_And_Close_The_Document()
    {
        var employeeId = CreateEmployee("DOC-ONCE");
        var typeId = CreateDocumentType("DOC-ONCE-T", CreateNotificationConfig(), frequency: NotificationFrequency.Once);
        var documentId = CreateEmployeeDocument(employeeId, typeId, Today.AddDays(5));

        await DocumentExpiryAsync(Now);
        await DocumentExpiryAsync(Now.AddDays(1));
        await DocumentExpiryAsync(Now.AddDays(7));

        Deliveries.Count.ShouldBe(1);
        var log = SentLog().Single();
        log.RecordId.ShouldBe(documentId);
        log.WasSent.ShouldBeTrue();
        log.IsFinal.ShouldBeTrue();
    }

    [Fact]
    public async Task Document_Expiry_Daily_Should_Repeat_Next_Day_Not_Same_Day_And_Stop_At_The_Limit()
    {
        var employeeId = CreateEmployee("DOC-DAILY");
        var typeId = CreateDocumentType("DOC-DAILY-T", CreateNotificationConfig(), frequency: NotificationFrequency.Daily, countLimit: 2);
        CreateEmployeeDocument(employeeId, typeId, Today.AddDays(5));

        await DocumentExpiryAsync(Now);
        await DocumentExpiryAsync(Now.AddHours(4));   // the worker's next pass, same day
        await DocumentExpiryAsync(Now.AddDays(1));
        await DocumentExpiryAsync(Now.AddDays(2));

        Deliveries.Count.ShouldBe(2);
        SentLog().Count.ShouldBe(2);
        SentLog()[0].IsFinal.ShouldBeFalse();
        SentLog()[1].IsFinal.ShouldBeTrue();
    }

    [Fact]
    public async Task Document_Expiry_Weekly_Should_Wait_Seven_Days()
    {
        var employeeId = CreateEmployee("DOC-WEEKLY");
        var typeId = CreateDocumentType("DOC-WEEKLY-T", CreateNotificationConfig(), frequency: NotificationFrequency.Weekly, countLimit: 5);
        CreateEmployeeDocument(employeeId, typeId, Today.AddDays(20));

        await DocumentExpiryAsync(Now);
        await DocumentExpiryAsync(Now.AddDays(6));
        await DocumentExpiryAsync(Now.AddDays(7));

        Deliveries.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Document_Expiry_Repeating_With_A_Zero_Limit_Should_Close_Without_Ever_Sending()
    {
        // LEGACY ODDITY, PINNED: "Daily" with a count limit of 0 is never allowed to send, and the same pass
        // sets NotificationSent because 0 >= 0. The document is closed silently.
        var employeeId = CreateEmployee("DOC-ZERO");
        var typeId = CreateDocumentType("DOC-ZERO-T", CreateNotificationConfig(), frequency: NotificationFrequency.Daily, countLimit: 0);
        CreateEmployeeDocument(employeeId, typeId, Today.AddDays(5));

        await DocumentExpiryAsync(Now);
        await DocumentExpiryAsync(Now.AddDays(1));

        Deliveries.ShouldBeEmpty();
        var log = SentLog().Single();
        log.WasSent.ShouldBeFalse();
        log.IsFinal.ShouldBeTrue();
    }

    [Fact]
    public async Task Document_Expiry_Should_Close_A_Superseded_Document_Without_Sending()
    {
        // Legacy: IsActive == Inactive (a newer document of the same type exists) -> NotificationSent, no send.
        var employeeId = CreateEmployee("DOC-OLD");
        var typeId = CreateDocumentType("DOC-OLD-T", CreateNotificationConfig());
        var old = CreateEmployeeDocument(employeeId, typeId, Today.AddDays(5), issueDate: Today.AddYears(-5));
        var renewed = CreateEmployeeDocument(employeeId, typeId, Today.AddYears(5), issueDate: Today.AddDays(-1));

        await DocumentExpiryAsync(Now);

        Deliveries.ShouldBeEmpty();
        var log = SentLog().Single();
        log.RecordId.ShouldBe(old);
        log.WasSent.ShouldBeFalse();
        log.IsFinal.ShouldBeTrue();
        SentLog().ShouldNotContain(l => l.RecordId == renewed);
    }

    [Fact]
    public async Task Document_Expiry_Should_Close_A_Document_Whose_Type_Has_No_Configuration()
    {
        // LEGACY ODDITY, PINNED: closed for good -- adding a configuration later does not bring it back.
        var employeeId = CreateEmployee("DOC-NOCFG");
        var typeId = CreateDocumentType("DOC-NOCFG-T", notificationConfigId: null);
        CreateEmployeeDocument(employeeId, typeId, Today.AddDays(5));

        await DocumentExpiryAsync(Now);

        var configAddedLater = CreateNotificationConfig();
        UsingDbContext(context =>
        {
            context.Set<DocumentType>().Single(t => t.Id == typeId).NotificationConfigId = configAddedLater;
        });

        await DocumentExpiryAsync(Now.AddDays(1));

        Deliveries.ShouldBeEmpty();
        SentLog().Single().IsFinal.ShouldBeTrue();
    }

    [Theory]
    [InlineData(NotificationFrequency.Once, 0, 5, 0, true)]      // Once ignores the limit entirely
    [InlineData(NotificationFrequency.Daily, 0, 0, -1, false)]   // a zero limit never sends
    [InlineData(NotificationFrequency.Daily, 3, 0, -1, true)]    // never sent before
    [InlineData(NotificationFrequency.Daily, 3, 1, 0, false)]    // already sent today
    [InlineData(NotificationFrequency.Daily, 3, 1, 1, true)]
    [InlineData(NotificationFrequency.Daily, 3, 3, 9, false)]    // limit reached
    [InlineData(NotificationFrequency.Weekly, 3, 1, 6, false)]
    [InlineData(NotificationFrequency.Weekly, 3, 1, 7, true)]
    [InlineData(NotificationFrequency.Monthly, 3, 1, 29, false)]
    [InlineData(NotificationFrequency.Monthly, 3, 1, 30, true)]
    public void Document_Expiry_Frequency_Rule_Should_Match_Legacy(
        NotificationFrequency frequency, int limit, int count, int daysSinceLast, bool expected)
    {
        var lastSentOn = daysSinceLast < 0 ? default : Today.AddDays(-daysSinceLast);

        FixedDailyNotificationService.AllowDocumentExpirySend(frequency, limit, count, lastSentOn, Today)
            .ShouldBe(expected);
    }

    // ======================================================================================================
    // Company document expiry
    // ======================================================================================================

    [Fact]
    public async Task Company_Document_Expiry_Should_Send_Once_And_Set_The_Flag()
    {
        var configId = CreateNotificationConfig();
        var typeId = CreateDocumentType("CO-DUE-T", configId, notificationPeriod: 10, belongsTo: DocumentBelongsTo.Organization);
        var dueId = CreateOrganizationDocument(typeId, Today.AddDays(10));
        var notDueId = CreateOrganizationDocument(typeId, Today.AddDays(11));

        var sent = await InUnitOfWorkAsync(() => _service.SendCompanyDocumentExpiryNotificationsAsync(Now));
        await InUnitOfWorkAsync(() => _service.SendCompanyDocumentExpiryNotificationsAsync(Now.AddHours(4)));

        sent.ShouldBe(1);
        Deliveries.Count.ShouldBe(1);
        Deliveries[0].NotificationName.ShouldBe(HcmNotificationNames.CompanyDocumentExpiry);
        OrganizationDocumentRow(dueId).NotificationSent.ShouldBeTrue();
        OrganizationDocumentRow(notDueId).NotificationSent.ShouldBeFalse();
    }

    [Fact]
    public async Task Company_Document_Expiry_Should_Set_The_Flag_Even_Without_A_Configuration()
    {
        var typeId = CreateDocumentType("CO-NOCFG-T", notificationConfigId: null, belongsTo: DocumentBelongsTo.Organization);
        var documentId = CreateOrganizationDocument(typeId, Today.AddDays(1));

        var sent = await InUnitOfWorkAsync(() => _service.SendCompanyDocumentExpiryNotificationsAsync(Now));

        sent.ShouldBe(0);
        Deliveries.ShouldBeEmpty();
        OrganizationDocumentRow(documentId).NotificationSent.ShouldBeTrue();
    }

    // ======================================================================================================
    // Probation
    // ======================================================================================================

    [Fact]
    public async Task Probation_Should_Send_Exactly_Reminder_Days_Before_The_End_Once_That_Day()
    {
        var configId = CreateNotificationConfig();
        CreateSystemNotification(FixedDailyNotificationService.ProbationNotificationName, configId, reminderDays: 14);
        var due = CreateEmployee("PROB-DUE", probationEndDate: Today.AddDays(14));
        CreateEmployee("PROB-EARLY", probationEndDate: Today.AddDays(13));
        CreateEmployee("PROB-LATE", probationEndDate: Today.AddDays(15));
        CreateEmployee("PROB-LEAVER", status: EmployeeStatus.Inactive, probationEndDate: Today.AddDays(14));

        var sent = await InUnitOfWorkAsync(() => _service.SendProbationNotificationsAsync(Now));
        await InUnitOfWorkAsync(() => _service.SendProbationNotificationsAsync(Now.AddHours(4)));

        sent.ShouldBe(1);
        Deliveries.Count.ShouldBe(1);
        ((Employee)Deliveries[0].Subject).Id.ShouldBe(due);
        Deliveries[0].NotificationName.ShouldBe(HcmNotificationNames.ProbationEnd);
    }

    [Fact]
    public async Task Probation_Should_Do_Nothing_Without_An_Active_System_Notification()
    {
        CreateSystemNotification(FixedDailyNotificationService.ProbationNotificationName, CreateNotificationConfig(), reminderDays: 0, isActive: false);
        CreateEmployee("PROB-OFF", probationEndDate: Today);

        (await InUnitOfWorkAsync(() => _service.SendProbationNotificationsAsync(Now))).ShouldBe(0);
        Deliveries.ShouldBeEmpty();
    }

    [Fact]
    public async Task Probation_Should_Fall_Back_To_The_Template_Coded_After_The_Notification()
    {
        var templateId = CreateEmailTemplate(FixedDailyNotificationService.ProbationNotificationName);
        CreateSystemNotification(FixedDailyNotificationService.ProbationNotificationName, CreateNotificationConfig(), reminderDays: 0);
        CreateEmployee("PROB-TPL", probationEndDate: Today);

        await InUnitOfWorkAsync(() => _service.SendProbationNotificationsAsync(Now));

        Deliveries.Single().EmailTemplate.Id.ShouldBe(templateId);
    }

    // ======================================================================================================
    // Separation
    // ======================================================================================================

    [Fact]
    public async Task Separation_Should_Send_Exactly_Reminder_Days_Before_The_Last_Day()
    {
        var configId = CreateNotificationConfig();
        CreateSystemNotification(FixedDailyNotificationService.SeparationNotificationName, configId, reminderDays: 7);
        var due = CreateSeparation(CreateEmployee("SEP-DUE"), Today.AddDays(7));
        CreateSeparation(CreateEmployee("SEP-OTHER"), Today.AddDays(8));

        var sent = await InUnitOfWorkAsync(() => _service.SendSeparationNotificationsAsync(Now));
        await InUnitOfWorkAsync(() => _service.SendSeparationNotificationsAsync(Now.AddHours(4)));

        sent.ShouldBe(1);
        ((EmployeeSeparation)Deliveries.Single().Subject).Id.ShouldBe(due);
        Deliveries.Single().NotificationName.ShouldBe(HcmNotificationNames.Separation);
    }

    [Fact]
    public async Task Separation_Should_Name_The_First_Leaver_In_Every_Notice_Of_The_Run()
    {
        // LEGACY ODDITY, PINNED: legacy built the "EmployeeName" merge value once, from the first separation in
        // the list, and reused it for every notice. Two people leaving on the same day both get the first name.
        CreateSystemNotification(FixedDailyNotificationService.SeparationNotificationName, CreateNotificationConfig(), reminderDays: 0);
        CreateSeparation(CreateEmployee("SEP-FIRST"), Today);
        CreateSeparation(CreateEmployee("SEP-SECOND"), Today);

        await InUnitOfWorkAsync(() => _service.SendSeparationNotificationsAsync(Now));

        Deliveries.Count.ShouldBe(2);
        Deliveries.ShouldAllBe(d => (string)d.MergeValues["EmployeeName"] == "SEP-FIRST");
    }

    // ======================================================================================================
    // Annual leave balance over the limit
    // ======================================================================================================

    [Fact]
    public async Task Annual_Leave_Limit_Should_Send_Once_When_The_Balance_Reaches_The_Sub_Group_Limit()
    {
        CreateSystemNotification(FixedDailyNotificationService.AnnualLeaveMaxLimitNotificationName, CreateNotificationConfig(), reminderDays: 0);
        var annualLeave = CreateAbsenceType(FixedDailyNotificationService.AnnualLeaveAbsenceTypeCode);
        var subGroupId = CreateSubGroupWithLimit(annualLeave, limitDays: 40);

        var atLimit = CreateEmployee("AL-AT");
        AssignToSubGroup(atLimit, subGroupId);
        var atLimitQuota = CreateQuota(atLimit, annualLeave, accrued: 45, used: 5);   // balance 40

        var below = CreateEmployee("AL-BELOW");
        AssignToSubGroup(below, subGroupId);
        var belowQuota = CreateQuota(below, annualLeave, accrued: 45, used: 6);       // balance 39

        var sent = await InUnitOfWorkAsync(() => _service.SendAnnualLeaveMaxLimitNotificationsAsync(Now));
        await InUnitOfWorkAsync(() => _service.SendAnnualLeaveMaxLimitNotificationsAsync(Now.AddHours(4)));

        sent.ShouldBe(1);
        var delivery = Deliveries.Single();
        ((Employee)delivery.Subject).Id.ShouldBe(atLimit);
        delivery.MergeValues["Employee.AbsenceQuota"].ShouldBe(40d.ToString());
        QuotaRow(atLimitQuota).NotificationSent.ShouldBeTrue();
        QuotaRow(belowQuota).NotificationSent.ShouldBeFalse();
    }

    [Fact]
    public async Task Annual_Leave_Limit_Should_Only_Read_Absence_Type_Code_One()
    {
        CreateSystemNotification(FixedDailyNotificationService.AnnualLeaveMaxLimitNotificationName, CreateNotificationConfig(), reminderDays: 0);
        var sickLeave = CreateAbsenceType("3");
        var subGroupId = CreateSubGroupWithLimit(sickLeave, limitDays: 1);
        var employeeId = CreateEmployee("AL-SICK");
        AssignToSubGroup(employeeId, subGroupId);
        CreateQuota(employeeId, sickLeave, accrued: 50, used: 0);

        (await InUnitOfWorkAsync(() => _service.SendAnnualLeaveMaxLimitNotificationsAsync(Now))).ShouldBe(0);
    }

    [Fact]
    public void Annual_Leave_Balance_Should_Be_Legacys_Rounded_Formula()
    {
        // Accrued less (used + adjusted + forfeited), rounded to two places: 30.456 - 13.5 = 16.956 -> 16.96.
        var quota = new EmployeeAbsenceQuota { QuotaAccrued = 30.456, QuotaUsed = 10, QuotaAdjusted = 1.5, QuotaForfeited = 2 };

        FixedDailyNotificationService.QuotaBalance(quota).ShouldBe(16.96);
    }

    // ======================================================================================================
    // News
    // ======================================================================================================

    [Fact]
    public async Task News_At_The_Root_Level_Should_Reach_Every_Employee_With_A_Current_Assignment_Once()
    {
        var root = CreateOrganizationUnitType(OrganizationUnitType.Level0Code);
        var first = CreateEmployee("NEWS-A", userId: 991101);
        var second = CreateEmployee("NEWS-B", userId: 991102);
        CreateEmployee("NEWS-NOASSIGN", userId: 991103);
        Assign(first, null);
        Assign(second, null);
        var newsId = CreateNews(root, null, sendNotification: true);

        var sent = await InUnitOfWorkAsync(() => _service.SendNewsNotificationsAsync(Now));
        await InUnitOfWorkAsync(() => _service.SendNewsNotificationsAsync(Now.AddHours(4)));

        sent.ShouldBe(1);
        var inApp = InAppDeliveries.Single();
        inApp.Name.ShouldBe(HcmNotificationNames.News);
        inApp.Message.ShouldBe("News body");
        inApp.UserIds.ShouldBe(new long[] { 991101, 991102 }, ignoreOrder: true);
        NewsRow(newsId).NotificationSent.ShouldBeTrue();
    }

    [Fact]
    public async Task News_For_A_Unit_Should_Reach_That_Unit_And_Below_Only()
    {
        var departmentLevel = CreateOrganizationUnitType("Level2");
        var division = CreateOrganizationUnit("00001", null);
        var team = CreateOrganizationUnit("00001.00001", division);
        var elsewhere = CreateOrganizationUnit("00002", null);

        var inDivision = CreateEmployee("NEWS-DIV", userId: 991201);
        var inTeam = CreateEmployee("NEWS-TEAM", userId: 991202);
        var outside = CreateEmployee("NEWS-OUT", userId: 991203);
        Assign(inDivision, division);
        Assign(inTeam, team);
        Assign(outside, elsewhere);

        CreateNews(departmentLevel, division, sendNotification: true);

        await InUnitOfWorkAsync(() => _service.SendNewsNotificationsAsync(Now));

        InAppDeliveries.Single().UserIds.ShouldBe(new long[] { 991201, 991202 }, ignoreOrder: true);
    }

    [Fact]
    public async Task News_With_No_Organisation_Level_Should_Reach_Nobody_But_Still_Be_Marked_Sent()
    {
        // LEGACY ODDITY, PINNED: legacy threw on the missing level, swallowed it, and marked the news sent.
        var employeeId = CreateEmployee("NEWS-NOLEVEL", userId: 991301);
        Assign(employeeId, null);
        var newsId = CreateNews(null, null, sendNotification: true);

        var sent = await InUnitOfWorkAsync(() => _service.SendNewsNotificationsAsync(Now));

        sent.ShouldBe(0);
        InAppDeliveries.Single().UserIds.ShouldBeEmpty();
        NewsRow(newsId).NotificationSent.ShouldBeTrue();
    }

    [Fact]
    public async Task News_Should_Not_Go_Out_Before_Its_Issue_Date_Or_After_Expiry()
    {
        var root = CreateOrganizationUnitType(OrganizationUnitType.Level0Code);
        var future = CreateNews(root, null, sendNotification: true, issueDate: Today.AddDays(1));
        var expired = CreateNews(root, null, sendNotification: true, issueDate: Today.AddDays(-10), expiryDate: Today.AddDays(-1));

        await InUnitOfWorkAsync(() => _service.SendNewsNotificationsAsync(Now));

        InAppDeliveries.ShouldBeEmpty();
        NewsRow(future).NotificationSent.ShouldBeFalse();
        NewsRow(expired).NotificationSent.ShouldBeFalse();
    }

    [Fact]
    public async Task News_Email_Should_Use_The_Title_And_Comments_And_The_Units_Own_Level()
    {
        var root = CreateOrganizationUnitType(OrganizationUnitType.Level0Code);
        var company = CreateOrganizationUnit("00009", null);
        CreateOrgUnitProfile(company, root);
        CreateEmployee("NEWS-MAIL1");
        CreateEmployee("NEWS-MAIL2");
        CreateEmployee("NEWS-MAILLEAVER", status: EmployeeStatus.Inactive);

        // The news's own level field is left empty: legacy's email leg read the UNIT's level, not this one.
        CreateNews(null, company, sendNotification: false, sendEmail: true);

        var sent = await InUnitOfWorkAsync(() => _service.SendNewsNotificationsAsync(Now));

        sent.ShouldBe(1);
        InAppDeliveries.ShouldBeEmpty();
        _emails.Count.ShouldBe(2);
        _emails.ShouldAllBe(e => e.Template.Subject == "News title" && e.Template.EmailBodyText == "News body");
        _emails.Select(e => e.Recipient.Code).ShouldBe(new[] { "NEWS-MAIL1", "NEWS-MAIL2" }, ignoreOrder: true);
    }

    // ======================================================================================================
    // Fixtures
    // ======================================================================================================

    private void CreateSystemNotification(string name, int configId, int reminderDays, bool isActive = true)
    {
        UsingDbContext(context =>
        {
            context.Set<SystemNotification>().Add(new SystemNotification
            {
                TenantId = TenantId,
                NotificationName = name,
                NotificationConfigId = configId,
                ReminderDays = reminderDays,
                IsActive = isActive
            });
        });
    }

    private int CreateOrganizationDocument(int typeId, DateTime expiryDate)
    {
        var unitId = CreateOrganizationUnit("0" + Guid.NewGuid().ToString("N").Substring(0, 4), null);
        return UsingDbContext(context =>
        {
            var document = context.Set<OrganizationDocument>().Add(new OrganizationDocument
            {
                TenantId = TenantId,
                OrganizationUnitId = unitId,
                DocumentTypeId = typeId,
                DocumentNum = "CO-" + Guid.NewGuid().ToString("N").Substring(0, 4),
                IssueDate = expiryDate.AddYears(-1),
                ExpiryDate = expiryDate
            }).Entity;
            context.SaveChanges();
            return document.Id;
        });
    }

    private OrganizationDocument OrganizationDocumentRow(int id) =>
        UsingDbContext(context => context.Set<OrganizationDocument>().Single(d => d.Id == id));

    private int CreateSeparation(int employeeId, DateTime lastDay)
    {
        return UsingDbContext(context =>
        {
            var reason = context.Set<SeparationReason>().Add(new SeparationReason
            {
                TenantId = TenantId,
                Code = "R" + Guid.NewGuid().ToString("N").Substring(0, 4),
                NameEn = "Reason",
                NameAr = "Reason"
            }).Entity;
            context.SaveChanges();

            var separation = context.Set<EmployeeSeparation>().Add(new EmployeeSeparation
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                SubmissionDate = lastDay.AddDays(-30),
                LastDayInService = lastDay,
                PlannedLastDayInService = lastDay,
                SeparationType = SeparationType.Resignation,
                SeparationReasonId = reason.Id
            }).Entity;
            context.SaveChanges();
            return separation.Id;
        });
    }

    private int CreateAbsenceType(string code)
    {
        return UsingDbContext(context =>
        {
            var type = context.Set<AbsenceType>().Add(new AbsenceType { TenantId = TenantId, Code = code, NameEn = "Leave " + code, NameAr = "Leave " + code }).Entity;
            context.SaveChanges();
            return type.Id;
        });
    }

    private int CreateSubGroupWithLimit(int absenceTypeId, int limitDays)
    {
        return UsingDbContext(context =>
        {
            var group = context.Set<EmployeeGroup>().Add(new EmployeeGroup { TenantId = TenantId, NameEn = "G", NameAr = "G" }).Entity;
            context.SaveChanges();

            var subGroup = context.Set<EmployeeSubGroup>().Add(new EmployeeSubGroup { TenantId = TenantId, EmployeeGroupId = group.Id, NameEn = "SG", NameAr = "SG" }).Entity;
            context.SaveChanges();

            context.Set<EmployeeSubGroupAbsence>().Add(new EmployeeSubGroupAbsence
            {
                TenantId = TenantId,
                EmployeeSubGroupId = subGroup.Id,
                AbsenceTypeId = absenceTypeId,
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(9999, 12, 31),
                TotalDays = 30,
                MaxLimitReachNotificationInDays = limitDays
            });
            context.SaveChanges();

            return subGroup.Id;
        });
    }

    private void AssignToSubGroup(int employeeId, int subGroupId) => Assign(employeeId, null, subGroupId);

    private void Assign(int employeeId, long? organizationUnitId, int? subGroupId = null)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                StartDate = Today.AddYears(-1),
                EndDate = new DateTime(9999, 12, 31),
                OrganizationUnitId = organizationUnitId,
                EmployeeSubGroupId = subGroupId
            });
        });
    }

    private int CreateQuota(int employeeId, int absenceTypeId, double accrued, double used)
    {
        return UsingDbContext(context =>
        {
            var quota = context.Set<EmployeeAbsenceQuota>().Add(new EmployeeAbsenceQuota
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                AbsenceTypeId = absenceTypeId,
                EntitlementFrom = Today.AddMonths(-6),
                EntitlementTo = Today.AddMonths(6),
                DeductionFrom = Today.AddMonths(-6),
                DeductionTo = Today.AddMonths(18),
                QuotaAccrued = accrued,
                QuotaUsed = used
            }).Entity;
            context.SaveChanges();
            return quota.Id;
        });
    }

    private EmployeeAbsenceQuota QuotaRow(int id) =>
        UsingDbContext(context => context.Set<EmployeeAbsenceQuota>().Single(q => q.Id == id));

    private int CreateOrganizationUnitType(string code)
    {
        return UsingDbContext(context =>
        {
            var type = context.Set<OrganizationUnitType>().Add(new OrganizationUnitType { TenantId = TenantId, Code = code, NameEn = code, NameAr = code }).Entity;
            context.SaveChanges();
            return type.Id;
        });
    }

    private long CreateOrganizationUnit(string code, long? parentId)
    {
        return UsingDbContext(context =>
        {
            var unit = context.Set<OrganizationUnit>().Add(new OrganizationUnit(TenantId, "Unit " + code, parentId) { Code = code }).Entity;
            context.SaveChanges();
            return unit.Id;
        });
    }

    private void CreateOrgUnitProfile(long organizationUnitId, int organizationUnitTypeId)
    {
        UsingDbContext(context =>
        {
            context.Set<OrgUnitProfile>().Add(new OrgUnitProfile
            {
                TenantId = TenantId,
                OrganizationUnitId = organizationUnitId,
                OrganizationUnitTypeId = organizationUnitTypeId,
                Code = "P" + organizationUnitId,
                NameAr = "P" + organizationUnitId
            });
        });
    }

    private int CreateNews(
        int? organizationUnitTypeId,
        long? organizationUnitId,
        bool sendNotification,
        bool sendEmail = false,
        DateTime? issueDate = null,
        DateTime? expiryDate = null)
    {
        return UsingDbContext(context =>
        {
            var newsType = context.Set<NewsType>().Add(new NewsType { TenantId = TenantId, Code = "NT" + Guid.NewGuid().ToString("N").Substring(0, 4), NameEn = "General", NameAr = "General" }).Entity;
            context.SaveChanges();

            var news = context.Set<News>().Add(new News
            {
                TenantId = TenantId,
                NewsTypeId = newsType.Id,
                Title = "News title",
                Comments = "News body",
                IssueDate = issueDate ?? Today.AddDays(-1),
                ExpiryDate = expiryDate ?? Today.AddDays(10),
                OrganizationUnitTypeId = organizationUnitTypeId,
                OrganizationUnitId = organizationUnitId,
                SendNotification = sendNotification,
                SendEmail = sendEmail
            }).Entity;
            context.SaveChanges();
            return news.Id;
        });
    }

    private News NewsRow(int id) => UsingDbContext(context => context.Set<News>().Single(n => n.Id == id));
}
