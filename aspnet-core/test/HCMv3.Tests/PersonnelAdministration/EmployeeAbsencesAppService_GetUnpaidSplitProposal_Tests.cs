using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// Legacy <c>EmployeeAbsenceController.CheckQuota</c> -> <c>SetAdditionalUnpaidLeave</c> (365-858): a leave that
/// asks for more than is available is cut back to the whole days available, and the rest is offered as
/// additional unpaid leave. Every day is a working day unless a test says otherwise.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeAbsencesAppService_GetUnpaidSplitProposal_Tests : EmployeeAbsencesAppServiceTestBase
{
    /// <summary>Monday.</summary>
    private static readonly DateTime Day = new(2024, 3, 4);

    private static GetEmployeeAbsencePreviewInput Ask(int employeeId, int typeId, DateTime start, DateTime end) => new()
    {
        EmployeeId = employeeId,
        AbsenceTypeId = typeId,
        StartDate = start,
        EndDate = end
    };

    private int AccrualType(string code, Action<AbsenceType> configure = null)
    {
        return CreateAbsenceType(code, t =>
        {
            t.HaveQuotaAccrual = true;
            configure?.Invoke(t);
        });
    }

    [Fact]
    public async Task Should_Cut_The_Leave_Back_To_The_Whole_Days_Of_Balance()
    {
        // Arrange: 3.5 days of balance, 7 days asked for.
        var employeeId = CreateEmployee("EUS01");
        var typeId = AccrualType("EUS01");
        CreateQuota(employeeId, typeId, accrued: 3.5);
        CreateAbsenceType("EUS1U", nameEn: "Unpaid Leave");

        // Act
        var result = await Service.GetUnpaidSplitProposal(Ask(employeeId, typeId, Day, Day.AddDays(6)));

        // Assert
        result.NeedsSplit.ShouldBeTrue();
        result.AvailableDays.ShouldBe(3.5);
        result.AppliedDays.ShouldBe(7);
        result.EndDate.ShouldBe(Day.AddDays(2));
        result.TotalLeaveDays.ShouldBe(3);
        result.CalendarDays.ShouldBe(3);
    }

    [Fact]
    public async Task Should_Propose_The_Rest_As_Unpaid_Leave_To_The_Original_End_Date()
    {
        // Arrange
        var employeeId = CreateEmployee("EUS02");
        var typeId = AccrualType("EUS02");
        CreateQuota(employeeId, typeId, accrued: 3.5);
        var unpaidId = CreateAbsenceType("EUS2U", nameEn: "Unpaid Leave");

        // Act
        var result = await Service.GetUnpaidSplitProposal(Ask(employeeId, typeId, Day, Day.AddDays(6)));

        // Assert
        result.ExtAbsenceTypeId.ShouldBe(unpaidId);
        result.ExtAbsenceTypeName.ShouldBe("Unpaid Leave");
        result.ExtStartDate.ShouldBe(Day.AddDays(3));
        result.ExtEndDate.ShouldBe(Day.AddDays(6));
        result.ExtTotalLeaveDays.ShouldBe(4);
        result.ExtCalendarDays.ShouldBe(4);
        result.Message.ShouldContain("Unpaid Leave");
    }

    [Fact]
    public async Task Should_Walk_The_Paid_Part_Past_A_Week_Off()
    {
        // Arrange: Friday off. Wednesday 06/03 + 3 countable days = Wed, Thu, Sat -> ends Saturday 09/03.
        var employeeId = CreateEmployee("EUS03", fridayOff: true);
        var typeId = AccrualType("EUS03");
        CreateQuota(employeeId, typeId, accrued: 3);
        CreateAbsenceType("EUS3U", nameEn: "Unpaid Leave");
        var start = new DateTime(2024, 3, 6);

        // Act
        var result = await Service.GetUnpaidSplitProposal(Ask(employeeId, typeId, start, new DateTime(2024, 3, 12)));

        // Assert
        result.EndDate.ShouldBe(new DateTime(2024, 3, 9));
        result.TotalLeaveDays.ShouldBe(3);
        result.CalendarDays.ShouldBe(4);
        result.ExtStartDate.ShouldBe(new DateTime(2024, 3, 10));
    }

    [Fact]
    public async Task Should_Not_Split_A_Leave_Within_The_Balance()
    {
        // Arrange
        var employeeId = CreateEmployee("EUS04");
        var typeId = AccrualType("EUS04");
        CreateQuota(employeeId, typeId, accrued: 10);
        CreateAbsenceType("EUS4U", nameEn: "Unpaid Leave");

        // Act
        var result = await Service.GetUnpaidSplitProposal(Ask(employeeId, typeId, Day, Day.AddDays(6)));

        // Assert
        result.NeedsSplit.ShouldBeFalse();
        result.AvailableDays.ShouldBe(10);
    }

    [Fact]
    public async Task Should_Count_The_Allowed_Negative_Balance_As_Available()
    {
        // Arrange: 3 days of balance plus a negative limit of 4 covers a 7-day leave.
        var employeeId = CreateEmployee("EUS05");
        var typeId = AccrualType("EUS05", t =>
        {
            t.EnableNegativeBalance = true;
            t.NegativeBalanceLimit = 4;
            t.TotalDaysAllowed = 30;
        });
        CreateQuota(employeeId, typeId, accrued: 3);
        CreateAbsenceType("EUS5U", nameEn: "Unpaid Leave");

        // Act
        var result = await Service.GetUnpaidSplitProposal(Ask(employeeId, typeId, Day, Day.AddDays(6)));

        // Assert
        result.AvailableDays.ShouldBeGreaterThan(3);
    }

    [Fact]
    public async Task Should_Cut_At_The_Max_Limit_When_Unpaid_Is_Created_Above_It()
    {
        // Arrange: plenty of balance, but at most 2 days per leave with the rest unpaid.
        var employeeId = CreateEmployee("EUS06");
        var typeId = AccrualType("EUS06", t =>
        {
            t.ApplyMaxLeave = true;
            t.MaxLimitInDays = 2;
            t.CreateUnpaidAboveMaxLimitDays = true;
        });
        CreateQuota(employeeId, typeId, accrued: 30);
        CreateAbsenceType("EUS6U", nameEn: "Unpaid Leave");

        // Act
        var result = await Service.GetUnpaidSplitProposal(Ask(employeeId, typeId, Day, Day.AddDays(4)));

        // Assert
        result.NeedsSplit.ShouldBeTrue();
        result.IsMaxLimitSplit.ShouldBeTrue();
        result.EndDate.ShouldBe(Day.AddDays(1));
        result.ExtStartDate.ShouldBe(Day.AddDays(2));
    }

    [Fact]
    public async Task Should_Propose_Sick_Leave_Unpaid_For_A_Sick_Leave_Over_Its_Allowance()
    {
        // Arrange: a sick type with 5 days a year on the salary profile, 7 days asked for.
        var employeeId = CreateEmployee("EUS07");
        var sickId = CreateAbsenceType("3", t => t.QuotaType = QuotaType.SickLeave);
        GiveSalaryProfile(employeeId, new[] { sickId }, allowanceTypeId: sickId, allowanceDays: 5);
        CreateAbsenceType("EUS7U", nameEn: "Unpaid Leave");
        var sickUnpaidId = CreateAbsenceType("EUS7S", nameEn: "Sick Leave Unpaid");

        // Act
        var result = await Service.GetUnpaidSplitProposal(Ask(employeeId, sickId, Day, Day.AddDays(6)));

        // Assert
        result.NeedsSplit.ShouldBeTrue();
        result.EndDate.ShouldBe(Day.AddDays(4));
        result.ExtAbsenceTypeId.ShouldBe(sickUnpaidId);
    }

    [Fact]
    public async Task Should_Fall_Back_To_The_Code_2_Type_When_No_Unpaid_Leave_Is_Named()
    {
        // Arrange
        var employeeId = CreateEmployee("EUS08");
        var typeId = AccrualType("EUS08");
        CreateQuota(employeeId, typeId, accrued: 2);
        var codeTwoId = CreateAbsenceType("2", nameEn: "Leave Without Pay");

        // Act
        var result = await Service.GetUnpaidSplitProposal(Ask(employeeId, typeId, Day, Day.AddDays(4)));

        // Assert
        result.ExtAbsenceTypeId.ShouldBe(codeTwoId);
    }

    [Fact]
    public async Task Should_Say_So_When_No_Unpaid_Type_Is_Set_Up()
    {
        // Arrange
        var employeeId = CreateEmployee("EUS09");
        var typeId = AccrualType("EUS09");
        CreateQuota(employeeId, typeId, accrued: 2);

        // Act
        var result = await Service.GetUnpaidSplitProposal(Ask(employeeId, typeId, Day, Day.AddDays(4)));

        // Assert
        result.NeedsSplit.ShouldBeTrue();
        result.UnpaidLeaveTypeMissing.ShouldBeTrue();
        result.ExtAbsenceTypeId.ShouldBeNull();
        result.Message.ShouldContain("no unpaid leave type is set up");
    }

    [Fact]
    public async Task Should_Not_Split_An_Hourly_Leave()
    {
        // Arrange
        var employeeId = CreateEmployee("EUS10");
        var typeId = AccrualType("EUS10", Partial);
        CreateQuota(employeeId, typeId, accrued: 0);
        CreateAbsenceType("EUS0U", nameEn: "Unpaid Leave");
        var input = Ask(employeeId, typeId, Day, Day);
        input.StartTime = Day.AddHours(9);
        input.EndTime = Day.AddHours(13);

        // Act
        var result = await Service.GetUnpaidSplitProposal(input);

        // Assert
        result.NeedsSplit.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Not_Split_A_Type_That_Is_Not_Quota_Controlled()
    {
        // Arrange
        var employeeId = CreateEmployee("EUS11");
        var typeId = CreateAbsenceType("EUS11");
        CreateAbsenceType("EUS1V", nameEn: "Unpaid Leave");

        // Act
        var result = await Service.GetUnpaidSplitProposal(Ask(employeeId, typeId, Day, Day.AddDays(30)));

        // Assert
        result.NeedsSplit.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Not_Split_When_Not_A_Whole_Day_Is_Left()
    {
        // Arrange: half a day of balance -- legacy offered nothing and the save refuses.
        var employeeId = CreateEmployee("EUS12");
        var typeId = AccrualType("EUS12");
        CreateQuota(employeeId, typeId, accrued: 0.5);
        CreateAbsenceType("EUS2V", nameEn: "Unpaid Leave");

        // Act
        var result = await Service.GetUnpaidSplitProposal(Ask(employeeId, typeId, Day, Day.AddDays(2)));

        // Assert
        result.NeedsSplit.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Credit_Back_The_Days_Of_The_Leave_Being_Edited()
    {
        // Arrange: 5 accrued, a 3-day leave already charged; extending it to 5 days still fits.
        var employeeId = CreateEmployee("EUS13");
        var typeId = AccrualType("EUS13");
        CreateQuota(employeeId, typeId, accrued: 5);
        CreateAbsenceType("EUS3V", nameEn: "Unpaid Leave");
        var leave = await CreateAndGetAsync(Leave(employeeId, typeId, Day, Day.AddDays(2)));
        var input = Ask(employeeId, typeId, Day, Day.AddDays(4));
        input.EmployeeAbsenceId = leave.Id;

        // Act
        var result = await Service.GetUnpaidSplitProposal(input);

        // Assert
        result.NeedsSplit.ShouldBeFalse();
        result.AvailableDays.ShouldBe(5);
    }

    [Fact]
    public async Task Proceeding_With_The_Proposal_Should_Save_A_Paid_Leave_And_An_Unpaid_Leave()
    {
        // Arrange
        var employeeId = CreateEmployee("EUS14");
        var typeId = AccrualType("EUS14");
        CreateQuota(employeeId, typeId, accrued: 3.5);
        var unpaidId = CreateAbsenceType("EUS4V", nameEn: "Unpaid Leave");
        var proposal = await Service.GetUnpaidSplitProposal(Ask(employeeId, typeId, Day, Day.AddDays(6)));
        var input = Leave(employeeId, typeId, Day, proposal.EndDate!.Value);
        input.ExtAbsenceTypeId = proposal.ExtAbsenceTypeId;
        input.ExtStartDate = proposal.ExtStartDate;
        input.ExtEndDate = proposal.ExtEndDate;

        // Act
        await Service.CreateOrEdit(input);

        // Assert
        var leaves = GetLeaves(employeeId);
        leaves.Single(a => a.AbsenceTypeId == typeId).TotalLeaveDays.ShouldBe(3);
        leaves.Single(a => a.AbsenceTypeId == unpaidId).TotalLeaveDays.ShouldBe(4);
        GetQuota(employeeId, typeId).QuotaUsed.ShouldBe(3);
    }
}
