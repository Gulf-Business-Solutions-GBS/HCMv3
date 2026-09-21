using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Integrations.Services;

/// <inheritdoc cref="IIntegrationConnectionLogger"/>
public class IntegrationConnectionLogger : HCMv3DomainServiceBase, IIntegrationConnectionLogger
{
    private readonly IRepository<IntegrationConnectionLog> _repository;
    private readonly IAbpSession _abpSession;

    public IntegrationConnectionLogger(IRepository<IntegrationConnectionLog> repository, IAbpSession abpSession)
    {
        _repository = repository;
        _abpSession = abpSession;
    }

    [UnitOfWork]
    public virtual async Task LogAsync(
        string integrationKey,
        IntegrationAttemptType attemptType,
        bool succeeded,
        string message,
        int durationMs,
        int? itemCount = null)
    {
        await _repository.InsertAsync(new IntegrationConnectionLog
        {
            TenantId = _abpSession.GetTenantId(),
            IntegrationKey = integrationKey,
            AttemptType = attemptType,
            Succeeded = succeeded,
            Message = Truncate(message),
            DurationMs = durationMs,
            ItemCount = itemCount
        });
    }

    [UnitOfWork]
    public virtual async Task<IReadOnlyDictionary<IntegrationAttemptType, IntegrationConnectionLog>> GetLatestAsync(
        string integrationKey)
    {
        var rows = await _repository.GetAll()
            .Where(l => l.IntegrationKey == integrationKey)
            .GroupBy(l => l.AttemptType)
            .Select(g => g.OrderByDescending(l => l.CreationTime).ThenByDescending(l => l.Id).First())
            .ToListAsync();

        return rows.ToDictionary(r => r.AttemptType);
    }

    [UnitOfWork]
    public virtual async Task<IReadOnlyDictionary<string, IReadOnlyDictionary<IntegrationAttemptType, IntegrationConnectionLog>>>
        GetLatestForAllAsync()
    {
        var rows = await _repository.GetAll()
            .GroupBy(l => new { l.IntegrationKey, l.AttemptType })
            .Select(g => g.OrderByDescending(l => l.CreationTime).ThenByDescending(l => l.Id).First())
            .ToListAsync();

        return rows
            .GroupBy(r => r.IntegrationKey)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyDictionary<IntegrationAttemptType, IntegrationConnectionLog>)
                    g.ToDictionary(r => r.AttemptType),
                StringComparer.OrdinalIgnoreCase);
    }

    [UnitOfWork]
    public virtual async Task<IReadOnlyList<IntegrationConnectionLog>> GetRecentAsync(string integrationKey, int maxCount)
    {
        return await _repository.GetAll()
            .Where(l => l.IntegrationKey == integrationKey)
            .OrderByDescending(l => l.CreationTime)
            .ThenByDescending(l => l.Id)
            .Take(maxCount)
            .ToListAsync();
    }

    private static string Truncate(string message)
    {
        if (string.IsNullOrEmpty(message) || message.Length <= IntegrationConnectionLog.MaxMessageLength)
        {
            return message;
        }

        return message.Substring(0, IntegrationConnectionLog.MaxMessageLength);
    }
}
