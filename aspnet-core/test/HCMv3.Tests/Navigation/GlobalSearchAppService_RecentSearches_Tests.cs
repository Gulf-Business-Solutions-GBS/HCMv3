using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Configuration;
using Abp.UI;
using HCMv3.Authorization.Users;
using HCMv3.Configuration;
using HCMv3.GeneralMasterInfo;
using HCMv3.Navigation;
using HCMv3.Navigation.Dto;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Navigation;

/// <summary>
/// The header search's recent list: the last five results a user opened, kept per user in AbpSettings.
/// </summary>
// ReSharper disable once InconsistentNaming
public class GlobalSearchAppService_RecentSearches_Tests : AppTestBase
{
    private readonly IGlobalSearchAppService _globalSearchAppService;

    public GlobalSearchAppService_RecentSearches_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _globalSearchAppService = Resolve<IGlobalSearchAppService>();
    }

    [Fact]
    public async Task Should_Start_Empty()
    {
        (await _globalSearchAppService.GetRecentSearches()).Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_List_Newest_First_And_Move_A_Repeat_To_The_Top()
    {
        var employee = CreateEmployee("RS-001", "Recent Person", "شخص");

        await _globalSearchAppService.AddRecentSearch(ScreenEntry("/app/admin/wage-types"));
        await _globalSearchAppService.AddRecentSearch(PersonEntry(employee.Id));
        await _globalSearchAppService.AddRecentSearch(ScreenEntry("/app/admin/wage-types"));

        var items = (await _globalSearchAppService.GetRecentSearches()).Items;

        items.Count.ShouldBe(2);
        items[0].Route.ShouldBe("/app/admin/wage-types");
        items[1].Kind.ShouldBe(GlobalSearchRecentKind.Person);
        items[1].Person.FullNameEn.ShouldBe("Recent Person");
    }

    [Fact]
    public async Task Should_Keep_Only_The_Last_Five()
    {
        for (var i = 1; i <= 7; i++)
        {
            await _globalSearchAppService.AddRecentSearch(ScreenEntry("/app/admin/screen-" + i));
        }

        var routes = (await _globalSearchAppService.GetRecentSearches()).Items.Select(x => x.Route).ToList();

        routes.ShouldBe(new[]
        {
            "/app/admin/screen-7", "/app/admin/screen-6", "/app/admin/screen-5", "/app/admin/screen-4", "/app/admin/screen-3"
        });
    }

    [Fact]
    public async Task Should_Leave_Out_A_Person_Who_No_Longer_Exists()
    {
        var employee = CreateEmployee("RS-002", "Gone Person", "شخص");
        await _globalSearchAppService.AddRecentSearch(PersonEntry(employee.Id));

        UsingDbContext(context =>
        {
            var entity = context.Set<Employee>().Single(x => x.Id == employee.Id);
            entity.IsDeleted = true;
        });

        (await _globalSearchAppService.GetRecentSearches()).Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Remove_One_And_Clear_All()
    {
        await _globalSearchAppService.AddRecentSearch(ScreenEntry("/app/admin/wage-types"));
        await _globalSearchAppService.AddRecentSearch(ScreenEntry("/app/admin/employees"));

        await _globalSearchAppService.RemoveRecentSearch(ScreenEntry("/app/admin/wage-types"));
        (await _globalSearchAppService.GetRecentSearches()).Items.Select(x => x.Route)
            .ShouldBe(new[] { "/app/admin/employees" });

        await _globalSearchAppService.ClearRecentSearches();
        (await _globalSearchAppService.GetRecentSearches()).Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Keep_Each_Users_List_To_Themselves()
    {
        await _globalSearchAppService.AddRecentSearch(ScreenEntry("/app/admin/wage-types"));

        var userName = "recent" + Guid.NewGuid().ToString("N").Substring(0, 8);
        var user = new User
        {
            TenantId = AbpSession.TenantId,
            UserName = userName,
            Name = "Other",
            Surname = "User",
            EmailAddress = userName + "@gbskw.com",
            IsEmailConfirmed = true,
            Password = "AM4OLBpptxBYmM79lGOX9egzZk3vIQU3d/gFCJzaBjAPXzYIK3tQ2N7X4fcrHtElTw=="
        };
        user.SetNormalizedNames();
        UsingDbContext(context => context.Users.Add(user));
        LoginAsTenant(Abp.MultiTenancy.AbpTenantBase.DefaultTenantName, userName);

        (await _globalSearchAppService.GetRecentSearches()).Items.ShouldBeEmpty();

        UsingDbContext(context => context.Settings
                .Count(x => x.Name == AppSettings.UserManagement.RecentGlobalSearches && x.UserId != null && x.TenantId == AbpSession.TenantId))
            .ShouldBe(1);
    }

    [Fact]
    public async Task Should_Refuse_An_Entry_That_Points_Nowhere()
    {
        await Should.ThrowAsync<UserFriendlyException>(() =>
            _globalSearchAppService.AddRecentSearch(new GlobalSearchRecentItemInput { Kind = GlobalSearchRecentKind.Action, Route = "https://elsewhere.example" }));
        await Should.ThrowAsync<UserFriendlyException>(() =>
            _globalSearchAppService.AddRecentSearch(new GlobalSearchRecentItemInput { Kind = GlobalSearchRecentKind.Person }));
    }

    [Fact]
    public async Task Should_Read_A_Damaged_Value_As_An_Empty_List()
    {
        await _globalSearchAppService.AddRecentSearch(ScreenEntry("/app/admin/wage-types"));
        UsingDbContext(context =>
        {
            context.Settings.Single(x => x.Name == AppSettings.UserManagement.RecentGlobalSearches && x.UserId == AbpSession.UserId)
                .Value = "not json";
        });
        Resolve<Abp.Runtime.Caching.ICacheManager>().GetUserSettingsCache().Clear();

        (await _globalSearchAppService.GetRecentSearches()).Items.ShouldBeEmpty();
    }

    private static GlobalSearchRecentItemInput ScreenEntry(string route) =>
        new() { Kind = GlobalSearchRecentKind.Action, Route = route };

    private static GlobalSearchRecentItemInput PersonEntry(int employeeId) =>
        new() { Kind = GlobalSearchRecentKind.Person, EmployeeId = employeeId };

    private Employee CreateEmployee(string code, string fullNameEn, string fullNameAr)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Set<Nationality>().Add(new Nationality
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
        });

        Employee employee = null;
        UsingDbContext(context =>
        {
            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                FirstNameEn = "F",
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = fullNameEn,
                FullNameAr = fullNameAr,
                BirthDate = new DateTime(1990, 1, 1),
                Gender = Gender.Male,
                NationalityId = nationality.Id
            }).Entity;
        });

        return employee;
    }
}
