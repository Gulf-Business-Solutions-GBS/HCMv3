using System;
using System.Threading.Tasks;
using HCMv3.TravelManagement;
using HCMv3.TravelManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TravelManagement;

// ReSharper disable once InconsistentNaming
public class TravelFeedbacksAppService_Tests : TravelManagementAppServiceTestBase
{
    private TravelFeedbacksAppService CreateAppService()
    {
        // The fake submitter inserts the row and leaves WorkflowStatus null, which is exactly what the real
        // one does when the type is not workflow-enabled -- and this context has no catalog to route to.
        return CreateService(() => new TravelFeedbacksAppService(
            FakeRepository.For<TravelFeedback>(Context),
            new HCMv3.Tests.PerformanceManagement.FakeAdminTransactionSubmitter()));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_TravelFeedback()
    {
        var employeeId = CreateEmployee();
        var travelTypeId = CreateTravelType();
        var travelId = CreateTravel(employeeId, travelTypeId);
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTravelFeedbackDto
        {
            EmployeeId = employeeId,
            TravelId = travelId,
            ClaimDate = new DateTime(2024, 3, 10),
            ClaimAmount = 250,
            Comments = "Reimbursement"
        });

        var result = await service.GetAll(new GetAllTravelFeedbacksInput { EmployeeId = employeeId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].ClaimAmount.ShouldBe(250);
        result.Items[0].TravelId.ShouldBe(travelId);
    }

    [Fact]
    public async Task Should_Create_TravelFeedback_Without_Travel()
    {
        var employeeId = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTravelFeedbackDto
        {
            EmployeeId = employeeId,
            ClaimDate = new DateTime(2024, 3, 10),
            ClaimAmount = 100
        });

        var result = await service.GetAll(new GetAllTravelFeedbacksInput { EmployeeId = employeeId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].TravelId.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Filter_By_EmployeeId()
    {
        var employee1 = CreateEmployee();
        var employee2 = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTravelFeedbackDto
        {
            EmployeeId = employee1,
            ClaimDate = new DateTime(2024, 1, 1),
            ClaimAmount = 10
        });
        await service.CreateOrEdit(new CreateOrEditTravelFeedbackDto
        {
            EmployeeId = employee2,
            ClaimDate = new DateTime(2024, 1, 1),
            ClaimAmount = 20
        });

        var result = await service.GetAll(new GetAllTravelFeedbacksInput { EmployeeId = employee1 });
        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployeeId.ShouldBe(employee1);
    }
}
