using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.PayrollManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class TicketClassesAppService_Tests : PayrollManagementAppServiceTestBase
{
    private TicketClassesAppService CreateAppService()
    {
        return CreateService(() => new TicketClassesAppService(FakeRepository.For<TicketClass>(Context)));
    }

    [Fact]
    public async Task Should_Create_TicketClass()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            Code = "TC1",
            NameEn = "Economy",
            NameAr = "اقتصادي"
        });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Economy");
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Code()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { Code = "TC2", NameEn = "Business", NameAr = "رجال الاعمال" });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { Code = "TC2", NameEn = "First", NameAr = "الأولى" }));
    }
}
