using System.Threading.Tasks;
using Abp.UI;
using HCMv3.AccommodationManagement;
using HCMv3.AccommodationManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.AccommodationManagement;

// ReSharper disable once InconsistentNaming
public class BuildingsAppService_Tests : AccommodationManagementAppServiceTestBase
{
    private BuildingsAppService CreateAppService()
    {
        return CreateService(() => new BuildingsAppService(FakeRepository.For<Building>(Context)));
    }

    private int CreateLocation(string code)
    {
        var entity = new Location
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code
        };
        Context.Locations.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_Building()
    {
        var locationId = CreateLocation("LOC1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditBuildingDto
        {
            Code = "BLD1",
            NameEn = "Building One",
            NameAr = "المبنى الأول",
            LocationId = locationId
        });

        var result = await service.GetAll(new GetAllBuildingsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].LocationName.ShouldBe("LOC1");
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Name()
    {
        var locationId = CreateLocation("LOC2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditBuildingDto
        {
            Code = "BLD2",
            NameEn = "Duplicate Building",
            NameAr = "مبنى مكرر",
            LocationId = locationId
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditBuildingDto
            {
                Code = "BLD3",
                NameEn = "Duplicate Building",
                NameAr = "مبنى آخر",
                LocationId = locationId
            }));
    }
}
