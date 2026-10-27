using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.GovtRelationsManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GovtRelationsManagement;

// ReSharper disable once InconsistentNaming
public class BusinessTypesAppService_Tests : GovtRelationsManagementAppServiceTestBase
{
    private BusinessTypesAppService CreateAppService()
    {
        return CreateService(() => new BusinessTypesAppService(FakeRepository.For<BusinessType>(Context)));
    }

    [Fact]
    public async Task Should_Create_BusinessType()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            Code = "BT1",
            NameEn = "Trading",
            NameAr = "تجارة"
        });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Trading");
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Name()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            Code = "BT2",
            NameEn = "Services",
            NameAr = "خدمات"
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
            {
                Code = "BT3",
                NameEn = "Services",
                NameAr = "خدمات2"
            }));
    }
}
