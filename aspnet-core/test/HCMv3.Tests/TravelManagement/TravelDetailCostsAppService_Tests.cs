using System;
using System.Threading.Tasks;
using HCMv3.TravelManagement;
using HCMv3.TravelManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TravelManagement;

// ReSharper disable once InconsistentNaming
public class TravelDetailCostsAppService_Tests : TravelManagementAppServiceTestBase
{
    private TravelDetailCostsAppService CreateAppService()
    {
        // The leg and travel repositories are what scope a cost line to a trip the caller can see -- see
        // TravelDetailCostsAppService.GetAll.
        return CreateService(() => new TravelDetailCostsAppService(
            FakeRepository.For<TravelDetailCost>(Context),
            FakeRepository.For<TravelDetail>(Context),
            FakeRepository.For<Travel>(Context),
            TravelCostRollupTestFactory.Create(Context)));
    }

    private int CreateTravelDetail()
    {
        var employeeId = CreateEmployee();
        var travelTypeId = CreateTravelType();
        var travelId = CreateTravel(employeeId, travelTypeId);
        var departureId = CreateTravelDestination("Riyadh");
        var arrivalId = CreateTravelDestination("Jeddah");

        var detail = new TravelDetail
        {
            TenantId = AbpSession.TenantId ?? 1,
            TravelId = travelId,
            DepartureId = departureId,
            ArrivalId = arrivalId,
            DepartureDate = new DateTime(2024, 3, 1),
            ArrivalDate = new DateTime(2024, 3, 5)
        };
        Context.TravelDetails.Add(detail);
        Context.SaveChanges();
        return detail.Id;
    }

    private int CreateExpenseType()
    {
        var entity = new ExpenseType { TenantId = AbpSession.TenantId ?? 1, NameEn = "Meals", NameAr = "Meals" };
        Context.ExpenseTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_TravelDetailCost()
    {
        var detailId = CreateTravelDetail();
        var expenseTypeId = CreateExpenseType();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTravelDetailCostDto
        {
            TravelDetailId = detailId,
            ExpenseTypeId = expenseTypeId,
            Amount = 75,
            Comments = "Dinner"
        });

        var result = await service.GetAll(new GetAllTravelDetailCostsInput { TravelDetailId = detailId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].Amount.ShouldBe(75);
        result.Items[0].ExpenseTypeNameEn.ShouldBe("Meals");
    }

    [Fact]
    public async Task Should_Update_TravelDetailCost()
    {
        var detailId = CreateTravelDetail();
        var expenseTypeId = CreateExpenseType();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTravelDetailCostDto
        {
            TravelDetailId = detailId,
            ExpenseTypeId = expenseTypeId,
            Amount = 75
        });
        var created = (await service.GetAll(new GetAllTravelDetailCostsInput { TravelDetailId = detailId })).Items[0];

        await service.CreateOrEdit(new CreateOrEditTravelDetailCostDto
        {
            Id = created.Id,
            TravelDetailId = detailId,
            ExpenseTypeId = expenseTypeId,
            Amount = 120
        });

        var edited = await service.GetTravelDetailCostForEdit(new Abp.Application.Services.Dto.EntityDto(created.Id));
        edited.TravelDetailCost.Amount.ShouldBe(120);
    }

    [Fact]
    public async Task Should_Delete_TravelDetailCost()
    {
        var detailId = CreateTravelDetail();
        var expenseTypeId = CreateExpenseType();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTravelDetailCostDto
        {
            TravelDetailId = detailId,
            ExpenseTypeId = expenseTypeId,
            Amount = 75
        });
        var created = (await service.GetAll(new GetAllTravelDetailCostsInput { TravelDetailId = detailId })).Items[0];

        await service.Delete(new Abp.Application.Services.Dto.EntityDto(created.Id));

        var result = await service.GetAll(new GetAllTravelDetailCostsInput { TravelDetailId = detailId });
        result.TotalCount.ShouldBe(0);
    }
}
