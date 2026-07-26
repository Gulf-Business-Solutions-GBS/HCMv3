using System;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.Workflow;
using HCMv3.Workflow.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

// ReSharper disable once InconsistentNaming
public class EmployeeWorkflowRolesAppService_Tests : WorkflowAppServiceTestBase
{
    private EmployeeWorkflowRolesAppService CreateAppService()
    {
        return CreateService(() => new EmployeeWorkflowRolesAppService(FakeRepository.For<EmployeeWorkflowRole>(Context)));
    }

    private int CreateWorkflowRole()
    {
        var entity = new WorkflowRole
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = Guid.NewGuid().ToString("N").Substring(0, 6),
            NameEn = "HR Manager " + Guid.NewGuid().ToString("N").Substring(0, 6),
            NameAr = "A",
            WorkflowRoleGroup = WorkflowRoleGroup.Department
        };
        Context.WorkflowRoles.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_EmployeeWorkflowRole()
    {
        var service = CreateAppService();
        var employeeId = CreateEmployee();
        var workflowRoleId = CreateWorkflowRole();

        await service.CreateOrEdit(new CreateOrEditEmployeeWorkflowRoleDto
        {
            EmployeeId = employeeId,
            WorkflowRoleId = workflowRoleId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31)
        });

        var result = await service.GetAll(new GetAllEmployeeWorkflowRolesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployeeId.ShouldBe(employeeId);
        result.Items[0].WorkflowRoleId.ShouldBe(workflowRoleId);
    }

    [Fact]
    public async Task Should_Throw_When_EndDate_Before_StartDate()
    {
        var service = CreateAppService();
        var employeeId = CreateEmployee();
        var workflowRoleId = CreateWorkflowRole();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditEmployeeWorkflowRoleDto
            {
                EmployeeId = employeeId,
                WorkflowRoleId = workflowRoleId,
                StartDate = new DateTime(2026, 12, 31),
                EndDate = new DateTime(2026, 1, 1)
            }));
    }

    [Fact]
    public async Task Should_Filter_By_EmployeeId()
    {
        var service = CreateAppService();
        var employee1 = CreateEmployee();
        var employee2 = CreateEmployee();
        var workflowRoleId = CreateWorkflowRole();

        await service.CreateOrEdit(new CreateOrEditEmployeeWorkflowRoleDto
        {
            EmployeeId = employee1,
            WorkflowRoleId = workflowRoleId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31)
        });
        await service.CreateOrEdit(new CreateOrEditEmployeeWorkflowRoleDto
        {
            EmployeeId = employee2,
            WorkflowRoleId = workflowRoleId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31)
        });

        var result = await service.GetAll(new GetAllEmployeeWorkflowRolesInput { EmployeeId = employee2 });
        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployeeId.ShouldBe(employee2);
    }

    [Fact]
    public async Task Should_Delete_EmployeeWorkflowRole()
    {
        var service = CreateAppService();
        var employeeId = CreateEmployee();
        var workflowRoleId = CreateWorkflowRole();

        await service.CreateOrEdit(new CreateOrEditEmployeeWorkflowRoleDto
        {
            EmployeeId = employeeId,
            WorkflowRoleId = workflowRoleId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31)
        });
        var created = await service.GetAll(new GetAllEmployeeWorkflowRolesInput());
        var id = created.Items[0].Id;

        await service.Delete(new EntityDto(id));

        var result = await service.GetAll(new GetAllEmployeeWorkflowRolesInput());
        result.TotalCount.ShouldBe(0);
    }
}
