using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Integrations.Services;

/// <inheritdoc cref="IIntegrationSettingStore"/>
/// <remarks>
/// Encryption goes through <see cref="IntegrationSecret"/> so that every place touching a credential
/// agrees on whether a stored value is ciphertext.
/// </remarks>
public class IntegrationSettingStore : HCMv3DomainServiceBase, IIntegrationSettingStore
{
    private readonly IRepository<IntegrationSetting> _repository;
    private readonly IAbpSession _abpSession;

    public IntegrationSettingStore(IRepository<IntegrationSetting> repository, IAbpSession abpSession)
    {
        _repository = repository;
        _abpSession = abpSession;
    }

    [UnitOfWork]
    public virtual async Task<string> GetAsync(string integrationKey, string settingKey)
    {
        var row = await _repository.FirstOrDefaultAsync(s =>
            s.IntegrationKey == integrationKey && s.SettingKey == settingKey);

        return row == null ? null : Reveal(row);
    }

    [UnitOfWork]
    public virtual async Task<IReadOnlyDictionary<string, string>> GetAllAsync(string integrationKey)
    {
        var rows = await _repository.GetAll()
            .Where(s => s.IntegrationKey == integrationKey)
            .ToListAsync();

        return rows.ToDictionary(r => r.SettingKey, Reveal, StringComparer.OrdinalIgnoreCase);
    }

    [UnitOfWork]
    public virtual async Task<IReadOnlyCollection<string>> GetKeysWithValueAsync(string integrationKey)
    {
        return await _repository.GetAll()
            .Where(s => s.IntegrationKey == integrationKey && s.Value != null && s.Value != "")
            .Select(s => s.SettingKey)
            .ToListAsync();
    }

    [UnitOfWork]
    public virtual async Task SetAsync(string integrationKey, string settingKey, string value, bool isSecret)
    {
        var row = await _repository.FirstOrDefaultAsync(s =>
            s.IntegrationKey == integrationKey && s.SettingKey == settingKey);

        if (string.IsNullOrWhiteSpace(value))
        {
            if (row != null)
            {
                await _repository.DeleteAsync(row);
            }

            return;
        }

        var stored = isSecret ? IntegrationSecret.Protect(value) : value;

        if (row == null)
        {
            await _repository.InsertAsync(new IntegrationSetting
            {
                TenantId = _abpSession.GetTenantId(),
                IntegrationKey = integrationKey,
                SettingKey = settingKey,
                Value = stored,
                IsSecret = isSecret
            });

            return;
        }

        row.Value = stored;
        row.IsSecret = isSecret;
        await _repository.UpdateAsync(row);
    }

    [UnitOfWork]
    public virtual async Task RemoveAllAsync(string integrationKey)
    {
        await _repository.DeleteAsync(s => s.IntegrationKey == integrationKey);
    }

    private static string Reveal(IntegrationSetting row)
    {
        return row.IsSecret ? IntegrationSecret.Reveal(row.Value) : row.Value;
    }
}
