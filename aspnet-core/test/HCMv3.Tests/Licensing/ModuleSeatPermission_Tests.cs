using System.Threading.Tasks;
using Abp.Authorization;
using Abp.MultiTenancy;
using HCMv3.Authorization;
using HCMv3.Licensing;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Licensing;

/// <summary>Option B: with per-user licences on, a user without a licence for a module is refused that module.</summary>
// ReSharper disable once InconsistentNaming
public class ModuleSeatPermission_Tests : LicensingTestBase
{
    private async Task<bool> IsGrantedAsUserAsync(string userName, string permission)
    {
        LoginAsTenant(AbpTenantBase.DefaultTenantName, userName);
        return await IsGrantedAsync(permission);
    }

    private async Task<bool> IsGrantedAsync(string permission)
    {
        var granted = false;
        await WithUnitOfWorkAsync(async () => granted = await Resolve<IPermissionChecker>().IsGrantedAsync(permission));
        return granted;
    }

    [Fact]
    public async Task Should_Not_Need_A_Licence_While_Per_User_Licences_Are_Off()
    {
        var user = CreateUser(true, AppPermissions.Ess, AppPermissions.Ess_MyLeave);

        (await IsGrantedAsUserAsync(user.UserName, AppPermissions.Ess_MyLeave)).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Refuse_A_Module_To_A_User_Without_Its_Licence()
    {
        await SetTenantFeatureAsync(HcmFeatures.Seats, "true");
        var user = CreateUser(true, AppPermissions.Ess, AppPermissions.Ess_MyLeave);

        (await IsGrantedAsUserAsync(user.UserName, AppPermissions.Ess_MyLeave)).ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Grant_A_Module_To_A_User_Holding_Its_Licence()
    {
        await SetTenantFeatureAsync(HcmFeatures.Seats, "true");
        var user = CreateUser(true, AppPermissions.Ess, AppPermissions.Ess_MyLeave);
        GiveLicence(user.Id, LicenceModule.Ess);

        (await IsGrantedAsUserAsync(user.UserName, AppPermissions.Ess_MyLeave)).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Need_The_Admin_Licence_As_Well_For_A_Module_Inside_The_Admin_Application()
    {
        await SetTenantFeatureAsync(HcmFeatures.Seats, "true");
        var user = CreateUser(true, AppPermissions.Pages, AppPermissions.Pages_Recruitment);
        GiveLicence(user.Id, LicenceModule.Recruitment);

        (await IsGrantedAsUserAsync(user.UserName, AppPermissions.Pages_Recruitment)).ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Grant_A_Module_Inside_The_Admin_Application_With_Both_Licences()
    {
        await SetTenantFeatureAsync(HcmFeatures.Seats, "true");
        var user = CreateUser(true, AppPermissions.Pages, AppPermissions.Pages_Recruitment);
        GiveLicence(user.Id, LicenceModule.Admin);
        GiveLicence(user.Id, LicenceModule.Recruitment);

        (await IsGrantedAsUserAsync(user.UserName, AppPermissions.Pages_Recruitment)).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Not_Need_A_Licence_For_An_Administrator()
    {
        await SetTenantFeatureAsync(HcmFeatures.Seats, "true");

        // The default tenant admin holds the static Admin role and no licence at all.
        (await IsGrantedAsync(AppPermissions.Pages_Recruitment)).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Still_Refuse_A_Switched_Off_Module_To_An_Administrator()
    {
        await SetTenantFeatureAsync(HcmFeatures.Seats, "true");
        await SetTenantFeatureAsync(HcmFeatures.ModuleRecruitment, "false");

        (await IsGrantedAsync(AppPermissions.Pages_Recruitment)).ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Not_Check_Licences_While_An_Administrator_Impersonates()
    {
        await SetTenantFeatureAsync(HcmFeatures.Seats, "true");
        var adminId = AbpSession.UserId;
        var user = CreateUser(true, AppPermissions.Ess, AppPermissions.Ess_MyLeave);

        LoginAsTenant(AbpTenantBase.DefaultTenantName, user.UserName);
        AbpSession.ImpersonatorUserId = adminId;
        AbpSession.ImpersonatorTenantId = TenantId;

        (await IsGrantedAsync(AppPermissions.Ess_MyLeave)).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Refuse_Sign_In_To_A_User_With_Neither_An_Admin_Nor_An_Ess_Licence()
    {
        await SetTenantFeatureAsync(HcmFeatures.Seats, "true");
        var user = CreateUser(true, AppPermissions.Ess);

        await Should.ThrowAsync<Abp.UI.UserFriendlyException>(() =>
            Resolve<IUserModuleLicenceChecker>().EnsureCanSignInAsync(TenantId, user.Id));
    }

    [Fact]
    public async Task Should_Allow_Sign_In_To_A_User_With_An_Ess_Licence()
    {
        await SetTenantFeatureAsync(HcmFeatures.Seats, "true");
        var user = CreateUser(true, AppPermissions.Ess);
        GiveLicence(user.Id, LicenceModule.Ess);

        await Resolve<IUserModuleLicenceChecker>().EnsureCanSignInAsync(TenantId, user.Id);
    }
}
