using System.Threading.Tasks;
using Abp.UI;
using HCMv3.BenefitsManagement;
using HCMv3.GeneralMasterInfo.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.BenefitsManagement;

// ReSharper disable once InconsistentNaming
public class InsuranceProvidersAppService_Tests : BenefitsManagementAppServiceTestBase
{
    private InsuranceProvidersAppService CreateAppService()
    {
        return CreateService(() => new InsuranceProvidersAppService(FakeRepository.For<InsuranceProvider>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_InsuranceProvider()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            NameEn = "ACME Insurance",
            NameAr = "ايكمي"
        });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("ACME Insurance");
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_Throw_When_NameEn_Duplicate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Prov1", NameAr = "Prov1" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Prov1", NameAr = "Other" }));
    }
}
