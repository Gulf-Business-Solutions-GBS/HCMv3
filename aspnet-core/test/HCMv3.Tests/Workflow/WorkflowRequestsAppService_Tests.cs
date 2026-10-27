using System.Threading.Tasks;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.Workflow;
using HCMv3.Workflow.Dto;
using HCMv3.Workflow.Services;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

// ReSharper disable once InconsistentNaming
public class WorkflowRequestsAppService_Tests : WorkflowAppServiceTestBase
{
    private WorkflowRequestsAppService CreateAppService(IWorkflowApprovalService workflowApprovalService = null, IEmployeeSessionService employeeSessionService = null)
    {
        return CreateService(() => new WorkflowRequestsAppService(
            FakeRepository.For<WorkflowRequest>(Context),
            workflowApprovalService ?? Substitute.For<IWorkflowApprovalService>(),
            employeeSessionService ?? Substitute.For<IEmployeeSessionService>()));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_WorkflowRequest()
    {
        var employeeId = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditWorkflowRequestDto
        {
            EmployeeId = employeeId,
            RequestType = "LeaveRequest",
            RequestNumber = "REQ-0001",
            RequestStatus = RequestStatus.Draft,
            WorkflowStep = 1
        });

        var result = await service.GetAll(new GetAllWorkflowRequestsInput { EmployeeId = employeeId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].RequestNumber.ShouldBe("REQ-0001");
        result.Items[0].RequestStatus.ShouldBe(RequestStatus.Draft);
    }

    [Fact]
    public async Task Should_Update_WorkflowRequest_Status()
    {
        var employeeId = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditWorkflowRequestDto
        {
            EmployeeId = employeeId,
            RequestType = "LeaveRequest",
            RequestNumber = "REQ-0002",
            RequestStatus = RequestStatus.Open
        });

        var created = (await service.GetAll(new GetAllWorkflowRequestsInput())).Items[0];

        await service.CreateOrEdit(new CreateOrEditWorkflowRequestDto
        {
            Id = created.Id,
            EmployeeId = employeeId,
            RequestType = "LeaveRequest",
            RequestNumber = "REQ-0002",
            RequestStatus = RequestStatus.Approved
        });

        var updated = (await service.GetAll(new GetAllWorkflowRequestsInput())).Items[0];
        updated.RequestStatus.ShouldBe(RequestStatus.Approved);
    }

    [Fact]
    public async Task Should_Resolve_Acting_Employee_And_Delegate_Approve_To_Domain_Service()
    {
        var employee = new Employee
        {
            TenantId = AbpSession.TenantId ?? 1,
            Id = CreateEmployee()
        };

        var employeeSessionService = Substitute.For<IEmployeeSessionService>();
        employeeSessionService.GetCurrentEmployeeAsync().Returns(Task.FromResult(employee));

        var workflowApprovalService = Substitute.For<IWorkflowApprovalService>();

        var service = CreateAppService(workflowApprovalService, employeeSessionService);

        await service.ApproveWorkflowRequest(new ApproveWorkflowRequestDto { RequestId = 42, Remark = "OK" });

        await workflowApprovalService.Received(1).ApproveAsync(42, employee.Id, "OK");
    }

    [Fact]
    public async Task Should_Throw_When_Acting_User_Has_No_Employee_Record()
    {
        var employeeSessionService = Substitute.For<IEmployeeSessionService>();
        employeeSessionService.GetCurrentEmployeeAsync().Returns(Task.FromResult<Employee>(null));

        var service = CreateAppService(employeeSessionService: employeeSessionService);

        await Should.ThrowAsync<Abp.UI.UserFriendlyException>(() =>
            service.SubmitWorkflowRequest(new Abp.Application.Services.Dto.EntityDto(42)));
    }
}
