using System.Threading.Tasks;
using HCMv3.Workflow;
using HCMv3.Workflow.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

// ReSharper disable once InconsistentNaming
public class WorkflowRequestsAppService_Tests : WorkflowAppServiceTestBase
{
    private WorkflowRequestsAppService CreateAppService()
    {
        return CreateService(() => new WorkflowRequestsAppService(FakeRepository.For<WorkflowRequest>(Context)));
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
}
