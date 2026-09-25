using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Timing;
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

    /// <summary>
    /// An employee with a staff number and a searchable name -- the base helper leaves both null, which the
    /// free-text filter has nothing to match against.
    /// </summary>
    private int CreateEmployee(string code, string fullNameEn)
    {
        var id = CreateEmployee();
        var employee = Context.Employees.Single(e => e.Id == id);
        employee.Code = code;
        employee.FullNameEn = fullNameEn;
        Context.SaveChanges();
        return id;
    }

    private async Task AssignAsync(EmployeeWorkflowRolesAppService service, int employeeId, int workflowRoleId, DateTime start, DateTime end)
    {
        await service.CreateOrEdit(new CreateOrEditEmployeeWorkflowRoleDto
        {
            EmployeeId = employeeId,
            WorkflowRoleId = workflowRoleId,
            StartDate = start,
            EndDate = end
        });
    }

    [Fact]
    public async Task Should_Filter_By_WorkflowRoleId()
    {
        var service = CreateAppService();
        var employeeId = CreateEmployee();
        var hrRoleId = CreateWorkflowRole();
        var financeRoleId = CreateWorkflowRole();

        await AssignAsync(service, employeeId, hrRoleId, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
        await AssignAsync(service, employeeId, financeRoleId, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        var result = await service.GetAll(new GetAllEmployeeWorkflowRolesInput { WorkflowRoleId = financeRoleId });

        result.TotalCount.ShouldBe(1);
        result.Items[0].WorkflowRoleId.ShouldBe(financeRoleId);
    }

    [Fact]
    public async Task Should_Filter_By_Employee_Code()
    {
        var service = CreateAppService();
        var matching = CreateEmployee("4409", "Ahmad Ali");
        var other = CreateEmployee("5510", "Sara Hassan");
        var workflowRoleId = CreateWorkflowRole();

        await AssignAsync(service, matching, workflowRoleId, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
        await AssignAsync(service, other, workflowRoleId, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        var result = await service.GetAll(new GetAllEmployeeWorkflowRolesInput { Filter = "4409" });

        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployeeId.ShouldBe(matching);
        result.Items[0].EmployeeCode.ShouldBe("4409");
    }

    [Fact]
    public async Task Should_Filter_By_Employee_Name()
    {
        var service = CreateAppService();
        var matching = CreateEmployee("4409", "Ahmad Ali");
        var other = CreateEmployee("5510", "Sara Hassan");
        var workflowRoleId = CreateWorkflowRole();

        await AssignAsync(service, matching, workflowRoleId, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
        await AssignAsync(service, other, workflowRoleId, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        var result = await service.GetAll(new GetAllEmployeeWorkflowRolesInput { Filter = "Sara" });

        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployeeId.ShouldBe(other);
    }

    /// <summary>
    /// The case the flag exists for: an assignment whose range has passed is invisible to approver
    /// resolution, so the chain pointing at the role finds nobody while the row still sits in the grid.
    /// </summary>
    [Fact]
    public async Task Should_Report_Expired_Assignment_As_Not_Active_Today()
    {
        var service = CreateAppService();
        var employeeId = CreateEmployee();
        var workflowRoleId = CreateWorkflowRole();
        var today = Clock.Now.Date;

        await AssignAsync(service, employeeId, workflowRoleId, today.AddYears(-2), today.AddDays(-1));

        var result = await service.GetAll(new GetAllEmployeeWorkflowRolesInput());

        result.TotalCount.ShouldBe(1);
        result.Items[0].IsActiveToday.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Report_Current_Assignment_As_Active_Today()
    {
        var service = CreateAppService();
        var employeeId = CreateEmployee();
        var workflowRoleId = CreateWorkflowRole();
        var today = Clock.Now.Date;

        await AssignAsync(service, employeeId, workflowRoleId, today.AddDays(-1), today.AddDays(1));

        var result = await service.GetAll(new GetAllEmployeeWorkflowRolesInput());

        result.Items[0].IsActiveToday.ShouldBeTrue();
    }

    /// <summary>
    /// Inclusive at both ends: an assignment ending today is still today's assignment.
    /// </summary>
    [Fact]
    public async Task Should_Treat_Assignment_Ending_Today_As_Active()
    {
        var service = CreateAppService();
        var employeeId = CreateEmployee();
        var workflowRoleId = CreateWorkflowRole();
        var today = Clock.Now.Date;

        await AssignAsync(service, employeeId, workflowRoleId, today.AddMonths(-1), today);

        var result = await service.GetAll(new GetAllEmployeeWorkflowRolesInput());

        result.Items[0].IsActiveToday.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Keep_Only_Active_Assignments_When_OnlyActiveToday_Is_Set()
    {
        var service = CreateAppService();
        var current = CreateEmployee();
        var expired = CreateEmployee();
        var workflowRoleId = CreateWorkflowRole();
        var today = Clock.Now.Date;

        await AssignAsync(service, current, workflowRoleId, today.AddDays(-5), today.AddDays(5));
        await AssignAsync(service, expired, workflowRoleId, today.AddYears(-2), today.AddDays(-1));

        var result = await service.GetAll(new GetAllEmployeeWorkflowRolesInput { OnlyActiveToday = true });

        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployeeId.ShouldBe(current);
    }
}
