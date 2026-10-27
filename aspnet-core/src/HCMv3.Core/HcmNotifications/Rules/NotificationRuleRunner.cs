using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using HCMv3.BusinessRules;
using HCMv3.GeneralMasterInfo;
using HCMv3.SystemSecurity;

namespace HCMv3.HcmNotifications.Rules;

/// <inheritdoc cref="INotificationRuleRunner"/>
/// <remarks>
/// <para>
/// DELIBERATE DIFFERENCES FROM LEGACY (owner decisions, design note batch 8 round B section 3):
/// </para>
/// <list type="bullet">
/// <item><description>Once per record PER RULE, logged in <see cref="NotificationSentLog"/>, instead of one
/// <c>NotificationSent</c> flag on the record shared by every rule -- two rules on the same table now both
/// fire.</description></item>
/// <item><description>The sub type filters. Legacy showed it and ignored it.</description></item>
/// <item><description>Errors are logged, per rule and per record, instead of swallowed. A rule that cannot
/// run is skipped and named in the log; the other rules still run.</description></item>
/// </list>
/// <para>
/// KEPT FROM LEGACY: a matched record is logged as done even when the configuration addresses nobody today,
/// exactly as legacy set its flag after the send whatever the send found. Otherwise a record about an
/// employee with no manager would be re-evaluated and re-"sent" to nobody every four hours forever.
/// </para>
/// </remarks>
public class NotificationRuleRunner : HCMv3DomainServiceBase, INotificationRuleRunner
{
    private readonly IRepository<NotificationMaster> _ruleRepository;
    private readonly IRepository<NotificationConfig> _notificationConfigRepository;
    private readonly IRepository<EmailTemplate> _emailTemplateRepository;
    private readonly IRepository<NotificationSentLog, long> _sentLogRepository;
    private readonly INotificationRuleSourceRegistry _sourceRegistry;
    private readonly INotificationCriterionEvaluator _criterionEvaluator;
    private readonly INotificationDeliveryService _deliveryService;

    public NotificationRuleRunner(
        IRepository<NotificationMaster> ruleRepository,
        IRepository<NotificationConfig> notificationConfigRepository,
        IRepository<EmailTemplate> emailTemplateRepository,
        IRepository<NotificationSentLog, long> sentLogRepository,
        INotificationRuleSourceRegistry sourceRegistry,
        INotificationCriterionEvaluator criterionEvaluator,
        INotificationDeliveryService deliveryService)
    {
        _ruleRepository = ruleRepository;
        _notificationConfigRepository = notificationConfigRepository;
        _emailTemplateRepository = emailTemplateRepository;
        _sentLogRepository = sentLogRepository;
        _sourceRegistry = sourceRegistry;
        _criterionEvaluator = criterionEvaluator;
        _deliveryService = deliveryService;
    }

    [UnitOfWork]
    public virtual async Task<NotificationRuleRunSummary> RunAsync(DateTime today)
    {
        var summary = new NotificationRuleRunSummary();
        var tenantId = CurrentUnitOfWork.GetTenantId();

        var ruleIds = _ruleRepository.GetAll()
            .Where(r => r.IsEnabled)
            .OrderBy(r => r.Id)
            .Select(r => r.Id)
            .ToList();

        foreach (var ruleId in ruleIds)
        {
            try
            {
                // Its own unit of work, saved record by record: a notice already handed to the bell or the mail
                // server cannot be taken back, so its log row must not be rolled back by a failure later in the
                // same run -- or the next run would send it again.
                using (var unitOfWork = UnitOfWorkManager.Begin(TransactionScopeOption.RequiresNew))
                {
                    using (UnitOfWorkManager.Current.SetTenantId(tenantId))
                    {
                        var sent = await RunRuleAsync(ruleId, today, tenantId);

                        if (sent == null)
                        {
                            summary.RulesSkipped++;
                        }
                        else
                        {
                            summary.RulesRun++;
                            summary.NotificationsSent += sent.Value;
                        }
                    }

                    await unitOfWork.CompleteAsync();
                }
            }
            catch (Exception exception)
            {
                summary.RulesFailed++;
                Logger.Error(
                    $"Notification rule {ruleId} (tenant {tenantId}) could not run: {exception.Message}",
                    exception);
            }
        }

        return summary;
    }

    /// <summary>Null when the rule was skipped (unknown type, missing configuration).</summary>
    private async Task<int?> RunRuleAsync(int ruleId, DateTime today, int? tenantId)
    {
        var rule = _ruleRepository.GetAll().FirstOrDefault(r => r.Id == ruleId);
        if (rule == null || !rule.IsEnabled)
        {
            return null;
        }

        var source = _sourceRegistry.Find(rule.NotificationType);
        if (source == null)
        {
            Logger.Warn(
                $"Notification rule {rule.Id} is about '{rule.NotificationType}', which is not a notification type "
                + "HCM supports. It was skipped; choose a type on the Notification Master screen.");
            return null;
        }

        var config = _notificationConfigRepository.GetAll().FirstOrDefault(c => c.Id == rule.NotificationConfigId);
        if (config == null)
        {
            Logger.Warn($"Notification rule {rule.Id} names notification configuration {rule.NotificationConfigId}, which does not exist. It was skipped.");
            return null;
        }

        // Throws on a criterion that does not parse -- legacy XAF criteria text among them. Caught by the
        // caller, so the rule is named in the log and the others still run.
        var criterion = _criterionEvaluator.Compile(rule.Criterion);

        var template = config.EmailTemplateId.HasValue
            ? _emailTemplateRepository.GetAll().FirstOrDefault(t => t.Id == config.EmailTemplateId.Value)
            : null;

        var notificationName = HcmNotificationNames.ForRuleSource(source.Code);
        var sent = 0;

        await ForEachMatchAsync(source, criterion, rule.NotificationSubTypeId, rule.Id, today, tenantId, async candidate =>
        {
            var delivery = await _deliveryService.DeliverAsync(new NotificationDelivery
            {
                NotificationName = notificationName,
                NotificationConfig = config,
                Subject = candidate.Record,
                EmailTemplate = template,
                FallbackMessage = L("NotificationRuleMessage", L(source.DisplayNameKey), candidate.Label)
            });

            var wasSent = delivery.InAppCount > 0 || delivery.EmailSent;

            await _sentLogRepository.InsertAsync(LogRow(rule.Id, notificationName, source.Code, candidate.RecordId, today, delivery.RecipientCount, wasSent));
            await CurrentUnitOfWork.SaveChangesAsync();

            if (wasSent)
            {
                sent++;
            }
        });

        return sent;
    }

    [UnitOfWork]
    public virtual async Task<NotificationRulePreview> PreviewAsync(
        string sourceCode,
        int? subTypeId,
        string criterionJson,
        int? notificationMasterId,
        DateTime today)
    {
        var preview = new NotificationRulePreview();
        var source = _sourceRegistry.Find(sourceCode);

        var errors = _criterionEvaluator.Validate(source, criterionJson);
        if (errors.Count > 0)
        {
            preview.Errors.AddRange(errors);
            return preview;
        }

        var criterion = _criterionEvaluator.Compile(criterionJson);
        var tenantId = CurrentUnitOfWork.GetTenantId();

        var logged = notificationMasterId.HasValue
            ? new HashSet<long>(_sentLogRepository.GetAll()
                .Where(l => l.NotificationMasterId == notificationMasterId.Value && l.SourceCode == source.Code)
                .Select(l => l.RecordId)
                .ToList())
            : new HashSet<long>();

        await ForEachMatchAsync(source, criterion, subTypeId, null, today, tenantId, candidate =>
        {
            preview.MatchCount++;

            if (!logged.Contains(candidate.RecordId))
            {
                preview.NotYetSentCount++;
            }

            if (preview.Samples.Count < NotificationRuleConsts.PreviewSampleSize)
            {
                // The record itself is an entity; the preview only needs to say which one.
                candidate.Record = null;
                preview.Samples.Add(candidate);
            }

            return Task.CompletedTask;
        });

        return preview;
    }

    [UnitOfWork]
    public virtual async Task<int> MarkExistingAsSentAsync(int notificationMasterId, DateTime today)
    {
        var rule = _ruleRepository.Get(notificationMasterId);

        var source = _sourceRegistry.Find(rule.NotificationType)
                     ?? throw new RuleDefinitionException(
                         "'" + rule.NotificationType + "' is not a notification type HCM supports.");

        var criterion = _criterionEvaluator.Compile(rule.Criterion);
        var tenantId = CurrentUnitOfWork.GetTenantId();
        var notificationName = HcmNotificationNames.ForRuleSource(source.Code);
        var marked = 0;

        await ForEachMatchAsync(source, criterion, rule.NotificationSubTypeId, rule.Id, today, tenantId, async candidate =>
        {
            await _sentLogRepository.InsertAsync(LogRow(rule.Id, notificationName, source.Code, candidate.RecordId, today, 0, wasSent: false));
            marked++;
        });

        await CurrentUnitOfWork.SaveChangesAsync();

        return marked;
    }

    /// <summary>
    /// Walks the source a page at a time in id order and calls <paramref name="onMatch"/> for each record the
    /// criterion accepts. A record the criterion cannot evaluate (bad data, say) is logged and passed over --
    /// it is not logged as sent, so it is tried again once the data is fixed.
    /// </summary>
    private async Task ForEachMatchAsync(
        INotificationRuleSource source,
        NotificationCriterion criterion,
        int? subTypeId,
        int? excludeLoggedForRuleId,
        DateTime today,
        int? tenantId,
        Func<NotificationRuleCandidate, Task> onMatch)
    {
        long after = 0;

        while (true)
        {
            var page = await source.GetCandidatesAsync(new NotificationRuleCandidateQuery
            {
                SubTypeId = subTypeId,
                ExcludeLoggedForRuleId = excludeLoggedForRuleId,
                AfterRecordId = after,
                MaxCount = NotificationRuleConsts.CandidatePageSize
            });

            foreach (var candidate in page)
            {
                bool matched;
                try
                {
                    matched = _criterionEvaluator.Matches(criterion, source, candidate, today, tenantId);
                }
                catch (RuleExecutionException exception)
                {
                    Logger.Warn(
                        $"The notification criterion could not be evaluated for {source.Code} {candidate.RecordId}: {exception.Message}");
                    continue;
                }

                if (matched)
                {
                    await onMatch(candidate);
                }
            }

            if (page.Count < NotificationRuleConsts.CandidatePageSize)
            {
                return;
            }

            after = page[^1].RecordId;
        }
    }

    private NotificationSentLog LogRow(
        int ruleId,
        string notificationName,
        string sourceCode,
        long recordId,
        DateTime today,
        int recipientCount,
        bool wasSent)
    {
        return new NotificationSentLog
        {
            TenantId = CurrentUnitOfWork.GetTenantId() ?? 0,
            NotificationMasterId = ruleId,
            NotificationName = notificationName,
            SourceCode = sourceCode,
            RecordId = recordId,
            SentDate = today.Date,
            RecipientCount = recipientCount,
            WasSent = wasSent,
            IsFinal = true
        };
    }
}
