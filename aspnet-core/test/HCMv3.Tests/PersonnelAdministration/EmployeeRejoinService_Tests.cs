using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.TimeManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// Legacy <c>EmployeeRejoin</c> save/delete (<c>PerformValidations</c>, <c>ValidatePreviousEntry</c>,
/// <c>SaveEmployeeInformation</c>, <c>UpdatePreviousValues</c>), run end to end through
/// <see cref="IEmployeeRejoinsAppService"/>.
///
/// <para>The scenario: an employee who joined on 01/01/2015, left on 31/12/2023 (Inactive after the F&amp;F) and
/// rejoins on 01/03/2024. Balances on the old record: 30 days of leave accrued, 5 used (25 left); 50 indemnity days;
/// 2 own tickets.</para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeRejoinService_Tests : FullAndFinalTestBase
{
    private static readonly DateTime Joined = new DateTime(2015, 1, 1);
    private static readonly DateTime LastDay = new DateTime(2023, 12, 31);
    private static readonly DateTime RejoinDate = new DateTime(2024, 3, 1);
    private static readonly DateTime DayBefore = new DateTime(2024, 2, 29);
    private static readonly DateTime OpenEnded = new DateTime(9999, 12, 31);

    private const long CivilId = 287010112345;

    private readonly IEmployeeRejoinsAppService _appService;

    public EmployeeRejoinService_Tests()
    {
        _appService = Resolve<IEmployeeRejoinsAppService>();
    }

    #region << Keep the code >>

    [Fact]
    public async Task Should_Make_The_Employee_Active_Again_When_Keeping_The_Code()
    {
        var oldId = SeedFormerEmployee();

        await _appService.CreateOrEdit(Rejoin(oldId));

        EmployeeById(oldId).Status.ShouldBe(EmployeeStatus.Active);
        NewEmployeesOf(oldId).ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Zero_The_Leave_Balance_The_Day_Before_The_Rejoin_When_It_Is_Not_Carried_Forward()
    {
        var oldId = SeedFormerEmployee();
        SeedLeaveBalance(oldId, accrued: 30, used: 5);

        await _appService.CreateOrEdit(Rejoin(oldId));

        var adjustment = LiveAbsAdjustments(oldId).ShouldHaveSingleItem();
        adjustment.AdjustmentType.ShouldBe(AdjustmentType.Adjust);
        adjustment.EffectiveDate.ShouldBe(DayBefore);
        adjustment.QuotaAdjusted.ShouldBe(25);
        adjustment.Comments.ShouldBe(EmployeeRejoinService.NilQuotaComment);
    }

    [Fact]
    public async Task Should_Zero_The_Indemnity_Balance_With_A_Reset_Gratuity_Record()
    {
        var oldId = SeedFormerEmployee();
        SeedGratuityBalance(oldId, 50);

        await _appService.CreateOrEdit(Rejoin(oldId));

        var gratuity = LiveGratuities(oldId).ShouldHaveSingleItem();
        gratuity.GratuityPaidDays.ShouldBe(50);
        gratuity.GratuityPaidDate.ShouldBe(DayBefore);
        gratuity.ResetGratuityCalc.ShouldBeTrue();
        gratuity.Comments.ShouldBe(EmployeeRejoinService.NilQuotaComment);
    }

    /// <summary>
    /// LEGACY BUG FIXED (owner's decision 2): legacy line 643 zeroed the ticket balance with the INDEMNITY balance
    /// (here 50). The adjustment now carries the ticket balance itself.
    /// </summary>
    [Fact]
    public async Task Should_Zero_The_Ticket_Balance_With_The_Ticket_Balance_Not_The_Indemnity_Balance()
    {
        var oldId = SeedFormerEmployee();
        SeedGratuityBalance(oldId, 50);
        SeedTicketBalance(oldId, 2);

        await _appService.CreateOrEdit(Rejoin(oldId));

        var adjustment = LiveTktAdjustments(oldId).ShouldHaveSingleItem();
        adjustment.QuotaAdjusted.ShouldBe(2);
        adjustment.AdjustmentType.ShouldBe(AdjustmentType.Adjust);
        adjustment.EffectiveDate.ShouldBe(DayBefore);
        adjustment.TicketPaymentType.ShouldBe(TicketPaymentType.PayInAdvance);
        adjustment.Comments.ShouldBe(EmployeeRejoinService.NilQuotaComment);
    }

    /// <summary>Legacy 684-693: each OPEN loan is recovered externally, dated on that loan's own end date.</summary>
    [Fact]
    public async Task Should_Recover_Each_Open_Loan_Externally_On_Its_Own_End_Date()
    {
        var oldId = SeedFormerEmployee();
        var typeA = SeedLoanType("A");
        var open = SeedLoan(oldId, typeA, balance: 120, fixedAmount: 10, endDate: new DateTime(2025, 6, 30));
        var closed = SeedLoan(oldId, typeA, balance: 0, fixedAmount: 10, endDate: new DateTime(2023, 6, 30));

        await _appService.CreateOrEdit(Rejoin(oldId));

        var recovery = LiveRecoveries(oldId).ShouldHaveSingleItem();
        recovery.EmployeeLoanId.ShouldBe(open);
        recovery.LoanRecoveryMethod.ShouldBe(LoanRecoveryMethod.RecoveryExternal);
        recovery.RecoveryAmount.ShouldBe(120);
        recovery.RecoveryDate.ShouldBe(new DateTime(2025, 6, 30));
        recovery.Comments.ShouldBe(EmployeeRejoinService.NilLoanComment);

        Loan(open).BalanceDueAmount.ShouldBe(0);
        LiveRecoveries(oldId).ShouldNotContain(r => r.EmployeeLoanId == closed);
    }

    [Fact]
    public async Task Should_Leave_Carried_Forward_Balances_Alone_When_Keeping_The_Code()
    {
        var oldId = SeedFormerEmployee();
        SeedLeaveBalance(oldId, accrued: 30, used: 5);
        SeedGratuityBalance(oldId, 50);
        SeedTicketBalance(oldId, 2);
        var loan = SeedLoan(oldId, SeedLoanType("A"), balance: 120, fixedAmount: 10, endDate: new DateTime(2025, 6, 30));

        await _appService.CreateOrEdit(Rejoin(oldId, leave: true, ticket: true, indemnity: true, loan: true));

        LiveAbsAdjustments(oldId).ShouldBeEmpty();
        LiveGratuities(oldId).ShouldBeEmpty();
        LiveTktAdjustments(oldId).ShouldBeEmpty();
        LiveRecoveries(oldId).ShouldBeEmpty();
        Loan(loan).BalanceDueAmount.ShouldBe(120);
    }

    /// <summary>Legacy <c>OnDeleting</c> -> <c>UpdatePreviousValues</c>: everything the save wrote goes, and the employee is Inactive again.</summary>
    [Fact]
    public async Task Should_Undo_The_Zeroing_And_Make_The_Employee_Inactive_On_Delete()
    {
        var oldId = SeedFormerEmployee();
        SeedLeaveBalance(oldId, accrued: 30, used: 5);
        SeedGratuityBalance(oldId, 50);
        SeedTicketBalance(oldId, 2);
        var loan = SeedLoan(oldId, SeedLoanType("A"), balance: 120, fixedAmount: 10, endDate: new DateTime(2025, 6, 30));
        await _appService.CreateOrEdit(Rejoin(oldId));

        await _appService.Delete(new EntityDto(RejoinIdOf(oldId)));

        EmployeeById(oldId).Status.ShouldBe(EmployeeStatus.Inactive);
        LiveAbsAdjustments(oldId).ShouldBeEmpty();
        LiveGratuities(oldId).ShouldBeEmpty();
        LiveTktAdjustments(oldId).ShouldBeEmpty();
        LiveRecoveries(oldId).ShouldBeEmpty();
        Loan(loan).BalanceDueAmount.ShouldBe(120);
    }

    /// <summary>An edit undoes the previous save first, so moving the date moves the zeroing record -- never doubles it.</summary>
    [Fact]
    public async Task Should_Move_Not_Duplicate_The_Zeroing_Records_On_Edit()
    {
        var oldId = SeedFormerEmployee();
        SeedLeaveBalance(oldId, accrued: 30, used: 5);
        SeedGratuityBalance(oldId, 50);
        await _appService.CreateOrEdit(Rejoin(oldId));

        var edit = Rejoin(oldId, rejoinDate: new DateTime(2024, 4, 1));
        edit.Id = RejoinIdOf(oldId);
        await _appService.CreateOrEdit(edit);

        LiveAbsAdjustments(oldId).ShouldHaveSingleItem().EffectiveDate.ShouldBe(new DateTime(2024, 3, 31));
        LiveGratuities(oldId).ShouldHaveSingleItem().GratuityPaidDate.ShouldBe(new DateTime(2024, 3, 31));
        EmployeeById(oldId).Status.ShouldBe(EmployeeStatus.Active);
    }

    [Fact]
    public async Task Should_Remove_The_Zeroing_When_An_Edit_Ticks_Carry_Forward()
    {
        var oldId = SeedFormerEmployee();
        SeedLeaveBalance(oldId, accrued: 30, used: 5);
        await _appService.CreateOrEdit(Rejoin(oldId));

        var edit = Rejoin(oldId, leave: true);
        edit.Id = RejoinIdOf(oldId);
        await _appService.CreateOrEdit(edit);

        LiveAbsAdjustments(oldId).ShouldBeEmpty();
    }

    #endregion

    #region << New code >>

    /// <summary>
    /// Legacy 460-520. Copied: names, title, birth date, nationality, religion, gender, marital status, country,
    /// phones, addresses, qualification, blood group, civil ID, email, birth place. Not copied: access ID, login,
    /// probation days. Decision 1: the Arabic full name comes from the ARABIC parts.
    /// </summary>
    [Fact]
    public async Task Should_Create_A_New_Active_Employee_From_The_Legacy_Field_List()
    {
        var oldId = SeedFormerEmployee();

        await _appService.CreateOrEdit(Rejoin(oldId, newCode: true));

        var old = EmployeeById(oldId);
        var created = NewEmployeesOf(oldId).ShouldHaveSingleItem();

        created.Status.ShouldBe(EmployeeStatus.Active);
        created.JoiningDate.ShouldBe(RejoinDate);
        created.PreviousEmpCode.ShouldBe(old.Code);
        created.Code.ShouldNotBeNullOrWhiteSpace();
        created.Code.ShouldNotBe(old.Code);

        created.FirstNameEn.ShouldBe("Leaver");
        created.SecondNameEn.ShouldBe("Second");
        created.FirstNameAr.ShouldBe("مغادر");
        created.SecondNameAr.ShouldBe("ثاني");
        created.FullNameEn.ShouldBe("Leaver Second Test");
        created.FullNameAr.ShouldBe("مغادر ثاني تجربة");

        created.BirthDate.ShouldBe(new DateTime(1990, 5, 17));
        created.NationalityId.ShouldBe(old.NationalityId);
        created.Gender.ShouldBe(old.Gender);
        created.CivilID.ShouldBe(CivilId);
        created.Email.ShouldBe("leaver@example.com");
        created.Mobile.ShouldBe(96512345678);
        created.CurrentAddress.ShouldBe("Salmiya");
        created.BloodGroup.ShouldBe("O+");

        created.AccessID.ShouldBeNull();
        created.LoginID.ShouldBeNull();
        created.UserId.ShouldBeNull();
    }

    /// <summary>
    /// LEGACY PARITY, pinned: legacy wrote ProbationEndDate = rejoin date + the OLD probation days, but its property
    /// setter replaced that with JoiningDate + ProbationDays, and ProbationDays was never copied. What legacy stored
    /// is zero days, ending on the rejoin date.
    /// </summary>
    [Fact]
    public async Task Should_Give_The_New_Employee_No_Probation_As_Legacy_Stored_It()
    {
        var oldId = SeedFormerEmployee();

        await _appService.CreateOrEdit(Rejoin(oldId, newCode: true));

        var created = NewEmployeesOf(oldId).ShouldHaveSingleItem();
        created.ProbationDays.ShouldBe(0);
        created.ProbationEndDate.ShouldBe(RejoinDate);
    }

    /// <summary>Decision 3: the Employment Type is copied, so the code comes from that type's number series.</summary>
    [Fact]
    public async Task Should_Take_The_Code_From_The_Number_Series_Of_The_Old_Employment_Type()
    {
        var oldId = SeedFormerEmployee(EmploymentType.Temporary);
        UsingDbContext(context =>
        {
            context.Set<NumberSeries>().Any(s => s.EmploymentType == EmploymentType.ALL).ShouldBeFalse();
            context.Set<NumberSeries>().Add(new NumberSeries
            {
                TenantId = TenantId, Code = "RJT", Name = "Rejoin temporary", Prefix = "RJT-", Formatting = "000",
                EmploymentType = EmploymentType.Temporary
            });
        });

        await _appService.CreateOrEdit(Rejoin(oldId, newCode: true));

        var created = NewEmployeesOf(oldId).ShouldHaveSingleItem();
        created.EmploymentType.ShouldBe(EmploymentType.Temporary);
        created.Code.ShouldBe("RJT-001");
    }

    [Fact]
    public async Task Should_Leave_The_Old_Employee_As_It_Was()
    {
        var oldId = SeedFormerEmployee();
        SeedLeaveBalance(oldId, accrued: 30, used: 5);
        SeedGratuityBalance(oldId, 50);
        SeedTicketBalance(oldId, 2);
        var loan = SeedLoan(oldId, SeedLoanType("A"), balance: 120, fixedAmount: 10, endDate: new DateTime(2025, 6, 30));

        await _appService.CreateOrEdit(Rejoin(oldId, newCode: true));

        EmployeeById(oldId).Status.ShouldBe(EmployeeStatus.Inactive);
        LiveAbsAdjustments(oldId).ShouldBeEmpty();
        LiveGratuities(oldId).ShouldBeEmpty();
        LiveTktAdjustments(oldId).ShouldBeEmpty();
        LiveRecoveries(oldId).ShouldBeEmpty();
        Loan(loan).BalanceDueAmount.ShouldBe(120);
    }

    /// <summary>Legacy 559-568: a Time Transfer of the old balance, type "1", on the rejoin date.</summary>
    [Fact]
    public async Task Should_Carry_The_Leave_Balance_Forward_As_A_Time_Transfer_On_The_Rejoin_Date()
    {
        var oldId = SeedFormerEmployee();
        SeedLeaveBalance(oldId, accrued: 30, used: 5);
        var transferTypeId = SeedTimeTransferTypeOne();

        await _appService.CreateOrEdit(Rejoin(oldId, newCode: true, leave: true));

        var newId = NewEmployeesOf(oldId).ShouldHaveSingleItem().Id;
        var transfer = UsingDbContext(c => c.Set<TimeTransfer>().Where(t => t.EmployeeId == newId && !t.IsDeleted).ToList()).ShouldHaveSingleItem();
        transfer.TransferDate.ShouldBe(RejoinDate);
        transfer.TransferCount.ShouldBe(25);
        transfer.TransferTypeId.ShouldBe(transferTypeId);
    }

    /// <summary>Legacy 603-613: a new indemnity quota of the old balance, deductions from 9 months after joining.</summary>
    [Fact]
    public async Task Should_Open_An_Indemnity_Quota_With_Deductions_From_Nine_Months_After_Rejoining()
    {
        var oldId = SeedFormerEmployee();
        SeedGratuityBalance(oldId, 50);
        UsingDbContext(c => c.Set<EmployeeGratuityQuota>().First(q => q.EmployeeId == oldId).QuotaUsed = 12.5);

        await _appService.CreateOrEdit(Rejoin(oldId, newCode: true, indemnity: true));

        var newId = NewEmployeesOf(oldId).ShouldHaveSingleItem().Id;
        var quota = UsingDbContext(c => c.Set<EmployeeGratuityQuota>().Where(q => q.EmployeeId == newId && !q.IsDeleted).ToList()).ShouldHaveSingleItem();
        quota.QuotaAccrued.ShouldBe(37.5);
        quota.QuotaUsed.ShouldBe(0);
        quota.EntitlementFrom.ShouldBe(RejoinDate);
        quota.EntitlementTo.ShouldBe(OpenEnded);
        quota.DeductionFrom.ShouldBe(new DateTime(2024, 12, 1));
        quota.DeductionTo.ShouldBe(OpenEnded);
    }

    /// <summary>Legacy 651-661, plus the ticket type HCMv3 requires (legacy left it blank).</summary>
    [Fact]
    public async Task Should_Open_A_Ticket_Quota_With_Deductions_From_Nine_Months_After_Rejoining()
    {
        var oldId = SeedFormerEmployee();
        SeedTicketBalance(oldId, 2);

        await _appService.CreateOrEdit(Rejoin(oldId, newCode: true, ticket: true));

        var newId = NewEmployeesOf(oldId).ShouldHaveSingleItem().Id;
        var quota = UsingDbContext(c => c.Set<EmployeeTicketQuota>().Where(q => q.EmployeeId == newId && !q.IsDeleted).ToList()).ShouldHaveSingleItem();
        quota.QuotaAccrued.ShouldBe(2);
        quota.TicketTypeId.ShouldBe(UsingDbContext(c => c.Set<TicketType>().First(t => t.Code == "1").Id));
        quota.EntitlementFrom.ShouldBe(RejoinDate);
        quota.DeductionFrom.ShouldBe(new DateTime(2024, 12, 1));
        quota.DeductionTo.ShouldBe(OpenEnded);
    }

    /// <summary>
    /// Legacy 701-754: one new loan per loan type, summing the open balances and the instalments, running 2 years
    /// from rejoining. A loan with nothing left is not carried.
    /// </summary>
    [Fact]
    public async Task Should_Merge_The_Open_Loans_By_Type_Into_One_Two_Year_Loan_Each()
    {
        var oldId = SeedFormerEmployee();
        var typeA = SeedLoanType("A");
        var typeB = SeedLoanType("B");
        SeedLoan(oldId, typeA, balance: 100, fixedAmount: 10, endDate: new DateTime(2025, 1, 31));
        SeedLoan(oldId, typeA, balance: 50, fixedAmount: 5, endDate: new DateTime(2024, 12, 31));
        SeedLoan(oldId, typeA, balance: 0, fixedAmount: 20, endDate: new DateTime(2023, 1, 31));
        SeedLoan(oldId, typeB, balance: 30, fixedAmount: 3, endDate: new DateTime(2024, 9, 30));

        await _appService.CreateOrEdit(Rejoin(oldId, newCode: true, loan: true));

        var newId = NewEmployeesOf(oldId).ShouldHaveSingleItem().Id;
        var loans = UsingDbContext(c => c.Set<EmployeeLoan>().Where(l => l.EmployeeId == newId && !l.IsDeleted).OrderBy(l => l.LoanTypeId).ToList());
        loans.Count.ShouldBe(2);

        var a = loans.Single(l => l.LoanTypeId == typeA);
        a.ActualAmount.ShouldBe(150);
        a.DueAmount.ShouldBe(150);
        a.BalanceDueAmount.ShouldBe(150);
        a.FixedAmount.ShouldBe(15);
        a.StartDate.ShouldBe(RejoinDate);
        a.RecoveryStartDate.ShouldBe(RejoinDate);
        a.EndDate.ShouldBe(new DateTime(2026, 3, 1));
        a.IncludeInLoanPay.ShouldBeTrue();

        var b = loans.Single(l => l.LoanTypeId == typeB);
        b.BalanceDueAmount.ShouldBe(30);
        b.FixedAmount.ShouldBe(3);
    }

    [Fact]
    public async Task Should_Not_Bring_Over_Balances_That_Are_Not_Ticked()
    {
        var oldId = SeedFormerEmployee();
        SeedLeaveBalance(oldId, accrued: 30, used: 5);
        SeedGratuityBalance(oldId, 50);
        SeedTicketBalance(oldId, 2);
        SeedLoan(oldId, SeedLoanType("A"), balance: 120, fixedAmount: 10, endDate: new DateTime(2025, 6, 30));
        SeedTimeTransferTypeOne();

        await _appService.CreateOrEdit(Rejoin(oldId, newCode: true));

        var newId = NewEmployeesOf(oldId).ShouldHaveSingleItem().Id;
        UsingDbContext(c =>
        {
            c.Set<TimeTransfer>().Any(t => t.EmployeeId == newId && !t.IsDeleted).ShouldBeFalse();
            c.Set<EmployeeGratuityQuota>().Any(q => q.EmployeeId == newId && !q.IsDeleted).ShouldBeFalse();
            c.Set<EmployeeTicketQuota>().Any(q => q.EmployeeId == newId && !q.IsDeleted).ShouldBeFalse();
            c.Set<EmployeeLoan>().Any(l => l.EmployeeId == newId && !l.IsDeleted).ShouldBeFalse();
        });
    }

    /// <summary>Refused before anything is written: no new employee and no rejoin row are left behind.</summary>
    [Fact]
    public async Task Should_Refuse_To_Carry_Leave_Forward_Without_Time_Transfer_Type_One()
    {
        var oldId = SeedFormerEmployee();
        SeedLeaveBalance(oldId, accrued: 30, used: 5);

        var ex = await Should.ThrowAsync<UserFriendlyException>(
            () => _appService.CreateOrEdit(Rejoin(oldId, newCode: true, leave: true)));

        ex.Message.ShouldStartWith("Time Transfer Type with code '1' not found");
        NewEmployeesOf(oldId).ShouldBeEmpty();
        UsingDbContext(c => c.Set<EmployeeRejoin>().Any(r => r.EmployeeId == oldId && !r.IsDeleted)).ShouldBeFalse();
    }

    /// <summary>Legacy <c>ValidatePreviousEntry</c> 338-351: once the new employee exists the rejoin is frozen.</summary>
    [Fact]
    public async Task Should_Refuse_To_Edit_Or_Delete_A_Rejoin_Once_Its_New_Employee_Exists()
    {
        var oldId = SeedFormerEmployee();
        await _appService.CreateOrEdit(Rejoin(oldId, newCode: true));
        var rejoinId = RejoinIdOf(oldId);

        var edit = Rejoin(oldId, newCode: true, rejoinDate: new DateTime(2024, 4, 1));
        edit.Id = rejoinId;
        var editEx = await Should.ThrowAsync<UserFriendlyException>(() => _appService.CreateOrEdit(edit));
        var deleteEx = await Should.ThrowAsync<UserFriendlyException>(() => _appService.Delete(new EntityDto(rejoinId)));

        editEx.Message.ShouldStartWith("The New employee created from this rejoining entry.");
        deleteEx.Message.ShouldStartWith("The New employee created from this rejoining entry.");
        NewEmployeesOf(oldId).Count.ShouldBe(1);
    }

    /// <summary>The duplicate civil ID check skips the employee's own previous code (legacy Employee.cs 1640).</summary>
    [Fact]
    public async Task Should_Allow_The_Same_Civil_Id_As_The_Previous_Code_While_The_Old_Record_Is_Active()
    {
        var oldId = SeedFormerEmployee();
        UsingDbContext(c => c.Set<Employee>().Find(oldId).Status = EmployeeStatus.Active);

        await _appService.CreateOrEdit(Rejoin(oldId, newCode: true));

        NewEmployeesOf(oldId).ShouldHaveSingleItem().CivilID.ShouldBe(CivilId);
    }

    [Fact]
    public async Task Should_Still_Refuse_A_Civil_Id_Held_By_Another_Active_Employee()
    {
        var oldId = SeedFormerEmployee();
        UsingDbContext(c =>
        {
            var other = c.Set<Employee>().Find(oldId);
            c.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId, Code = "OTHER" + Guid.NewGuid().ToString("N").Substring(0, 5),
                FirstNameEn = "Other", LastNameEn = "Person", FirstNameAr = "آخر", LastNameAr = "شخص",
                NationalityId = other.NationalityId, JoiningDate = Joined, CivilID = CivilId, Status = EmployeeStatus.Active
            });
        });

        await Should.ThrowAsync<UserFriendlyException>(() => _appService.CreateOrEdit(Rejoin(oldId, newCode: true)));

        NewEmployeesOf(oldId).ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_List_The_New_Employee_And_Link_It_Back_To_The_Old_Record()
    {
        var oldId = SeedFormerEmployee();
        await _appService.CreateOrEdit(Rejoin(oldId, newCode: true));
        var created = NewEmployeesOf(oldId).ShouldHaveSingleItem();

        var list = await _appService.GetAll(new GetAllEmployeeRejoinsInput { EmployeeId = oldId });
        var row = list.Items.ShouldHaveSingleItem();
        row.CreateNewEmployeeCode.ShouldBeTrue();
        row.NewEmployeeId.ShouldBe(created.Id);
        row.NewEmployeeCode.ShouldBe(created.Code);

        var profile = await Resolve<IEmployeesAppService>().GetEmployeeProfile(new EntityDto(created.Id));
        profile.PreviousEmployeeId.ShouldBe(oldId);
    }

    #endregion

    #region << Validation (legacy PerformValidations) >>

    [Fact]
    public async Task Should_Refuse_A_Rejoin_Dated_Before_A_Later_Exit()
    {
        var oldId = SeedFormerEmployee();
        UsingDbContext(c =>
        {
            var reasonId = c.Set<EmployeeSeparation>().First(s => s.EmployeeId == oldId).SeparationReasonId;
            c.Set<EmployeeSeparation>().Add(new EmployeeSeparation
            {
                TenantId = TenantId, EmployeeId = oldId, SubmissionDate = new DateTime(2024, 5, 1),
                LastDayInService = new DateTime(2024, 5, 31), PlannedLastDayInService = new DateTime(2024, 5, 31),
                SeparationType = SeparationType.Resignation, SeparationReasonId = reasonId
            });
        });

        var ex = await Should.ThrowAsync<UserFriendlyException>(() => _appService.CreateOrEdit(Rejoin(oldId)));

        ex.Message.ShouldStartWith("The Exit has already been made for this employee.");
    }

    [Fact]
    public async Task Should_Refuse_A_Rejoin_Dated_Before_A_Later_Full_And_Final()
    {
        var oldId = SeedFormerEmployee();
        UsingDbContext(c => c.Set<EmployeeFnFPayment>().Add(new EmployeeFnFPayment
        {
            TenantId = TenantId, EmployeeId = oldId, CompensationDate = new DateTime(2024, 6, 30)
        }));

        var ex = await Should.ThrowAsync<UserFriendlyException>(() => _appService.CreateOrEdit(Rejoin(oldId)));

        ex.Message.ShouldStartWith("The Fnf Payment has already been made for this employee.");
    }

    [Fact]
    public async Task Should_Refuse_A_Rejoin_Once_A_Regular_Payroll_Has_Run_From_The_Rejoin_Date()
    {
        var oldId = SeedFormerEmployee();
        UsingDbContext(c => c.Set<SalaryRegister>().Add(new SalaryRegister
        {
            TenantId = TenantId, EmployeeId = oldId, PayrollRunType = PayrollRunType.Regular,
            ForPeriod = new DateTime(2024, 3, 1), InPeriod = new DateTime(2024, 3, 1)
        }));

        var ex = await Should.ThrowAsync<UserFriendlyException>(() => _appService.CreateOrEdit(Rejoin(oldId)));

        ex.Message.ShouldStartWith("The salary register has already been processed for this employee.");
    }

    /// <summary>A retro row (ForPeriod != InPeriod) is not a regular run, so it does not block (legacy 289).</summary>
    [Fact]
    public async Task Should_Not_Count_A_Retro_Payroll_Row()
    {
        var oldId = SeedFormerEmployee();
        UsingDbContext(c => c.Set<SalaryRegister>().Add(new SalaryRegister
        {
            TenantId = TenantId, EmployeeId = oldId, PayrollRunType = PayrollRunType.Regular,
            ForPeriod = new DateTime(2024, 3, 1), InPeriod = new DateTime(2024, 4, 1)
        }));

        await Should.NotThrowAsync(() => _appService.CreateOrEdit(Rejoin(oldId)));
    }

    [Fact]
    public async Task Should_Refuse_A_Rejoin_Dated_Before_A_Later_Rejoin()
    {
        var oldId = SeedFormerEmployee();
        UsingDbContext(c => c.Set<EmployeeRejoin>().Add(new EmployeeRejoin
        {
            TenantId = TenantId, EmployeeId = oldId, RejoinDate = new DateTime(2024, 9, 1)
        }));

        var ex = await Should.ThrowAsync<UserFriendlyException>(() => _appService.CreateOrEdit(Rejoin(oldId)));

        ex.Message.ShouldStartWith("The Rejoin has already been made for this employee.");
    }

    #endregion

    #region << Seed and read helpers >>

    /// <summary>The leaver of the scenario, made Inactive (as the F&amp;F leaves them) with the fields the copy reads.</summary>
    private int SeedFormerEmployee(EmploymentType employmentType = EmploymentType.Permanent)
    {
        var seeded = SeedLeaver(Joined, LastDay, SeparationType.Resignation);

        UsingDbContext(context =>
        {
            var employee = context.Set<Employee>().Find(seeded.EmployeeId);
            employee.SecondNameEn = "Second";
            employee.SecondNameAr = "ثاني";
            employee.FullNameEn = "Leaver Second Test";
            employee.FullNameAr = "مغادر ثاني تجربة";
            employee.BirthDate = new DateTime(1990, 5, 17);
            employee.Gender = Gender.Female;
            employee.CivilID = CivilId;
            employee.AccessID = "ACC-" + seeded.EmployeeId;
            employee.LoginID = "leaver" + seeded.EmployeeId;
            employee.Email = "leaver@example.com";
            employee.Mobile = 96512345678;
            employee.CurrentAddress = "Salmiya";
            employee.BloodGroup = "O+";
            employee.ProbationDays = 90;
            employee.ProbationEndDate = Joined.AddDays(90);
            employee.EmploymentType = employmentType;
            employee.Status = EmployeeStatus.Inactive;
        });

        return seeded.EmployeeId;
    }

    private static CreateOrEditEmployeeRejoinDto Rejoin(
        int employeeId, bool newCode = false, bool leave = false, bool ticket = false, bool indemnity = false,
        bool loan = false, DateTime? rejoinDate = null)
    {
        return new CreateOrEditEmployeeRejoinDto
        {
            EmployeeId = employeeId,
            RejoinDate = rejoinDate ?? RejoinDate,
            CreateNewEmployeeCode = newCode,
            CarryForwardLeaveBalance = leave,
            CarryForwardTicketBalance = ticket,
            CarryForwardIndemnityBalance = indemnity,
            CarryForwardLoanBalance = loan
        };
    }

    private int SeedLoanType(string code)
    {
        return UsingDbContext(context =>
        {
            var type = context.Set<LoanType>().Add(new LoanType
            {
                TenantId = TenantId, Code = code + Guid.NewGuid().ToString("N").Substring(0, 4), NameEn = "Loan " + code, NameAr = "قرض " + code
            }).Entity;
            context.SaveChanges();
            return type.Id;
        });
    }

    private int SeedLoan(int employeeId, int loanTypeId, double balance, double fixedAmount, DateTime endDate)
    {
        return UsingDbContext(context =>
        {
            var loan = context.Set<EmployeeLoan>().Add(new EmployeeLoan
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                LoanTypeId = loanTypeId,
                StartDate = new DateTime(2022, 1, 1),
                RecoveryStartDate = new DateTime(2022, 1, 1),
                EndDate = endDate,
                ActualAmount = 500,
                DueAmount = balance,
                BalanceDueAmount = balance,
                FixedAmount = fixedAmount
            }).Entity;
            context.SaveChanges();
            return loan.Id;
        });
    }

    private int SeedTimeTransferTypeOne()
    {
        return UsingDbContext(context =>
        {
            var type = context.Set<TimeTransferType>().FirstOrDefault(t => t.Code == "1")
                       ?? context.Set<TimeTransferType>().Add(new TimeTransferType { TenantId = TenantId, Code = "1", NameEn = "Opening", NameAr = "افتتاحي" }).Entity;
            context.SaveChanges();
            return type.Id;
        });
    }

    private Employee EmployeeById(int id) => UsingDbContext(c => c.Set<Employee>().First(e => e.Id == id));

    private EmployeeLoan Loan(int id) => UsingDbContext(c => c.Set<EmployeeLoan>().First(l => l.Id == id));

    private List<Employee> NewEmployeesOf(int oldId)
    {
        var oldCode = EmployeeById(oldId).Code;
        return UsingDbContext(c => c.Set<Employee>().Where(e => e.PreviousEmpCode == oldCode && !e.IsDeleted).ToList());
    }

    private int RejoinIdOf(int employeeId)
        => UsingDbContext(c => c.Set<EmployeeRejoin>().Where(r => r.EmployeeId == employeeId && !r.IsDeleted).OrderBy(r => r.Id).First().Id);

    private List<EmployeeAbsAdjustment> LiveAbsAdjustments(int employeeId)
        => UsingDbContext(c => c.Set<EmployeeAbsAdjustment>().Where(r => r.EmployeeId == employeeId && !r.IsDeleted).ToList());

    private List<EmployeeGratuity> LiveGratuities(int employeeId)
        => UsingDbContext(c => c.Set<EmployeeGratuity>().Where(r => r.EmployeeId == employeeId && !r.IsDeleted).ToList());

    private List<EmployeeTktAdjustment> LiveTktAdjustments(int employeeId)
        => UsingDbContext(c => c.Set<EmployeeTktAdjustment>().Where(r => r.EmployeeId == employeeId && !r.IsDeleted).ToList());

    private List<EmployeeLoanRecovery> LiveRecoveries(int employeeId)
        => UsingDbContext(c => c.Set<EmployeeLoanRecovery>().Where(r => r.EmployeeLoan.EmployeeId == employeeId && !r.IsDeleted).ToList());

    #endregion
}
