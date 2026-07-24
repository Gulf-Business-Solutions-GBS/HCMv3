using System.Threading.Tasks;
using Abp.UI;
using HCMv3.AccommodationManagement;
using HCMv3.AccommodationManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.AccommodationManagement;

// ReSharper disable once InconsistentNaming
public class AccommodationServiceTypesAppService_Tests : AccommodationManagementAppServiceTestBase
{
    private AccommodationServiceTypesAppService CreateAppService()
    {
        return CreateService(() => new AccommodationServiceTypesAppService(FakeRepository.For<AccommodationServiceType>(Context)));
    }

    [Fact]
    public async Task Should_Create_AccommodationServiceType()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditAccommodationServiceTypeDto
        {
            Code = "AST1",
            NameEn = "Electricity",
            NameAr = "كهرباء"
        });

        var result = await service.GetAll(new GetAllAccommodationServiceTypesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Electricity");
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Name()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditAccommodationServiceTypeDto
        {
            Code = "AST2",
            NameEn = "Duplicate Service",
            NameAr = "خدمة مكررة"
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditAccommodationServiceTypeDto
            {
                Code = "AST3",
                NameEn = "Duplicate Service",
                NameAr = "خدمة أخرى"
            }));
    }
}
