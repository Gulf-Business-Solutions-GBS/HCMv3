using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.TravelManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TravelManagement;

// ReSharper disable once InconsistentNaming
public class ExpenseTypesAppService_Tests : TravelManagementAppServiceTestBase
{
    private ExpenseTypesAppService CreateAppService()
    {
        return CreateService(() => new ExpenseTypesAppService(FakeRepository.For<ExpenseType>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_ExpenseType()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            NameEn = "Meals",
            NameAr = "وجبات"
        });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Meals");
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_Throw_When_NameEn_Duplicate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Fuel", NameAr = "Fuel" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Fuel", NameAr = "Other" }));
    }
}
