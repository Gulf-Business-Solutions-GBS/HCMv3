using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Timing;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.Storage;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// What HR's save works out itself rather than taking from the form (legacy <c>EmployeeAbsence</c>'s derived
/// fields), the additional unpaid leave row (legacy <c>SaveAdditionalUnpaidLeave</c>, EA 4612-4640) and the
/// fractional settlement (legacy <c>SaveFractionAdjustment</c>, EA 4688-4747).
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeAbsencesAppService_Save_Tests : EmployeeAbsencesAppServiceTestBase
{
    /// <summary>Monday.</summary>
    private static readonly DateTime Day = new(2024, 3, 4);

    #region Derived fields

    [Fact]
    public async Task Should_Store_AbsenceHours_For_An_Hourly_Leave()
    {
        // Arrange: 08:00-16:00 duty (480 minutes); 09:00-13:00 is 4 hours.
        var employeeId = CreateEmployee("ESV01");
        var typeId = CreateAbsenceType("ESV01", Partial);
        var input = Leave(employeeId, typeId, Day, Day);
        input.StartTime = Day.AddHours(9);
        input.EndTime = Day.AddHours(13);

        // Act
        var leave = await CreateAndGetAsync(input);

        // Assert
        leave.AbsenceHours.ShouldBe(4);
    }

    [Fact]
    public async Task Should_Charge_An_Hourly_Leave_As_A_Fraction_Of_A_Day()
    {
        // Arrange: 240 of 480 planned minutes.
        var employeeId = CreateEmployee("ESV02");
        var typeId = CreateAbsenceType("ESV02", Partial);
        var input = Leave(employeeId, typeId, Day, Day);
        input.StartTime = Day.AddHours(9);
        input.EndTime = Day.AddHours(13);

        // Act
        var leave = await CreateAndGetAsync(input);

        // Assert
        leave.TotalLeaveDays.ShouldBe(0.5);
    }

    [Fact]
    public async Task Should_Clear_Times_For_A_Type_That_Does_Not_Allow_Partial_Leave()
    {
        // Arrange
        var employeeId = CreateEmployee("ESV03");
        var typeId = CreateAbsenceType("ESV03");
        var input = Leave(employeeId, typeId, Day, Day);
        input.StartTime = Day.AddHours(9);
        input.EndTime = Day.AddHours(13);

        // Act
        var leave = await CreateAndGetAsync(input);

        // Assert
        leave.StartTime.ShouldBe(default);
        leave.EndTime.ShouldBe(default);
        leave.AbsenceHours.ShouldBe(0);
        leave.TotalLeaveDays.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Clear_Times_For_A_Full_Day_Only_Type()
    {
        // Arrange
        var employeeId = CreateEmployee("ESV04");
        var typeId = CreateAbsenceType("ESV04", t =>
        {
            Partial(t);
            t.LeaveDurationType = LeaveDurationType.FullDay;
        });
        var input = Leave(employeeId, typeId, Day, Day);
        input.StartTime = Day.AddHours(9);
        input.EndTime = Day.AddHours(13);

        // Act
        var leave = await CreateAndGetAsync(input);

        // Assert
        leave.AbsenceHours.ShouldBe(0);
        leave.StartTime.ShouldBe(default);
    }

    [Fact]
    public async Task Should_Take_Half_Day_Times_From_The_Duty()
    {
        // Arrange: 08:00-16:00, first half is 08:00-12:00.
        SetTimeSettings(s => s.EnableAutoHalfDayLeave = true);
        var employeeId = CreateEmployee("ESV05");
        var typeId = CreateAbsenceType("ESV05", Partial);
        var input = Leave(employeeId, typeId, Day, Day);
        input.HalfdayType = HalfdayType.StartOfDuty;

        // Act
        var leave = await CreateAndGetAsync(input);

        // Assert
        leave.StartTime.ShouldBe(Day.AddHours(8));
        leave.EndTime.ShouldBe(Day.AddHours(12));
        leave.TotalLeaveDays.ShouldBe(0.5);
    }

    [Fact]
    public async Task Should_Ignore_IsReturned_Sent_By_The_Form()
    {
        // Arrange
        var employeeId = CreateEmployee("ESV06");
        var typeId = CreateAbsenceType("ESV06");
        var input = Leave(employeeId, typeId, Day, Day.AddDays(2));
        input.IsReturned = true;

        // Act
        var leave = await CreateAndGetAsync(input);

        // Assert
        leave.IsReturned.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Mark_Returned_On_An_On_Time_Return()
    {
        // Arrange
        var employeeId = CreateEmployee("ESV07");
        var typeId = CreateAbsenceType("ESV07");
        var input = Leave(employeeId, typeId, Day, Day.AddDays(2));
        input.ReturnDate = Day.AddDays(3);

        // Act
        var leave = await CreateAndGetAsync(input);

        // Assert
        leave.IsReturned.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Reset_IsReturned_When_The_ReturnDate_Is_Cleared()
    {
        // Arrange
        var employeeId = CreateEmployee("ESV08");
        var typeId = CreateAbsenceType("ESV08");
        var input = Leave(employeeId, typeId, Day, Day.AddDays(2));
        input.ReturnDate = Day.AddDays(3);
        var leave = await CreateAndGetAsync(input);
        var edit = await EditOf(leave.Id);
        edit.ReturnDate = default;

        // Act
        await Service.CreateOrEdit(edit);

        // Assert
        GetLeaves(employeeId).Single().IsReturned.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Save_ExtTotalLeaveDays_Less_The_Main_Types_Exclusions()
    {
        // Arrange: Friday off; ext Sunday 10/03 - Saturday 16/03 is 7 calendar days, 6 leave days.
        var employeeId = CreateEmployee("ESV09", fridayOff: true);
        var typeId = CreateAbsenceType("ESV09");
        var unpaidId = CreateAbsenceType("ESV9U", nameEn: "Unpaid Leave");
        var input = Leave(employeeId, typeId, Day, Day.AddDays(2));
        input.ExtAbsenceTypeId = unpaidId;
        input.ExtStartDate = new DateTime(2024, 3, 10);
        input.ExtEndDate = new DateTime(2024, 3, 16);

        // Act
        var leave = await CreateAndGetAsync(input);

        // Assert
        leave.ExtTotalLeaveDays.ShouldBe(6);
    }

    [Fact]
    public async Task Should_Fill_An_Empty_PaymentDate_From_The_Payroll_Settings()
    {
        // Arrange: starts on the 4th, so the 1st.
        SetPayrollSettings(s => s.LeaveAdvPaymentdateBefore15 = true);
        var employeeId = CreateEmployee("ESV10");
        var typeId = CreateAbsenceType("ESV10");

        // Act
        var leave = await CreateAndGetAsync(Leave(employeeId, typeId, Day, Day.AddDays(1)));

        // Assert
        leave.PaymentDate.ShouldBe(new DateTime(2024, 3, 1));
    }

    [Fact]
    public async Task Should_Attach_Files_Staged_On_The_Form()
    {
        // Arrange
        var employeeId = CreateEmployee("ESV11");
        var typeId = CreateAbsenceType("ESV11");
        var token = Guid.NewGuid().ToString("N");
        Resolve<ITempFileCacheManager>().SetFile(token, new TempFileInfo("note.pdf", "application/pdf", new byte[] { 1, 2, 3 }));
        var input = Leave(employeeId, typeId, Day, Day);
        input.AttachmentFileTokens = new List<string> { token };

        // Act
        var leave = await CreateAndGetAsync(input);

        // Assert
        var attached = UsingDbContext(context => context.Set<AppAttachment>()
            .Count(a => a.EntityType == "EmployeeAbsence" && a.EntityId == leave.Id.ToString()));
        attached.ShouldBe(1);
    }

    #endregion

    #region Additional unpaid leave (EA 1940-1953, 4612-4640)

    private CreateOrEditEmployeeAbsenceDto LeaveWithExtra(int employeeId, int typeId, int unpaidId)
    {
        var input = Leave(employeeId, typeId, Day, Day.AddDays(2));
        input.ExtAbsenceTypeId = unpaidId;
        input.ExtStartDate = Day.AddDays(3);
        input.ExtEndDate = Day.AddDays(4);
        return input;
    }

    [Fact]
    public async Task Should_Book_The_Additional_Unpaid_Leave_As_Its_Own_Row()
    {
        // Arrange
        var employeeId = CreateEmployee("ESX01");
        var typeId = CreateAbsenceType("ESX01");
        var unpaidId = CreateAbsenceType("ESX1U", nameEn: "Unpaid Leave");

        // Act
        await Service.CreateOrEdit(LeaveWithExtra(employeeId, typeId, unpaidId));

        // Assert
        var unpaid = GetLeaves(employeeId).Single(a => a.AbsenceTypeId == unpaidId);
        unpaid.StartDate.ShouldBe(Day.AddDays(3));
        unpaid.EndDate.ShouldBe(Day.AddDays(4));
        unpaid.TotalLeaveDays.ShouldBe(2);
        unpaid.LeavePaymentType.ShouldBe(LeavePaymentType.PayWithPayroll);
    }

    [Fact]
    public async Task Should_Not_Book_The_Additional_Unpaid_Leave_Twice_When_The_Leave_Is_Saved_Again()
    {
        // Arrange
        var employeeId = CreateEmployee("ESX02");
        var typeId = CreateAbsenceType("ESX02");
        var unpaidId = CreateAbsenceType("ESX2U", nameEn: "Unpaid Leave");
        var leave = await CreateAndGetAsync(LeaveWithExtra(employeeId, typeId, unpaidId));
        var edit = await EditOf(leave.Id);
        edit.Comments = "re-saved";

        // Act
        await Service.CreateOrEdit(edit);

        // Assert
        GetLeaves(employeeId).Count(a => a.AbsenceTypeId == unpaidId).ShouldBe(1);
    }

    [Fact]
    public async Task Should_Move_The_Additional_Unpaid_Leave_When_Its_Dates_Change()
    {
        // Arrange
        var employeeId = CreateEmployee("ESX03");
        var typeId = CreateAbsenceType("ESX03");
        var unpaidId = CreateAbsenceType("ESX3U", nameEn: "Unpaid Leave");
        var leave = await CreateAndGetAsync(LeaveWithExtra(employeeId, typeId, unpaidId));
        var edit = await EditOf(leave.Id);
        edit.ExtEndDate = Day.AddDays(6);

        // Act
        await Service.CreateOrEdit(edit);

        // Assert
        var unpaid = GetLeaves(employeeId).Single(a => a.AbsenceTypeId == unpaidId);
        unpaid.EndDate.ShouldBe(Day.AddDays(6));
        unpaid.TotalLeaveDays.ShouldBe(4);
    }

    [Fact]
    public async Task Should_Remove_The_Additional_Unpaid_Leave_When_It_Is_Cleared()
    {
        // Arrange
        var employeeId = CreateEmployee("ESX04");
        var typeId = CreateAbsenceType("ESX04");
        var unpaidId = CreateAbsenceType("ESX4U", nameEn: "Unpaid Leave");
        var leave = await CreateAndGetAsync(LeaveWithExtra(employeeId, typeId, unpaidId));
        var edit = await EditOf(leave.Id);
        edit.ExtAbsenceTypeId = null;
        edit.ExtStartDate = null;
        edit.ExtEndDate = null;

        // Act
        await Service.CreateOrEdit(edit);

        // Assert
        GetLeaves(employeeId).ShouldNotContain(a => a.AbsenceTypeId == unpaidId);
    }

    [Fact]
    public async Task Should_Charge_The_Unpaid_Types_Quota_When_It_Has_One()
    {
        // Arrange
        var employeeId = CreateEmployee("ESX05");
        var typeId = CreateAbsenceType("ESX05");
        var unpaidId = CreateAbsenceType("ESX5U", nameEn: "Unpaid Leave");
        CreateQuota(employeeId, unpaidId, accrued: 30);

        // Act
        await Service.CreateOrEdit(LeaveWithExtra(employeeId, typeId, unpaidId));

        // Assert
        GetQuota(employeeId, unpaidId).QuotaUsed.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Refuse_An_Additional_Unpaid_Leave_Without_Dates()
    {
        // Arrange
        var employeeId = CreateEmployee("ESX06");
        var typeId = CreateAbsenceType("ESX06");
        var unpaidId = CreateAbsenceType("ESX6U", nameEn: "Unpaid Leave");
        var input = LeaveWithExtra(employeeId, typeId, unpaidId);
        input.ExtEndDate = null;

        // Act
        var exception = await Should.ThrowAsync<UserFriendlyException>(() => Service.CreateOrEdit(input));

        // Assert
        exception.Message.ShouldContain("start and the end date of the additional unpaid leave");
    }

    [Fact]
    public async Task Should_Refuse_An_Additional_Unpaid_Leave_Ending_Before_It_Starts()
    {
        // Arrange
        var employeeId = CreateEmployee("ESX07");
        var typeId = CreateAbsenceType("ESX07");
        var unpaidId = CreateAbsenceType("ESX7U", nameEn: "Unpaid Leave");
        var input = LeaveWithExtra(employeeId, typeId, unpaidId);
        input.ExtStartDate = Day.AddDays(6);
        input.ExtEndDate = Day.AddDays(5);

        // Act
        var exception = await Should.ThrowAsync<UserFriendlyException>(() => Service.CreateOrEdit(input));

        // Assert
        exception.Message.ShouldContain("cannot end before it starts");
    }

    [Fact]
    public async Task Should_Refuse_An_Additional_Unpaid_Leave_Overlapping_The_Leave_Itself()
    {
        // Arrange
        var employeeId = CreateEmployee("ESX08");
        var typeId = CreateAbsenceType("ESX08");
        var unpaidId = CreateAbsenceType("ESX8U", nameEn: "Unpaid Leave");
        var input = LeaveWithExtra(employeeId, typeId, unpaidId);
        input.ExtStartDate = Day.AddDays(2);

        // Act
        var exception = await Should.ThrowAsync<UserFriendlyException>(() => Service.CreateOrEdit(input));

        // Assert
        exception.Message.ShouldContain("overlap the leave's own start and end date");
    }

    #endregion

    #region Settle fractional balance (EA 4688-4747)

    private async Task<(EmployeeAbsAdjustment Adjustment, int TypeId)> SettleScenarioAsync(string code, bool settingOn, bool ticked, double accrued)
    {
        SetTimeSettings(s => s.EnableSettleFractionalLeaveBalance = settingOn);
        var employeeId = CreateEmployee(code);
        var typeId = CreateAbsenceType(code, t => t.HaveQuotaAccrual = true);
        CreateQuota(employeeId, typeId, accrued);
        var input = Leave(employeeId, typeId, Day, Day.AddDays(2));
        input.SettleFractionalBalance = ticked;
        await Service.CreateOrEdit(input);

        var adjustment = UsingDbContext(context => context.Set<EmployeeAbsAdjustment>().AsNoTracking().FirstOrDefault(a => a.EmployeeId == employeeId));
        return (adjustment, typeId);
    }

    [Fact]
    public async Task Should_Settle_The_Fraction_Left_In_The_Balance_As_A_Fractional_Adjustment()
    {
        // Arrange / Act: 10.5 accrued less 3 days leaves 7.5.
        var (adjustment, typeId) = await SettleScenarioAsync("ESF01", settingOn: true, ticked: true, accrued: 10.5);

        // Assert
        adjustment.ShouldNotBeNull();
        adjustment.AdjustmentType.ShouldBe(AdjustmentType.Fractional);
        adjustment.QuotaAdjusted.ShouldBe(0.5);
        adjustment.AbsenceTypeId.ShouldBe(typeId);
        adjustment.EffectiveDate.ShouldBe(Day.AddDays(2));
    }

    [Fact]
    public async Task Should_Add_The_Settled_Fraction_To_The_Annual_Quota()
    {
        // Arrange / Act: legacy empAbsAdjustment.Save() runs the adjustment's OnSaving, which adds the fraction
        // to the annual (Code "1") quota's QuotaAdjusted. 10.5 accrued less 3 days leaves 7.5 -> 0.5 settled.
        var (adjustment, typeId) = await SettleScenarioAsync("1", settingOn: true, ticked: true, accrued: 10.5);

        // Assert
        adjustment.ShouldNotBeNull();
        var quota = GetQuota(adjustment.EmployeeId, typeId);
        quota.QuotaAdjusted.ShouldBe(0.5);
    }

    [Fact]
    public async Task Should_Not_Settle_When_The_Balance_Is_Whole()
    {
        // Arrange / Act
        var (adjustment, _) = await SettleScenarioAsync("ESF02", settingOn: true, ticked: true, accrued: 10);

        // Assert
        adjustment.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Not_Settle_Unless_Ticked()
    {
        // Arrange / Act
        var (adjustment, _) = await SettleScenarioAsync("ESF03", settingOn: true, ticked: false, accrued: 10.5);

        // Assert
        adjustment.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Not_Settle_When_The_Time_Settings_Do_Not_Enable_It()
    {
        // Arrange / Act
        var (adjustment, _) = await SettleScenarioAsync("ESF04", settingOn: false, ticked: true, accrued: 10.5);

        // Assert
        adjustment.ShouldBeNull();
    }

    #endregion

    #region Quota on edit

    [Fact]
    public async Task Should_Credit_The_Leaves_Own_Days_Back_When_An_Accrual_Leave_Is_Edited()
    {
        // Arrange: 5 accrued, 5-day leave uses it all; editing a comment must not be refused for balance.
        var employeeId = CreateEmployee("ESQ01");
        var typeId = CreateAbsenceType("ESQ01", t => t.HaveQuotaAccrual = true);
        CreateQuota(employeeId, typeId, accrued: 5);
        var leave = await CreateAndGetAsync(Leave(employeeId, typeId, Day, Day.AddDays(4)));
        var edit = await EditOf(leave.Id);
        edit.Comments = "edited";

        // Act
        await Service.CreateOrEdit(edit);

        // Assert
        GetQuota(employeeId, typeId).QuotaUsed.ShouldBe(5);
    }

    #endregion
}
