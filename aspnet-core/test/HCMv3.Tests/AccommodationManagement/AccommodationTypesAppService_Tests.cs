using System.Threading.Tasks;
using Abp.UI;
using HCMv3.AccommodationManagement;
using HCMv3.AccommodationManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.AccommodationManagement;

// ReSharper disable once InconsistentNaming
public class AccommodationTypesAppService_Tests : AccommodationManagementAppServiceTestBase
{
    private AccommodationTypesAppService CreateAppService()
    {
        return CreateService(() => new AccommodationTypesAppService(FakeRepository.For<AccommodationType>(Context)));
    }

    [Fact]
    public async Task Should_Create_AccommodationType()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditAccommodationTypeDto
        {
            Code = "AT1",
            NameEn = "Company Owned",
            NameAr = "ملك الشركة"
        });

        var result = await service.GetAll(new GetAllAccommodationTypesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Company Owned");
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Code()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditAccommodationTypeDto
        {
            Code = "AT2",
            NameEn = "Type A",
            NameAr = "نوع أ"
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditAccommodationTypeDto
            {
                Code = "AT2",
                NameEn = "Type B",
                NameAr = "نوع ب"
            }));
    }
}
