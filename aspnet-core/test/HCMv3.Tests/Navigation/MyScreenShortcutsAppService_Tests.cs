using System.Linq;
using System.Threading.Tasks;
using HCMv3.Navigation;
using HCMv3.Navigation.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Navigation;

/// <summary>
/// Tests for the per-user screen favourites and recent history behind the navigation shortcuts and the
/// dashboard's quick links.
///
/// The emphasis is on the two things that would be wrong in ways nobody notices: ordering that looks
/// plausible but ranks the wrong screens, and one user's rail leaking into another's.
/// </summary>
// ReSharper disable once InconsistentNaming
public class MyScreenShortcutsAppService_Tests : AppTestBase
{
    private readonly IMyScreenShortcutsAppService _appService;

    public MyScreenShortcutsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IMyScreenShortcutsAppService>();
    }

    [Fact]
    public async Task Should_Start_With_Nothing()
    {
        var shortcuts = await _appService.GetMyShortcuts();

        shortcuts.Favorites.ShouldBeEmpty();
        shortcuts.Recent.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Record_A_Visit_And_Count_Repeats()
    {
        await _appService.RecordVisit(new RecordScreenVisitInput { ScreenCode = "S-PA-EMP" });
        await _appService.RecordVisit(new RecordScreenVisitInput { ScreenCode = "S-PA-EMP" });

        var shortcuts = await _appService.GetMyShortcuts();

        shortcuts.Recent.Length.ShouldBe(1);
        shortcuts.Recent[0].ScreenCode.ShouldBe("S-PA-EMP");
        shortcuts.Recent[0].VisitCount.ShouldBe(2);
        shortcuts.Recent[0].LastVisitedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task Should_Return_Only_The_Five_Most_Recent()
    {
        // Seven screens visited; the rail shows five. A "recent" list long enough to need scanning is
        // just the menu again.
        for (var i = 1; i <= 7; i++)
        {
            await _appService.RecordVisit(new RecordScreenVisitInput { ScreenCode = "S-" + i });
        }

        var shortcuts = await _appService.GetMyShortcuts();

        shortcuts.Recent.Length.ShouldBe(5);
        shortcuts.Recent.Select(r => r.ScreenCode).ShouldNotContain("S-1");
    }

    [Fact]
    public async Task Should_Let_A_Screen_Be_Favourited_Before_It_Is_Ever_Visited()
    {
        // Pinning something you are about to start using is legitimate. The row exists with no visit,
        // which is exactly the case the recent list has to exclude.
        await _appService.SetFavorite(new SetScreenFavoriteInput { ScreenCode = "S-NEW", IsFavorite = true });

        var shortcuts = await _appService.GetMyShortcuts();

        shortcuts.Favorites.Length.ShouldBe(1);
        shortcuts.Favorites[0].ScreenCode.ShouldBe("S-NEW");
        shortcuts.Favorites[0].LastVisitedAt.ShouldBeNull();

        // Never visited, so it is not a recent -- not merely sorted last.
        shortcuts.Recent.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Order_Favourites_By_Use_Not_By_When_They_Were_Pinned()
    {
        // Pinned first, used once.
        await _appService.SetFavorite(new SetScreenFavoriteInput { ScreenCode = "S-RARE", IsFavorite = true });
        await _appService.RecordVisit(new RecordScreenVisitInput { ScreenCode = "S-RARE" });

        // Pinned second, used daily. This is the one that must come first.
        await _appService.SetFavorite(new SetScreenFavoriteInput { ScreenCode = "S-DAILY", IsFavorite = true });
        for (var i = 0; i < 5; i++)
        {
            await _appService.RecordVisit(new RecordScreenVisitInput { ScreenCode = "S-DAILY" });
        }

        var shortcuts = await _appService.GetMyShortcuts();

        shortcuts.Favorites[0].ScreenCode.ShouldBe("S-DAILY");
    }

    [Fact]
    public async Task Should_Unfavourite_Without_Losing_The_Visit_History()
    {
        await _appService.RecordVisit(new RecordScreenVisitInput { ScreenCode = "S-PA-EMP" });
        await _appService.SetFavorite(new SetScreenFavoriteInput { ScreenCode = "S-PA-EMP", IsFavorite = true });
        await _appService.SetFavorite(new SetScreenFavoriteInput { ScreenCode = "S-PA-EMP", IsFavorite = false });

        var shortcuts = await _appService.GetMyShortcuts();

        shortcuts.Favorites.ShouldBeEmpty();

        // Unpinning is not "forget I was ever here" -- it still belongs in recents.
        shortcuts.Recent.Length.ShouldBe(1);
        shortcuts.Recent[0].VisitCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Ignore_Unfavouriting_Something_That_Was_Never_Recorded()
    {
        // A stale click from a screen removed since. Creating a row to say "not a favourite" would be
        // storing the absence of a fact.
        await _appService.SetFavorite(new SetScreenFavoriteInput { ScreenCode = "S-GONE", IsFavorite = false });

        var shortcuts = await _appService.GetMyShortcuts();

        shortcuts.Favorites.ShouldBeEmpty();
        shortcuts.Recent.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Not_Show_Another_Users_Shortcuts()
    {
        // Which screens somebody uses says what they work on -- payroll, disciplinaries, terminations.
        // One user reading another's rail would be a real disclosure, not a cosmetic one.
        await _appService.RecordVisit(new RecordScreenVisitInput { ScreenCode = "S-ADMIN-ONLY" });
        await _appService.SetFavorite(new SetScreenFavoriteInput { ScreenCode = "S-ADMIN-ONLY", IsFavorite = true });

        // Same tenant, different person. The rows are scoped by user, not by tenant, so a colleague in
        // the same tenant is exactly the case that would leak if the filter were wrong.
        AbpSession.UserId = AbpSession.UserId + 1;

        var otherUsersShortcuts = await _appService.GetMyShortcuts();

        otherUsersShortcuts.Recent.ShouldBeEmpty();
        otherUsersShortcuts.Favorites.ShouldBeEmpty();
    }
}
