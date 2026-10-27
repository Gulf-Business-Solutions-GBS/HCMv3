using System.Threading.Tasks;
using Abp.UI;
using HCMv3.CompetencyDevelopment;
using HCMv3.CompetencyDevelopment.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.CompetencyDevelopment;

// ReSharper disable once InconsistentNaming
public class CDFormsAppService_Tests : CompetencyDevelopmentAppServiceTestBase
{
    private CDFormsAppService CreateAppService()
    {
        return CreateService(() => new CDFormsAppService(FakeRepository.For<CDForm>(Context)));
    }

    private int CreatePeriod(string name)
    {
        var entity = new CDPeriod { TenantId = AbpSession.TenantId ?? 1, Name = name };
        Context.CDPeriods.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_CDForm()
    {
        var periodId = CreatePeriod("2026 Annual");
        var employeeId = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditCDFormDto
        {
            CDPeriodId = periodId,
            EmployeeId = employeeId
        });

        var result = await service.GetAll(new GetAllCDFormsInput { CDPeriodId = periodId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].CDPeriodName.ShouldBe("2026 Annual");
        result.Items[0].EmployeeName.ShouldBe("F L");
    }

    [Fact]
    public async Task Should_Throw_When_Duplicate_Period_And_Employee()
    {
        var periodId = CreatePeriod("2026 Mid");
        var employeeId = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditCDFormDto { CDPeriodId = periodId, EmployeeId = employeeId });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditCDFormDto { CDPeriodId = periodId, EmployeeId = employeeId }));
    }
}
