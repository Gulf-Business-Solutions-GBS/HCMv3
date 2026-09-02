using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.Timing;
using Abp.UI;
using HCMv3.Navigation.Dto;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Navigation;

/// <summary>
/// The signed-in user's screen favourites and recent history -- the store behind the navigation rail's
/// shortcuts and the dashboard's quick links.
///
/// SCOPED TO THE CALLER, ALWAYS. Every query filters on <c>AbpSession.UserId</c> and no method accepts a
/// user id. Which screens a person uses says what they work on -- payroll, disciplinaries, terminations
/// -- so one user reading another's rail would be a real disclosure, not a cosmetic one.
/// </summary>
[AbpAuthorize]
public class MyScreenShortcutsAppService : HCMv3AppServiceBase, IMyScreenShortcutsAppService
{
    /// <summary>
    /// How many recents the rail shows. Five because that is what was asked for, and because a "recent"
    /// list long enough to need scanning is just the menu again.
    /// </summary>
    private const int RecentCount = 5;

    private readonly IRepository<UserScreenUsage> _repository;

    public MyScreenShortcutsAppService(IRepository<UserScreenUsage> repository)
    {
        _repository = repository;
    }

    public async Task<MyScreenShortcutsDto> GetMyShortcuts()
    {
        var userId = GetCurrentUserIdOrFail();

        var rows = await _repository.GetAll()
            .Where(r => r.UserId == userId)
            .ToListAsync();

        return new MyScreenShortcutsDto
        {
            Favorites = rows
                .Where(r => r.IsFavorite)
                // Most-used first: a favourites list ordered by when it was pinned puts the screen you
                // pinned once and never opened above the one you use every morning.
                .OrderByDescending(r => r.VisitCount)
                .ThenByDescending(r => r.LastVisitedAt)
                .Select(Map)
                .ToArray(),

            Recent = rows
                // Null LastVisitedAt means favourited but never opened since -- excluded rather than
                // sorted to an end, because it is not a visit and does not belong in a visit list.
                .Where(r => r.LastVisitedAt.HasValue)
                .OrderByDescending(r => r.LastVisitedAt)
                .Take(RecentCount)
                .Select(Map)
                .ToArray(),
        };
    }

    public async Task SetFavorite(SetScreenFavoriteInput input)
    {
        var userId = GetCurrentUserIdOrFail();
        var row = await FindAsync(userId, input.ScreenKey);

        if (row == null)
        {
            // Favouriting a screen never visited is legitimate -- someone pinning what they are about to
            // start using -- so the row is created with no visit rather than refused.
            if (!input.IsFavorite)
            {
                return;
            }

            await _repository.InsertAsync(new UserScreenUsage
            {
                TenantId = AbpSession.TenantId ?? 0,
                UserId = userId,
                ScreenKey = input.ScreenKey.Trim(),
                IsFavorite = true,
            });

            return;
        }

        row.IsFavorite = input.IsFavorite;
        await _repository.UpdateAsync(row);
    }

    public async Task RecordVisit(RecordScreenVisitInput input)
    {
        var userId = GetCurrentUserIdOrFail();
        var row = await FindAsync(userId, input.ScreenKey);
        var now = Clock.Now;

        if (row == null)
        {
            await _repository.InsertAsync(new UserScreenUsage
            {
                TenantId = AbpSession.TenantId ?? 0,
                UserId = userId,
                ScreenKey = input.ScreenKey.Trim(),
                IsFavorite = false,
                LastVisitedAt = now,
                VisitCount = 1,
            });

            return;
        }

        row.LastVisitedAt = now;
        row.VisitCount += 1;
        await _repository.UpdateAsync(row);
    }

    private Task<UserScreenUsage> FindAsync(long userId, string screenCode)
    {
        var code = screenCode?.Trim();

        return _repository.GetAll()
            .FirstOrDefaultAsync(r => r.UserId == userId && r.ScreenKey == code);
    }

    /// <summary>
    /// The session must carry a user. [AbpAuthorize] already guarantees it, but reading
    /// <c>AbpSession.UserId.Value</c> unguarded would turn any future gap in that guarantee into a
    /// NullReferenceException instead of a clear failure.
    /// </summary>
    private long GetCurrentUserIdOrFail()
    {
        return AbpSession.UserId ?? throw new UserFriendlyException(L("YouAreNotLoggedIn"));
    }

    private static ScreenUsageDto Map(UserScreenUsage row) => new ScreenUsageDto
    {
        ScreenKey = row.ScreenKey,
        IsFavorite = row.IsFavorite,
        LastVisitedAt = row.LastVisitedAt,
        VisitCount = row.VisitCount,
    };
}
