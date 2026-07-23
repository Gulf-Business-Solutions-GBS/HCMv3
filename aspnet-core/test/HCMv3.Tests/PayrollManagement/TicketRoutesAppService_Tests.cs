using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.PayrollManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class TicketRoutesAppService_Tests : PayrollManagementAppServiceTestBase
{
    private TicketRoutesAppService CreateAppService()
    {
        return CreateService(() => new TicketRoutesAppService(FakeRepository.For<TicketRoute>(Context)));
    }

    [Fact]
    public async Task Should_Create_TicketRoute()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            Code = "TR1",
            NameEn = "Dubai-Cairo",
            NameAr = "دبي-القاهرة"
        });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Dubai-Cairo");
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Code()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { Code = "TR2", NameEn = "Dubai-Riyadh", NameAr = "دبي-الرياض" });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { Code = "TR2", NameEn = "Dubai-Amman", NameAr = "دبي-عمان" }));
    }
}
