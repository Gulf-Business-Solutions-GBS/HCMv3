using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.Workflow;
using HCMv3.Workflow.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

// ReSharper disable once InconsistentNaming
public class WorkflowRolesAppService_Tests : WorkflowAppServiceTestBase
{
    private WorkflowRolesAppService CreateAppService()
    {
        return CreateService(() => new WorkflowRolesAppService(
            FakeRepository.For<WorkflowRole>(Context),
            FakeRepository.For<EmployeeWorkflowRole>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_WorkflowRole()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditWorkflowRoleDto
        {
            NameEn = "HR Manager",
            NameAr = "مدير الموارد البشرية",
            WorkflowRoleGroup = WorkflowRoleGroup.Department
        });

        var result = await service.GetAll(new GetAllWorkflowRolesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("HR Manager");
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_Throw_When_NameEn_Duplicate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditWorkflowRoleDto
        {
            NameEn = "HR Manager",
            NameAr = "A",
            WorkflowRoleGroup = WorkflowRoleGroup.Department
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditWorkflowRoleDto
            {
                NameEn = "HR Manager",
                NameAr = "B",
                WorkflowRoleGroup = WorkflowRoleGroup.Company
            }));
    }

    private void Assign(int employeeId, int roleId, DateTime start, DateTime end)
    {
        Context.EmployeeWorkflowRoles.Add(new EmployeeWorkflowRole
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeId = employeeId,
            WorkflowRoleId = roleId,
            StartDate = start,
            EndDate = end
        });
        Context.SaveChanges();
    }

    /// <summary>
    /// "How many employees are assigned to this role?" -- counted on today, since only those assignments are
    /// visible to approver routing. Expired and future assignments are not counted, and one person with two
    /// overlapping assignments is one holder.
    /// </summary>
    [Fact]
    public async Task Should_Count_The_Employees_Holding_The_Role_Today()
    {
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditWorkflowRoleDto { NameEn = "HR", NameAr = "HR" });
        var roleId = Context.WorkflowRoles.Single().Id;

        var current = CreateEmployee();
        var alsoCurrent = CreateEmployee();
        var expired = CreateEmployee();
        var future = CreateEmployee();
        var today = DateTime.Today;
        var openEnded = new DateTime(9999, 12, 31);

        Assign(current, roleId, today.AddDays(-10), openEnded);
        Assign(current, roleId, today.AddDays(-5), openEnded);
        Assign(alsoCurrent, roleId, today, today);
        Assign(expired, roleId, today.AddYears(-1), today.AddDays(-1));
        Assign(future, roleId, today.AddDays(1), openEnded);

        var result = await service.GetAll(new GetAllWorkflowRolesInput());

        result.Items.Single().EmployeeCount.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Report_Zero_For_A_Role_Nobody_Holds()
    {
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditWorkflowRoleDto { NameEn = "HR", NameAr = "HR" });

        var result = await service.GetAll(new GetAllWorkflowRolesInput());

        result.Items.Single().EmployeeCount.ShouldBe(0);
    }
}
