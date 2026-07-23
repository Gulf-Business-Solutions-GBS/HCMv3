using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class TempBenefitTypesAppService_Tests : PayrollManagementAppServiceTestBase
{
    private TempBenefitTypesAppService CreateAppService()
    {
        return CreateService(() => new TempBenefitTypesAppService(FakeRepository.For<TempBenefitType>(Context)));
    }

    [Fact]
    public async Task Should_Create_TempBenefitType()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTempBenefitTypeDto
        {
            Grade = "Grade1",
            Ticket = "1",
            SimCard = "1",
            MinSalary = "1000",
            MaxSalary = "5000"
        });

        var result = await service.GetAll(new GetAllTempBenefitTypesInput { Filter = "Grade1" });
        result.TotalCount.ShouldBe(1);
        result.Items[0].Grade.ShouldBe("Grade1");
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Grade()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTempBenefitTypeDto { Grade = "Grade2" });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditTempBenefitTypeDto { Grade = "Grade2" }));
    }
}
