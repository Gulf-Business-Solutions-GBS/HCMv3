using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.SystemSettings;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// What an exemption HR records does to payroll and to the loan, against legacy
/// <c>EmployeeLoanExemption.SetRetroForExemptionChange</c> and <c>OnDeleting</c>: the employee's retro date moves
/// back to the first of the exemption month (the earlier of the two months when it is moved), and the loan's
/// recovery end date is recomputed at once -- also with workflow off, when no approval ever runs.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeLoanExemptionsAppService_Retro_Tests : AppTestBase
{
    private const int TenantId = 1;
    private static readonly DateTime OpenEnded = new DateTime(9999, 12, 31);
    private static readonly DateTime JoiningDate = new DateTime(2020, 1, 1);

    // 1200 at 100 a month from January 2026 ends 31 Dec 2026; one exempted month ends it 31 Jan 2027.
    private static readonly DateTime ScheduledEnd = new DateTime(2026, 12, 31);
    private static readonly DateTime EndAfterOneSkip = new DateTime(2027, 1, 31);

    private readonly IEmployeeLoanExemptionsAppService _appService;
    private readonly int _employeeId;
    private readonly int _loanId;

    public EmployeeLoanExemptionsAppService_Retro_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeLoanExemptionsAppService>();

        _employeeId = CreateEmployee("LXR-001");
        CreateAssignment(_employeeId, CreatePayrollArea());
        _loanId = SeedLoan(CreateLoanType());
    }

    [Fact]
    public async Task A_Recorded_Exemption_Should_Set_The_Retro_Date_To_The_First_Of_Its_Month()
    {
        await _appService.CreateOrEdit(Input(new DateTime(2026, 6, 15)));

        EarliestMasterDataChange().ShouldBe(new DateTime(2026, 6, 1));
    }

    [Fact]
    public async Task A_Recorded_Exemption_Should_Push_The_Recovery_End_Date_Out_At_Once()
    {
        SetAutoUpdateRecoveryEndDate(true);

        await _appService.CreateOrEdit(Input(new DateTime(2026, 6, 15)));

        LoanEndDate().ShouldBe(EndAfterOneSkip);
    }

    [Fact]
    public async Task Moving_An_Exemption_Should_Set_The_Retro_Date_From_The_Earlier_Month()
    {
        var id = SeedExemption(new DateTime(2026, 7, 10));

        var input = Input(new DateTime(2026, 9, 20));
        input.Id = id;
        await _appService.CreateOrEdit(input);

        EarliestMasterDataChange().ShouldBe(new DateTime(2026, 7, 1));
    }

    [Fact]
    public async Task Editing_Only_The_Comments_Should_Not_Set_A_Retro_Date()
    {
        var id = SeedExemption(new DateTime(2026, 7, 10));

        var input = Input(new DateTime(2026, 7, 10));
        input.Id = id;
        input.Comments = "Only the comment";
        await _appService.CreateOrEdit(input);

        EarliestMasterDataChange().ShouldBe(default);
    }

    [Fact]
    public async Task Deleting_An_Exemption_Should_Set_The_Retro_Date_And_Walk_The_End_Date_Back()
    {
        SetAutoUpdateRecoveryEndDate(true);
        await _appService.CreateOrEdit(Input(new DateTime(2026, 8, 5)));
        LoanEndDate().ShouldBe(EndAfterOneSkip);
        var id = UsingDbContext(c => c.Set<EmployeeLoanExemption>().Single(e => e.EmployeeLoanId == _loanId).Id);

        await _appService.Delete(new EntityDto(id));

        LoanEndDate().ShouldBe(ScheduledEnd);
        EarliestMasterDataChange().ShouldBe(new DateTime(2026, 8, 1));
    }

    // ------------------------------------------------------------------ helpers

    private CreateOrEditEmployeeLoanExemptionDto Input(DateTime on) =>
        new CreateOrEditEmployeeLoanExemptionDto { EmployeeLoanId = _loanId, ExemptionDate = on };

    private DateTime LoanEndDate() =>
        UsingDbContext(c => c.Set<EmployeeLoan>().IgnoreQueryFilters().Single(l => l.Id == _loanId).EndDate);

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

    private int SeedExemption(DateTime on)
    {
        return UsingDbContext(c =>
        {
            var row = c.Set<EmployeeLoanExemption>().Add(new EmployeeLoanExemption
            {
                TenantId = TenantId, EmployeeId = _employeeId, EmployeeLoanId = _loanId, ExemptionDate = on
            }).Entity;
            c.SaveChanges();
            return row.Id;
        });
    }

    private int SeedLoan(int loanTypeId)
    {
        return UsingDbContext(c =>
        {
            var loan = c.Set<EmployeeLoan>().Add(new EmployeeLoan
            {
                TenantId = TenantId,
                EmployeeId = _employeeId,
                LoanTypeId = loanTypeId,
                StartDate = new DateTime(2026, 1, 1),
                RecoveryStartDate = new DateTime(2026, 1, 10),
                EndDate = ScheduledEnd,
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
