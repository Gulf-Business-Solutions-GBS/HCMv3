using System.Threading.Tasks;
using HCMv3.PerformanceManagement;
using HCMv3.PerformanceManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PerformanceManagement;

// ReSharper disable once InconsistentNaming
public class PAGoalsAppService_Tests : PerformanceManagementAppServiceTestBase
{
    private PAGoalsAppService CreateAppService()
    {
        return CreateService(() => new PAGoalsAppService(FakeRepository.For<PAGoal>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_Goal()
    {
        var periodId = CreatePAPeriod("2026");
        var employeeId = CreateEmployee();
        var paFormId = CreatePAForm(periodId, employeeId);
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAGoalDto { PAFormId = paFormId, Goal = "Improve sales" });

        var result = await service.GetAll(new GetAllPAGoalsInput { PAFormId = paFormId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].Goal.ShouldBe("Improve sales");
    }

    [Fact]
    public async Task Should_Update_Goal()
    {
        var periodId = CreatePAPeriod("2026");
        var employeeId = CreateEmployee();
        var paFormId = CreatePAForm(periodId, employeeId);
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAGoalDto { PAFormId = paFormId, Goal = "A" });

        var all = await service.GetAll(new GetAllPAGoalsInput { PAFormId = paFormId });
        var id = all.Items[0].Id;

        var forEdit = await service.GetPAGoalForEdit(new Abp.Application.Services.Dto.EntityDto(id));
        forEdit.PAGoal.Goal = "B";
        await service.CreateOrEdit(forEdit.PAGoal);

        var updated = await service.GetPAGoalForEdit(new Abp.Application.Services.Dto.EntityDto(id));
        updated.PAGoal.Goal.ShouldBe("B");
    }

    [Fact]
    public async Task Should_Delete_Goal()
    {
        var periodId = CreatePAPeriod("2026");
        var employeeId = CreateEmployee();
        var paFormId = CreatePAForm(periodId, employeeId);
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAGoalDto { PAFormId = paFormId, Goal = "A" });

        var all = await service.GetAll(new GetAllPAGoalsInput { PAFormId = paFormId });
        var id = all.Items[0].Id;

        await service.Delete(new Abp.Application.Services.Dto.EntityDto(id));

        var afterDelete = await service.GetAll(new GetAllPAGoalsInput { PAFormId = paFormId });
        afterDelete.TotalCount.ShouldBe(0);
    }
}
