using System.Linq;
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
        return CreateService(() => new PAEmployeesAppService(
            FakeRepository.For<PAEmployee>(Context),
            new FakeAdminTransactionSubmitter()));
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
            FormTypeId = formTypeId
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
    public async Task Should_Not_Write_The_Legacy_Comment_Columns_On_Create()
    {
        // RequestComments and WorkflowComments are legacy: they exist only so appraisals migrated from HRMS
        // keep the text their approvers typed. New comments belong to the WorkflowComment thread, so a new
        // appraisal must leave both columns empty rather than starting another concatenated blob.
        var employeeId = CreateEmployee();
        var cycleId = CreatePACycle("C1");
        var formTypeId = CreateFormType();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAEmployeeDto
        {
            EmployeeId = employeeId,
            PACycleId = cycleId,
            FormTypeId = formTypeId,
            ProposedIncrement = "10%"
        });

        var row = Context.Set<PAEmployee>().Single();

        row.RequestComments.ShouldBeNull();
        row.WorkflowComments.ShouldBeNull();
    }

    [Fact]
    public async Task Editing_Should_Leave_Migrated_Comment_History_Untouched()
    {
        // The other half of "keep the column, stop writing it". Since the edit DTO no longer carries these
        // fields, an edit must not blank them either -- otherwise the first time anyone opened and saved a
        // migrated appraisal, its entire approval history would be silently erased.
        var employeeId = CreateEmployee();
        var cycleId = CreatePACycle("C1");
        var formTypeId = CreateFormType();

        var migrated = new PAEmployee
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeId = employeeId,
            PACycleId = cycleId,
            FormTypeId = formTypeId,
            RequestComments = "Please review",
            WorkflowComments = "Approved by Ahmed - 01 Jan 2024"
        };
        Context.Set<PAEmployee>().Add(migrated);
        Context.SaveChanges();

        var service = CreateAppService();

        var forEdit = await service.GetPAEmployeeForEdit(new Abp.Application.Services.Dto.EntityDto(migrated.Id));
        forEdit.PAEmployee.ProposedJobTitle = "Senior Analyst";
        await service.CreateOrEdit(forEdit.PAEmployee);

        var row = Context.Set<PAEmployee>().Single(e => e.Id == migrated.Id);

        row.ProposedJobTitle.ShouldBe("Senior Analyst");
        row.RequestComments.ShouldBe("Please review");
        row.WorkflowComments.ShouldBe("Approved by Ahmed - 01 Jan 2024");
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
