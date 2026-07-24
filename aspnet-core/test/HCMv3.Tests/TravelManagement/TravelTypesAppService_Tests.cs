using System.Threading.Tasks;
using Abp.UI;
using HCMv3.TravelManagement;
using HCMv3.TravelManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TravelManagement;

// ReSharper disable once InconsistentNaming
public class TravelTypesAppService_Tests : TravelManagementAppServiceTestBase
{
    private TravelTypesAppService CreateAppService()
    {
        return CreateService(() => new TravelTypesAppService(FakeRepository.For<TravelType>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_TravelType_With_WageType()
    {
        var wageTypeId = CreateWageType();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTravelTypeDto
        {
            NameEn = "Business Travel",
            NameAr = "سفر عمل",
            WageTypeId = wageTypeId
        });

        var result = await service.GetAll(new GetAllTravelTypesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Business Travel");
        result.Items[0].WageTypeId.ShouldBe(wageTypeId);
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_Throw_When_NameEn_Duplicate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTravelTypeDto { NameEn = "Local", NameAr = "Local" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditTravelTypeDto { NameEn = "Local", NameAr = "Other" }));
    }

    [Fact]
    public async Task Should_Update_TravelType()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTravelTypeDto { NameEn = "Old", NameAr = "Old" });
        var list = await service.GetAll(new GetAllTravelTypesInput());
        var id = list.Items[0].Id;

        await service.CreateOrEdit(new CreateOrEditTravelTypeDto { Id = id, NameEn = "New", NameAr = "New" });

        var edited = await service.GetTravelTypeForEdit(new Abp.Application.Services.Dto.EntityDto(id));
        edited.TravelType.NameEn.ShouldBe("New");
    }
}
