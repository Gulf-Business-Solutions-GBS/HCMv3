using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using HCMv3.BusinessRules.Services;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.HcmNotifications.Rules;

/// <summary>
/// The shared half of every source: paging in id order, the sub type filter, and leaving out records the
/// rule has already logged -- all in SQL, so a rule that has fired about most of a large table only reads
/// what is left.
///
/// <para>
/// A derived source states only what is specific to it: its records, its sub type column, its criterion
/// fields and how to label a record. That keeps each new source the "one small class" the design promised.
/// </para>
/// </summary>
public abstract class NotificationRuleSourceBase<TEntity> : INotificationRuleSource, ITransientDependency
    where TEntity : class, IEntity<int>
{
    private static readonly MethodInfo QueryableContainsLong = typeof(Queryable)
        .GetMethods()
        .Single(m => m.Name == nameof(Queryable.Contains) && m.GetParameters().Length == 2)
        .MakeGenericMethod(typeof(long));

    private readonly IRepository<NotificationSentLog, long> _sentLogRepository;
    private RuleBaseObjectDescriptor _criterion;

    protected NotificationRuleSourceBase(
        IRepository<TEntity> repository,
        IRepository<NotificationSentLog, long> sentLogRepository)
    {
        Repository = repository;
        _sentLogRepository = sentLogRepository;
    }

    protected IRepository<TEntity> Repository { get; }

    public abstract string Code { get; }

    public abstract string DisplayNameKey { get; }

    public abstract string SubTypeDisplayNameKey { get; }

    public abstract string ParameterName { get; }

    /// <summary>Built once per instance: descriptors are static metadata.</summary>
    public RuleBaseObjectDescriptor Criterion => _criterion ??= BuildCriterion();

    protected abstract RuleBaseObjectDescriptor BuildCriterion();

    /// <summary>The records a rule may fire about, with whatever the label needs included.</summary>
    protected virtual IQueryable<TEntity> Records() => Repository.GetAll();

    /// <summary>Records of one sub type. Only called when <see cref="SubTypeDisplayNameKey"/> is set.</summary>
    protected virtual Expression<Func<TEntity, bool>> SubTypeFilter(int subTypeId) => null;

    protected abstract DateTime? SubjectDate(TEntity record);

    protected abstract string Describe(TEntity record);

    public virtual Task<List<NotificationRuleSubTypeItem>> GetSubTypesAsync()
    {
        return Task.FromResult(new List<NotificationRuleSubTypeItem>());
    }

    public async Task<List<NotificationRuleCandidate>> GetCandidatesAsync(NotificationRuleCandidateQuery query)
    {
        query ??= new NotificationRuleCandidateQuery();

        var records = Records();

        if (query.SubTypeId.HasValue && SubTypeDisplayNameKey != null)
        {
            var filter = SubTypeFilter(query.SubTypeId.Value);
            if (filter != null)
            {
                records = records.Where(filter);
            }
        }

        // Built against the concrete Id property rather than written as "e => e.Id", which on a generic entity
        // compiles to a cast to IEntity<int> -- an expression shape providers have not always translated.
        var entity = Expression.Parameter(typeof(TEntity), "e");
        var id = Expression.Property(entity, nameof(IEntity<int>.Id));

        var after = (int)Math.Min(Math.Max(query.AfterRecordId, 0), int.MaxValue);
        records = records.Where(Expression.Lambda<Func<TEntity, bool>>(
            Expression.GreaterThan(id, Expression.Constant(after)),
            entity));

        if (query.ExcludeLoggedForRuleId.HasValue)
        {
            var ruleId = query.ExcludeLoggedForRuleId.Value;
            var logged = _sentLogRepository.GetAll()
                .Where(l => l.NotificationMasterId == ruleId && l.SourceCode == Code)
                .Select(l => l.RecordId);

            records = records.Where(Expression.Lambda<Func<TEntity, bool>>(
                Expression.Not(Expression.Call(
                    QueryableContainsLong,
                    logged.Expression,
                    Expression.Convert(id, typeof(long)))),
                entity));
        }

        var page = await records
            .OrderBy(Expression.Lambda<Func<TEntity, int>>(id, entity))
            .Take(Math.Max(1, query.MaxCount))
            .ToListAsync();

        return page.Select(record => new NotificationRuleCandidate
        {
            RecordId = record.Id,
            Record = record,
            SubjectDate = SubjectDate(record),
            Label = Describe(record)
        }).ToList();
    }

    public void Bind(RuleExecutionContext context, NotificationRuleCandidate candidate)
    {
        context.WithParameter(ParameterName, candidate.Record);
    }

    /// <summary>An unset (0001-01-01) date reads as no date at all on the preview and history.</summary>
    protected static DateTime? DateOrNull(DateTime value) => value == default ? null : value;
}
