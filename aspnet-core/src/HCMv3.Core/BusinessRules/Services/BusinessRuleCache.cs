using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Caching;
using HCMv3.BusinessRules.Definition;

namespace HCMv3.BusinessRules.Services;

/// <summary>
/// A rule parsed once and ready to run: the definition tree plus the assignment settings that govern how
/// the runner treats it.
///
/// <para>
/// Immutable after construction, and the evaluator never mutates the tree it walks, so one instance is
/// safely shared across every request in the process. That is the whole point of compiling: parsing JSON on
/// every employee save would put a measurable cost on the hot path for an answer that changes perhaps twice
/// a year.
/// </para>
/// </summary>
public class CompiledBusinessRule
{
    public int RuleId { get; set; }

    public string Code { get; set; }

    public string NameEn { get; set; }

    public RulePurpose Purpose { get; set; }

    public RuleDefinition Definition { get; set; }

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public int ExecutionOrder { get; set; }

    public bool ContinueOnError { get; set; }

    public bool IsTraced { get; set; }

    /// <summary>
    /// Set when the stored definition would not parse. The rule is kept in the set rather than dropped so
    /// the runner can report it -- a broken rule that silently disappears looks exactly like a rule that
    /// decided not to match, and the two need very different responses.
    /// </summary>
    public string CompileError { get; set; }

    public bool IsEffectiveOn(DateTime date)
    {
        return EffectiveFrom.Date <= date.Date && (EffectiveTo == null || EffectiveTo.Value.Date >= date.Date);
    }
}

/// <summary>
/// The armed, parsed rules for a base object and event. Everything the runner needs to answer "what runs
/// here" without touching the database.
/// </summary>
public interface IBusinessRuleCache
{
    Task<IReadOnlyList<CompiledBusinessRule>> GetAsync(int? tenantId, string baseObject, RuleEventType eventType);

    /// <summary>
    /// Drops the compiled set. Called whenever a rule or assignment changes.
    ///
    /// <para>
    /// Clears EVERY tenant's entry, not just the caller's. Per-tenant eviction would need the cache to track
    /// which keys belong to which tenant, and the cost of being wrong -- a tenant running a rule they
    /// deleted -- is far worse than the cost of being coarse, which is one extra parse per base object on
    /// the next save.
    /// </para>
    /// </summary>
    Task InvalidateAsync();
}

/// <inheritdoc cref="IBusinessRuleCache"/>
public class BusinessRuleCache : IBusinessRuleCache, ISingletonDependency
{
    private readonly ICacheManager _cacheManager;
    private readonly IRepository<BusinessRuleAssignment> _assignmentRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public BusinessRuleCache(
        ICacheManager cacheManager,
        IRepository<BusinessRuleAssignment> assignmentRepository,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _cacheManager = cacheManager;
        _assignmentRepository = assignmentRepository;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public async Task<IReadOnlyList<CompiledBusinessRule>> GetAsync(
        int? tenantId,
        string baseObject,
        RuleEventType eventType)
    {
        if (string.IsNullOrWhiteSpace(baseObject) || eventType == RuleEventType.None)
        {
            return Array.Empty<CompiledBusinessRule>();
        }

        var key = BuildKey(tenantId, baseObject, eventType);

        var cached = await GetCache().GetOrDefaultAsync(key);

        if (cached != null)
        {
            return cached;
        }

        var compiled = await LoadAsync(tenantId, baseObject, eventType);

        await GetCache().SetAsync(key, compiled);

        return compiled;
    }

    public Task InvalidateAsync()
    {
        return GetCache().ClearAsync();
    }

    private ITypedCache<string, List<CompiledBusinessRule>> GetCache()
    {
        return _cacheManager.GetCache<string, List<CompiledBusinessRule>>(BusinessRuleConsts.RuleSetCacheName);
    }

    private async Task<List<CompiledBusinessRule>> LoadAsync(
        int? tenantId,
        string baseObject,
        RuleEventType eventType)
    {
        // The tenant filter is ambient, so this must run inside a unit of work scoped to the tenant whose
        // rules we are loading. Without SetTenantId a background worker resolving the cache would read the
        // host's (empty) rule set and then serve it to a tenant under that tenant's key.
        using var unitOfWork = _unitOfWorkManager.Begin();
        using (_unitOfWorkManager.Current.SetTenantId(tenantId))
        {
            var rows = await Task.FromResult(_assignmentRepository.GetAll()
                .Where(a => a.IsActive && a.BaseObject == baseObject && a.EventType == eventType)
                .Select(a => new
                {
                    a.ExecutionOrder,
                    a.ContinueOnError,
                    a.IsTraced,
                    a.BusinessRuleId,
                    a.BusinessRule.Code,
                    a.BusinessRule.NameEn,
                    a.BusinessRule.Purpose,
                    a.BusinessRule.DefinitionJson,
                    a.BusinessRule.EffectiveFrom,
                    a.BusinessRule.EffectiveTo,
                    RuleIsActive = a.BusinessRule.IsActive
                })
                .Where(a => a.RuleIsActive)
                .OrderBy(a => a.ExecutionOrder)
                .ThenBy(a => a.BusinessRuleId)
                .ToList());

            var compiled = new List<CompiledBusinessRule>(rows.Count);

            foreach (var row in rows)
            {
                var rule = new CompiledBusinessRule
                {
                    RuleId = row.BusinessRuleId,
                    Code = row.Code,
                    NameEn = row.NameEn,
                    Purpose = row.Purpose,
                    EffectiveFrom = row.EffectiveFrom,
                    EffectiveTo = row.EffectiveTo,
                    ExecutionOrder = row.ExecutionOrder,
                    ContinueOnError = row.ContinueOnError,
                    IsTraced = row.IsTraced
                };

                try
                {
                    rule.Definition = RuleDefinitionSerializer.Parse(row.DefinitionJson);
                }
                catch (RuleDefinitionException ex)
                {
                    // Recorded, not thrown. One unparseable rule must not take out every other rule armed
                    // on the same event -- the runner reports this one and carries on with the rest.
                    rule.CompileError = ex.Message;
                }

                compiled.Add(rule);
            }

            await unitOfWork.CompleteAsync();

            return compiled;
        }
    }

    private static string BuildKey(int? tenantId, string baseObject, RuleEventType eventType)
    {
        return (tenantId?.ToString() ?? "host") + "|" + baseObject.ToUpperInvariant() + "|" + (int)eventType;
    }
}
