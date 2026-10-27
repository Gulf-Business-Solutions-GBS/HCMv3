using System.Threading.Tasks;
using Abp.UI;
using HCMv3.BenefitsManagement;
using HCMv3.GeneralMasterInfo.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.BenefitsManagement;

// ReSharper disable once InconsistentNaming
public class InsurancePlansAppService_Tests : BenefitsManagementAppServiceTestBase
{
    private InsurancePlansAppService CreateAppService()
    {
        return CreateService(() => new InsurancePlansAppService(FakeRepository.For<InsurancePlan>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_InsurancePlan()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            NameEn = "Gold Plan",
            NameAr = "الذهبية"
        });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Gold Plan");
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_Throw_When_NameEn_Duplicate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Silver", NameAr = "Silver" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Silver", NameAr = "Other" }));
    }
}
