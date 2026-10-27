using System;
using System.Threading.Tasks;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// A step's approvers when it names a workflow role, a manager level, or both.
///
/// <para>
/// A role on its own goes to the role's holders (stored as <see cref="WorkflowApproverPersonType.Person"/>);
/// a manager level on its own goes to that manager; both together go to both. Before this, the role on a
/// manager step was ignored although the screen showed it as set.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class WorkflowApproverResolver_ManagerAndRole_Tests : WorkflowAppServiceTestBase
{
    private WorkflowApproverResolver CreateResolver()
    {
        var settingsAccessor = Substitute.For<HCMv3.SystemSettings.Services.ISysSettingsAccessor>();
        settingsAccessor.GetWorkflowSettingsAsync()
            .Returns(Task.FromResult(new HCMv3.SystemSettings.ManageWorkflowSettings()));

        return new WorkflowApproverResolver(
            FakeRepository.For<EmployeeWorkflowRole>(Context),
            FakeRepository.For<EmployeeManager>(Context),
            FakeRepository.For<EmployeeOrgAssignment>(Context),
            FakeRepository.For<RequestDetail>(Context),
            FakeRepository.For<RequestLog>(Context),
            Substitute.For<Abp.Domain.Repositories.IRepository<HCMv3.OrganizationManagement.OrganizationUnitType>>(),
            new WorkflowDelegationResolver(
                FakeRepository.For<EmployeeDelegation>(Context),
                FakeRepository.For<Employee>(Context)),
            settingsAccessor,
            Substitute.For<HCMv3.OrganizationManagement.Services.IOrgHierarchyService>());
    }

    private int CreateRoleHeldBy(int employeeId)
    {
        var role = new WorkflowRole { TenantId = AbpSession.TenantId ?? 1, Code = "HR", NameEn = "HR", NameAr = "HR" };
        Context.WorkflowRoles.Add(role);
        Context.SaveChanges();

        Context.EmployeeWorkflowRoles.Add(new EmployeeWorkflowRole
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeId = employeeId,
            WorkflowRoleId = role.Id,
            StartDate = DateTime.Today.AddDays(-1),
            EndDate = new DateTime(9999, 12, 31)
        });
        Context.SaveChanges();

        return role.Id;
    }

    private void SetManager(int employeeId, int managerId)
    {
        Context.EmployeeManagers.Add(new EmployeeManager
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeId = employeeId,
            ManagerId = managerId,
            StartDate = DateTime.Today.AddYears(-1),
            EndDate = new DateTime(9999, 12, 31)
        });
        Context.SaveChanges();
    }

    private static WorkflowRequest RequestFor(int employeeId) => new() { EmployeeId = employeeId };

    [Fact]
    public void Should_Route_A_Manager_Step_That_Also_Names_A_Role_To_Both()
    {
        var requesterId = CreateEmployee();
        var managerId = CreateEmployee();
        var hrId = CreateEmployee();
        SetManager(requesterId, managerId);
        var roleId = CreateRoleHeldBy(hrId);

        var step = new RequestDetail
        {
            Step = 2,
            ApproverPerson = WorkflowApproverPersonType.PersonManager,
            WorkflowRoleId = roleId
        };

        var approvers = CreateResolver().ResolveForStep(RequestFor(requesterId), new RequestMaster(), step);

        approvers.ShouldBe(new[] { managerId, hrId }, ignoreOrder: true);
    }

    [Fact]
    public void Should_Route_A_Manager_Step_Without_A_Role_To_The_Manager_Only()
    {
        var requesterId = CreateEmployee();
        var managerId = CreateEmployee();
        var hrId = CreateEmployee();
        SetManager(requesterId, managerId);
        CreateRoleHeldBy(hrId);

        var step = new RequestDetail { Step = 2, ApproverPerson = WorkflowApproverPersonType.PersonManager };

        var approvers = CreateResolver().ResolveForStep(RequestFor(requesterId), new RequestMaster(), step);

        approvers.ShouldBe(new[] { managerId });
    }

    [Fact]
    public void Should_Not_List_Someone_Twice_When_The_Manager_Also_Holds_The_Role()
    {
        var requesterId = CreateEmployee();
        var managerId = CreateEmployee();
        SetManager(requesterId, managerId);
        var roleId = CreateRoleHeldBy(managerId);

        var step = new RequestDetail
        {
            Step = 2,
            ApproverPerson = WorkflowApproverPersonType.PersonManager,
            WorkflowRoleId = roleId
        };

        var approvers = CreateResolver().ResolveForStep(RequestFor(requesterId), new RequestMaster(), step);

        approvers.ShouldBe(new[] { managerId });
    }

    [Fact]
    public void Should_Still_Reach_The_Role_Holders_When_The_Requester_Has_No_Manager()
    {
        var requesterId = CreateEmployee();
        var hrId = CreateEmployee();
        var roleId = CreateRoleHeldBy(hrId);

        var step = new RequestDetail
        {
            Step = 2,
            ApproverPerson = WorkflowApproverPersonType.PersonManager,
            WorkflowRoleId = roleId
        };

        var approvers = CreateResolver().ResolveForStep(RequestFor(requesterId), new RequestMaster(), step);

        approvers.ShouldBe(new[] { hrId });
    }
}
