using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// The separation's on-hold flags (legacy <c>EmployeeSeparation</c> "OnHold Information" and the "Set OnHold"
/// block of <c>SaveOtherInfo</c>, lines 498-538; <c>OnDeleting</c>, lines 326-348; the OnHold checks in
/// <c>PerformValidations</c>, lines 418-447, and <c>ValidatePreviousEntry</c>, lines 642-662).
///
/// <para>
/// Payroll reads <see cref="EmployeeOnHold"/>, never the separation's flags, so every test here asserts on the
/// on-hold row: that is the thing that actually holds a leaver's pay.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeSeparationsAppService_OnHold_Tests : AppTestBase
{
    private readonly IEmployeeSeparationsAppService _appService;

    public EmployeeSeparationsAppService_OnHold_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeSeparationsAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    #region Fixtures

    private int CreateEmployee(string code)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Set<Nationality>().Add(new Nationality
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
        });

        Employee employee = null;
        UsingDbContext(context =>
        {
            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId,
                Code = code,
                FirstNameEn = "F",
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "F L",
                FullNameAr = "ف ل",
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                NationalityId = nationality.Id
            }).Entity;
        });

        return employee.Id;
    }

    private int CreateReason(string code)
    {
        SeparationReason reason = null;
        UsingDbContext(context =>
        {
            reason = context.Set<SeparationReason>().Add(new SeparationReason
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });

        return reason.Id;
    }

    private void CreateOnHold(int employeeId, DateTime start, DateTime end)
    {
        UsingDbContext(context => context.Set<EmployeeOnHold>().Add(new EmployeeOnHold
        {
            TenantId = TenantId,
            EmployeeId = employeeId,
            StartDate = start,
            EndDate = end,
            OnHold = true
        }));
    }

    private static CreateOrEditEmployeeSeparationDto Separation(int employeeId, int reasonId)
    {
        return new CreateOrEditEmployeeSeparationDto
        {
            EmployeeId = employeeId,
            SubmissionDate = new DateTime(2026, 3, 1),
            LastDayInService = new DateTime(2026, 3, 31),
            SeparationType = SeparationType.Resignation,
            SeparationReasonId = reasonId
        };
    }

    private List<EmployeeOnHold> LiveHoldsFor(int employeeId) =>
        UsingDbContext(c => c.Set<EmployeeOnHold>()
            .IgnoreQueryFilters()
            .Where(o => o.EmployeeId == employeeId && !o.IsDeleted)
            .OrderBy(o => o.StartDate)
            .ToList());

    private EmployeeSeparation SeparationRow(int employeeId) =>
        UsingDbContext(c => c.Set<EmployeeSeparation>().Single(s => s.EmployeeId == employeeId));

    #endregion

    #region Creating the hold

    [Fact]
    public async Task Should_Create_The_OnHold_Record_From_The_Flags_When_A_From_Date_And_A_Flag_Are_Set()
    {
        var employeeId = CreateEmployee("SOH100");
        var reasonId = CreateReason("SOH100R");

        var input = Separation(employeeId, reasonId);
        input.OnHoldFromDate = new DateTime(2026, 3, 15);
        input.OnHoldTillDate = new DateTime(2026, 5, 10);
        input.OnHoldBasicPay = true;
        input.OnHoldLeavePay = true;
        input.OnHoldIndemnityPay = true;
        input.OnHoldAirTicketPay = true;
        input.OnHoldLoanDeduction = true;
        input.OnHoldAllEarnings = true;
        input.OnHoldAllDeductions = true;
        input.OnHoldAll = true;
        input.BankTransfer = OnHoldStatus.Hold;

        await _appService.CreateOrEdit(input);

        // Legacy lines 521-536, field for field, with OnHoldAll landing on EmployeeOnHold.OnHold.
        var hold = LiveHoldsFor(employeeId).ShouldHaveSingleItem();
        hold.OnHold.ShouldBeTrue();
        hold.OnHoldBasicPay.ShouldBeTrue();
        hold.OnHoldLeavePay.ShouldBeTrue();
        hold.OnHoldIndemnityPay.ShouldBeTrue();
        hold.OnHoldAirTicketPay.ShouldBeTrue();
        hold.OnHoldLoanDeduction.ShouldBeTrue();
        hold.OnHoldAllEarnings.ShouldBeTrue();
        hold.OnHoldAllDeductions.ShouldBeTrue();
        hold.BankTransfer.ShouldBe(OnHoldStatus.Hold);
    }

    [Fact]
    public async Task Should_Normalise_The_Hold_And_The_Separation_Dates_To_Whole_Months()
    {
        // Legacy lines 515-518 wrote the normalised dates back onto the separation itself; that stored date is
        // what later edits use to find the hold again.
        var employeeId = CreateEmployee("SOH110");
        var reasonId = CreateReason("SOH110R");

        var input = Separation(employeeId, reasonId);
        input.OnHoldFromDate = new DateTime(2026, 3, 15);
        input.OnHoldTillDate = new DateTime(2026, 4, 10);
        input.OnHoldBasicPay = true;

        await _appService.CreateOrEdit(input);

        var hold = LiveHoldsFor(employeeId).ShouldHaveSingleItem();
        hold.StartDate.ShouldBe(new DateTime(2026, 3, 1));
        hold.EndDate.ShouldBe(new DateTime(2026, 4, 30));

        var separation = SeparationRow(employeeId);
        separation.OnHoldFromDate.ShouldBe(new DateTime(2026, 3, 1));
        separation.OnHoldTillDate.ShouldBe(new DateTime(2026, 4, 30));
    }

    [Fact]
    public async Task Should_Not_Create_A_Hold_When_No_Flag_Is_Ticked()
    {
        // Legacy lines 510-513: a from-date on its own creates nothing, and the dates are left as typed.
        var employeeId = CreateEmployee("SOH120");
        var reasonId = CreateReason("SOH120R");

        var input = Separation(employeeId, reasonId);
        input.OnHoldFromDate = new DateTime(2026, 3, 15);
        input.OnHoldTillDate = new DateTime(2026, 4, 10);

        await _appService.CreateOrEdit(input);

        LiveHoldsFor(employeeId).ShouldBeEmpty();
        SeparationRow(employeeId).OnHoldFromDate.ShouldBe(new DateTime(2026, 3, 15));
    }

    [Fact]
    public async Task Should_Not_Create_A_Hold_When_A_Flag_Is_Ticked_Without_A_From_Date()
    {
        var employeeId = CreateEmployee("SOH130");
        var reasonId = CreateReason("SOH130R");

        var input = Separation(employeeId, reasonId);
        input.OnHoldBasicPay = true;

        await _appService.CreateOrEdit(input);

        LiveHoldsFor(employeeId).ShouldBeEmpty();
    }

    [Fact]
    public async Task Legacy_Parity_A_Missing_Till_Date_Is_Refused_By_The_Hold_Date_Check()
    {
        // Legacy's till date was default(DateTime); normalised it is 31 Jan 0001, before the start, and the
        // hold's own EndDate check refused it. Pinned rather than "fixed" to a one-month default.
        var employeeId = CreateEmployee("SOH140");
        var reasonId = CreateReason("SOH140R");

        var input = Separation(employeeId, reasonId);
        input.OnHoldFromDate = new DateTime(2026, 3, 15);
        input.OnHoldBasicPay = true;

        await Should.ThrowAsync<UserFriendlyException>(() => _appService.CreateOrEdit(input));
    }

    [Fact]
    public async Task Should_Refuse_A_Hold_That_Overlaps_One_Already_Entered()
    {
        // The same overlap rule the On Hold tab enforces -- legacy ran EmployeeOnHold.OnSaving for this row too.
        var employeeId = CreateEmployee("SOH150");
        var reasonId = CreateReason("SOH150R");
        CreateOnHold(employeeId, new DateTime(2026, 1, 1), new DateTime(2026, 3, 31));

        var input = Separation(employeeId, reasonId);
        input.OnHoldFromDate = new DateTime(2026, 3, 1);
        input.OnHoldTillDate = new DateTime(2026, 4, 30);
        input.OnHoldBasicPay = true;

        await Should.ThrowAsync<UserFriendlyException>(() => _appService.CreateOrEdit(input));
    }

    #endregion

    #region Editing replaces, clearing removes

    private async Task<int> CreateWithHoldAsync(int employeeId, int reasonId, DateTime from, DateTime till)
    {
        var input = Separation(employeeId, reasonId);
        input.OnHoldFromDate = from;
        input.OnHoldTillDate = till;
        input.OnHoldBasicPay = true;
        await _appService.CreateOrEdit(input);

        return SeparationRow(employeeId).Id;
    }

    private async Task<CreateOrEditEmployeeSeparationDto> ForEditAsync(int id) =>
        (await _appService.GetEmployeeSeparationForEdit(new EntityDto(id))).EmployeeSeparation;

    [Fact]
    public async Task Editing_The_Dates_Should_Replace_The_Hold_Not_Add_A_Second_One()
    {
        // Legacy lines 500-508: the previous hold is deleted first, then the new one is created.
        var employeeId = CreateEmployee("SOH200");
        var reasonId = CreateReason("SOH200R");
        var id = await CreateWithHoldAsync(employeeId, reasonId, new DateTime(2026, 3, 1), new DateTime(2026, 3, 31));

        var edit = await ForEditAsync(id);
        edit.OnHoldFromDate = new DateTime(2026, 4, 20);
        edit.OnHoldTillDate = new DateTime(2026, 6, 5);
        await _appService.CreateOrEdit(edit);

        var hold = LiveHoldsFor(employeeId).ShouldHaveSingleItem();
        hold.StartDate.ShouldBe(new DateTime(2026, 4, 1));
        hold.EndDate.ShouldBe(new DateTime(2026, 6, 30));
    }

    [Fact]
    public async Task Re_Saving_Unchanged_Should_Keep_Exactly_One_Hold()
    {
        // The old hold overlaps the new one completely. Unless the delete is saved before the overlap check,
        // the hold conflicts with its own replacement and every re-save of a separation fails.
        var employeeId = CreateEmployee("SOH210");
        var reasonId = CreateReason("SOH210R");
        var id = await CreateWithHoldAsync(employeeId, reasonId, new DateTime(2026, 3, 1), new DateTime(2026, 3, 31));

        await _appService.CreateOrEdit(await ForEditAsync(id));

        LiveHoldsFor(employeeId).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Clearing_Every_Flag_Should_Remove_The_Hold()
    {
        var employeeId = CreateEmployee("SOH220");
        var reasonId = CreateReason("SOH220R");
        var id = await CreateWithHoldAsync(employeeId, reasonId, new DateTime(2026, 3, 1), new DateTime(2026, 3, 31));

        var edit = await ForEditAsync(id);
        edit.OnHoldBasicPay = false;
        await _appService.CreateOrEdit(edit);

        LiveHoldsFor(employeeId).ShouldBeEmpty();
    }

    [Fact]
    public async Task Changing_The_Employee_Should_Move_The_Hold_To_The_New_Employee()
    {
        // Legacy matched the previous hold by prevEmployee, not by the employee being saved.
        var firstId = CreateEmployee("SOH230A");
        var secondId = CreateEmployee("SOH230B");
        var reasonId = CreateReason("SOH230R");
        var id = await CreateWithHoldAsync(firstId, reasonId, new DateTime(2026, 3, 1), new DateTime(2026, 3, 31));

        var edit = await ForEditAsync(id);
        edit.EmployeeId = secondId;
        await _appService.CreateOrEdit(edit);

        LiveHoldsFor(firstId).ShouldBeEmpty();
        LiveHoldsFor(secondId).ShouldHaveSingleItem().StartDate.ShouldBe(new DateTime(2026, 3, 1));
    }

    [Fact]
    public async Task Editing_Should_Leave_A_Hold_It_Did_Not_Create_Alone()
    {
        // Only the hold starting on the separation's previous from-date is "its" hold.
        var employeeId = CreateEmployee("SOH240");
        var reasonId = CreateReason("SOH240R");
        CreateOnHold(employeeId, new DateTime(2025, 1, 1), new DateTime(2025, 1, 31));
        var id = await CreateWithHoldAsync(employeeId, reasonId, new DateTime(2026, 3, 1), new DateTime(2026, 3, 31));

        var edit = await ForEditAsync(id);
        edit.OnHoldBasicPay = false;
        await _appService.CreateOrEdit(edit);

        LiveHoldsFor(employeeId).ShouldHaveSingleItem().StartDate.ShouldBe(new DateTime(2025, 1, 1));
    }

    #endregion

    #region Deleting

    [Fact]
    public async Task Deleting_The_Separation_Should_Delete_Its_Hold()
    {
        var employeeId = CreateEmployee("SOH300");
        var reasonId = CreateReason("SOH300R");
        var id = await CreateWithHoldAsync(employeeId, reasonId, new DateTime(2026, 3, 1), new DateTime(2026, 3, 31));

        await _appService.Delete(new EntityDto(id));

        LiveHoldsFor(employeeId).ShouldBeEmpty();
    }

    #endregion

    #region Legacy's "an on-hold was entered after this one" refusals

    [Fact]
    public async Task Should_Refuse_A_New_Separation_When_A_Later_Hold_Already_Exists()
    {
        // Legacy PerformValidations lines 418-447.
        var employeeId = CreateEmployee("SOH400");
        var reasonId = CreateReason("SOH400R");
        CreateOnHold(employeeId, new DateTime(2026, 6, 1), new DateTime(2026, 6, 30));

        var input = Separation(employeeId, reasonId);
        input.OnHoldFromDate = new DateTime(2026, 3, 1);
        input.OnHoldTillDate = new DateTime(2026, 3, 31);
        input.OnHoldBasicPay = true;

        await Should.ThrowAsync<UserFriendlyException>(() => _appService.CreateOrEdit(input));
        LiveHoldsFor(employeeId).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Refuse_Deleting_A_Separation_When_A_Later_Hold_Exists()
    {
        // Legacy ValidatePreviousEntry lines 642-662, reached from OnDeleting.
        var employeeId = CreateEmployee("SOH410");
        var reasonId = CreateReason("SOH410R");
        var id = await CreateWithHoldAsync(employeeId, reasonId, new DateTime(2026, 3, 1), new DateTime(2026, 3, 31));
        CreateOnHold(employeeId, new DateTime(2026, 7, 1), new DateTime(2026, 7, 31));

        await Should.ThrowAsync<UserFriendlyException>(() => _appService.Delete(new EntityDto(id)));
        LiveHoldsFor(employeeId).Count.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Refuse_Editing_A_Separation_When_A_Later_Hold_Exists()
    {
        var employeeId = CreateEmployee("SOH420");
        var reasonId = CreateReason("SOH420R");
        var id = await CreateWithHoldAsync(employeeId, reasonId, new DateTime(2026, 3, 1), new DateTime(2026, 3, 31));
        CreateOnHold(employeeId, new DateTime(2026, 7, 1), new DateTime(2026, 7, 31));

        var edit = await ForEditAsync(id);
        edit.Comments = "changed";

        await Should.ThrowAsync<UserFriendlyException>(() => _appService.CreateOrEdit(edit));
    }

    [Fact]
    public async Task Should_Allow_A_Separation_Without_On_Hold_Dates_Whatever_Holds_Exist()
    {
        // Both refusals are keyed off the on-hold from-date; a separation that has none is not blocked.
        var employeeId = CreateEmployee("SOH430");
        var reasonId = CreateReason("SOH430R");
        CreateOnHold(employeeId, new DateTime(2026, 7, 1), new DateTime(2026, 7, 31));

        await _appService.CreateOrEdit(Separation(employeeId, reasonId));

        SeparationRow(employeeId).ShouldNotBeNull();
    }

    #endregion

    #region The plain fields

    [Fact]
    public async Task Should_Round_Trip_Revoke_Comments_And_Comments()
    {
        var employeeId = CreateEmployee("SOH500");
        var reasonId = CreateReason("SOH500R");

        var input = Separation(employeeId, reasonId);
        input.RevokeComments = "Revoked after appeal";
        input.Comments = "Exit interview done";
        await _appService.CreateOrEdit(input);

        var edit = await ForEditAsync(SeparationRow(employeeId).Id);
        edit.RevokeComments.ShouldBe("Revoked after appeal");
        edit.Comments.ShouldBe("Exit interview done");
    }

    #endregion
}
