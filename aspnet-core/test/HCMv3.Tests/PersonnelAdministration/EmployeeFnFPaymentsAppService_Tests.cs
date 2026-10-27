using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// Tests for <see cref="IEmployeeFnFPaymentsAppService"/>. Saving an F&amp;F now runs legacy's settlement side
/// effects, so every entry needs what legacy needs: an exit record, and balances to compensate against.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeFnFPaymentsAppService_Tests : FullAndFinalTestBase
{
    private static readonly DateTime LastDay = new DateTime(2024, 6, 30);

    private readonly IEmployeeFnFPaymentsAppService _appService;

    public EmployeeFnFPaymentsAppService_Tests()
    {
        _appService = Resolve<IEmployeeFnFPaymentsAppService>();
    }

    private int SeedLeaverWithBalances()
    {
        var seeded = SeedLeaver(new DateTime(2020, 7, 1), LastDay, SeparationType.Termination);
        SeedGratuityBalance(seeded.EmployeeId, 60);
        SeedLeaveBalance(seeded.EmployeeId, 30);
        return seeded.EmployeeId;
    }

    [Fact]
    public async Task Should_Create_FnFPayment_And_Write_The_Gratuity_Row()
    {
        //Arrange
        var employeeId = SeedLeaverWithBalances();

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeFnFPaymentDto
        {
            EmployeeId = employeeId,
            CompensationDate = LastDay,
            LeaveCompensateDays = 10,
            GratuityCompensateDays = 45,
            IndemnityCompensateAmount = 2000
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeFnFPaymentsInput { EmployeeId = employeeId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].IndemnityCompensateAmount.ShouldBe(2000);

        UsingDbContext(context =>
            context.Set<EmployeeGratuity>().Single(g => g.EmployeeId == employeeId && !g.IsDeleted).GratuityPaidDays.ShouldBe(45));
    }

    [Fact]
    public async Task Should_Reject_Create_Without_An_Exit_Record()
    {
        //Arrange
        var seeded = SeedLeaver(new DateTime(2020, 7, 1), LastDay, SeparationType.Termination);
        UsingDbContext(context => context.Set<EmployeeSeparation>().Find(seeded.SeparationId).RevokeDate = LastDay);

        //Act
        var ex = await Should.ThrowAsync<UserFriendlyException>(() => _appService.CreateOrEdit(new CreateOrEditEmployeeFnFPaymentDto
        {
            EmployeeId = seeded.EmployeeId,
            CompensationDate = LastDay
        }));

        //Assert
        ex.Message.ShouldStartWith("Exit entry not found");
        UsingDbContext(context => context.Set<EmployeeFnFPayment>().Any(f => f.EmployeeId == seeded.EmployeeId && !f.IsDeleted).ShouldBeFalse());
    }

    [Fact]
    public async Task Should_Update_FnFPayment_And_Replace_The_Gratuity_Row()
    {
        //Arrange
        // Another leaver first, so this F&F's id differs from its exit's id -- see the pinned legacy quirk
        // FullAndFinalSettlementService_Tests.Should_Reject_An_Edit_Whose_Id_Matches_The_Exit_Id_As_Legacy_Does.
        SeedLeaver(new DateTime(2020, 7, 1), LastDay, SeparationType.Termination);
        var employeeId = SeedLeaverWithBalances();

        await _appService.CreateOrEdit(new CreateOrEditEmployeeFnFPaymentDto
        {
            EmployeeId = employeeId,
            CompensationDate = LastDay,
            LeaveCompensateDays = 10,
            GratuityCompensateDays = 45
        });

        var created = await _appService.GetAll(new GetAllEmployeeFnFPaymentsInput { EmployeeId = employeeId });
        var id = created.Items[0].Id;

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeFnFPaymentDto
        {
            Id = id,
            EmployeeId = employeeId,
            CompensationDate = LastDay,
            LeaveCompensateDays = 20,
            GratuityCompensateDays = 50
        });

        //Assert
        var updated = await _appService.GetEmployeeFnFPaymentForEdit(new EntityDto(id));
        updated.EmployeeFnFPayment.LeaveCompensateDays.ShouldBe(20);

        UsingDbContext(context =>
        {
            var rows = context.Set<EmployeeGratuity>().Where(g => g.EmployeeId == employeeId && !g.IsDeleted).ToList();
            rows.Count.ShouldBe(1);
            rows[0].GratuityPaidDays.ShouldBe(50);
        });
    }

    [Fact]
    public async Task Should_Delete_FnFPayment_And_Undo_Its_Records()
    {
        //Arrange
        var employeeId = SeedLeaverWithBalances();
        await _appService.CreateOrEdit(new CreateOrEditEmployeeFnFPaymentDto
        {
            EmployeeId = employeeId,
            CompensationDate = LastDay,
            GratuityCompensateDays = 45
        });
        var id = (await _appService.GetAll(new GetAllEmployeeFnFPaymentsInput { EmployeeId = employeeId })).Items[0].Id;

        //Act
        await _appService.Delete(new EntityDto(id));

        //Assert
        UsingDbContext(context =>
        {
            context.Set<EmployeeFnFPayment>().Any(f => f.Id == id && !f.IsDeleted).ShouldBeFalse();
            context.Set<EmployeeGratuity>().Any(g => g.EmployeeId == employeeId && !g.IsDeleted).ShouldBeFalse();
            context.Set<Employee>().Find(employeeId).Status.ShouldBe(EmployeeStatus.Active);
        });
    }

    [Fact]
    public async Task Should_Return_Balances_And_The_Suggested_Indemnity()
    {
        //Arrange
        var seeded = SeedLeaver(new DateTime(2020, 7, 1), LastDay, SeparationType.Resignation);
        SeedGratuityBalance(seeded.EmployeeId, 60);

        //Act
        var balances = await _appService.GetFullAndFinalBalances(new EntityDto(seeded.EmployeeId));

        //Assert
        balances.GratuityAccrualDays.ShouldBe(60);
        balances.GratuityRemainingDays.ShouldBe(60);
        balances.SuggestedIndemnityDays.ShouldBe(30);
    }
}
