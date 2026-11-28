using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Timing;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// The save checks of legacy <c>EmployeeAbsence.PerformValidations</c> that HR's leave screen now applies:
/// earliest evaluation date (1605-1614), return date (1622-1634, 1753-1757), leave type exceptions (1957-1992)
/// and the hourly-leave time overlap (1815-1833).
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeAbsencesAppService_Validation_Tests : EmployeeAbsencesAppServiceTestBase
{
    /// <summary>Monday.</summary>
    private static readonly DateTime Day = new(2024, 3, 4);

    #region Earliest evaluation date

    [Fact]
    public async Task Should_Refuse_A_Leave_Straddling_The_Earliest_Evaluation_Date()
    {
        // Arrange
        SetTimeSettings(s => s.EarliestEvaluationDate = Day.AddDays(1));
        var employeeId = CreateEmployee("EVE01");
        var typeId = CreateAbsenceType("EVE01");

        // Act
        var exception = await Should.ThrowAsync<UserFriendlyException>(() =>
            Service.CreateOrEdit(Leave(employeeId, typeId, Day, Day.AddDays(2))));

        // Assert
        exception.Message.ShouldContain("earliest evaluation date");
    }

    [Fact]
    public async Task Should_Accept_A_Leave_Starting_On_The_Earliest_Evaluation_Date()
    {
        // Arrange
        SetTimeSettings(s => s.EarliestEvaluationDate = Day);
        var employeeId = CreateEmployee("EVE02");
        var typeId = CreateAbsenceType("EVE02");

        // Act
        await Service.CreateOrEdit(Leave(employeeId, typeId, Day, Day.AddDays(2)));

        // Assert
        GetLeaves(employeeId).Count.ShouldBe(1);
    }

    #endregion

    #region Return date

    [Fact]
    public async Task Should_Refuse_A_Future_Return_Date_Unless_The_Settings_Allow_It()
    {
        // Arrange
        var today = Clock.Now.Date;
        var employeeId = CreateEmployee("EVR01");
        var typeId = CreateAbsenceType("EVR01");
        var input = Leave(employeeId, typeId, today.AddDays(5), today.AddDays(6));
        input.ReturnDate = today.AddDays(7);

        // Act
        var exception = await Should.ThrowAsync<UserFriendlyException>(() => Service.CreateOrEdit(input));

        // Assert
        exception.Message.ShouldContain("cannot be in the future");
    }

    [Fact]
    public async Task Should_Accept_A_Future_Return_Date_When_The_Settings_Allow_It()
    {
        // Arrange
        SetTimeSettings(s => s.AllowFutureDatedReturnFromLeave = true);
        var today = Clock.Now.Date;
        var employeeId = CreateEmployee("EVR02");
        var typeId = CreateAbsenceType("EVR02");
        var input = Leave(employeeId, typeId, today.AddDays(5), today.AddDays(6));
        input.ReturnDate = today.AddDays(7);

        // Act
        await Service.CreateOrEdit(input);

        // Assert
        GetLeaves(employeeId).Single().IsReturned.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Refuse_A_Return_Date_On_The_Start_Date()
    {
        // Arrange
        var employeeId = CreateEmployee("EVR03");
        var typeId = CreateAbsenceType("EVR03");
        var input = Leave(employeeId, typeId, Day, Day.AddDays(2));
        input.ReturnDate = Day;

        // Act
        var exception = await Should.ThrowAsync<UserFriendlyException>(() => Service.CreateOrEdit(input));

        // Assert
        exception.Message.ShouldContain("must be after the leave start date");
    }

    [Fact]
    public async Task Should_Refuse_An_Early_Return_For_A_Type_That_Must_Be_Taken_In_Full()
    {
        // Arrange
        var employeeId = CreateEmployee("EVR04");
        var typeId = CreateAbsenceType("EVR04", t => t.AllowToReturnEarly = false);
        var input = Leave(employeeId, typeId, Day, Day.AddDays(4));
        input.ReturnDate = Day.AddDays(2);

        // Act
        var exception = await Should.ThrowAsync<UserFriendlyException>(() => Service.CreateOrEdit(input));

        // Assert
        exception.Message.ShouldContain("must be taken in full");
    }

    [Fact]
    public async Task Should_End_The_Leave_The_Day_Before_An_Early_Return()
    {
        // Arrange
        var employeeId = CreateEmployee("EVR05");
        var typeId = CreateAbsenceType("EVR05");
        var input = Leave(employeeId, typeId, Day, Day.AddDays(4));
        input.ReturnDate = Day.AddDays(2);

        // Act
        await Service.CreateOrEdit(input);

        // Assert
        var leave = GetLeaves(employeeId).Single();
        leave.EndDate.ShouldBe(Day.AddDays(1));
        leave.InitialEndDate.ShouldBe(Day.AddDays(4));
        leave.TotalLeaveDays.ShouldBe(2);
        leave.IsReturned.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Refuse_Rejoining_On_A_Week_Off()
    {
        // Arrange: Friday 08/03 is the weekly off; the leave ends Thursday 07/03.
        var employeeId = CreateEmployee("EVR06", fridayOff: true);
        var typeId = CreateAbsenceType("EVR06");
        var input = Leave(employeeId, typeId, Day, Day.AddDays(3));
        input.ReturnDate = Day.AddDays(4);

        // Act
        var exception = await Should.ThrowAsync<UserFriendlyException>(() => Service.CreateOrEdit(input));

        // Assert
        exception.Message.ShouldContain("must rejoin on a regular day");
    }

    [Fact]
    public async Task Should_Accept_Rejoining_On_The_First_Working_Day_After_A_Week_Off()
    {
        // Arrange: back on Saturday 09/03, Friday being off -- on time.
        var employeeId = CreateEmployee("EVR07", fridayOff: true);
        var typeId = CreateAbsenceType("EVR07");
        var input = Leave(employeeId, typeId, Day, Day.AddDays(3));
        input.ReturnDate = Day.AddDays(5);

        // Act
        await Service.CreateOrEdit(input);

        // Assert
        GetLeaves(employeeId).Single().IsReturned.ShouldBeTrue();
    }

    #endregion

    #region Leave type exceptions (EA 1957-1992)

    private void AddException(int absenceTypeId, int exceptAbsenceTypeId)
    {
        UsingDbContext(context => context.Set<AbsenceTypeException>().Add(new AbsenceTypeException
        {
            TenantId = TenantId,
            AbsenceTypeId = absenceTypeId,
            ExceptAbsenceTypeId = exceptAbsenceTypeId
        }));
    }

    [Fact]
    public async Task Should_Refuse_A_Leave_Following_A_Leave_Of_An_Exception_Type()
    {
        // Arrange
        var employeeId = CreateEmployee("EVX01");
        var annual = CreateAbsenceType("EVX1A");
        var sick = CreateAbsenceType("EVX1S");
        AddException(annual, sick);
        await Service.CreateOrEdit(Leave(employeeId, sick, Day, Day.AddDays(1)));

        // Act
        var exception = await Should.ThrowAsync<UserFriendlyException>(() =>
            Service.CreateOrEdit(Leave(employeeId, annual, Day.AddDays(2), Day.AddDays(3))));

        // Assert
        exception.Message.ShouldContain("may not follow or precede");
    }

    [Fact]
    public async Task Should_Refuse_An_Exception_Listed_The_Other_Way_Round()
    {
        // Arrange: the exception is recorded on the sick type, the new leave is the annual one before it.
        var employeeId = CreateEmployee("EVX02");
        var annual = CreateAbsenceType("EVX2A");
        var sick = CreateAbsenceType("EVX2S");
        AddException(sick, annual);
        await Service.CreateOrEdit(Leave(employeeId, sick, Day.AddDays(2), Day.AddDays(3)));

        // Act
        var exception = await Should.ThrowAsync<UserFriendlyException>(() =>
            Service.CreateOrEdit(Leave(employeeId, annual, Day, Day.AddDays(1))));

        // Assert
        exception.Message.ShouldContain("may not follow or precede");
    }

    [Fact]
    public async Task Should_Accept_An_Exception_Type_With_A_Day_In_Between()
    {
        // Arrange
        var employeeId = CreateEmployee("EVX03");
        var annual = CreateAbsenceType("EVX3A");
        var sick = CreateAbsenceType("EVX3S");
        AddException(annual, sick);
        await Service.CreateOrEdit(Leave(employeeId, sick, Day, Day.AddDays(1)));

        // Act
        await Service.CreateOrEdit(Leave(employeeId, annual, Day.AddDays(3), Day.AddDays(4)));

        // Assert
        GetLeaves(employeeId).Count.ShouldBe(2);
    }

    #endregion

    #region Overlap

    private CreateOrEditEmployeeAbsenceDto Hourly(int employeeId, int typeId, int fromHour, int toHour)
    {
        var input = Leave(employeeId, typeId, Day, Day);
        input.StartTime = Day.AddHours(fromHour);
        input.EndTime = Day.AddHours(toHour);
        return input;
    }

    [Fact]
    public async Task Should_Accept_Two_Hourly_Leaves_On_The_Same_Day_That_Do_Not_Overlap()
    {
        // Arrange
        var employeeId = CreateEmployee("EVO01");
        var typeId = CreateAbsenceType("EVO01", Partial);
        await Service.CreateOrEdit(Hourly(employeeId, typeId, 8, 10));

        // Act
        await Service.CreateOrEdit(Hourly(employeeId, typeId, 10, 12));

        // Assert
        GetLeaves(employeeId).Count.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Refuse_Two_Hourly_Leaves_Whose_Times_Overlap()
    {
        // Arrange
        var employeeId = CreateEmployee("EVO02");
        var typeId = CreateAbsenceType("EVO02", Partial);
        await Service.CreateOrEdit(Hourly(employeeId, typeId, 8, 11));

        // Act
        var exception = await Should.ThrowAsync<UserFriendlyException>(() =>
            Service.CreateOrEdit(Hourly(employeeId, typeId, 10, 12)));

        // Assert
        exception.Message.ShouldContain("overlaps these times");
    }

    [Fact]
    public async Task Should_Refuse_An_Hourly_Leave_On_A_Day_Of_Full_Day_Leave()
    {
        // Arrange
        var employeeId = CreateEmployee("EVO03");
        var typeId = CreateAbsenceType("EVO03", Partial);
        await Service.CreateOrEdit(Leave(employeeId, typeId, Day, Day.AddDays(1)));

        // Act
        var exception = await Should.ThrowAsync<UserFriendlyException>(() =>
            Service.CreateOrEdit(Hourly(employeeId, typeId, 9, 10)));

        // Assert
        exception.Message.ShouldContain("overlap");
    }

    #endregion
}
