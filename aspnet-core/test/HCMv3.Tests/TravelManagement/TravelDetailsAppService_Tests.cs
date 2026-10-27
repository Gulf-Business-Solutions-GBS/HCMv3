using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.TravelManagement;
using HCMv3.TravelManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TravelManagement;

// ReSharper disable once InconsistentNaming
public class TravelDetailsAppService_Tests : TravelManagementAppServiceTestBase
{
    private TravelDetailsAppService CreateAppService()
    {
        // The travel repository is what scopes a leg to a trip the caller can see -- see
        // TravelDetailsAppService.GetAll.
        return CreateService(() => new TravelDetailsAppService(
            FakeRepository.For<TravelDetail>(Context),
            FakeRepository.For<TravelDetailCost>(Context),
            FakeRepository.For<TravelPerDiem>(Context),
            FakeRepository.For<Travel>(Context),
            TravelCostRollupTestFactory.Create(Context)));
    }

    private (int travelId, int departureId, int arrivalId) CreateTravelWithDestinations()
    {
        var employeeId = CreateEmployee();
        var travelTypeId = CreateTravelType();
        var travelId = CreateTravel(employeeId, travelTypeId);
        var departureId = CreateTravelDestination("Riyadh");
        var arrivalId = CreateTravelDestination("Jeddah");
        return (travelId, departureId, arrivalId);
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_TravelDetail()
    {
        var (travelId, departureId, arrivalId) = CreateTravelWithDestinations();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTravelDetailDto
        {
            TravelId = travelId,
            DepartureId = departureId,
            ArrivalId = arrivalId,
            DepartureDate = new DateTime(2024, 3, 1),
            ArrivalDate = new DateTime(2024, 3, 5),
            TotalNights = 4
        });

        var result = await service.GetAll(new GetAllTravelDetailsInput { TravelId = travelId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].DepartureNameEn.ShouldBe("Riyadh");
        result.Items[0].ArrivalNameEn.ShouldBe("Jeddah");
        result.Items[0].TotalAmount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Throw_When_ArrivalDate_Earlier_Than_DepartureDate()
    {
        var (travelId, departureId, arrivalId) = CreateTravelWithDestinations();
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditTravelDetailDto
            {
                TravelId = travelId,
                DepartureId = departureId,
                ArrivalId = arrivalId,
                DepartureDate = new DateTime(2024, 3, 5),
                ArrivalDate = new DateTime(2024, 3, 1)
            }));
    }

    [Fact]
    public async Task Should_Block_Delete_When_TravelDetailCost_Children_Exist()
    {
        var (travelId, departureId, arrivalId) = CreateTravelWithDestinations();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTravelDetailDto
        {
            TravelId = travelId,
            DepartureId = departureId,
            ArrivalId = arrivalId,
            DepartureDate = new DateTime(2024, 3, 1),
            ArrivalDate = new DateTime(2024, 3, 5)
        });
        var detail = (await service.GetAll(new GetAllTravelDetailsInput { TravelId = travelId })).Items[0];

        var expenseType = new ExpenseType { TenantId = AbpSession.TenantId ?? 1, NameEn = "Meals", NameAr = "Meals" };
        Context.ExpenseTypes.Add(expenseType);
        Context.SaveChanges();

        Context.TravelDetailCosts.Add(new TravelDetailCost
        {
            TenantId = AbpSession.TenantId ?? 1,
            TravelDetailId = detail.Id,
            ExpenseTypeId = expenseType.Id,
            Amount = 50
        });
        Context.SaveChanges();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.Delete(new Abp.Application.Services.Dto.EntityDto(detail.Id)));
    }

    [Fact]
    public async Task Should_Calculate_Expected_PerDiem()
    {
        var (travelId, departureId, arrivalId) = CreateTravelWithDestinations();
        var groupId = CreateEmployeeGroup();
        var subGroupId = CreateEmployeeSubGroup(groupId);

        Context.TravelPerDiems.Add(new TravelPerDiem
        {
            TenantId = AbpSession.TenantId ?? 1,
            FromDestinationId = departureId,
            ToDestinationId = arrivalId,
            EmployeeGroupId = groupId,
            EmployeeSubGroupId = subGroupId,
            Amount = 100
        });
        Context.SaveChanges();

        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTravelDetailDto
        {
            TravelId = travelId,
            DepartureId = departureId,
            ArrivalId = arrivalId,
            DepartureDate = new DateTime(2024, 3, 1),
            ArrivalDate = new DateTime(2024, 3, 5),
            TotalNights = 4
        });
        var detail = (await service.GetAll(new GetAllTravelDetailsInput { TravelId = travelId })).Items[0];

        var output = await service.CalculateExpectedPerDiemAsync(new CalculateExpectedPerDiemInput
        {
            TravelDetailId = detail.Id,
            EmployeeGroupId = groupId,
            EmployeeSubGroupId = subGroupId
        });

        output.PerDiemRate.ShouldBe(100);
        output.PerDiemAmount.ShouldBe(400);
        output.PerDiemPayPercentage.ShouldBe(100);
        output.PerDiemPayAmount.ShouldBe(400);
    }

    [Fact]
    public async Task Should_Default_PerDiemPayPercentage_To_100()
    {
        var (travelId, departureId, arrivalId) = CreateTravelWithDestinations();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTravelDetailDto
        {
            TravelId = travelId,
            DepartureId = departureId,
            ArrivalId = arrivalId,
            DepartureDate = new DateTime(2024, 3, 1),
            ArrivalDate = new DateTime(2024, 3, 5)
        });

        var item = (await service.GetAll(new GetAllTravelDetailsInput { TravelId = travelId })).Items[0];
        item.PerDiemPayPercentage.ShouldBe(100);

        var forEdit = await service.GetTravelDetailForEdit(new Abp.Application.Services.Dto.EntityDto(item.Id));
        forEdit.TravelDetail.PerDiemPayPercentage.ShouldBe(100);
    }

    [Fact]
    public async Task Should_Apply_PerDiemPayPercentage_To_The_Paid_Amount_Only()
    {
        var (travelId, departureId, arrivalId) = CreateTravelWithDestinations();
        var groupId = CreateEmployeeGroup();
        var subGroupId = CreateEmployeeSubGroup(groupId);

        Context.TravelPerDiems.Add(new TravelPerDiem
        {
            TenantId = AbpSession.TenantId ?? 1,
            FromDestinationId = departureId,
            ToDestinationId = arrivalId,
            EmployeeGroupId = groupId,
            EmployeeSubGroupId = subGroupId,
            Amount = 125
        });
        Context.SaveChanges();

        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTravelDetailDto
        {
            TravelId = travelId,
            DepartureId = departureId,
            ArrivalId = arrivalId,
            DepartureDate = new DateTime(2024, 3, 1),
            ArrivalDate = new DateTime(2024, 3, 4),
            TotalNights = 3,
            PerDiemPayPercentage = 50
        });
        var detail = (await service.GetAll(new GetAllTravelDetailsInput { TravelId = travelId })).Items[0];

        var output = await service.CalculateExpectedPerDiemAsync(new CalculateExpectedPerDiemInput
        {
            TravelDetailId = detail.Id,
            EmployeeGroupId = groupId,
            EmployeeSubGroupId = subGroupId
        });

        // Legacy PerDiemAmount (nights x rate) ignores the percentage; legacy payroll paid
        // PerDiemAmount * PerDiemPayPercentage / 100.
        output.PerDiemAmount.ShouldBe(375);
        output.PerDiemPayPercentage.ShouldBe(50);
        output.PerDiemPayAmount.ShouldBe(187.5);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100.01)]
    public async Task Should_Throw_When_PerDiemPayPercentage_Is_Outside_0_To_100(double percentage)
    {
        var (travelId, departureId, arrivalId) = CreateTravelWithDestinations();
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditTravelDetailDto
            {
                TravelId = travelId,
                DepartureId = departureId,
                ArrivalId = arrivalId,
                DepartureDate = new DateTime(2024, 3, 1),
                ArrivalDate = new DateTime(2024, 3, 5),
                PerDiemPayPercentage = percentage
            }));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public async Task Should_Accept_PerDiemPayPercentage_At_The_Bounds(double percentage)
    {
        var (travelId, departureId, arrivalId) = CreateTravelWithDestinations();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTravelDetailDto
        {
            TravelId = travelId,
            DepartureId = departureId,
            ArrivalId = arrivalId,
            DepartureDate = new DateTime(2024, 3, 1),
            ArrivalDate = new DateTime(2024, 3, 5),
            PerDiemPayPercentage = percentage
        });

        var item = (await service.GetAll(new GetAllTravelDetailsInput { TravelId = travelId })).Items[0];
        item.PerDiemPayPercentage.ShouldBe(percentage);
    }

    [Fact]
    public async Task Should_Give_No_PerDiem_On_An_Applicant_Trip()
    {
        // Legacy TravelDetail.PerDiemRate/PerDiemAmount return 0 when Travel.Employee is null: per diem is
        // priced by the traveller's salary profile, and an applicant has none -- even when a matching rate exists.
        var applicantId = CreateApplicant();
        var travelTypeId = CreateTravelType();
        var travelId = CreateApplicantTravel(applicantId, travelTypeId);
        var departureId = CreateTravelDestination("Kuwait");
        var arrivalId = CreateTravelDestination("Dubai");
        var groupId = CreateEmployeeGroup();
        var subGroupId = CreateEmployeeSubGroup(groupId);

        Context.TravelPerDiems.Add(new TravelPerDiem
        {
            TenantId = AbpSession.TenantId ?? 1,
            FromDestinationId = departureId,
            ToDestinationId = arrivalId,
            EmployeeGroupId = groupId,
            EmployeeSubGroupId = subGroupId,
            Amount = 100
        });
        Context.SaveChanges();

        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditTravelDetailDto
        {
            TravelId = travelId,
            DepartureId = departureId,
            ArrivalId = arrivalId,
            DepartureDate = new DateTime(2024, 1, 1),
            ArrivalDate = new DateTime(2024, 1, 4),
            TotalNights = 3
        });
        var detail = (await service.GetAll(new GetAllTravelDetailsInput { TravelId = travelId })).Items[0];

        var output = await service.CalculateExpectedPerDiemAsync(new CalculateExpectedPerDiemInput
        {
            TravelDetailId = detail.Id,
            EmployeeGroupId = groupId,
            EmployeeSubGroupId = subGroupId
        });

        output.PerDiemRate.ShouldBe(0);
        output.PerDiemAmount.ShouldBe(0);
        output.PerDiemPayAmount.ShouldBe(0);
    }
}
