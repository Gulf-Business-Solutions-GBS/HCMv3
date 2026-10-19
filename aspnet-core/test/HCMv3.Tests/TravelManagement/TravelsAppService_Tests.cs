using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.TravelManagement;
using HCMv3.TravelManagement.Dto;
using HCMv3.Workflow.Services;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TravelManagement;

// ReSharper disable once InconsistentNaming
public class TravelsAppService_Tests : TravelManagementAppServiceTestBase
{
    private TravelsAppService CreateAppService()
    {
        // A trip booked by the travel desk is raised through IAdminTransactionSubmitter, exactly as a
        // self-service request is. With no workflow catalog behind this private context the fake does what
        // the real submitter does for a type that is not routed: insert, and leave WorkflowStatus null.
        // The routing itself is covered end to end against the real engine in
        // HCMv3.Tests.Workflow.TravelWorkflow_Tests.
        return CreateService(() => new TravelsAppService(
            FakeRepository.For<Travel>(Context),
            new HCMv3.Tests.PerformanceManagement.FakeAdminTransactionSubmitter(),
            TravelCostRollupTestFactory.Create(Context),
            FakeRepository.For<HCMv3.Recruitment.Applicant>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_Travel()
    {
        var employeeId = CreateEmployee();
        var travelTypeId = CreateTravelType();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTravelDto
        {
            EmployeeId = employeeId,
            TravelTypeId = travelTypeId,
            StartDate = new DateTime(2024, 3, 1),
            EndDate = new DateTime(2024, 3, 10),
            Comments = "Business trip"
        });

        var result = await service.GetAll(new GetAllTravelsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployeeId.ShouldBe(employeeId);
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
        result.Items[0].TotalTravelCost.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Throw_When_EndDate_Earlier_Than_StartDate()
    {
        var employeeId = CreateEmployee();
        var travelTypeId = CreateTravelType();
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditTravelDto
            {
                EmployeeId = employeeId,
                TravelTypeId = travelTypeId,
                StartDate = new DateTime(2024, 3, 10),
                EndDate = new DateTime(2024, 3, 1)
            }));
    }

    [Fact]
    public async Task Should_Filter_By_EmployeeId()
    {
        var employee1 = CreateEmployee();
        var employee2 = CreateEmployee();
        var travelTypeId = CreateTravelType();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTravelDto
        {
            EmployeeId = employee1,
            TravelTypeId = travelTypeId,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 1, 5)
        });
        await service.CreateOrEdit(new CreateOrEditTravelDto
        {
            EmployeeId = employee2,
            TravelTypeId = travelTypeId,
            StartDate = new DateTime(2024, 2, 1),
            EndDate = new DateTime(2024, 2, 5)
        });

        var result = await service.GetAll(new GetAllTravelsInput { EmployeeId = employee1 });
        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployeeId.ShouldBe(employee1);
    }

    // ---- Employee or applicant (legacy Travel.Applicant, items 114/117) ----

    [Fact]
    public async Task Should_Create_Travel_For_An_Applicant_Without_An_Employee()
    {
        var applicantId = CreateApplicant("Sara");
        var travelTypeId = CreateTravelType();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTravelDto
        {
            ApplicantId = applicantId,
            TravelTypeId = travelTypeId,
            StartDate = new DateTime(2024, 3, 1),
            EndDate = new DateTime(2024, 3, 3)
        });

        var result = await service.GetAll(new GetAllTravelsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployeeId.ShouldBeNull();
        result.Items[0].ApplicantId.ShouldBe(applicantId);
        result.Items[0].ApplicantFullNameEn.ShouldBe("Sara Applicant");
        // Recorded, not routed: an applicant has no manager or org unit to route by.
        result.Items[0].WorkflowStatus.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Not_Send_An_Applicant_Trip_For_Approval()
    {
        var applicantId = CreateApplicant();
        var travelTypeId = CreateTravelType();
        var submitter = Substitute.For<IAdminTransactionSubmitter>();
        var service = CreateService(() => new TravelsAppService(
            FakeRepository.For<Travel>(Context),
            submitter,
            TravelCostRollupTestFactory.Create(Context),
            FakeRepository.For<HCMv3.Recruitment.Applicant>(Context)));

        await service.CreateOrEdit(new CreateOrEditTravelDto
        {
            ApplicantId = applicantId,
            TravelTypeId = travelTypeId,
            StartDate = new DateTime(2024, 3, 1),
            EndDate = new DateTime(2024, 3, 3)
        });

        _ = submitter.DidNotReceiveWithAnyArgs()
            .CreateAndSubmitAsync<Travel>(default, default, default, default);
        Context.Travels.Count(t => t.ApplicantId == applicantId).ShouldBe(1);
    }

    [Fact]
    public async Task Should_Refuse_A_Travel_With_Neither_Employee_Nor_Applicant()
    {
        var travelTypeId = CreateTravelType();
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditTravelDto
            {
                TravelTypeId = travelTypeId,
                StartDate = new DateTime(2024, 3, 1),
                EndDate = new DateTime(2024, 3, 3)
            }));

        Context.Travels.Count().ShouldBe(0);
    }

    [Fact]
    public async Task Should_Refuse_A_Travel_With_Both_Employee_And_Applicant()
    {
        var employeeId = CreateEmployee();
        var applicantId = CreateApplicant();
        var travelTypeId = CreateTravelType();
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditTravelDto
            {
                EmployeeId = employeeId,
                ApplicantId = applicantId,
                TravelTypeId = travelTypeId,
                StartDate = new DateTime(2024, 3, 1),
                EndDate = new DateTime(2024, 3, 3)
            }));

        Context.Travels.Count().ShouldBe(0);
    }

    [Fact]
    public async Task Should_Filter_By_ApplicantId()
    {
        var employeeId = CreateEmployee();
        var applicantId = CreateApplicant();
        var travelTypeId = CreateTravelType();
        CreateTravel(employeeId, travelTypeId);
        var applicantTravelId = CreateApplicantTravel(applicantId, travelTypeId);
        var service = CreateAppService();

        var result = await service.GetAll(new GetAllTravelsInput { ApplicantId = applicantId });

        result.TotalCount.ShouldBe(1);
        result.Items[0].Id.ShouldBe(applicantTravelId);
    }

    [Fact]
    public async Task Should_Refuse_Moving_A_Trip_Sent_For_Approval_To_An_Applicant()
    {
        var employeeId = CreateEmployee();
        var applicantId = CreateApplicant();
        var travelTypeId = CreateTravelType();
        var travelId = CreateTravel(employeeId, travelTypeId);
        var travel = Context.Travels.Single(t => t.Id == travelId);
        travel.WorkflowRequestId = 4242;
        Context.SaveChanges();
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditTravelDto
            {
                Id = travelId,
                ApplicantId = applicantId,
                TravelTypeId = travelTypeId,
                StartDate = travel.StartDate,
                EndDate = travel.EndDate
            }));
    }

    [Fact]
    public async Task Should_Leave_An_Applicant_Trip_Total_Unrounded()
    {
        // Legacy CurrencyService.Rounding(null employee, value) returns the value as-is; the factory's
        // currency rounds to 3 decimals for an employee.
        var rollup = TravelCostRollupTestFactory.Create(Context, 3);
        var travel = new Travel
        {
            ApplicantId = 1,
            TravelDetails = new List<TravelDetail>
            {
                new TravelDetail
                {
                    TravelDetailCosts = new List<TravelDetailCost> { new TravelDetailCost { Amount = 1.23456 } }
                }
            }
        };

        await rollup.ApplyToNewTravelAsync(travel);

        travel.TotalTravelCost.ShouldBe(1.23456);
    }

    [Fact]
    public async Task Should_List_Applicants_For_The_Travel_Forms()
    {
        var applicantId = CreateApplicant("Omar");
        var service = CreateAppService();

        var result = await service.GetApplicantsForLookup();

        result.Items.Count.ShouldBe(1);
        result.Items[0].Id.ShouldBe(applicantId);
        result.Items[0].FullNameEn.ShouldBe("Omar Applicant");
    }
}
