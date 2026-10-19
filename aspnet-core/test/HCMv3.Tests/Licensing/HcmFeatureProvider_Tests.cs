using System.Threading.Tasks;
using Abp.Application.Features;
using HCMv3.Licensing;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Licensing;

// ReSharper disable once InconsistentNaming
public class HcmFeatureProvider_Tests : LicensingTestBase
{
    private readonly IFeatureManager _featureManager;
    private readonly IFeatureChecker _featureChecker;

    public HcmFeatureProvider_Tests()
    {
        _featureManager = Resolve<IFeatureManager>();
        _featureChecker = Resolve<IFeatureChecker>();
    }

    [Theory]
    [InlineData(HcmFeatures.ModuleEss)]
    [InlineData(HcmFeatures.ModuleMobile)]
    [InlineData(HcmFeatures.ModulePerformance)]
    [InlineData(HcmFeatures.ModuleRecruitment)]
    public void Should_Declare_Module_Features_Switched_On_By_Default(string name)
    {
        var feature = _featureManager.GetOrNull(name);

        feature.ShouldNotBeNull();
        feature.DefaultValue.ShouldBe("true");
    }

    [Theory]
    [InlineData(HcmFeatures.MaxActiveEmployees)]
    [InlineData(HcmFeatures.SeatsAdmin)]
    [InlineData(HcmFeatures.SeatsEss)]
    [InlineData(HcmFeatures.SeatsMobile)]
    [InlineData(HcmFeatures.SeatsPerformance)]
    [InlineData(HcmFeatures.SeatsRecruitment)]
    public void Should_Declare_Count_Features_Unlimited_By_Default(string name)
    {
        var feature = _featureManager.GetOrNull(name);

        feature.ShouldNotBeNull();
        feature.DefaultValue.ShouldBe("0");
    }

    [Fact]
    public void Should_Declare_Per_User_Licences_Switched_Off_By_Default()
    {
        var feature = _featureManager.GetOrNull(HcmFeatures.Seats);

        feature.ShouldNotBeNull();
        feature.DefaultValue.ShouldBe("false");
    }

    [Fact]
    public void Should_Declare_Seat_Counts_Under_The_Per_User_Licence_Switch()
    {
        _featureManager.Get(HcmFeatures.SeatsEss).Parent.Name.ShouldBe(HcmFeatures.Seats);
    }

    [Fact]
    public async Task Should_Leave_Every_Module_On_For_A_Tenant_Whose_Edition_Says_Nothing()
    {
        bool ess = false, recruitment = false, seats = true;
        await WithUnitOfWorkAsync(async () =>
        {
            ess = await _featureChecker.IsEnabledAsync(TenantId, HcmFeatures.ModuleEss);
            recruitment = await _featureChecker.IsEnabledAsync(TenantId, HcmFeatures.ModuleRecruitment);
            seats = await _featureChecker.IsEnabledAsync(TenantId, HcmFeatures.Seats);
        });

        ess.ShouldBeTrue();
        recruitment.ShouldBeTrue();
        seats.ShouldBeFalse();
    }
}
