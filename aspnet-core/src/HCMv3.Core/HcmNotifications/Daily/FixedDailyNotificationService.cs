using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Organizations;
using HCMv3.GeneralMasterInfo;
using HCMv3.GovtRelationsManagement;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.SystemSecurity.Services;
using HCMv3.Workflow.Services;

namespace HCMv3.HcmNotifications.Daily;

/// <inheritdoc cref="IFixedDailyNotificationService"/>
/// <remarks>
/// Ported from legacy <c>HCM10.Module\BusinessObjects\Services\NotificationService.cs</c>,
/// <c>SendRealTimeNotification</c> (the employee document, organization document, news and annual leave max
/// limit regions), <c>SendNotification_Separation</c>, <c>SendNotification_Probation</c> and
/// <c>SendNotification(News, ...)</c>; and <c>EmailService.SendEmail_ForAnnouncement</c>.
///
/// <para>
/// CHANNELS. Legacy's in-app leg created a <c>TaskEvent</c> whose subject was the configuration's template
/// subject; here it is a Zero notification under an HCM definition carrying the same text, through
/// <see cref="INotificationDeliveryService"/>. The email leg is the same templated email service as before.
/// </para>
///
/// <para>
/// TEMPLATE. Legacy sent <c>NotificationConfig.EmailTemplate</c>. That is used first; for the three driven by
/// a <c>SystemNotification</c> row, a template whose code is the notification name is the fallback -- the
/// convention the already-ported birthday and anniversary notices established while the configuration had no
/// template column.
/// </para>
/// </remarks>
public class FixedDailyNotificationService : HCMv3DomainServiceBase, IFixedDailyNotificationService
{
    public const string ProbationNotificationName = "PROBATION_NOTIFICATION";
    public const string SeparationNotificationName = "Separation_NOTIFICATION";
    public const string AnnualLeaveMaxLimitNotificationName = "ANNUALLEAVEMAXLIMITREACHED_NOTIFICATION";

    /// <summary>Legacy's annual leave absence type code, hard-coded there too.</summary>
    public const string AnnualLeaveAbsenceTypeCode = "1";

    /// <summary>The source codes the fixed notices log under, beside the rule sources'.</summary>
    public const string NewsSourceCode = "News";
    public const string AbsenceQuotaSourceCode = "EmployeeAbsenceQuota";
    public const string SeparationSourceCode = "EmployeeSeparation";

    private readonly IRepository<EmployeeDocument> _employeeDocumentRepository;
    private readonly IRepository<OrganizationDocument> _organizationDocumentRepository;
    private readonly IRepository<DocumentType> _documentTypeRepository;
    private readonly IRepository<NotificationConfig> _notificationConfigRepository;
    private readonly IRepository<EmailTemplate> _emailTemplateRepository;
    private readonly IRepository<SystemNotification> _systemNotificationRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeSponsor> _employeeSponsorRepository;
    private readonly IRepository<Sponsor> _sponsorRepository;
    private readonly IRepository<EmployeeSeparation> _separationRepository;
    private readonly IRepository<EmployeeSubGroupAbsence> _subGroupAbsenceRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<EmployeeAbsenceQuota> _absenceQuotaRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IRepository<News> _newsRepository;
    private readonly IRepository<OrganizationUnit, long> _organizationUnitRepository;
    private readonly IRepository<OrgUnitProfile> _orgUnitProfileRepository;
    private readonly IRepository<OrganizationUnitType> _organizationUnitTypeRepository;
    private readonly IRepository<NotificationSentLog, long> _sentLogRepository;
    private readonly IWorkflowDelegationResolver _delegationResolver;
    private readonly INotificationDeliveryService _deliveryService;
    private readonly IEmailService _emailService;

    public FixedDailyNotificationService(
        IRepository<EmployeeDocument> employeeDocumentRepository,
        IRepository<OrganizationDocument> organizationDocumentRepository,
        IRepository<DocumentType> documentTypeRepository,
        IRepository<NotificationConfig> notificationConfigRepository,
        IRepository<EmailTemplate> emailTemplateRepository,
        IRepository<SystemNotification> systemNotificationRepository,
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeSponsor> employeeSponsorRepository,
        IRepository<Sponsor> sponsorRepository,
        IRepository<EmployeeSeparation> separationRepository,
        IRepository<EmployeeSubGroupAbsence> subGroupAbsenceRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<EmployeeAbsenceQuota> absenceQuotaRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        IRepository<News> newsRepository,
        IRepository<OrganizationUnit, long> organizationUnitRepository,
        IRepository<OrgUnitProfile> orgUnitProfileRepository,
        IRepository<OrganizationUnitType> organizationUnitTypeRepository,
        IRepository<NotificationSentLog, long> sentLogRepository,
        IWorkflowDelegationResolver delegationResolver,
        INotificationDeliveryService deliveryService,
        IEmailService emailService)
    {
        _employeeDocumentRepository = employeeDocumentRepository;
        _organizationDocumentRepository = organizationDocumentRepository;
        _documentTypeRepository = documentTypeRepository;
        _notificationConfigRepository = notificationConfigRepository;
        _emailTemplateRepository = emailTemplateRepository;
        _systemNotificationRepository = systemNotificationRepository;
        _employeeRepository = employeeRepository;
        _employeeSponsorRepository = employeeSponsorRepository;
        _sponsorRepository = sponsorRepository;
        _separationRepository = separationRepository;
        _subGroupAbsenceRepository = subGroupAbsenceRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _absenceQuotaRepository = absenceQuotaRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _newsRepository = newsRepository;
        _organizationUnitRepository = organizationUnitRepository;
        _orgUnitProfileRepository = orgUnitProfileRepository;
        _organizationUnitTypeRepository = organizationUnitTypeRepository;
        _sentLogRepository = sentLogRepository;
        _delegationResolver = delegationResolver;
        _deliveryService = deliveryService;
        _emailService = emailService;
    }

    // ------------------------------------------------------------------------------------------------------
    // Employee document expiry
    // ------------------------------------------------------------------------------------------------------

    [UnitOfWork]
    public virtual async Task<int> SendEmployeeDocumentExpiryNotificationsAsync(DateTime now)
    {
        var sent = 0;
        var today = now.Date;
        var name = HcmNotificationNames.EmployeeDocumentExpiry;

        // Legacy: "NotificationSent" on the document. Here: a final row in the sent log for this notice.
        var finished = _sentLogRepository.GetAll()
            .Where(l => l.NotificationName == name && l.IsFinal)
            .Select(l => l.RecordId);

        foreach (var documentType in _documentTypeRepository.GetAll().ToList())
        {
            // Legacy: ExpiryDate.AddDays(-NotificationPeriod) <= DateTime.Now -- the same inequality moved to the
            // other side, so it is a plain column comparison. DateTime.Now with its time of day, as legacy had.
            var dueBy = now.AddDays(documentType.NotificationPeriod);

            var documents = _employeeDocumentRepository.GetAll()
                .Where(d => d.DocumentTypeId == documentType.Id
                            && d.Employee.Status == EmployeeStatus.Active
                            && d.ExpiryDate != default
                            && d.ExpiryDate <= dueBy
                            && !finished.Contains((long)d.Id))
                .OrderBy(d => d.Id)
                .ToList();

            if (documents.Count == 0)
            {
                continue;
            }

            var config = FindConfig(documentType.NotificationConfigId);

            foreach (var document in documents)
            {
                try
                {
                    if (await SendEmployeeDocumentExpiryAsync(document, documentType, config, now))
                    {
                        sent++;
                    }

                    await CurrentUnitOfWork.SaveChangesAsync();
                }
                catch (Exception exception)
                {
                    // Legacy collected these into errString and carried on with the next document.
                    Logger.Error(
                        $"Employee document expiry notice failed for document {document.Id} ({documentType.NameEn}): {exception.Message}",
                        exception);
                }
            }
        }

        return sent;
    }

    private async Task<bool> SendEmployeeDocumentExpiryAsync(
        EmployeeDocument document,
        DocumentType documentType,
        NotificationConfig config,
        DateTime now)
    {
        var name = HcmNotificationNames.EmployeeDocumentExpiry;
        var today = now.Date;

        // Legacy: an inactive document (a newer one of the same type exists), or a type with no configuration,
        // is closed off without sending -- and stays closed even if a configuration is added later.
        if (config == null || DocumentStatusOf(document, today) == DocumentStatus.Inactive)
        {
            await LogAsync(name, NotificationRuleSourceCodes.EmployeeDocument, document.Id, today, 0, wasSent: false, isFinal: true);
            return false;
        }

        var history = _sentLogRepository.GetAll()
            .Where(l => l.NotificationName == name && l.RecordId == document.Id && l.WasSent)
            .Select(l => l.SentDate)
            .ToList();

        var frequency = documentType.NotificationFrequency;
        var countLimit = documentType.NotificationCountLimit;
        var count = history.Count;
        var lastSentOn = history.Count == 0 ? default : history.Max().Date;

        var allowToSend = AllowDocumentExpirySend(frequency, countLimit, count, lastSentOn, today);
        var delivered = false;
        var recipientCount = 0;

        if (allowToSend)
        {
            var result = await _deliveryService.DeliverAsync(new NotificationDelivery
            {
                NotificationName = name,
                NotificationConfig = config,
                Subject = document,
                EmailTemplate = TemplateOf(config, null),
                MergeValues = new Dictionary<string, object>
                {
                    ["EmployeeSponsor"] = SponsorNameAt(document.EmployeeId, document.ExpiryDate)
                },
                FallbackMessage = L(
                    "EmployeeDocumentExpiryNotificationMessage",
                    documentType.NameEn,
                    document.DocumentNum,
                    document.ExpiryDate.ToString("dd/MM/yyyy"))
            });

            delivered = result.InAppCount > 0 || result.EmailSent;
            recipientCount = result.RecipientCount;
            count++;
        }

        // Legacy: NotificationSent = (NotificationCount >= NotificationCountLimit || frequency == Once), set after
        // every pass -- including passes that sent nothing, which is how a count limit of 0 closes a document
        // without it ever being sent.
        var isFinal = count >= countLimit || frequency == NotificationFrequency.Once;

        if (allowToSend || isFinal)
        {
            // A send counts towards the limit whether or not anybody was reachable, as legacy's
            // NotificationCount + 1 did.
            await LogAsync(name, NotificationRuleSourceCodes.EmployeeDocument, document.Id, today,
                recipientCount, wasSent: allowToSend, isFinal: isFinal);
        }

        return allowToSend && delivered;
    }

    /// <summary>
    /// Legacy "Find SendNofiticationAndEmail", verbatim. Note the order: "Once" is allowed without looking at the
    /// count limit at all, and a repeating frequency with a count limit of zero is never allowed.
    /// </summary>
    public static bool AllowDocumentExpirySend(
        NotificationFrequency frequency,
        int countLimit,
        int count,
        DateTime lastSentOn,
        DateTime today)
    {
        if (frequency == NotificationFrequency.Once)
        {
            return true;
        }

        if (countLimit <= count)
        {
            return false;
        }

        if (lastSentOn == default)
        {
            return true;
        }

        var gapDays = today.Date.Subtract(lastSentOn.Date).TotalDays;

        return frequency switch
        {
            NotificationFrequency.Daily => gapDays >= 1,
            NotificationFrequency.Weekly => gapDays >= 7,
            NotificationFrequency.Monthly => gapDays >= 30,
            _ => false
        };
    }

    private DocumentStatus DocumentStatusOf(EmployeeDocument document, DateTime today)
    {
        var latestOtherIssueDate = _employeeDocumentRepository.GetAll()
            .Where(d => d.Id != document.Id
                        && d.EmployeeId == document.EmployeeId
                        && d.DocumentTypeId == document.DocumentTypeId)
            .Select(d => (DateTime?)d.IssueDate)
            .Max();

        return DocumentStatusCalculator.Calculate(document.IssueDate, document.ExpiryDate, latestOtherIssueDate, today);
    }

    /// <summary>Legacy "Get EmployeeSponsor": the sponsor in effect on the expiry date, by English name, or "".</summary>
    private string SponsorNameAt(int employeeId, DateTime date)
    {
        var sponsorId = _employeeSponsorRepository.GetAll()
            .Where(s => s.EmployeeId == employeeId && s.StartDate <= date && s.EndDate >= date)
            .Select(s => (int?)s.SponsorId)
            .FirstOrDefault();

        if (sponsorId == null)
        {
            return string.Empty;
        }

        return _sponsorRepository.GetAll()
            .Where(s => s.Id == sponsorId.Value)
            .Select(s => s.NameEn)
            .FirstOrDefault() ?? string.Empty;
    }

    // ------------------------------------------------------------------------------------------------------
    // Company (organization) document expiry
    // ------------------------------------------------------------------------------------------------------

    [UnitOfWork]
    public virtual async Task<int> SendCompanyDocumentExpiryNotificationsAsync(DateTime now)
    {
        var sent = 0;
        var today = now.Date;

        foreach (var documentType in _documentTypeRepository.GetAll().ToList())
        {
            var dueBy = now.AddDays(documentType.NotificationPeriod);

            var documents = _organizationDocumentRepository.GetAll()
                .Where(d => d.DocumentTypeId == documentType.Id
                            && d.ExpiryDate != default
                            && d.ExpiryDate <= dueBy
                            && !d.NotificationSent)
                .OrderBy(d => d.Id)
                .ToList();

            if (documents.Count == 0)
            {
                continue;
            }

            var config = FindConfig(documentType.NotificationConfigId);

            foreach (var document in documents)
            {
                try
                {
                    var delivered = false;
                    var recipientCount = 0;

                    if (config != null)
                    {
                        var result = await _deliveryService.DeliverAsync(new NotificationDelivery
                        {
                            NotificationName = HcmNotificationNames.CompanyDocumentExpiry,
                            NotificationConfig = config,
                            Subject = document,
                            EmailTemplate = TemplateOf(config, null),
                            FallbackMessage = L(
                                "CompanyDocumentExpiryNotificationMessage",
                                documentType.NameEn,
                                document.DocumentNum,
                                document.ExpiryDate.ToString("dd/MM/yyyy"))
                        });

                        delivered = result.InAppCount > 0 || result.EmailSent;
                        recipientCount = result.RecipientCount;
                    }

                    // Legacy: set whether or not a configuration was found, so a document whose type has none is
                    // never looked at again.
                    document.NotificationSent = true;

                    await LogAsync(HcmNotificationNames.CompanyDocumentExpiry, NotificationRuleSourceCodes.CompanyDocument,
                        document.Id, today, recipientCount, wasSent: delivered, isFinal: true);

                    await CurrentUnitOfWork.SaveChangesAsync();

                    if (delivered)
                    {
                        sent++;
                    }
                }
                catch (Exception exception)
                {
                    Logger.Error(
                        $"Company document expiry notice failed for document {document.Id} ({documentType.NameEn}): {exception.Message}",
                        exception);
                }
            }
        }

        return sent;
    }

    // ------------------------------------------------------------------------------------------------------
    // Probation
    // ------------------------------------------------------------------------------------------------------

    [UnitOfWork]
    public virtual async Task<int> SendProbationNotificationsAsync(DateTime now)
    {
        var today = now.Date;
        var systemNotification = ActiveSystemNotification(ProbationNotificationName);
        var config = systemNotification == null ? null : FindConfig(systemNotification.NotificationConfigId);

        if (config == null)
        {
            return 0;
        }

        // Legacy: ProbationEndDate.Date.AddDays(-ReminderDays) == today, as a day-wide range on the column.
        var target = today.AddDays(systemNotification.ReminderDays);
        var dayAfter = target.AddDays(1);

        var employees = _employeeRepository.GetAll()
            .Where(e => e.Status == EmployeeStatus.Active
                        && e.ProbationEndDate >= target
                        && e.ProbationEndDate < dayAfter)
            .OrderBy(e => e.Id)
            .ToList();

        var template = TemplateOf(config, ProbationNotificationName);
        var sent = 0;

        foreach (var employee in employees)
        {
            if (SentToday(HcmNotificationNames.ProbationEnd, employee.Id, today))
            {
                continue;
            }

            var result = await _deliveryService.DeliverAsync(new NotificationDelivery
            {
                NotificationName = HcmNotificationNames.ProbationEnd,
                NotificationConfig = config,
                Subject = employee,
                EmailTemplate = template,
                FallbackMessage = L("ProbationEndNotificationMessage", employee.FullNameEn, employee.ProbationEndDate.ToString("dd/MM/yyyy"))
            });

            await LogAsync(HcmNotificationNames.ProbationEnd, NotificationRuleSourceCodes.Employee, employee.Id, today,
                result.RecipientCount, wasSent: true, isFinal: false);
            await CurrentUnitOfWork.SaveChangesAsync();

            if (result.InAppCount > 0 || result.EmailSent)
            {
                sent++;
            }
        }

        return sent;
    }

    // ------------------------------------------------------------------------------------------------------
    // Separation
    // ------------------------------------------------------------------------------------------------------

    [UnitOfWork]
    public virtual async Task<int> SendSeparationNotificationsAsync(DateTime now)
    {
        var today = now.Date;
        var systemNotification = ActiveSystemNotification(SeparationNotificationName);
        var config = systemNotification == null ? null : FindConfig(systemNotification.NotificationConfigId);

        if (config == null)
        {
            return 0;
        }

        var target = today.AddDays(systemNotification.ReminderDays);
        var dayAfter = target.AddDays(1);

        // Legacy filtered on SeparationType != null and Employee != null only: no employee status, and a revoked
        // separation is not excluded. Kept.
        var separations = _separationRepository.GetAll()
            .Where(s => s.LastDayInService >= target && s.LastDayInService < dayAfter)
            .OrderBy(s => s.Id)
            .ToList();

        if (separations.Count == 0)
        {
            return 0;
        }

        // LEGACY ODDITY, KEPT AND PINNED: the "EmployeeName" merge value is built ONCE, from the FIRST separation
        // in the list, and that same name is sent in every notice of the run. With two employees leaving on the
        // same day, both notices name the first one.
        var firstEmployeeId = separations[0].EmployeeId;
        var firstEmployeeName = _employeeRepository.GetAll()
            .Where(e => e.Id == firstEmployeeId)
            .Select(e => e.FirstNameEn)
            .FirstOrDefault();

        var mergeValues = new Dictionary<string, object> { ["EmployeeName"] = firstEmployeeName };
        var template = TemplateOf(config, SeparationNotificationName);
        var sent = 0;

        foreach (var separation in separations)
        {
            if (SentToday(HcmNotificationNames.Separation, separation.Id, today))
            {
                continue;
            }

            var result = await _deliveryService.DeliverAsync(new NotificationDelivery
            {
                NotificationName = HcmNotificationNames.Separation,
                NotificationConfig = config,
                Subject = separation,
                EmailTemplate = template,
                MergeValues = mergeValues,
                FallbackMessage = L("SeparationNotificationMessage", firstEmployeeName, separation.LastDayInService.ToString("dd/MM/yyyy"))
            });

            await LogAsync(HcmNotificationNames.Separation, SeparationSourceCode, separation.Id, today,
                result.RecipientCount, wasSent: true, isFinal: false);
            await CurrentUnitOfWork.SaveChangesAsync();

            if (result.InAppCount > 0 || result.EmailSent)
            {
                sent++;
            }
        }

        return sent;
    }

    // ------------------------------------------------------------------------------------------------------
    // Annual leave balance over the limit
    // ------------------------------------------------------------------------------------------------------

    [UnitOfWork]
    public virtual async Task<int> SendAnnualLeaveMaxLimitNotificationsAsync(DateTime now)
    {
        var today = now.Date;
        var systemNotification = ActiveSystemNotification(AnnualLeaveMaxLimitNotificationName);
        var config = systemNotification == null ? null : FindConfig(systemNotification.NotificationConfigId);

        if (config == null)
        {
            return 0;
        }

        var annualLeaveTypeIds = _absenceTypeRepository.GetAll()
            .Where(t => t.Code == AnnualLeaveAbsenceTypeCode)
            .Select(t => t.Id)
            .ToList();

        if (annualLeaveTypeIds.Count == 0)
        {
            return 0;
        }

        var subGroupLimits = _subGroupAbsenceRepository.GetAll()
            .Where(a => annualLeaveTypeIds.Contains(a.AbsenceTypeId) && a.MaxLimitReachNotificationInDays != 0)
            .OrderBy(a => a.Id)
            .Select(a => new { a.EmployeeSubGroupId, a.MaxLimitReachNotificationInDays })
            .ToList();

        if (subGroupLimits.Count == 0)
        {
            return 0;
        }

        // Legacy: "...Where(r => r.EmployeeSubGroup.Oid == ...).Select(r => r.MaxLimitReachNotificationInDays).FirstOrDefault()"
        // -- the first row for the sub group wins when there are several.
        var limitBySubGroup = subGroupLimits
            .GroupBy(a => a.EmployeeSubGroupId)
            .ToDictionary(g => g.Key, g => g.First().MaxLimitReachNotificationInDays);

        var subGroupIds = limitBySubGroup.Keys.ToList();

        var assignments = _orgAssignmentRepository.GetAll()
            .Where(a => a.Employee.Status == EmployeeStatus.Active
                        && a.StartDate <= today
                        && a.EndDate >= today
                        && a.EmployeeSubGroupId != null
                        && subGroupIds.Contains(a.EmployeeSubGroupId.Value))
            .OrderBy(a => a.Id)
            .Select(a => new { a.EmployeeId, EmployeeSubGroupId = a.EmployeeSubGroupId.Value })
            .ToList();

        var template = TemplateOf(config, AnnualLeaveMaxLimitNotificationName);
        var sent = 0;

        foreach (var assignment in assignments)
        {
            var maxLimitDays = limitBySubGroup[assignment.EmployeeSubGroupId];

            var quota = _absenceQuotaRepository.GetAll()
                .Where(q => q.EmployeeId == assignment.EmployeeId
                            && annualLeaveTypeIds.Contains(q.AbsenceTypeId)
                            && q.EntitlementFrom <= today
                            && q.EntitlementTo >= today
                            && !q.NotificationSent)
                .OrderBy(q => q.Id)
                .FirstOrDefault();

            if (quota == null || QuotaBalance(quota) < maxLimitDays)
            {
                continue;
            }

            try
            {
                var employee = _employeeRepository.Get(assignment.EmployeeId);
                var balance = QuotaBalance(quota);

                var result = await _deliveryService.DeliverAsync(new NotificationDelivery
                {
                    NotificationName = HcmNotificationNames.AnnualLeaveMaxLimit,
                    NotificationConfig = config,
                    Subject = employee,
                    EmailTemplate = template,

                    // Legacy's key, dot and all: a template token with ObjectKey set and value
                    // "Employee.AbsenceQuota" picks it up.
                    MergeValues = new Dictionary<string, object> { ["Employee.AbsenceQuota"] = balance.ToString() },
                    FallbackMessage = L("AnnualLeaveMaxLimitNotificationMessage", employee.FullNameEn, balance)
                });

                // Legacy also set IsSystemGenerated on the quota; that column was not ported.
                quota.NotificationSent = true;

                await LogAsync(HcmNotificationNames.AnnualLeaveMaxLimit, AbsenceQuotaSourceCode, quota.Id, today,
                    result.RecipientCount, wasSent: true, isFinal: true);
                await CurrentUnitOfWork.SaveChangesAsync();

                if (result.InAppCount > 0 || result.EmailSent)
                {
                    sent++;
                }
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"Annual leave limit notice failed for employee {assignment.EmployeeId}: {exception.Message}",
                    exception);
            }
        }

        return sent;
    }

    /// <summary>Legacy <c>EmployeeAbsenceQuota.QuotaBalance</c>: accrued less used, adjusted and forfeited, to 2 places.</summary>
    public static double QuotaBalance(EmployeeAbsenceQuota quota)
    {
        return Math.Round(quota.QuotaAccrued - (quota.QuotaUsed + quota.QuotaAdjusted + quota.QuotaForfeited), 2);
    }

    // ------------------------------------------------------------------------------------------------------
    // News / announcements
    // ------------------------------------------------------------------------------------------------------

    [UnitOfWork]
    public virtual async Task<int> SendNewsNotificationsAsync(DateTime now)
    {
        var today = now.Date;
        var tomorrow = today.AddDays(1);

        var newsItems = _newsRepository.GetAll()
            .Where(n => n.NewsTypeId != null
                        && n.IssueDate < tomorrow
                        && n.ExpiryDate >= today
                        && !n.NotificationSent)
            .OrderBy(n => n.Id)
            .ToList();

        var sent = 0;

        foreach (var news in newsItems)
        {
            try
            {
                var reached = 0;

                if (news.SendNotification)
                {
                    var message = string.IsNullOrWhiteSpace(news.Comments) ? news.Title : news.Comments;

                    reached += await _deliveryService.DeliverInAppAsync(
                        HcmNotificationNames.News,
                        message,
                        InAppNewsAudience(news, today));
                }

                if (news.SendEmail)
                {
                    reached += await EmailNewsAsync(news, today);
                }

                news.NotificationSent = true;

                await LogAsync(HcmNotificationNames.News, NewsSourceCode, news.Id, today, reached,
                    wasSent: reached > 0, isFinal: true);
                await CurrentUnitOfWork.SaveChangesAsync();

                if (reached > 0)
                {
                    sent++;
                }
            }
            catch (Exception exception)
            {
                Logger.Error($"News notice failed for news {news.Id}: {exception.Message}", exception);
            }
        }

        return sent;
    }

    /// <summary>
    /// Legacy <c>SendNotification(News, ...)</c>'s audience, as users. Read off the NEWS's own organisation level:
    /// "Level0" (the root) reaches every active employee with a current org assignment; any other level reaches
    /// those whose current unit is the news's unit or below it. Each employee's stand-in, if they have one
    /// today, is told too.
    ///
    /// <para>
    /// LEGACY ODDITY, KEPT: a news item with no organisation level reaches nobody in the app. Legacy read
    /// <c>news.OrganizationUnitType.Code</c> without a null check, threw, caught it silently -- and then still
    /// marked the news as sent.
    /// </para>
    /// </summary>
    private List<long> InAppNewsAudience(News news, DateTime today)
    {
        if (news.OrganizationUnitTypeId == null)
        {
            return new List<long>();
        }

        var levelCode = _organizationUnitTypeRepository.GetAll()
            .Where(t => t.Id == news.OrganizationUnitTypeId.Value)
            .Select(t => t.Code)
            .FirstOrDefault();

        var current = _orgAssignmentRepository.GetAll()
            .Where(a => a.StartDate <= today && a.EndDate >= today && a.Employee.Status == EmployeeStatus.Active);

        List<int> employeeIds;

        if (levelCode == OrganizationUnitType.Level0Code)
        {
            employeeIds = current.Select(a => a.EmployeeId).Distinct().ToList();
        }
        else
        {
            var unitIds = UnitAndDescendantIds(news.OrganizationUnitId);
            employeeIds = current
                .Where(a => a.OrganizationUnitId != null && unitIds.Contains(a.OrganizationUnitId.Value))
                .Select(a => a.EmployeeId)
                .Distinct()
                .ToList();
        }

        var withDelegates = new HashSet<int>(employeeIds);
        foreach (var employeeId in employeeIds)
        {
            var standIn = _delegationResolver.ResolveDelegateFor(employeeId);
            if (standIn.HasValue)
            {
                withDelegates.Add(standIn.Value);
            }
        }

        var audienceIds = withDelegates.ToList();

        return _employeeRepository.GetAll()
            .Where(e => audienceIds.Contains(e.Id) && e.UserId != null)
            .Select(e => e.UserId.Value)
            .ToList();
    }

    /// <summary>
    /// Legacy <c>EmailService.SendEmail_ForAnnouncement</c>. Its audience is read off the news's UNIT's level
    /// (not the news's own level field, which the in-app leg reads -- legacy differed between the two and so
    /// does this): the root unit reaches every active employee; any other unit, the active employees currently
    /// in it or below it. No unit, or a unit with no level, emails nobody. Subject: the title (legacy's
    /// misnamed <c>DocumentNum</c>); body: the comments, as plain text. No stand-ins.
    /// </summary>
    private async Task<int> EmailNewsAsync(News news, DateTime today)
    {
        if (news.OrganizationUnitId == null)
        {
            return 0;
        }

        var levelCode = _orgUnitProfileRepository.GetAll()
            .Where(p => p.OrganizationUnitId == news.OrganizationUnitId.Value)
            .Select(p => p.OrganizationUnitType.Code)
            .FirstOrDefault();

        if (levelCode == null)
        {
            return 0;
        }

        List<Employee> employees;

        if (levelCode == OrganizationUnitType.Level0Code)
        {
            employees = _employeeRepository.GetAll().Where(e => e.Status == EmployeeStatus.Active).ToList();
        }
        else
        {
            var unitIds = UnitAndDescendantIds(news.OrganizationUnitId);
            var employeeIds = _orgAssignmentRepository.GetAll()
                .Where(a => a.StartDate <= today
                            && a.EndDate >= today
                            && a.OrganizationUnitId != null
                            && unitIds.Contains(a.OrganizationUnitId.Value))
                .Select(a => a.EmployeeId)
                .Distinct()
                .ToList();

            employees = _employeeRepository.GetAll()
                .Where(e => employeeIds.Contains(e.Id) && e.Status == EmployeeStatus.Active)
                .ToList();
        }

        // Legacy built the mail by hand from the news itself, with no template. A template object that is never
        // saved carries the same subject and body through the one email path this port has, which keeps the
        // self-service "email notification" switch and the email log working for news too.
        var adHocTemplate = new EmailTemplate
        {
            Subject = news.Title,
            EmailBodyText = news.Comments
        };

        var emailed = 0;

        foreach (var employee in employees.Where(e => !string.IsNullOrEmpty(e.Email)))
        {
            var response = await _emailService.SendEmailAsync(adHocTemplate, employee, news);
            if (response.Status)
            {
                emailed++;
            }
        }

        return emailed;
    }

    /// <summary>
    /// The unit and every unit below it. Legacy matched the news's unit against each of an assignment's seven
    /// org-level columns; this port keeps one unit per assignment on Zero's tree, where "below" is a code
    /// prefix.
    /// </summary>
    private List<long> UnitAndDescendantIds(long? organizationUnitId)
    {
        if (organizationUnitId == null)
        {
            return new List<long>();
        }

        var code = _organizationUnitRepository.GetAll()
            .Where(u => u.Id == organizationUnitId.Value)
            .Select(u => u.Code)
            .FirstOrDefault();

        if (code == null)
        {
            return new List<long>();
        }

        return _organizationUnitRepository.GetAll()
            .Where(u => u.Code.StartsWith(code))
            .Select(u => u.Id)
            .ToList();
    }

    // ------------------------------------------------------------------------------------------------------
    // Shared
    // ------------------------------------------------------------------------------------------------------

    private SystemNotification ActiveSystemNotification(string notificationName)
    {
        return _systemNotificationRepository.GetAll()
            .FirstOrDefault(n => n.NotificationName == notificationName && n.IsActive);
    }

    private NotificationConfig FindConfig(int? notificationConfigId)
    {
        return notificationConfigId.HasValue
            ? _notificationConfigRepository.GetAll().FirstOrDefault(c => c.Id == notificationConfigId.Value)
            : null;
    }

    /// <summary>The configuration's own template; failing that, the one coded after the notification.</summary>
    private EmailTemplate TemplateOf(NotificationConfig config, string fallbackCode)
    {
        if (config?.EmailTemplateId != null)
        {
            var own = _emailTemplateRepository.GetAll().FirstOrDefault(t => t.Id == config.EmailTemplateId.Value);
            if (own != null)
            {
                return own;
            }
        }

        return fallbackCode == null
            ? null
            : _emailTemplateRepository.GetAll().FirstOrDefault(t => t.Code == fallbackCode);
    }

    private bool SentToday(string notificationName, long recordId, DateTime today)
    {
        return _sentLogRepository.GetAll()
            .Any(l => l.NotificationName == notificationName && l.RecordId == recordId && l.SentDate == today);
    }

    private Task LogAsync(
        string notificationName,
        string sourceCode,
        long recordId,
        DateTime today,
        int recipientCount,
        bool wasSent,
        bool isFinal)
    {
        return _sentLogRepository.InsertAsync(new NotificationSentLog
        {
            TenantId = CurrentUnitOfWork.GetTenantId() ?? 0,
            NotificationMasterId = null,
            NotificationName = notificationName,
            SourceCode = sourceCode,
            RecordId = recordId,
            SentDate = today,
            RecipientCount = recipientCount,
            WasSent = wasSent,
            IsFinal = isFinal
        });
    }
}
