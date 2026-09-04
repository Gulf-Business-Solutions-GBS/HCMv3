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
/// NEVER DESTRUCTIVE, BUT NOT BLIND. It inserts screens that are missing and refreshes the fields the
/// APPLICATION owns on the ones that exist -- name, route, module, permission -- because a screen renamed
/// or moved in the code has to reach the people using it, and a row that never changes means Quick Nav
/// keeps offering last year's name and pointing at a route that has moved.
///
/// The CODE is never touched: that is the administrator's to re-map, and rewriting it on every start
/// would undo their decision silently.
///
/// Nothing is deleted. A screen that disappears from the application keeps its row, because favourites
/// and shared links still point at it and dropping it would make them vanish with no explanation. Such a
/// row is hidden at read time instead -- see ScreensAppService.FilterToPermittedAsync.
///
/// This runs per tenant, and is safe to run repeatedly.
/// </summary>
public interface IScreenSeeder : IDomainService
{
    void SeedForTenant(int? tenantId);
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

    public void SeedForTenant(int? tenantId)
    {
        // Null tenant means the HOST catalogue. SetTenantId accepts null and scopes the unit of work to
        // host-level rows, which is what a host administrator needs to see.
        using (_unitOfWorkManager.Current.SetTenantId(tenantId))
        {
            var existing = _screenRepository.GetAll().ToList();
            var existingByKey = existing.ToDictionary(s => s.ScreenKey, StringComparer.OrdinalIgnoreCase);

            // Codes already in use, so a generated one never collides with an administrator's rename.
            var usedCodes = new HashSet<string>(
                existing.Select(s => s.Code),
                StringComparer.OrdinalIgnoreCase);

            foreach (var seed in ScreenCatalog.All)
            {
                if (existingByKey.TryGetValue(seed.Key, out var row))
                {
                    // The application owns what a screen IS -- its name, where it lives, what it needs.
                    // An administrator owns only the Code. Leaving these alone meant a screen renamed or
                    // moved in the application kept its old row forever, so the rename never reached
                    // anybody: Quick Nav went on offering the previous name, and a moved route went on
                    // pointing at a page that was no longer there.
                    //
                    // Code is NOT touched. That is the one field an administrator may have re-mapped, and
                    // overwriting it would undo their decision on the next start.
                    row.Module = seed.Module;
                    row.NameKey = seed.NameKey;
                    row.Route = seed.Route;
                    row.PermissionName = seed.PermissionName;

                    _screenRepository.Update(row);
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
