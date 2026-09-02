using System;
using System.Collections.Generic;
using System.Linq;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Services;
using Abp.Domain.Uow;

namespace HCMv3.Navigation;

/// <summary>
/// Brings a tenant's screen catalogue up to date with the application's screens.
///
/// ADDITIVE, NEVER DESTRUCTIVE. It inserts screens that are missing and leaves every existing row
/// exactly as it is -- including a code an administrator has re-mapped. Nothing here updates or deletes.
/// A screen that disappears from the application keeps its row, because favourites and shared links
/// still point at it and silently dropping it would make them vanish with no explanation.
///
/// This runs per tenant, and is safe to run repeatedly.
/// </summary>
public interface IScreenSeeder : IDomainService
{
    void SeedForTenant(int tenantId);
}

public class ScreenSeeder : IScreenSeeder, ITransientDependency
{
    private readonly IRepository<Screen> _screenRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public ScreenSeeder(IRepository<Screen> screenRepository, IUnitOfWorkManager unitOfWorkManager)
    {
        _screenRepository = screenRepository;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public void SeedForTenant(int tenantId)
    {
        using (_unitOfWorkManager.Current.SetTenantId(tenantId))
        {
            var existing = _screenRepository.GetAll().ToList();
            var existingKeys = new HashSet<string>(
                existing.Select(s => s.ScreenKey),
                StringComparer.OrdinalIgnoreCase);

            // Codes already in use, so a generated one never collides with an administrator's rename.
            var usedCodes = new HashSet<string>(
                existing.Select(s => s.Code),
                StringComparer.OrdinalIgnoreCase);

            foreach (var seed in ScreenCatalog.All)
            {
                if (existingKeys.Contains(seed.Key))
                {
                    continue;
                }

                _screenRepository.Insert(new Screen
                {
                    TenantId = tenantId,
                    ScreenKey = seed.Key,
                    Code = NextFreeCode(seed.Code, usedCodes),
                    Module = seed.Module,
                    NameKey = seed.NameKey,
                    Route = seed.Route,
                    PermissionName = seed.PermissionName,
                });
            }
        }
    }

    /// <summary>
    /// The catalogue's code if it is free, otherwise the next number up in the same prefix.
    ///
    /// Needed because an administrator may have re-mapped some OTHER screen onto the code this one was
    /// assigned. Refusing to seed would leave the new screen without a code; overwriting would undo the
    /// administrator's decision. Taking the next free number does neither.
    /// </summary>
    private static string NextFreeCode(string preferred, HashSet<string> usedCodes)
    {
        if (usedCodes.Add(preferred))
        {
            return preferred;
        }

        var prefix = new string(preferred.TakeWhile(char.IsLetter).ToArray());
        var digits = preferred.Length - prefix.Length;
        var width = digits > 0 ? digits : 3;

        for (var i = 1; i < 10000; i++)
        {
            var candidate = prefix + i.ToString(new string('0', width));
            if (usedCodes.Add(candidate))
            {
                return candidate;
            }
        }

        // 10,000 screens in one module is not a real state; failing loudly beats an empty code.
        throw new InvalidOperationException($"No free screen code remains for prefix '{prefix}'.");
    }
}
