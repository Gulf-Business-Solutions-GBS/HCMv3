using System;

using Abp.Domain.Entities;
using Abp.Events.Bus.Entities;
using Abp.Events.Bus.Handlers;
using Abp.Runtime.Session;
using Abp.Threading;
using Castle.Core.Logging;
using HCMv3.BusinessRules.Services;

namespace HCMv3.BusinessRules.Hooks;

/// <summary>
/// Runs the rules armed on an entity's save events. One subclass per rule-enabled entity.
///
/// <para>
/// A subclass per entity, rather than one open-generic handler, because ABP's event bus dispatches on the
/// closed type <c>EntityCreatingEventData&lt;Employee&gt;</c> -- there is no way to subscribe to "any
/// entity" without subscribing to literally every save in the system. That constraint turns out to be the
/// feature: an entity is rule-enabled only when somebody has deliberately written five lines to make it so,
/// which is the explicit opt-in the whitelist design is built around.
/// </para>
///
/// <para><b>Cost when nothing is armed.</b> One cache lookup returning an empty list, then return. That is
/// the case for every entity in the system today, so the engine's presence is not something an unrelated
/// save can feel.</para>
///
/// <para><b>Why the synchronous bridge.</b> ABP triggers <c>EntityCreating</c> and <c>EntityUpdating</c>
/// synchronously from inside <c>SaveChanges</c> -- there is no async trigger for the pre-save events -- so
/// there is nowhere to await. <see cref="AsyncHelper.RunSync"/> is the same bridge ABP itself uses, and it
/// is safe here because ASP.NET Core has no synchronization context to deadlock against. The rule side is
/// kept cheap to match: the context is marked <c>SuppressDatabaseLogging</c> so nothing opens a nested
/// transaction while the outer save is in flight.</para>
///
/// <para><b>Registration is explicit</b>, in <c>HCMv3CoreModule.Initialize</c>. Conventional registration
/// binds a class to interfaces matching its name by suffix, and no sensible handler name ends in
/// <c>EventHandler`1</c>, so a conventionally registered hook would be registered as itself, the event bus
/// would never see it, and every rule armed on that entity would silently never fire. Registering the
/// closed handler interfaces by hand is what makes the wiring visible.</para>
/// </summary>
public abstract class BusinessRuleEntityHookBase<TEntity> :
    IEventHandler<EntityCreatingEventData<TEntity>>,
    IEventHandler<EntityUpdatingEventData<TEntity>>
    where TEntity : class, IEntity<int>
{
    protected BusinessRuleEntityHookBase(
        IBusinessRuleRunner runner,
        IRuleBaseObjectRegistry registry,
        IAbpSession session)
    {
        Runner = runner;
        Registry = registry;
        Session = session;
        Logger = NullLogger.Instance;
    }

    public ILogger Logger { get; set; }

    protected IBusinessRuleRunner Runner { get; }

    protected IRuleBaseObjectRegistry Registry { get; }

    protected IAbpSession Session { get; }

    /// <summary>The base object code whose rules this hook runs. Must match a registered descriptor.</summary>
    protected abstract string BaseObjectCode { get; }

    /// <summary>
    /// Registers the entity under the parameter names the descriptor declares. Subclasses that expose more
    /// than one parameter -- a companion "original record", say -- override this.
    /// </summary>
    protected abstract void PopulateParameters(RuleExecutionContext context, TEntity entity);

    public void HandleEvent(EntityCreatingEventData<TEntity> eventData)
    {
        Run(eventData.Entity);
    }

    public void HandleEvent(EntityUpdatingEventData<TEntity> eventData)
    {
        Run(eventData.Entity);
    }

    private void Run(TEntity entity)
    {
        if (entity == null)
        {
            return;
        }

        var baseObject = Registry.Find(BaseObjectCode);

        if (baseObject == null)
        {
            // The descriptor is gone but the hook is still wired. Loud in the log, harmless to the save --
            // refusing to save an employee because a rule descriptor was renamed would be a poor trade.
            Logger.Warn(
                "A business rule hook is registered for base object '" + BaseObjectCode
                + "', which no provider declares. No rules will run for it.");

            return;
        }

        var context = new RuleExecutionContext(
            baseObject,
            RuleEventType.BeforeSave,
            DateTime.Today,
            Session.TenantId)
        {
            // See the class remarks: no nested transaction while the outer SaveChanges is in flight.
            SuppressDatabaseLogging = true
        };

        PopulateParameters(context, entity);

        // Id is 0 on an insert; passing null rather than 0 keeps "no id yet" out of the log as a real key.
        AsyncHelper.RunSync(() => Runner.RunAsync(context, entity.Id == 0 ? null : entity.Id));
    }
}
