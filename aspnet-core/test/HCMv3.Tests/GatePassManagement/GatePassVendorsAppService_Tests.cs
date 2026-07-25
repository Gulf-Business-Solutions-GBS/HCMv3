using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GatePassManagement;
using HCMv3.GatePassManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GatePassManagement;

// ReSharper disable once InconsistentNaming
public class GatePassVendorsAppService_Tests : GatePassManagementAppServiceTestBase
{
    private GatePassVendorsAppService CreateAppService()
    {
        return CreateService(() => new GatePassVendorsAppService(FakeRepository.For<GatePassVendor>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_GatePassVendor()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditGatePassVendorDto
        {
            NameEn = "ABC Supplies",
            NameAr = "ABC",
            VendorAddress = "Kuwait City"
        });

        var result = await service.GetAll(new GetAllGatePassVendorsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("ABC Supplies");
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_Throw_When_NameEn_Duplicate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditGatePassVendorDto { NameEn = "Vendor A", NameAr = "A" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditGatePassVendorDto { NameEn = "Vendor A", NameAr = "Other" }));
    }
}
