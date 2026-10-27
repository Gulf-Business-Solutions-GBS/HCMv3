using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.AccommodationManagement;
using HCMv3.AccommodationManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.AccommodationManagement;

// ReSharper disable once InconsistentNaming
public class LocationsAppService_Tests : AccommodationManagementAppServiceTestBase
{
    private LocationsAppService CreateAppService()
    {
        return CreateService(() => new LocationsAppService(FakeRepository.For<Location>(Context)));
    }

    [Fact]
    public async Task Should_Create_Location()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditLocationDto
        {
            Code = "LOC1",
            NameEn = "Main Location",
            NameAr = "الموقع الرئيسي",
            Comments = "Head office area"
        });

        var result = await service.GetAll(new GetAllLocationsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Main Location");
        result.Items[0].Comments.ShouldBe("Head office area");
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Code()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditLocationDto
        {
            Code = "LOC2",
            NameEn = "Location A",
            NameAr = "الموقع أ"
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditLocationDto
            {
                Code = "LOC2",
                NameEn = "Location B",
                NameAr = "الموقع ب"
            }));
    }

    [Fact]
    public async Task Should_Auto_Assign_Code_When_Blank()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditLocationDto
        {
            Code = "5",
            NameEn = "Location Five",
            NameAr = "الموقع خمسة"
        });

        await service.CreateOrEdit(new CreateOrEditLocationDto
        {
            NameEn = "Location Auto",
            NameAr = "الموقع تلقائي"
        });

        var result = await service.GetAll(new GetAllLocationsInput());
        result.TotalCount.ShouldBe(2);

        var auto = result.Items.First(i => i.NameEn == "Location Auto");
        auto.Code.ShouldBe("6");
    }
}
