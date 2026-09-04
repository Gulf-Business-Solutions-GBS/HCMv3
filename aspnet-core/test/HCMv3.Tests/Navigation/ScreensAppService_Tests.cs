using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.UI;
using HCMv3.Navigation;
using HCMv3.Navigation.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Navigation;

/// <summary>
/// Tests for the screen catalogue, Screen Codes and quick links.
///
/// The emphasis is on the things that would be wrong invisibly: a catalogue that renumbers itself, a
/// duplicate code making references ambiguous, and a quick link advertising a screen the viewer is not
/// allowed to open.
/// </summary>
// ReSharper disable once InconsistentNaming
public class ScreensAppService_Tests : AppTestBase
{
    private readonly IScreensAppService _appService;

    public ScreensAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IScreensAppService>();
    }

    [Fact]
    public async Task Should_Seed_The_Catalogue_On_First_Read()
    {
        // Seeded lazily rather than at deploy time, so a tenant created after a release still has one.
        var bootstrap = await _appService.GetNavigationBootstrap();

        bootstrap.Screens.ShouldNotBeEmpty();
        bootstrap.Screens.ShouldAllBe(s => !string.IsNullOrWhiteSpace(s.Code));
        bootstrap.Screens.ShouldAllBe(s => !string.IsNullOrWhiteSpace(s.ScreenKey));
    }

    [Fact]
    public async Task Should_Assign_Every_Screen_A_Unique_Code_And_Key()
    {
        var bootstrap = await _appService.GetNavigationBootstrap();

        var codes = bootstrap.Screens.Select(s => s.Code).ToList();
        var keys = bootstrap.Screens.Select(s => s.ScreenKey).ToList();

        codes.Distinct().Count().ShouldBe(codes.Count);
        keys.Distinct().Count().ShouldBe(keys.Count);
    }

    [Fact]
    public async Task Should_Follow_The_Agreed_Code_Scheme()
    {
        // S, then module letters, then three digits -- SPA001, SPY001, SRPT001.
        var bootstrap = await _appService.GetNavigationBootstrap();

        bootstrap.Screens.ShouldAllBe(s => System.Text.RegularExpressions.Regex.IsMatch(s.Code, "^S[A-Z]+[0-9]{3}$"));
    }

    [Fact]
    public async Task Should_Not_Renumber_An_Existing_Screen_When_Seeding_Again()
    {
        // The whole point of the catalogue being a table. Codes end up in training material and support
        // tickets; a second deployment must not shuffle them.
        var first = await _appService.GetNavigationBootstrap();
        var before = first.Screens.OrderBy(s => s.ScreenKey).Select(s => s.ScreenKey + "=" + s.Code).ToList();

        var second = await _appService.GetNavigationBootstrap();
        var after = second.Screens.OrderBy(s => s.ScreenKey).Select(s => s.ScreenKey + "=" + s.Code).ToList();

        after.ShouldBe(before);
    }

    [Fact]
    public async Task Should_Keep_An_Administrators_Renamed_Code_Across_Seeding()
    {
        var bootstrap = await _appService.GetNavigationBootstrap();
        var screen = bootstrap.Screens.First();

        await _appService.UpdateScreenCode(new UpdateScreenCodeInput { Id = screen.Id, Code = "MYCODE1" });

        // Seeding again must not undo a deliberate decision.
        var after = await _appService.GetNavigationBootstrap();

        after.Screens.Single(s => s.Id == screen.Id).Code.ShouldBe("MYCODE1");
    }

    [Fact]
    public async Task Should_Refuse_A_Code_Another_Screen_Already_Answers_To()
    {
        var bootstrap = await _appService.GetNavigationBootstrap();
        var first = bootstrap.Screens[0];
        var second = bootstrap.Screens[1];

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _appService.UpdateScreenCode(new UpdateScreenCodeInput { Id = second.Id, Code = first.Code }));
    }

    [Fact]
    public async Task Should_Refuse_A_Blank_Code()
    {
        var bootstrap = await _appService.GetNavigationBootstrap();

        // AbpValidationException, not UserFriendlyException: [Required] on the input rejects a
        // whitespace-only string before the method body runs. The service keeps its own blank check
        // anyway -- it is the guard for a direct call that bypasses the validation interceptor -- but
        // through the API this is the exception that actually surfaces.
        await Assert.ThrowsAsync<Abp.Runtime.Validation.AbpValidationException>(() =>
            _appService.UpdateScreenCode(new UpdateScreenCodeInput { Id = bootstrap.Screens[0].Id, Code = "   " }));
    }

    [Fact]
    public async Task Should_Publish_A_Shared_Quick_Link()
    {
        var bootstrap = await _appService.GetNavigationBootstrap();
        var screen = bootstrap.Screens.First();

        await _appService.CreateOrEditSharedQuickLink(new CreateOrEditSharedQuickLinkInput
        {
            ScreenKey = screen.ScreenKey,
            DisplayOrder = 1,
        });

        var after = await _appService.GetNavigationBootstrap();

        after.SharedQuickLinks.Length.ShouldBe(1);
        after.SharedQuickLinks[0].ScreenKey.ShouldBe(screen.ScreenKey);
    }

    [Fact]
    public async Task Should_Refuse_A_Quick_Link_To_A_Screen_That_Does_Not_Exist()
    {
        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _appService.CreateOrEditSharedQuickLink(new CreateOrEditSharedQuickLinkInput
            {
                ScreenKey = "app.admin.nothing-here",
                DisplayOrder = 1,
            }));
    }

    [Fact]
    public async Task Should_Cap_Shared_Quick_Links_At_Six()
    {
        var bootstrap = await _appService.GetNavigationBootstrap();

        for (var i = 0; i < 9; i++)
        {
            await _appService.CreateOrEditSharedQuickLink(new CreateOrEditSharedQuickLinkInput
            {
                ScreenKey = bootstrap.Screens[i].ScreenKey,
                DisplayOrder = i,
            });
        }

        var after = await _appService.GetNavigationBootstrap();

        // Nine published, six shown. A tile long enough to scan is the menu again.
        after.SharedQuickLinks.Length.ShouldBe(6);
    }

    [Fact]
    public async Task Should_Default_To_Showing_Screen_Codes()
    {
        // Codes only become shared vocabulary if people see them, so the default is on.
        var bootstrap = await _appService.GetNavigationBootstrap();

        bootstrap.ShowScreenCodes.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Refuse_Code_Changes_Without_The_Permission()
    {
        var bootstrap = await _appService.GetNavigationBootstrap();
        var screen = bootstrap.Screens.First();

        // An ordinary user re-mapping codes would change vocabulary for everyone in the tenant.
        LoginAsDefaultTenantAdmin();
        await UsingDbContextAsync(async context =>
        {
            await Task.CompletedTask;
        });

        AbpSession.UserId = AbpSession.UserId + 1;

        await Assert.ThrowsAsync<AbpAuthorizationException>(() =>
            Resolve<IScreensAppService>().UpdateScreenCode(
                new UpdateScreenCodeInput { Id = screen.Id, Code = "OTHER1" }));
    }

    /// <summary>
    /// A catalogue row naming a permission the application no longer defines must HIDE that screen, not
    /// fail the whole bootstrap.
    ///
    /// This is the shape of a real outage. Removing the Demo UI Components feature deleted its
    /// permission from code, but the seeder is additive on purpose -- rows stay so that favourites
    /// pointing at them do not vanish -- so a row naming Pages.DemoUiComponents remained. ABP's
    /// IsGrantedAsync throws on an unknown permission rather than returning false, and that single row
    /// turned every navigation call into an HTTP 500: the entire menu gone because one feature was
    /// removed.
    /// </summary>
    [Fact]
    public async Task Should_Hide_A_Screen_Whose_Permission_No_Longer_Exists()
    {
        await _appService.GetNavigationBootstrap();

        await UsingDbContextAsync(async context =>
        {
            context.Screens.Add(new Screen
            {
                TenantId = AbpSession.TenantId,
                ScreenKey = "app.admin.removed-feature",
                Code = "SZZZ999",
                Module = "Removed",
                NameKey = "RemovedFeature",
                Route = "/app/admin/removed-feature",
                PermissionName = "Pages.ThisPermissionWasDeleted",
            });

            await context.SaveChangesAsync();
        });

        // Must not throw, and must not offer the screen.
        var bootstrap = await _appService.GetNavigationBootstrap();

        bootstrap.Screens.ShouldNotBeEmpty();
        bootstrap.Screens.ShouldNotContain(s => s.ScreenKey == "app.admin.removed-feature");
    }
}
