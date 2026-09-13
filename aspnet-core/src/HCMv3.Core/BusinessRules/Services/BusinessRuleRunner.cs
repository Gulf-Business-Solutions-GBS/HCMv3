using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Abp.Timing;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.BusinessRules.Definition;

namespace HCMv3.BusinessRules.Services;

/// <inheritdoc cref="IBusinessRuleRunner"/>
/// <remarks>
/// <para>
/// The hot path is the empty one. Almost every save of almost every entity has no rules armed against it,
/// so the first thing this does is a cache lookup that returns an empty list, and the cost of the whole
/// engine on that save is one dictionary hit. Everything expensive -- parsing, reflection over
/// descriptors, log writes -- happens only once a tenant has actually armed something.
/// </para>
/// <para>
/// EXECUTION LOG WRITES USE THEIR OWN UNIT OF WORK, and that is not incidental. A rule that aborts a save
/// rolls that save back; if the log entry explaining why were written in the same transaction it would roll
/// back with it, and the one record that would have told an administrator what happened is precisely the
/// one that never survives. <see cref="TransactionScopeOption.RequiresNew"/> keeps the explanation even
/// when the thing it explains is undone.
/// </para>
/// </remarks>
public class BusinessRuleRunner : HCMv3DomainServiceBase, IBusinessRuleRunner
{
    private readonly IBusinessRuleCache _cache;
    private readonly IRuleEvaluator _evaluator;
    private readonly IRuleBaseObjectRegistry _baseObjectRegistry;
    private readonly IRuleDefinitionValidator _validator;
    private readonly IRepository<BusinessRuleExecutionLog, long> _logRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IRuleEntityLoader _entityLoader;

    public BusinessRuleRunner(
        IBusinessRuleCache cache,
        IRuleEvaluator evaluator,
        IRuleBaseObjectRegistry baseObjectRegistry,
        IRuleDefinitionValidator validator,
        IRepository<BusinessRuleExecutionLog, long> logRepository,
        IUnitOfWorkManager unitOfWorkManager,
        IRuleEntityLoader entityLoader)
    {
        _cache = cache;
        _evaluator = evaluator;
        _baseObjectRegistry = baseObjectRegistry;
        _validator = validator;
        _logRepository = logRepository;
        _unitOfWorkManager = unitOfWorkManager;
        _entityLoader = entityLoader;
    }

    public async Task<BusinessRuleRunResult> RunAsync(RuleExecutionContext context, int? targetEntityId = null)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        // Supplied here rather than by every caller: following a declared reference is the engine's job,
        // and a caller that forgot to wire it would get rules silently reading empty related records.
        context.EntityLoader ??= _entityLoader;

        // Every live run is held to what its event permits. Set here, once, rather than trusted to each hook
        // and resolver to remember. TestAsync does not come through this method, so the authoring dry run
        // stays unrestricted.
        context.AllowedActionKinds ??= RuleEventActionPolicy.AllowedKindsFor(context.EventType);

        var result = new BusinessRuleRunResult();

        var rules = await _cache.GetAsync(context.TenantId, context.BaseObject.Code, context.EventType);

        if (rules.Count == 0)
        {
            return result;
        }

        foreach (var rule in rules)
        {
            if (!rule.IsEffectiveOn(context.ExecutionDate))
            {
                continue;
            }

            await RunOneAsync(rule, context, targetEntityId, result);
        }

        result.Trace = context.GetTrace();

        // Business errors are raised after every rule has had its say, so a user correcting a record is
        // told everything that is wrong with it rather than discovering the next problem on each retry.
        if (result.HasErrors)
        {
            throw new UserFriendlyException(string.Join(" ", result.Errors));
        }

        return result;
    }

    public async Task<bool> HasArmedRulesAsync(int? tenantId, string baseObjectCode, RuleEventType eventType, DateTime asOf)
    {
        var rules = await _cache.GetAsync(tenantId, baseObjectCode, eventType);

        return rules.Any(rule => rule.IsEffectiveOn(asOf.Date));
    }

    public async Task<BusinessRuleRunResult> TestAsync(
        string definitionJson,
        string baseObjectCode,
        RulePurpose purpose,
        RuleExecutionContext context)
    {
        context.EntityLoader ??= _entityLoader;

        var result = new BusinessRuleRunResult();
        var baseObject = _baseObjectRegistry.Find(baseObjectCode);

        if (baseObject == null)
        {
            result.Failures.Add("There is no rule base object named '" + baseObjectCode + "'.");
            return result;
        }

        RuleDefinition definition;

        try
        {
            definition = RuleDefinitionSerializer.Parse(definitionJson);
        }
        catch (RuleDefinitionException ex)
        {
            result.Failures.Add(ex.Message);
            return result;
        }

        foreach (var error in _validator.Validate(definition, baseObject, purpose))
        {
            result.Failures.Add(error);
        }

        if (result.HasFailures)
        {
            // Evaluating a rule already known to be invalid would produce a second, less useful error about
            // whichever problem happened to be reached first.
            return result;
        }

        try
        {
            var evaluation = _evaluator.Evaluate(definition, "(test)", context);

            result.RulesEvaluated = 1;
            result.RulesMatched = evaluation.ConditionMatched ? 1 : 0;
            result.Writes.AddRange(evaluation.Writes);

            foreach (var error in evaluation.Errors)
            {
                result.Errors.Add(LocalizeError(error));
            }
        }
        catch (RuleExecutionException ex)
        {
            result.Failures.Add(ex.Message);
        }

        result.Trace = context.GetTrace();

        return await Task.FromResult(result);
    }

    private async Task RunOneAsync(
        CompiledBusinessRule rule,
        RuleExecutionContext context,
        int? targetEntityId,
        BusinessRuleRunResult result)
    {
        if (rule.CompileError != null)
        {
            await FailAsync(rule, context, targetEntityId, result, rule.CompileError);
            return;
        }

        // Validation rules never write, whatever event they are armed on. Enforced here rather than trusted
        // to the author, so moving an assignment cannot turn a check into a mutation.
        var previousAllowWrites = context.AllowWrites;
        context.AllowWrites = previousAllowWrites && rule.Purpose != RulePurpose.Validate;

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var evaluation = _evaluator.Evaluate(rule.Definition, rule.Code, context);

            result.RulesEvaluated++;

            if (evaluation.ConditionMatched)
            {
                result.RulesMatched++;
            }

            result.Writes.AddRange(evaluation.Writes);
            result.RecordChanges.AddRange(evaluation.RecordChanges);

            foreach (var error in evaluation.Errors)
            {
                result.Errors.Add(LocalizeError(error));
            }

            stopwatch.Stop();

            if (rule.IsTraced)
            {
                await WriteLogAsync(
                    rule,
                    context,
                    targetEntityId,
                    succeeded: true,
                    conditionMatched: evaluation.ConditionMatched,
                    message: context.GetTrace(),
                    durationMs: (int)stopwatch.ElapsedMilliseconds);
            }
        }
        catch (RuleExecutionException ex)
        {
            stopwatch.Stop();
            await FailAsync(rule, context, targetEntityId, result, ex.Message, (int)stopwatch.ElapsedMilliseconds);
        }
        finally
        {
            context.AllowWrites = previousAllowWrites;
        }
    }

    private async Task FailAsync(
        CompiledBusinessRule rule,
        RuleExecutionContext context,
        int? targetEntityId,
        BusinessRuleRunResult result,
        string message,
        int durationMs = 0)
    {
        result.Failures.Add(rule.Code + ": " + message);

        Logger.Error("Business rule '" + rule.Code + "' failed: " + message);

        await WriteLogAsync(
            rule,
            context,
            targetEntityId,
            succeeded: false,
            conditionMatched: false,
            message: message,
            durationMs: durationMs);

        if (!rule.ContinueOnError)
        {
            // The user gets the rule's name, not its stack trace: they cannot fix the rule, but naming it
            // is what lets them tell an administrator which one to look at. The detail is in the log above.
            throw new UserFriendlyException(L("BusinessRuleFailed", rule.Code));
        }
    }

    private string LocalizeError(RuleRaisedError error)
    {
        try
        {
            return error.Args is { Length: > 0 }
                ? L(error.MessageKey, error.Args)
                : L(error.MessageKey);
        }
        catch (Exception)
        {
            // An unknown key must not mask the rule's intent. Showing the key beats showing nothing, and it
            // tells whoever reads it exactly which localization entry is missing.
            return error.MessageKey;
        }
    }

    private async Task WriteLogAsync(
        CompiledBusinessRule rule,
        RuleExecutionContext context,
        int? targetEntityId,
        bool succeeded,
        bool conditionMatched,
        string message,
        int durationMs)
    {
        if (context.SuppressDatabaseLogging)
        {
            // Running inside somebody else's SaveChanges. See RuleExecutionContext.SuppressDatabaseLogging:
            // the application log is the right place for this, and opening a nested transaction is not.
            Logger.Info(
                "Business rule '" + rule.Code + "' on " + context.BaseObject.Code + "/" + context.EventType
                + (succeeded ? " succeeded" : " FAILED") + ": " + message);

            return;
        }

        try
        {
            // Its own transaction. See the class remarks: a failure log written inside the transaction the
            // failure aborts does not survive to be read.
            using var unitOfWork = _unitOfWorkManager.Begin(TransactionScopeOption.RequiresNew);
            using (_unitOfWorkManager.Current.SetTenantId(context.TenantId))
            {
                await _logRepository.InsertAsync(new BusinessRuleExecutionLog
                {
                    BusinessRuleId = rule.RuleId,
                    RuleCode = rule.Code,
                    BaseObject = context.BaseObject.Code,
                    EventType = context.EventType,
                    TargetEntityId = targetEntityId,
                    Succeeded = succeeded,
                    ConditionMatched = conditionMatched,
                    Message = Truncate(message),
                    DurationMs = durationMs,
                    ExecutedAt = Clock.Now
                });

                await unitOfWork.CompleteAsync();
            }
        }
        catch (Exception ex)
        {
            // Logging must never be the reason a save fails. If the log table is unreachable the engine
            // still has a job to do, and swallowing here is the one place that is the right call.
            Logger.Warn("Could not write a business rule execution log for '" + rule.Code + "'.", ex);
        }
    }

    private static string Truncate(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return null;
        }

        return message.Length <= BusinessRuleConsts.MaxDescriptionLength
            ? message
            : message.Substring(0, BusinessRuleConsts.MaxDescriptionLength);
    }
}
