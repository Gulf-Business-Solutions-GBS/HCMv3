using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.Licensing;
using HCMv3.Licensing.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Licensing;

// ReSharper disable once InconsistentNaming
public class LicenceAssignmentAppService_Tests : LicensingTestBase
{
    private readonly ILicenceAssignmentAppService _service;

    public LicenceAssignmentAppService_Tests()
    {
        _service = Resolve<ILicenceAssignmentAppService>();
    }

    private Task<LicenceModuleUsageDto> AssignAsync(long userId, LicenceModule module, bool isAssigned = true)
    {
        return _service.SetUserModuleLicence(new SetUserModuleLicenceInput
        {
            UserId = userId,
            Module = module,
            IsAssigned = isAssigned
        });
    }

    [Fact]
    public async Task Should_Assign_A_Module_And_Count_The_Seat()
    {
        await SetTenantFeatureAsync(HcmFeatures.SeatsEss, "5");
        var user = CreateUser();

        var usage = await AssignAsync(user.Id, LicenceModule.Ess);

        CountLicences(user.Id, LicenceModule.Ess).ShouldBe(1);
        usage.UsedCount.ShouldBe(1);
        usage.SeatCount.ShouldBe(5);
    }

    [Fact]
    public async Task Should_Refuse_An_Assignment_When_No_Seat_Is_Left()
    {
        await SetTenantFeatureAsync(HcmFeatures.SeatsEss, "1");
        var first = CreateUser();
        var second = CreateUser();
        await AssignAsync(first.Id, LicenceModule.Ess);

        await Should.ThrowAsync<UserFriendlyException>(() => AssignAsync(second.Id, LicenceModule.Ess));

        CountLicences(second.Id, LicenceModule.Ess).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Allow_Any_Number_Of_Assignments_When_The_Count_Is_Unlimited()
    {
        var first = CreateUser();
        var second = CreateUser();

        await AssignAsync(first.Id, LicenceModule.Ess);
        var usage = await AssignAsync(second.Id, LicenceModule.Ess);

        usage.IsUnlimited.ShouldBeTrue();
        usage.UsedCount.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Not_Count_The_Licence_Of_An_Inactive_User()
    {
        await SetTenantFeatureAsync(HcmFeatures.SeatsEss, "1");
        var inactive = CreateUser(isActive: false);
        GiveLicence(inactive.Id, LicenceModule.Ess);
        var active = CreateUser();

        var usage = await AssignAsync(active.Id, LicenceModule.Ess);

        usage.UsedCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Refuse_To_Assign_An_Administrator()
    {
        var admin = await GetCurrentUserAsync();

        await Should.ThrowAsync<UserFriendlyException>(() => AssignAsync(admin.Id, LicenceModule.Ess));
    }

    [Fact]
    public async Task Should_Refuse_To_Assign_A_Module_Switched_Off_In_The_Edition()
    {
        await SetTenantFeatureAsync(HcmFeatures.ModuleRecruitment, "false");
        var user = CreateUser();

        await Should.ThrowAsync<UserFriendlyException>(() => AssignAsync(user.Id, LicenceModule.Recruitment));
    }

    [Fact]
    public async Task Should_Do_Nothing_When_Assigning_A_Module_Already_Held()
    {
        await SetTenantFeatureAsync(HcmFeatures.SeatsEss, "1");
        var user = CreateUser();
        await AssignAsync(user.Id, LicenceModule.Ess);

        await AssignAsync(user.Id, LicenceModule.Ess);

        CountLicences(user.Id, LicenceModule.Ess).ShouldBe(1);
    }

    [Fact]
    public async Task Should_Free_The_Seat_When_Unassigned()
    {
        await SetTenantFeatureAsync(HcmFeatures.SeatsEss, "1");
        var first = CreateUser();
        var second = CreateUser();
        await AssignAsync(first.Id, LicenceModule.Ess);

        var afterUnassign = await AssignAsync(first.Id, LicenceModule.Ess, isAssigned: false);
        await AssignAsync(second.Id, LicenceModule.Ess);

        afterUnassign.UsedCount.ShouldBe(0);
        CountLicences(first.Id, LicenceModule.Ess).ShouldBe(0);
        CountLicences(second.Id, LicenceModule.Ess).ShouldBe(1);
    }

    [Fact]
    public async Task Should_List_Only_Users_Holding_The_Module_When_Filtered_By_Assigned()
    {
        var holder = CreateUser();
        var other = CreateUser();
        GiveLicence(holder.Id, LicenceModule.Mobile);

        var result = await _service.GetUsers(new GetLicenceAssignmentUsersInput
        {
            Module = LicenceModule.Mobile,
            IsAssigned = true,
            MaxResultCount = 100
        });

        result.Items.Select(u => u.Id).ShouldContain(holder.Id);
        result.Items.Select(u => u.Id).ShouldNotContain(other.Id);
        result.Items.Single(u => u.Id == holder.Id).Modules.ShouldBe(new[] { LicenceModule.Mobile });
    }

    [Fact]
    public async Task Should_List_Only_Users_Without_The_Module_When_Filtered_By_Not_Assigned()
    {
        var holder = CreateUser();
        var other = CreateUser();
        GiveLicence(holder.Id, LicenceModule.Mobile);

        var result = await _service.GetUsers(new GetLicenceAssignmentUsersInput
        {
            Module = LicenceModule.Mobile,
            IsAssigned = false,
            MaxResultCount = 100
        });

        result.Items.Select(u => u.Id).ShouldContain(other.Id);
        result.Items.Select(u => u.Id).ShouldNotContain(holder.Id);
    }

    [Fact]
    public async Task Should_Flag_Administrators_In_The_User_List()
    {
        var admin = await GetCurrentUserAsync();

        var result = await _service.GetUsers(new GetLicenceAssignmentUsersInput { Filter = admin.UserName, MaxResultCount = 100 });

        result.Items.Single(u => u.Id == admin.Id).IsAdministrator.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Report_Every_Module_And_Whether_Licences_Are_Enforced()
    {
        await SetTenantFeatureAsync(HcmFeatures.Seats, "true");
        await SetTenantFeatureAsync(HcmFeatures.ModulePerformance, "false");

        var summary = await _service.GetSummary();

        summary.IsEnforced.ShouldBeTrue();
        summary.Modules.Select(m => m.Module).ShouldBe(LicenceModules.All.Select(d => d.Module));
        summary.Modules.Single(m => m.Module == LicenceModule.Performance).IsModuleEnabled.ShouldBeFalse();
        summary.Modules.Single(m => m.Module == LicenceModule.Admin).IsModuleEnabled.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Report_A_Module_Over_Its_Count()
    {
        await SetTenantFeatureAsync(HcmFeatures.SeatsEss, "1");
        GiveLicence(CreateUser().Id, LicenceModule.Ess);
        GiveLicence(CreateUser().Id, LicenceModule.Ess);

        var summary = await _service.GetSummary();

        summary.Modules.Single(m => m.Module == LicenceModule.Ess).IsOverLimit.ShouldBeTrue();
    }
}
