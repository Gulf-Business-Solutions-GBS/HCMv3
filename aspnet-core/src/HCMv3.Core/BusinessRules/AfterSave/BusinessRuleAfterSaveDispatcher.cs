using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.Localization;
using Abp.Runtime.Session;
using Abp.Threading;
using Abp.UI;
using Castle.Core.Logging;
using HCMv3.BusinessRules.Records;
using HCMv3.BusinessRules.Services;

namespace HCMv3.BusinessRules.AfterSave;

/// <summary>
/// Holds the records saved in a unit of work whose AfterSave rules still have to run, and runs them once the
/// save has written those records -- inside the same transaction.
/// </summary>
public interface IBusinessRuleAfterSaveDispatcher
{
    /// <summary>
    /// Queues <paramref name="entity"/> for its AfterSave rules. <paramref name="populateParameters"/> fills the
    /// rule context when the rules run; <paramref name="entityId"/> is read then too, because an inserted record
    /// has no id until its save has executed.
    /// </summary>
    void Enqueue(string baseObjectCode, Action<RuleExecutionContext> populateParameters, Func<int> entityId);

    /// <summary>Whether the current unit of work has records waiting for their AfterSave rules.</summary>
    bool HasPending { get; }

    /// <summary>
    /// Runs the AfterSave rules of every record queued so far and stages the changes they ask for. The caller
    /// saves those changes. <paramref name="round"/> counts the rounds of this save, so a chain of rules
    /// triggering rules stops after <see cref="BusinessRuleConsts.MaxAfterSaveRounds"/>.
    /// </summary>
    void RunPending(int round);

    /// <summary>
    /// The message for a follow-up change the database refused -- a required column left empty, a key pointing
    /// nowhere. Supplied here so the DbContext that catches the failure does not need localization of its own.
    /// </summary>
    string DescribeFollowUpSaveFailure(Exception exception);
}

/// <summary>
/// <inheritdoc cref="IBusinessRuleAfterSaveDispatcher"/>
///
/// <para>
/// <b>Why AfterSave is not simply ABP's EntityCreated event.</b> ABP raises EntityCreated and EntityUpdated
/// when the unit of work COMPLETES -- after the transaction has committed. Follow-up changes made there would be
/// a second transaction, and a follow-up that failed would leave the original saved: exactly what the business
/// ruled out ("raise an error and do nothing"). So the entity hook queues the record here during the save, and
/// <c>HCMv3DbContext</c> drains the queue straight after its SQL has run, while the transaction is still open.
/// The follow-up is saved into that transaction; anything that throws rolls both back.
/// </para>
///
/// <para>
/// <b>Why a singleton with its queue keyed by unit of work.</b> The hook that fills the queue is transient and
/// the DbContext that drains it is a different object again; they share nothing but the unit of work they run
/// in. A <see cref="ConditionalWeakTable{TKey,TValue}"/> keyed on that unit of work gives each save its own
/// queue and lets it be collected with the unit of work, so an abandoned save cannot leak entries.
/// </para>
/// </summary>
public class BusinessRuleAfterSaveDispatcher : IBusinessRuleAfterSaveDispatcher, ISingletonDependency
{
    private readonly ConditionalWeakTable<IActiveUnitOfWork, List<PendingAfterSave>> _pending = new();

    private readonly IIocResolver _iocResolver;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IAbpSession _session;
    private readonly ILocalizationManager _localizationManager;

    public BusinessRuleAfterSaveDispatcher(
        IIocResolver iocResolver,
        IUnitOfWorkManager unitOfWorkManager,
        IAbpSession session,
        ILocalizationManager localizationManager)
    {
        _iocResolver = iocResolver;
        _unitOfWorkManager = unitOfWorkManager;
        _session = session;
        _localizationManager = localizationManager;
        Logger = NullLogger.Instance;
    }

    public ILogger Logger { get; set; }

    public bool HasPending
    {
        get
        {
            var unitOfWork = _unitOfWorkManager.Current;

            return unitOfWork != null
                   && _pending.TryGetValue(unitOfWork, out var queue)
                   && queue.Count > 0;
        }
    }

    public void Enqueue(string baseObjectCode, Action<RuleExecutionContext> populateParameters, Func<int> entityId)
    {
        var unitOfWork = _unitOfWorkManager.Current
                         ?? throw new InvalidOperationException(
                             "AfterSave rules can only be queued inside a unit of work.");

        _pending.GetValue(unitOfWork, _ => new List<PendingAfterSave>())
            .Add(new PendingAfterSave(baseObjectCode, populateParameters, entityId));
    }

    public void RunPending(int round)
    {
        var unitOfWork = _unitOfWorkManager.Current;

        if (unitOfWork == null || !_pending.TryGetValue(unitOfWork, out var queue) || queue.Count == 0)
        {
            return;
        }

        if (round > BusinessRuleConsts.MaxAfterSaveRounds)
        {
            // Cleared before throwing, so the unit of work cannot be retried into the same loop.
            queue.Clear();

            throw new UserFriendlyException(
                _localizationManager.GetString(HCMv3Consts.LocalizationSourceName, "BusinessRulesKeptTriggeringEachOther"));
        }

        // Taken as a batch and cleared first: the changes these rules stage will queue records of their own when
        // they are saved, and those belong to the NEXT round.
        var batch = queue.ToArray();
        queue.Clear();

        using var registry = _iocResolver.ResolveAsDisposable<IRuleBaseObjectRegistry>();
        using var runner = _iocResolver.ResolveAsDisposable<IBusinessRuleRunner>();
        using var writer = _iocResolver.ResolveAsDisposable<IRuleRecordWriter>();

        foreach (var item in batch)
        {
            var baseObject = registry.Object.Find(item.BaseObjectCode);

            if (baseObject == null)
            {
                Logger.Warn("AfterSave rules were queued for base object '" + item.BaseObjectCode
                            + "', which no provider declares. They were skipped.");
                continue;
            }

            var context = new RuleExecutionContext(baseObject, RuleEventType.AfterSave, DateTime.Today, _session.TenantId)
            {
                // Still inside the save's SaveChanges; see BusinessRuleEntityHookBase.
                SuppressDatabaseLogging = true
            };

            item.PopulateParameters(context);

            // RunAsync throws on any rule error, so a rule set is applied whole or not at all.
            var result = AsyncHelper.RunSync(() => runner.Object.RunAsync(context, item.EntityId()));

            if (result.RecordChanges.Count > 0)
            {
                writer.Object.Apply(result.RecordChanges);
            }
        }
    }

    public string DescribeFollowUpSaveFailure(Exception exception)
    {
        // The innermost message is the database's own reason ("cannot insert NULL into column X"), which is
        // the one an administrator can act on; the outer EF wrapper only says that an update failed.
        var root = exception;

        while (root.InnerException != null)
        {
            root = root.InnerException;
        }

        Logger.Error("An AfterSave business rule's follow-up change could not be saved.", exception);

        return _localizationManager.GetString(HCMv3Consts.LocalizationSourceName, "BusinessRuleFollowUpSaveFailed")
            .Replace("{0}", root.Message);
    }

    private sealed class PendingAfterSave
    {
        public PendingAfterSave(string baseObjectCode, Action<RuleExecutionContext> populateParameters, Func<int> entityId)
        {
            BaseObjectCode = baseObjectCode;
            PopulateParameters = populateParameters;
            EntityId = entityId;
        }

        public string BaseObjectCode { get; }

        public Action<RuleExecutionContext> PopulateParameters { get; }

        public Func<int> EntityId { get; }
    }
}

/// <summary>For a DbContext built without the container -- design-time tooling and migrations.</summary>
public sealed class NullBusinessRuleAfterSaveDispatcher : IBusinessRuleAfterSaveDispatcher
{
    public static readonly NullBusinessRuleAfterSaveDispatcher Instance = new();

    public bool HasPending => false;

    public void Enqueue(string baseObjectCode, Action<RuleExecutionContext> populateParameters, Func<int> entityId)
    {
    }

    public void RunPending(int round)
    {
    }

    public string DescribeFollowUpSaveFailure(Exception exception) => exception.Message;
}
