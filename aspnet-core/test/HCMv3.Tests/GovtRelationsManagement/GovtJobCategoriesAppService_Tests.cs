using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.GovtRelationsManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GovtRelationsManagement;

// ReSharper disable once InconsistentNaming
public class GovtJobCategoriesAppService_Tests : GovtRelationsManagementAppServiceTestBase
{
    private GovtJobCategoriesAppService CreateAppService()
    {
        return CreateService(() => new GovtJobCategoriesAppService(FakeRepository.For<GovtJobCategory>(Context)));
    }

    [Fact]
    public async Task Should_Create_GovtJobCategory()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            Code = "GJC1",
            NameEn = "Administrative",
            NameAr = "إداري"
        });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Administrative");
    }
}
