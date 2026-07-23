using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class TicketPaxTypesAppService_Tests : PayrollManagementAppServiceTestBase
{
    private TicketPaxTypesAppService CreateAppService()
    {
        return CreateService(() => new TicketPaxTypesAppService(FakeRepository.For<TicketPaxType>(Context)));
    }

    [Fact]
    public async Task Should_Create_TicketPaxType()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTicketPaxTypeDto
        {
            Code = "1",
            NameEn = "Adult",
            NameAr = "بالغ",
            AgeFrom = 12,
            AgeTo = 200
        });

        var result = await service.GetAll(new GetAllTicketPaxTypesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Adult");
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Name()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTicketPaxTypeDto { Code = "2", NameEn = "Child", NameAr = "طفل", AgeFrom = 2, AgeTo = 12 });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditTicketPaxTypeDto { Code = "3", NameEn = "Child", NameAr = "طفل آخر", AgeFrom = 0, AgeTo = 2 }));
    }
}
