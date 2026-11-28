using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Castle.MicroKernel.Registration;
using HCMv3.PayrollManagement.Services;
using HCMv3.SystemSettings;
using NSubstitute;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// Legacy <c>UpdateQuotaInformation</c> (EA 2336-2339, 2413-2416) and <c>OnDeleting</c> (1340-1343): with the monthly
/// forfeit rule, saving or deleting an accrual-type leave works the monthly forfeit out again. The forfeit
/// calculation itself is <see cref="IAccrualService.CalculateForfeitForLeaveAsync"/>'s and is tested there; these
/// tests only pin when the leave screen asks for it.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeAbsencesAppService_Forfeit_Tests : EmployeeAbsencesAppServiceTestBase
{
    /// <summary>Monday.</summary>
    private static readonly DateTime Day = new(2024, 3, 4);

    private readonly IAccrualService _accrual = Substitute.For<IAccrualService>();

    public EmployeeAbsencesAppService_Forfeit_Tests()
    {
        LocalIocManager.IocContainer.Register(Component.For<IAccrualService>().Instance(_accrual).IsDefault());
    }

    private (int EmployeeId, int TypeId) AccrualLeaveSetup(string code, bool haveQuotaAccrual = true)
    {
        var employeeId = CreateEmployee(code);
        var typeId = CreateAbsenceType(code, t => t.HaveQuotaAccrual = haveQuotaAccrual);
        if (haveQuotaAccrual)
        {
            CreateQuota(employeeId, typeId, accrued: 30);
        }

        return (employeeId, typeId);
    }

    [Fact]
    public async Task Should_Recalculate_The_Monthly_Forfeit_After_Saving_An_Accrual_Leave()
    {
        // Arrange
        SetTimeSettings(s => s.ForfeitRule = ForfeitRule.Monthly);
        var (employeeId, typeId) = AccrualLeaveSetup("EFF01");

        // Act
        await Service.CreateOrEdit(Leave(employeeId, typeId, Day, Day.AddDays(2)));

        // Assert
        await _accrual.Received(1).CalculateForfeitForLeaveAsync(employeeId, Arg.Any<int?>(), Day, Day.AddDays(2), Arg.Is<int?>(x => x == null));
    }

    [Fact]
    public async Task Should_Not_Recalculate_The_Forfeit_Unless_The_Rule_Is_Monthly()
    {
        // Arrange
        SetTimeSettings(s => s.ForfeitRule = ForfeitRule.Yearly);
        var (employeeId, typeId) = AccrualLeaveSetup("EFF02");

        // Act
        await Service.CreateOrEdit(Leave(employeeId, typeId, Day, Day.AddDays(2)));

        // Assert
        await _accrual.DidNotReceiveWithAnyArgs().CalculateForfeitForLeaveAsync(default, default, default, default, default);
    }

    [Fact]
    public async Task Should_Not_Recalculate_The_Forfeit_For_A_Type_Without_Accrual()
    {
        // Arrange
        SetTimeSettings(s => s.ForfeitRule = ForfeitRule.Monthly);
        var (employeeId, typeId) = AccrualLeaveSetup("EFF03", haveQuotaAccrual: false);

        // Act
        await Service.CreateOrEdit(Leave(employeeId, typeId, Day, Day.AddDays(2)));

        // Assert
        await _accrual.DidNotReceiveWithAnyArgs().CalculateForfeitForLeaveAsync(default, default, default, default, default);
    }

    [Fact]
    public async Task Should_Recalculate_The_Forfeit_Over_Both_The_Old_And_New_Dates_When_Edited()
    {
        // Arrange
        SetTimeSettings(s => s.ForfeitRule = ForfeitRule.Monthly);
        var (employeeId, typeId) = AccrualLeaveSetup("EFF04");
        var leave = await CreateAndGetAsync(Leave(employeeId, typeId, Day, Day.AddDays(2)));
        var edit = await EditOf(leave.Id);
        edit.StartDate = Day.AddDays(10);
        edit.EndDate = Day.AddDays(11);
        _accrual.ClearReceivedCalls();

        // Act
        await Service.CreateOrEdit(edit);

        // Assert
        await _accrual.Received(1).CalculateForfeitForLeaveAsync(employeeId, Arg.Any<int?>(), Day, Day.AddDays(11), Arg.Is<int?>(x => x == null));
    }

    [Fact]
    public async Task Should_Recalculate_The_Monthly_Forfeit_After_Deleting_An_Accrual_Leave()
    {
        // Arrange
        SetTimeSettings(s => s.ForfeitRule = ForfeitRule.Monthly);
        var (employeeId, typeId) = AccrualLeaveSetup("EFF05");
        var leave = await CreateAndGetAsync(Leave(employeeId, typeId, Day, Day.AddDays(2)));
        _accrual.ClearReceivedCalls();

        // Act
        await Service.Delete(new EntityDto(leave.Id));

        // Assert
        await _accrual.Received(1).CalculateForfeitForLeaveAsync(employeeId, Arg.Any<int?>(), Day, Day.AddDays(2), Arg.Is<int?>(x => x == null));
    }
}
