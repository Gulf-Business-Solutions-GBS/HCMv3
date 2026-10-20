using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.SystemSettings;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// The loan instalment schedule as HR edits it, against legacy <c>EmployeeLoanInstallment</c>: its validation, the
/// end-date chaining, the loan's recovery end date (with <c>AutoUpdateLoanRecoveryEndDate</c> on) and the payroll
/// retro date. Also the first row a loan gets from its fixed amount when it comes into force.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeLoanInstallmentsAppService_Tests : AppTestBase
{
    private const int TenantId = 1;
    private static readonly DateTime OpenEnded = new DateTime(9999, 12, 31);
    private static readonly DateTime JoiningDate = new DateTime(2020, 1, 1);

    private readonly IEmployeeLoanInstallmentsAppService _appService;
    private readonly int _employeeId;
    private readonly int _loanTypeId;

    public EmployeeLoanInstallmentsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeLoanInstallmentsAppService>();

        _employeeId = CreateEmployee("LI-001");
        CreateAssignment(_employeeId, CreatePayrollArea());
        _loanTypeId = CreateLoanType();
    }

    // ------------------------------------------------------------------ saving and chaining

    [Fact]
    public async Task Should_Create_A_Row_That_Is_Open_Ended()
    {
        var loanId = SeedLoan();

        await _appService.CreateOrEdit(Input(loanId, new DateTime(2026, 1, 1), 100, "First"));

        var row = Rows(loanId).ShouldHaveSingleItem();
        row.StartDate.ShouldBe(new DateTime(2026, 1, 1));
        row.EndDate.ShouldBe(OpenEnded);
        row.Amount.ShouldBe(100);
        row.Remark.ShouldBe("First");
        row.TenantId.ShouldBe(TenantId);
    }

    [Fact]
    public async Task Should_Close_The_Earlier_Row_The_Day_Before_A_Later_One()
    {
        var loanId = SeedLoan();

        await _appService.CreateOrEdit(Input(loanId, new DateTime(2026, 1, 1), 100));
        await _appService.CreateOrEdit(Input(loanId, new DateTime(2026, 6, 1), 150));

        var rows = Rows(loanId);
        rows[0].EndDate.ShouldBe(new DateTime(2026, 5, 31));
        rows[1].EndDate.ShouldBe(OpenEnded);
    }

    [Fact]
    public async Task Should_Reopen_The_Earlier_Row_When_The_Later_One_Is_Deleted()
    {
        var loanId = SeedLoan();
        await _appService.CreateOrEdit(Input(loanId, new DateTime(2026, 1, 1), 100));
        await _appService.CreateOrEdit(Input(loanId, new DateTime(2026, 6, 1), 150));

        await _appService.Delete(new EntityDto(Rows(loanId)[1].Id));

        Rows(loanId).ShouldHaveSingleItem().EndDate.ShouldBe(OpenEnded);
    }

    [Fact]
    public async Task Should_Rechain_When_A_Row_Is_Moved()
    {
        var loanId = SeedLoan();
        await _appService.CreateOrEdit(Input(loanId, new DateTime(2026, 1, 1), 100));
        await _appService.CreateOrEdit(Input(loanId, new DateTime(2026, 6, 1), 150));

        var later = Rows(loanId)[1];
        var input = Input(loanId, new DateTime(2026, 9, 1), 150);
        input.Id = later.Id;
        await _appService.CreateOrEdit(input);

        Rows(loanId)[0].EndDate.ShouldBe(new DateTime(2026, 8, 31));
    }

    [Fact]
    public async Task GetAll_Should_List_Only_The_Rows_Of_The_Loan()
    {
        var loanId = SeedLoan();
        var otherLoanId = SeedLoan();
        await _appService.CreateOrEdit(Input(loanId, new DateTime(2026, 1, 1), 100));
        await _appService.CreateOrEdit(Input(otherLoanId, new DateTime(2026, 1, 1), 200));

        var result = await _appService.GetAll(new GetAllEmployeeLoanInstallmentsInput { EmployeeLoanId = loanId, MaxResultCount = 100 });

        result.TotalCount.ShouldBe(1);
        result.Items[0].Amount.ShouldBe(100);
        result.Items[0].EndDate.ShouldBe(OpenEnded);
    }

    [Fact]
    public async Task GetForEdit_Should_Return_The_Row()
    {
        var loanId = SeedLoan();
        await _appService.CreateOrEdit(Input(loanId, new DateTime(2026, 1, 1), 100, "Note"));

        var output = await _appService.GetEmployeeLoanInstallmentForEdit(new EntityDto(Rows(loanId)[0].Id));

        output.EmployeeLoanInstallment.EmployeeLoanId.ShouldBe(loanId);
        output.EmployeeLoanInstallment.StartDate.ShouldBe(new DateTime(2026, 1, 1));
        output.EmployeeLoanInstallment.Amount.ShouldBe(100);
        output.EmployeeLoanInstallment.Remark.ShouldBe("Note");
    }

    // ------------------------------------------------------------------ legacy validation

    [Fact]
    public async Task Should_Refuse_An_Effective_Date_That_Is_Not_The_First_Of_The_Month()
    {
        var loanId = SeedLoan();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _appService.CreateOrEdit(Input(loanId, new DateTime(2026, 1, 15), 100)));
    }

    [Fact]
    public async Task Should_Refuse_An_Amount_Of_Zero()
    {
        var loanId = SeedLoan();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _appService.CreateOrEdit(Input(loanId, new DateTime(2026, 1, 1), 0)));
    }

    [Fact]
    public async Task Should_Refuse_A_Month_Before_The_Recovery_Start_Month()
    {
        // Recovery starts 10 Jan 2026, so January itself is allowed and December is not.
        var loanId = SeedLoan();

        await _appService.CreateOrEdit(Input(loanId, new DateTime(2026, 1, 1), 100));
        await Should.ThrowAsync<UserFriendlyException>(() =>
            _appService.CreateOrEdit(Input(loanId, new DateTime(2025, 12, 1), 100)));
    }

    [Fact]
    public async Task Should_Refuse_A_Second_Row_For_The_Same_Month()
    {
        var loanId = SeedLoan();
        await _appService.CreateOrEdit(Input(loanId, new DateTime(2026, 3, 1), 100));

        var error = await Should.ThrowAsync<UserFriendlyException>(() =>
            _appService.CreateOrEdit(Input(loanId, new DateTime(2026, 3, 1), 120)));

        error.Message.ShouldContain("Mar-2026");
    }

    // ------------------------------------------------------------------ recovery end date

    /// <summary>
    /// 1200 due from Jan 2026: 100 a month Jan-May (500), then 175 from June recovers the remaining 700 in four
    /// months, June-September. End 30 Sep 2026.
    /// </summary>
    [Fact]
    public async Task Should_Recalculate_The_Recovery_End_Date_From_The_Schedule_When_The_Setting_Is_On()
    {
        SetAutoUpdateRecoveryEndDate(true);
        var loanId = SeedLoan();

        await _appService.CreateOrEdit(Input(loanId, new DateTime(2026, 1, 1), 100));
        LoanEndDate(loanId).ShouldBe(new DateTime(2026, 12, 31));

        await _appService.CreateOrEdit(Input(loanId, new DateTime(2026, 6, 1), 175));
        LoanEndDate(loanId).ShouldBe(new DateTime(2026, 9, 30));
    }

    [Fact]
    public async Task Should_Walk_The_Recovery_End_Date_Back_When_A_Row_Is_Deleted()
    {
        SetAutoUpdateRecoveryEndDate(true);
        var loanId = SeedLoan();
        await _appService.CreateOrEdit(Input(loanId, new DateTime(2026, 1, 1), 100));
        await _appService.CreateOrEdit(Input(loanId, new DateTime(2026, 6, 1), 175));

        await _appService.Delete(new EntityDto(Rows(loanId)[1].Id));

        LoanEndDate(loanId).ShouldBe(new DateTime(2026, 12, 31));
    }

    [Fact]
    public async Task Should_Leave_The_Recovery_End_Date_Alone_When_The_Setting_Is_Off()
    {
        SetAutoUpdateRecoveryEndDate(false);
        var loanId = SeedLoan();

        await _appService.CreateOrEdit(Input(loanId, new DateTime(2026, 6, 1), 175));

        LoanEndDate(loanId).ShouldBe(SeededEndDate);
    }

    // ------------------------------------------------------------------ payroll retro date

    [Fact]
    public async Task A_New_Row_Should_Set_The_Retro_Date_To_Its_Effective_Date()
    {
        var loanId = SeedLoan();

        await _appService.CreateOrEdit(Input(loanId, new DateTime(2026, 4, 1), 100));

        EarliestMasterDataChange().ShouldBe(new DateTime(2026, 4, 1));
    }

    [Fact]
    public async Task A_Moved_Row_Should_Set_The_Retro_Date_To_The_Earlier_Of_Its_Two_Dates()
    {
        var loanId = SeedLoan();
        var rowId = SeedInstallment(loanId, new DateTime(2026, 4, 1), 100);

        var input = Input(loanId, new DateTime(2026, 8, 1), 100);
        input.Id = rowId;
        await _appService.CreateOrEdit(input);

        EarliestMasterDataChange().ShouldBe(new DateTime(2026, 4, 1));
    }

    [Fact]
    public async Task A_Changed_Amount_Should_Set_The_Retro_Date_To_The_Effective_Date()
    {
        var loanId = SeedLoan();
        var rowId = SeedInstallment(loanId, new DateTime(2026, 5, 1), 100);

        var input = Input(loanId, new DateTime(2026, 5, 1), 120);
        input.Id = rowId;
        await _appService.CreateOrEdit(input);

        EarliestMasterDataChange().ShouldBe(new DateTime(2026, 5, 1));
    }

    [Fact]
    public async Task A_Save_That_Changes_Nothing_Payroll_Reads_Should_Not_Set_A_Retro_Date()
    {
        var loanId = SeedLoan();
        var rowId = SeedInstallment(loanId, new DateTime(2026, 5, 1), 100);

        var input = Input(loanId, new DateTime(2026, 5, 1), 100, "Only the remark");
        input.Id = rowId;
        await _appService.CreateOrEdit(input);

        EarliestMasterDataChange().ShouldBe(default);
    }

    [Fact]
    public async Task A_Deleted_Row_Should_Set_The_Retro_Date_To_Its_Effective_Date()
    {
        var loanId = SeedLoan();
        var rowId = SeedInstallment(loanId, new DateTime(2026, 7, 1), 100);

        await _appService.Delete(new EntityDto(rowId));

        EarliestMasterDataChange().ShouldBe(new DateTime(2026, 7, 1));
    }

    // ------------------------------------------------------------------ the first row of a loan

    [Fact]
    public async Task A_Loan_Recorded_In_Force_Should_Get_Its_First_Row_From_The_Fixed_Amount()
    {
        await Resolve<IEmployeeLoansAppService>().CreateOrEdit(new CreateOrEditEmployeeLoanDto
        {
            EmployeeId = _employeeId,
            LoanTypeId = _loanTypeId,
            StartDate = new DateTime(2026, 1, 1),
            RecoveryStartDate = new DateTime(2026, 2, 20),
            EndDate = new DateTime(2026, 12, 31),
            ActualAmount = 1200,
            DueAmount = 1200,
            FixedAmount = 100
        });

        var loanId = UsingDbContext(c => c.Set<EmployeeLoan>().IgnoreQueryFilters().Single(l => l.EmployeeId == _employeeId).Id);
        var row = Rows(loanId).ShouldHaveSingleItem();
        row.StartDate.ShouldBe(new DateTime(2026, 2, 1));
        row.Amount.ShouldBe(100);
        row.Remark.ShouldBe(EmployeeLoanInstallmentService.InitialInstallmentRemark);
    }

    [Fact]
    public async Task A_Loan_Without_A_Fixed_Amount_Should_Get_No_Row()
    {
        await Resolve<IEmployeeLoansAppService>().CreateOrEdit(new CreateOrEditEmployeeLoanDto
        {
            EmployeeId = _employeeId,
            LoanTypeId = _loanTypeId,
            StartDate = new DateTime(2026, 1, 1),
            RecoveryStartDate = new DateTime(2026, 2, 1),
            EndDate = new DateTime(2026, 12, 31),
            ActualAmount = 1200,
            DueAmount = 1200
        });

        UsingDbContext(c => c.Set<EmployeeLoanInstallment>().Any()).ShouldBeFalse();
    }

    [Fact]
    public async Task Ensuring_The_First_Row_Twice_Should_Not_Add_A_Second()
    {
        var loanId = SeedLoan();
        var service = Resolve<IEmployeeLoanInstallmentService>();

        using (var unitOfWork = Resolve<Abp.Domain.Uow.IUnitOfWorkManager>().Begin())
        {
            var loan = Resolve<Abp.Domain.Repositories.IRepository<EmployeeLoan>>().GetAll().IgnoreQueryFilters().Single(l => l.Id == loanId);
            await service.EnsureInitialInstallmentAsync(loan);
            await service.EnsureInitialInstallmentAsync(loan);
            await unitOfWork.CompleteAsync();
        }

        Rows(loanId).ShouldHaveSingleItem().StartDate.ShouldBe(new DateTime(2026, 1, 1));
    }

    [Fact]
    public async Task Deleting_A_Loan_Should_Delete_Its_Schedule()
    {
        var loanId = SeedLoan();
        SeedInstallment(loanId, new DateTime(2026, 1, 1), 100);

        await Resolve<IEmployeeLoansAppService>().Delete(new EntityDto(loanId));

        Rows(loanId).ShouldBeEmpty();
    }

    // ------------------------------------------------------------------ helpers

    private static readonly DateTime SeededEndDate = new DateTime(2027, 6, 30);

    private static CreateOrEditEmployeeLoanInstallmentDto Input(int loanId, DateTime start, double amount, string remark = null) =>
        new CreateOrEditEmployeeLoanInstallmentDto { EmployeeLoanId = loanId, StartDate = start, Amount = amount, Remark = remark };

    private System.Collections.Generic.List<EmployeeLoanInstallment> Rows(int loanId) =>
        UsingDbContext(c => c.Set<EmployeeLoanInstallment>()
            .Where(r => r.EmployeeLoanId == loanId && !r.IsDeleted)
            .OrderBy(r => r.StartDate)
            .ToList());

    private DateTime LoanEndDate(int loanId) =>
        UsingDbContext(c => c.Set<EmployeeLoan>().IgnoreQueryFilters().Single(l => l.Id == loanId).EndDate);

    private DateTime EarliestMasterDataChange() =>
        UsingDbContext(c => c.Set<EmployeePayrollStatus>()
            .Where(s => s.EmployeeId == _employeeId)
            .Select(s => s.EarliestMasterDataChange)
            .FirstOrDefault());

    private void SetAutoUpdateRecoveryEndDate(bool enabled) =>
        UsingDbContext(c => c.Set<ManagePayrollSettings>().Add(new ManagePayrollSettings
        {
            TenantId = TenantId,
            AutoUpdateLoanRecoveryEndDate = enabled
        }));

    private int SeedLoan()
    {
        return UsingDbContext(c =>
        {
            var loan = c.Set<EmployeeLoan>().Add(new EmployeeLoan
            {
                TenantId = TenantId,
                EmployeeId = _employeeId,
                LoanTypeId = _loanTypeId,
                StartDate = new DateTime(2026, 1, 1),
                RecoveryStartDate = new DateTime(2026, 1, 10),
                EndDate = SeededEndDate,
                ActualAmount = 1200,
                DueAmount = 1200,
                BalanceDueAmount = 1200,
                FixedAmount = 100,
                IncludeInLoanPay = true
            }).Entity;
            c.SaveChanges();
            return loan.Id;
        });
    }

    private int SeedInstallment(int loanId, DateTime start, double amount)
    {
        return UsingDbContext(c =>
        {
            var row = c.Set<EmployeeLoanInstallment>().Add(new EmployeeLoanInstallment
            {
                TenantId = TenantId, EmployeeLoanId = loanId, StartDate = start, EndDate = OpenEnded, Amount = amount
            }).Entity;
            c.SaveChanges();
            return row.Id;
        });
    }

    private int CreateLoanType()
    {
        return UsingDbContext(c =>
        {
            var type = c.Set<LoanType>().Add(new LoanType
            {
                TenantId = TenantId, Code = Guid.NewGuid().ToString("N")[..6], NameEn = "Car Loan", NameAr = "قرض سيارة"
            }).Entity;
            c.SaveChanges();
            return type.Id;
        });
    }

    private int CreatePayrollArea()
    {
        PayrollArea area = null;
        UsingDbContext(c =>
        {
            var currency = c.Set<Currency>().Add(new Currency { TenantId = TenantId, Code = "KWD", NameEn = "KWD", NameAr = "KWD", DecimalCount = 3 }).Entity;
            var period = c.Set<PayrollPeriod>().Add(new PayrollPeriod { TenantId = TenantId, Code = "M", NameEn = "Monthly", NameAr = "Monthly" }).Entity;
            c.SaveChanges();

            area = c.Set<PayrollArea>().Add(new PayrollArea
            {
                TenantId = TenantId, Code = "PA1", Name = "Main", PayrollPeriodId = period.Id, CurrencyId = currency.Id, CutOffDay = 25
            }).Entity;
        });
        return area.Id;
    }

    private void CreateAssignment(int employeeId, int payrollAreaId) =>
        UsingDbContext(c => c.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
        {
            TenantId = TenantId, EmployeeId = employeeId, StartDate = JoiningDate, EndDate = OpenEnded, PayrollAreaId = payrollAreaId
        }));

    private int CreateEmployee(string code)
    {
        Employee employee = null;
        UsingDbContext(c =>
        {
            var nationality = c.Set<Nationality>().Add(new Nationality
            {
                TenantId = TenantId, Code = Guid.NewGuid().ToString("N")[..8], NameEn = "N", NameAr = "N"
            }).Entity;
            c.SaveChanges();

            employee = c.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId,
                Code = code,
                FirstNameEn = "F",
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = code,
                FullNameAr = code,
                BirthDate = new DateTime(1990, 1, 1),
                Gender = Gender.Male,
                NationalityId = nationality.Id,
                JoiningDate = JoiningDate,
            }).Entity;
        });
        return employee.Id;
    }
}
