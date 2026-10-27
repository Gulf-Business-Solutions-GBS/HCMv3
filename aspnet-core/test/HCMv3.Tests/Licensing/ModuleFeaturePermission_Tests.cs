using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using HCMv3.Authorization;
using HCMv3.Licensing;
using HCMv3.Recruitment;
using HCMv3.Recruitment.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Licensing;

/// <summary>Option A: a module switched off in the tenant's edition takes its permissions away, and with them its menu and API.</summary>
// ReSharper disable once InconsistentNaming
public class ModuleFeaturePermission_Tests : LicensingTestBase
{
    private readonly IPermissionChecker _permissionChecker;
    private readonly IPermissionManager _permissionManager;

    public ModuleFeaturePermission_Tests()
    {
        _permissionChecker = Resolve<IPermissionChecker>();
        _permissionManager = Resolve<IPermissionManager>();
    }

    private async Task<bool> IsGrantedAsync(string permission)
    {
        var granted = false;
        await WithUnitOfWorkAsync(async () => granted = await _permissionChecker.IsGrantedAsync(permission));
        return granted;
    }

    [Fact]
    public void Should_Tie_A_Module_Root_Permission_To_Its_Licence()
    {
        var dependency = _permissionManager.GetPermission(AppPermissions.Pages_Recruitment).FeatureDependency
            .ShouldBeOfType<HcmModuleLicenceDependency>();

        dependency.ModuleFeatures.ShouldContain(HcmFeatures.ModuleRecruitment);
        dependency.Modules.ShouldContain(LicenceModule.Recruitment);
        dependency.Modules.ShouldContain(LicenceModule.Admin);
    }

    [Fact]
    public void Should_Tie_Every_Descendant_Of_A_Module_Root_To_Its_Licence()
    {
        var descendant = _permissionManager.GetPermission(AppPermissions.Pages_Recruitment_ApplicationStatuses_Create);

        descendant.FeatureDependency.ShouldBeOfType<HcmModuleLicenceDependency>()
            .ModuleFeatures.ShouldContain(HcmFeatures.ModuleRecruitment);
    }

    [Fact]
    public void Should_Tie_Mobile_Permissions_To_Both_Ess_And_Mobile()
    {
        var dependency = _permissionManager.GetPermission(AppPermissions.Ess_GeoPunch).FeatureDependency
            .ShouldBeOfType<HcmModuleLicenceDependency>();

        dependency.ModuleFeatures.ShouldBe(new[] { HcmFeatures.ModuleEss, HcmFeatures.ModuleMobile }, ignoreOrder: true);
    }

    [Fact]
    public async Task Should_Grant_A_Module_That_Is_Switched_On()
    {
        (await IsGrantedAsync(AppPermissions.Pages_Recruitment)).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Refuse_Every_Permission_Of_A_Module_Switched_Off()
    {
        await SetTenantFeatureAsync(HcmFeatures.ModuleRecruitment, "false");

        (await IsGrantedAsync(AppPermissions.Pages_Recruitment)).ShouldBeFalse();
        (await IsGrantedAsync(AppPermissions.Pages_Recruitment_ApplicationStatuses_Create)).ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Leave_Other_Modules_Alone_When_One_Is_Switched_Off()
    {
        await SetTenantFeatureAsync(HcmFeatures.ModuleRecruitment, "false");

        (await IsGrantedAsync(AppPermissions.Pages_PersonnelAdministration)).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Hide_A_Switched_Off_Module_From_The_Permission_List()
    {
        await SetTenantFeatureAsync(HcmFeatures.ModulePerformance, "false");

        _permissionManager.GetAllPermissions()
            .Select(p => p.Name)
            .ShouldNotContain(AppPermissions.Pages_PerformanceManagement);
    }

    [Fact]
    public async Task Should_Block_The_App_Services_Of_A_Switched_Off_Module()
    {
        await SetTenantFeatureAsync(HcmFeatures.ModuleRecruitment, "false");

        await Should.ThrowAsync<AbpAuthorizationException>(() =>
            Resolve<IApplicationStatusesAppService>().GetAll(new GetAllApplicationStatusesInput()));
    }
}
