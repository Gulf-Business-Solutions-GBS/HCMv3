using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.TimeManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

// ReSharper disable once InconsistentNaming
public class TimeTransferTypesAppService_Tests : TimeManagementAppServiceTestBase
{
    private TimeTransferTypesAppService CreateAppService()
    {
        return CreateService(() => new TimeTransferTypesAppService(FakeRepository.For<TimeTransferType>(Context)));
    }

    [Fact]
    public async Task Should_Create_TimeTransferType()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            Code = "TT1",
            NameEn = "Manual Transfer",
            NameAr = "تحويل يدوي"
        });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Manual Transfer");
    }

    [Fact]
    public async Task Should_Never_Allow_Delete()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            Code = "TT2",
            NameEn = "Auto Transfer",
            NameAr = "تحويل تلقائي"
        });

        var created = await service.GetAll(new GetAllSimpleLookupsInput());
        var id = created.Items[0].Id;

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.Delete(new EntityDto(id)));
    }
}
