using System.Threading.Tasks;
using HCMv3.PerformanceManagement;
using HCMv3.PerformanceManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PerformanceManagement;

// ReSharper disable once InconsistentNaming
public class PAEmployeesAppService_Tests : PerformanceManagementAppServiceTestBase
{
    private PAEmployeesAppService CreateAppService()
    {
        return CreateService(() => new PAEmployeesAppService(FakeRepository.For<PAEmployee>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_PAEmployee()
    {
        var employeeId = CreateEmployee();
        var cycleId = CreatePACycle("C1");
        var formTypeId = CreateFormType();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAEmployeeDto
        {
            EmployeeId = employeeId,
            PACycleId = cycleId,
            FormTypeId = formTypeId,
            RequestComments = "Comments"
        });

        var result = await service.GetAll(new GetAllPAEmployeesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployeeId.ShouldBe(employeeId);
        result.Items[0].PACycleId.ShouldBe(cycleId);
    }

    [Fact]
    public async Task Should_Update_PAEmployee()
    {
        var employeeId = CreateEmployee();
        var cycleId = CreatePACycle("C1");
        var formTypeId = CreateFormType();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAEmployeeDto { EmployeeId = employeeId, PACycleId = cycleId, FormTypeId = formTypeId });

        var all = await service.GetAll(new GetAllPAEmployeesInput());
        var id = all.Items[0].Id;

        var forEdit = await service.GetPAEmployeeForEdit(new Abp.Application.Services.Dto.EntityDto(id));
        forEdit.PAEmployee.ProposedIncrement = "10%";
        await service.CreateOrEdit(forEdit.PAEmployee);

        var updated = await service.GetPAEmployeeForEdit(new Abp.Application.Services.Dto.EntityDto(id));
        updated.PAEmployee.ProposedIncrement.ShouldBe("10%");
    }

    [Fact]
    public async Task Should_Delete_PAEmployee()
    {
        var employeeId = CreateEmployee();
        var cycleId = CreatePACycle("C1");
        var formTypeId = CreateFormType();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAEmployeeDto { EmployeeId = employeeId, PACycleId = cycleId, FormTypeId = formTypeId });

        var all = await service.GetAll(new GetAllPAEmployeesInput());
        var id = all.Items[0].Id;

        await service.Delete(new Abp.Application.Services.Dto.EntityDto(id));

        var afterDelete = await service.GetAll(new GetAllPAEmployeesInput());
        afterDelete.TotalCount.ShouldBe(0);
    }
}
