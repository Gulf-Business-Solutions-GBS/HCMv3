using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PerformanceManagement;
using HCMv3.PerformanceManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PerformanceManagement;

// ReSharper disable once InconsistentNaming
public class PAFormsAppService_Tests : PerformanceManagementAppServiceTestBase
{
    private PAFormsAppService CreateAppService()
    {
        return CreateService(() => new PAFormsAppService(FakeRepository.For<PAForm>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_PAForm()
    {
        var periodId = CreatePAPeriod("2026");
        var employeeId = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAFormDto
        {
            PAPeriodId = periodId,
            EmployeeId = employeeId,
            Comments = "C"
        });

        var result = await service.GetAll(new GetAllPAFormsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].PAPeriodId.ShouldBe(periodId);
        result.Items[0].EmployeeId.ShouldBe(employeeId);
    }

    [Fact]
    public async Task Should_Throw_When_Duplicate_Period_And_Employee()
    {
        var periodId = CreatePAPeriod("2026");
        var employeeId = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAFormDto { PAPeriodId = periodId, EmployeeId = employeeId });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditPAFormDto { PAPeriodId = periodId, EmployeeId = employeeId }));
    }

    [Fact]
    public async Task Should_Delete_PAForm()
    {
        var periodId = CreatePAPeriod("2026");
        var employeeId = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAFormDto { PAPeriodId = periodId, EmployeeId = employeeId });

        var all = await service.GetAll(new GetAllPAFormsInput());
        var id = all.Items[0].Id;

        await service.Delete(new Abp.Application.Services.Dto.EntityDto(id));

        var afterDelete = await service.GetAll(new GetAllPAFormsInput());
        afterDelete.TotalCount.ShouldBe(0);
    }
}
