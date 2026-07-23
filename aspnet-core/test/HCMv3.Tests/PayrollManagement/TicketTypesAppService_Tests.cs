using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class TicketTypesAppService_Tests : PayrollManagementAppServiceTestBase
{
    private TicketTypesAppService CreateAppService()
    {
        return CreateService(() => new TicketTypesAppService(FakeRepository.For<TicketType>(Context)));
    }

    private int CreateWageType(string code)
    {
        var entity = new WageType { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.WageTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_TicketType()
    {
        var wageTypeId = CreateWageType("WT1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTicketTypeDto
        {
            NameEn = "Annual Ticket",
            NameAr = "تذكرة سنوية",
            TicketBasisPeriod = LeaveBasisPeriod.CalendarDays,
            LapseAfter = 12,
            WageTypeId = wageTypeId
        });

        var result = await service.GetAll(new GetAllTicketTypesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Annual Ticket");
        result.Items[0].Code.ShouldBe("1");
    }

    [Fact]
    public async Task Should_Throw_When_WageType_Missing()
    {
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditTicketTypeDto
            {
                NameEn = "Family Ticket",
                NameAr = "تذكرة عائلية"
            }));
    }
}
