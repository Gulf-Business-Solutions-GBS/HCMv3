using System.Threading.Tasks;
using HCMv3.GatePassManagement;
using HCMv3.GeneralMasterInfo.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GatePassManagement;

// ReSharper disable once InconsistentNaming
public class PersonTypesAppService_Tests : GatePassManagementAppServiceTestBase
{
    private PersonTypesAppService CreateAppService()
    {
        return CreateService(() => new PersonTypesAppService(FakeRepository.For<PersonType>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_PersonType()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Visitor", NameAr = "زائر" });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Visitor");
    }
}
