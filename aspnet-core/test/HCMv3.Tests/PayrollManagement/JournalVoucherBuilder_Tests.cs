using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Costing;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// Balancing, sign-convention and cost-element-mapping coverage for the ONE shared JV builder that both the
/// costing-file and post-JV screens use. Legacy reference:
/// <c>HCM10.Web/UserControls/PayrollManagement/uscCostingFile.ascx.cs</c> lines 1760-2174.
/// </summary>
// ReSharper disable once InconsistentNaming
public class JournalVoucherBuilder_Tests : CostingTestBase
{
    private const string AreaName = "GBSIT";
    private static readonly DateTime PayInDate = new DateTime(2026, 4, 30);
    private static readonly DateTime PostingDate = new DateTime(2026, 5, 5);

    private int _tenantId;
    private int _employeeId;
    private int _ledgerAccountId;
    private int _offsetAccountId;
    private int _bankAccountId;

    private void Seed()
    {
        _tenantId = AbpSession.TenantId ?? 1;

        var currency = new Currency
        {
            TenantId = _tenantId, Code = "KWD", NameEn = "Kuwaiti Dinar", NameAr = "دينار",
            CurrencyShortFormEn = "KWD", CurrencyShortFormAr = "د.ك", DecimalCount = 3
        };
        Context.Currencies.Add(currency);

        var ledgerType = new AccountType { TenantId = _tenantId, Code = "L", NameEn = "Ledger", NameAr = "Ledger" };
        var bankType = new AccountType { TenantId = _tenantId, Code = "B", NameEn = "Bank", NameAr = "Bank" };
        Context.AccountTypes.AddRange(ledgerType, bankType);
        Context.SaveChanges();

        var ledgerAccount = new AccountInfo
        { TenantId = _tenantId, Code = "7101", NameEn = "Salary Expense", NameAr = "Salary", AccountTypeId = ledgerType.Id };
        var offsetAccount = new AccountInfo
        { TenantId = _tenantId, Code = "2201", NameEn = "Provision Liability", NameAr = "Prov", AccountTypeId = ledgerType.Id };
        var bankAccount = new AccountInfo
        { TenantId = _tenantId, Code = "1001", NameEn = "Payroll Bank", NameAr = "Bank", AccountTypeId = bankType.Id };
        Context.AccountInfos.AddRange(ledgerAccount, offsetAccount, bankAccount);

        // Seven distinct cost elements, one per slot, so the slot -> GL-dimension mapping is unambiguous.
        for (var slot = 1; slot <= 7; slot++)
        {
            Context.CostElements.Add(new CostElement
            { TenantId = _tenantId, Code = "CE" + slot, NameEn = "Cost Element " + slot, NameAr = "CE" + slot });
        }

        Context.SaveChanges();

        _ledgerAccountId = ledgerAccount.Id;
        _offsetAccountId = offsetAccount.Id;
        _bankAccountId = bankAccount.Id;

        var fiCompany = Context.CostElements.First(c => c.Code == "CE1");

        // PayrollArea.PayrollPeriodId and Employee.NationalityId are non-nullable FKs, so their
        // referenced rows must exist before the Sqlite FK constraints will accept the inserts.
        var payrollPeriod = new PayrollPeriod
        { TenantId = _tenantId, Code = "Jan-Dec", NameEn = "Jan-Dec", NameAr = "Jan-Dec" };
        Context.PayrollPeriods.Add(payrollPeriod);

        var nationality = new Nationality
        { TenantId = _tenantId, Code = "KW", NameEn = "Kuwaiti", NameAr = "كويتي" };
        Context.Nationalities.Add(nationality);
        Context.SaveChanges();

        Context.PayrollAreas.Add(new PayrollArea
        {
            TenantId = _tenantId,
            Code = "PA1",
            Name = AreaName,
            PayrollPeriodId = payrollPeriod.Id,
            CurrencyId = currency.Id,
            CutOffDay = 25,
            FICompanyId = fiCompany.Id,
            FIBankId = bankAccount.Id
        });

        Context.AXBatches.Add(new AXBatch
        { TenantId = _tenantId, JournalBatchNumber = "JB-0001", Name = "Batch", Description = "Batch" });

        var employee = new Employee
        {
            TenantId = _tenantId, Code = "E100",
            FirstNameEn = "Test", LastNameEn = "Employee",
            FirstNameAr = "تجربة", LastNameAr = "موظف",
            FullNameEn = "Test Employee", FullNameAr = "تجربة موظف",
            NationalityId = nationality.Id
        };
        Context.Employees.Add(employee);
        Context.SaveChanges();

        _employeeId = employee.Id;

        var basicPay = new EmployeeBasicPay
        {
            TenantId = _tenantId, EmployeeId = _employeeId,
            StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2099, 12, 31), Amount = 1000
        };
        Context.EmployeeBasicPays.Add(basicPay);
        Context.SaveChanges();
    }

    private int AddWageType(string code, string name, BillingType billingType, int payrollOrder = 1)
    {
        var wageType = new WageType
        {
            TenantId = _tenantId, Code = code, NameEn = name, NameAr = name,
            BillingType = billingType, PayrollOrder = payrollOrder, IncludeInFIPosting = true, Status = true
        };
        Context.WageTypes.Add(wageType);
        Context.SaveChanges();
        return wageType.Id;
    }

    private int AddCostRegister()
    {
        var register = new CostRegister
        {
            TenantId = _tenantId, EmployeeId = _employeeId,
            Period = PayInDate, PayrollRunType = PayrollRunType.Regular
        };
        Context.CostRegisters.Add(register);
        Context.SaveChanges();
        return register.Id;
    }

    private void AddDetail(int costRegisterId, int wageTypeId, double amount, bool allSevenSlots = true)
    {
        int? Slot(int n) => allSevenSlots
            ? Context.CostElements.First(c => c.Code == "CE" + n).Id
            : (int?)null;

        Context.CostRegisterDetails.Add(new CostRegisterDetail
        {
            TenantId = _tenantId,
            CostRegisterId = costRegisterId,
            WageTypeId = wageTypeId,
            AccountInfoId = _ledgerAccountId,
            CostElement1Id = Slot(1),
            CostElement2Id = Slot(2),
            CostElement3Id = Slot(3),
            CostElement4Id = Slot(4),
            CostElement5Id = Slot(5),
            CostElement6Id = Slot(6),
            CostElement7Id = Slot(7),
            Amount = amount
        });
        Context.SaveChanges();
    }

    private static CostingRunInput Input() => new CostingRunInput
    {
        PayrollAreaName = AreaName,
        EmployeeCode = "",
        PayInDate = PayInDate,
        PostingDate = PostingDate,
        PayrollRunType = PayrollRunType.Regular
    };

    // -----------------------------------------------------------------------------------------------
    // BALANCING -- the highest-priority invariant. A JV that does not balance is silent data corruption.
    // -----------------------------------------------------------------------------------------------

    [Fact]
    public async Task Should_Balance_Debits_And_Credits_For_Earnings_And_Deductions()
    {
        Seed();
        var earning = AddWageType("1001", "Basic Salary", BillingType.Earning, 1);
        var deduction = AddWageType("2001", "Loan Deduction", BillingType.Deduction, 2);

        var register = AddCostRegister();
        AddDetail(register, earning, 1000);
        AddDetail(register, deduction, 250);

        var result = await CreateBuilder(ResolverFor(AreaName, _employeeId)).BuildAsync(Input());

        // 1000 debit, 250 credit, plus a Net Pay credit of (1000 - 250) = 750 -> 1000 == 1000.
        result.TotalDebit.ShouldBe(1000);
        result.TotalCredit.ShouldBe(1000);
        result.IsBalanced.ShouldBeTrue();

        var netPay = result.Lines.Single(l => l.Description == JournalVoucherBuilder.NetPayDescription);
        netPay.Credit.ShouldBe(750);
        netPay.Debit.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Balance_When_Provision_Generates_Its_Own_Offset_Line()
    {
        Seed();
        var earning = AddWageType("1001", "Basic Salary", BillingType.Earning, 1);
        var provision = AddWageType("1066", "Annual Leave Provision", BillingType.Earning, 2);

        var register = AddCostRegister();
        AddDetail(register, earning, 900);
        AddDetail(register, provision, 100);

        // Wage code 1066 is in the hardcoded provision list, so it must produce a swapped offset line
        // even though its configured BillingType is Earning.
        Context.EmployeeWageAccounts.Add(new EmployeeWageAccount
        {
            TenantId = _tenantId, EmployeeId = _employeeId, WageTypeId = provision,
            StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2099, 12, 31),
            AccountInfoId = _ledgerAccountId, OffsetAccountInfoId = _offsetAccountId,
            CostPercentage = 100, MergeOffsetAccount = false
        });
        Context.SaveChanges();

        var result = await CreateBuilder(ResolverFor(AreaName, _employeeId)).BuildAsync(Input());

        var offsetLine = result.Lines.Single(l => l.LedgerDimensionMainAccount == "2201");
        offsetLine.Credit.ShouldBe(100);
        offsetLine.Debit.ShouldBe(0);

        // The provision pair self-balances (100 Dr / 100 Cr), so Net Pay reflects ONLY the earning.
        var netPay = result.Lines.Single(l => l.Description == JournalVoucherBuilder.NetPayDescription);
        netPay.Credit.ShouldBe(900);

        result.TotalDebit.ShouldBe(result.TotalCredit);
        result.IsBalanced.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Exclude_Provision_Codes_From_NetPay_So_The_Jv_Still_Balances()
    {
        Seed();
        var earning = AddWageType("1001", "Basic Salary", BillingType.Earning, 1);
        var indemnityProvision = AddWageType("1067", "Indemnity Provision", BillingType.Provision, 2);

        var register = AddCostRegister();
        AddDetail(register, earning, 500);
        AddDetail(register, indemnityProvision, 300);

        Context.EmployeeWageAccounts.Add(new EmployeeWageAccount
        {
            TenantId = _tenantId, EmployeeId = _employeeId, WageTypeId = indemnityProvision,
            StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2099, 12, 31),
            AccountInfoId = _ledgerAccountId, OffsetAccountInfoId = _offsetAccountId,
            CostPercentage = 100, MergeOffsetAccount = false
        });
        Context.SaveChanges();

        var result = await CreateBuilder(ResolverFor(AreaName, _employeeId)).BuildAsync(Input());

        // If 1067 leaked into the Net Pay total (the uscPostJV bug) this would be 800, not 500,
        // and the JV would not balance.
        var netPay = result.Lines.Single(l => l.Description == JournalVoucherBuilder.NetPayDescription);
        netPay.Credit.ShouldBe(500);
        result.IsBalanced.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Not_Emit_NetPay_Line_When_Net_Is_Exactly_Zero()
    {
        Seed();
        var earning = AddWageType("1001", "Basic Salary", BillingType.Earning, 1);
        var deduction = AddWageType("2001", "Full Recovery", BillingType.Deduction, 2);

        var register = AddCostRegister();
        AddDetail(register, earning, 400);
        AddDetail(register, deduction, 400);

        var result = await CreateBuilder(ResolverFor(AreaName, _employeeId)).BuildAsync(Input());

        // Legacy line 2125 skips the net line entirely when the total is 0 -- the JV already balances.
        result.Lines.ShouldNotContain(l => l.Description == JournalVoucherBuilder.NetPayDescription);
        result.TotalDebit.ShouldBe(400);
        result.TotalCredit.ShouldBe(400);
        result.IsBalanced.ShouldBeTrue();
    }

    // -----------------------------------------------------------------------------------------------
    // COST-ELEMENT SLOT MAPPING across all 7 slots.
    // -----------------------------------------------------------------------------------------------

    [Fact]
    public async Task Should_Map_All_Seven_Cost_Element_Slots_To_Their_Legacy_Gl_Dimensions()
    {
        Seed();
        var earning = AddWageType("1001", "Basic Salary", BillingType.Earning);

        var register = AddCostRegister();
        AddDetail(register, earning, 1000);

        var result = await CreateBuilder(ResolverFor(AreaName, _employeeId)).BuildAsync(Input());
        var line = result.Lines.First();

        // Legacy uscCostingFile.ascx.cs lines 1884, 1896-1901. The mapping is NOT slot-index order.
        line.CompanyAccounts.ShouldBe("CE1");             // slot 1 -> Company accounts (FI company)
        line.LedgerDimensionLocation.ShouldBe("CE2");     // slot 2 -> LedgerDimension.Location
        line.LedgerDimensionBrand.ShouldBe("CE3");        // slot 3 -> LedgerDimension.Brand
        line.LedgerDimensionDepartment.ShouldBe("CE4");   // slot 4 -> LedgerDimension.Department
        line.LedgerDimensionEmployees.ShouldBe("CE5");    // slot 5 -> LedgerDimension.Employees
        line.DefaultDimensionEmployees.ShouldBe("CE6");   // slot 6 -> DefaultDimension.Employees
        line.DefaultDimensionLocation.ShouldBe("CE7");    // slot 7 -> DefaultDimension.Location
    }

    [Fact]
    public async Task Should_Leave_Dimensions_Blank_When_Cost_Element_Slots_Are_Null()
    {
        Seed();
        var earning = AddWageType("1001", "Basic Salary", BillingType.Earning);

        var register = AddCostRegister();
        AddDetail(register, earning, 1000, allSevenSlots: false);

        var result = await CreateBuilder(ResolverFor(AreaName, _employeeId)).BuildAsync(Input());
        var line = result.Lines.First();

        line.CompanyAccounts.ShouldBeNull();
        line.LedgerDimensionLocation.ShouldBeNull();
        line.DefaultDimensionLocation.ShouldBeNull();

        // A missing cost element must not stop the amount from posting.
        line.Debit.ShouldBe(1000);
    }

    // -----------------------------------------------------------------------------------------------
    // SIGN CONVENTION and ROUNDING.
    // -----------------------------------------------------------------------------------------------

    [Fact]
    public async Task Should_Round_Each_Line_To_Three_Decimal_Places()
    {
        Seed();
        var earning = AddWageType("1001", "Basic Salary", BillingType.Earning);

        var register = AddCostRegister();
        AddDetail(register, earning, 333.33333333);

        var result = await CreateBuilder(ResolverFor(AreaName, _employeeId)).BuildAsync(Input());

        // Legacy line 1904: Math.Round(amount, 3).
        result.Lines.First().Debit.ShouldBe(333.333);
    }

    [Fact]
    public async Task Should_Keep_Negative_Amount_As_A_Negative_Debit_Not_Flip_It_To_Credit()
    {
        Seed();
        var earning = AddWageType("1001", "Salary Adjustment", BillingType.Earning);

        var register = AddCostRegister();
        AddDetail(register, earning, -150);

        var result = await CreateBuilder(ResolverFor(AreaName, _employeeId)).BuildAsync(Input());
        var line = result.Lines.First(l => l.Description == "Salary Adjustment");

        // Legacy chooses the side from BillingType only; it never inspects the sign.
        line.Debit.ShouldBe(-150);
        line.Credit.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Tolerate_Sub_Unit_Rounding_Remainder_Because_Legacy_Balances_On_Whole_Units()
    {
        Seed();
        var earning = AddWageType("1001", "Basic Salary", BillingType.Earning);

        var register = AddCostRegister();

        // Three thirds of 100 -> 33.333 each; the 0.001 remainder never reaches the whole-unit totals.
        AddDetail(register, earning, 33.3333);
        AddDetail(register, earning, 33.3333);
        AddDetail(register, earning, 33.3333);

        var result = await CreateBuilder(ResolverFor(AreaName, _employeeId)).BuildAsync(Input());

        // Raw sums differ by a sub-unit remainder...
        var rawDebit = result.Lines.Sum(l => l.Debit);
        var rawCredit = result.Lines.Sum(l => l.Credit);
        Math.Abs(rawDebit - rawCredit).ShouldBeLessThan(1.0);

        // ...but legacy's whole-unit rounding (line 1719-1722) still reports the JV as balanced.
        result.TotalDebit.ShouldBe(result.TotalCredit);
        result.IsBalanced.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Skip_Lines_With_Neither_Debit_Nor_Credit()
    {
        Seed();
        var timeType = AddWageType("3001", "Worked Hours", BillingType.TimeType);

        var register = AddCostRegister();
        AddDetail(register, timeType, 160);

        var result = await CreateBuilder(ResolverFor(AreaName, _employeeId)).BuildAsync(Input());

        // TimeType maps to neither side (legacy lines 1904-1905), so the gate at line 1945 drops it,
        // and with no rows there is no Net Pay line either.
        result.Lines.ShouldBeEmpty();
    }

    // -----------------------------------------------------------------------------------------------
    // MERGE OFFSET ACCOUNT.
    // -----------------------------------------------------------------------------------------------

    [Fact]
    public async Task Should_Accumulate_Offset_Amounts_Into_One_Line_When_MergeOffsetAccount_Is_Set()
    {
        Seed();
        var provision = AddWageType("1066", "Annual Leave Provision", BillingType.Earning);

        var register = AddCostRegister();

        // Two details for the SAME employee + wage type + identical cost-element slots.
        AddDetail(register, provision, 60);
        AddDetail(register, provision, 40);

        Context.EmployeeWageAccounts.Add(new EmployeeWageAccount
        {
            TenantId = _tenantId, EmployeeId = _employeeId, WageTypeId = provision,
            StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2099, 12, 31),
            AccountInfoId = _ledgerAccountId, OffsetAccountInfoId = _offsetAccountId,
            CostElement1Id = Context.CostElements.First(c => c.Code == "CE1").Id,
            CostElement2Id = Context.CostElements.First(c => c.Code == "CE2").Id,
            CostElement3Id = Context.CostElements.First(c => c.Code == "CE3").Id,
            CostElement4Id = Context.CostElements.First(c => c.Code == "CE4").Id,
            CostElement5Id = Context.CostElements.First(c => c.Code == "CE5").Id,
            CostElement6Id = Context.CostElements.First(c => c.Code == "CE6").Id,
            CostElement7Id = Context.CostElements.First(c => c.Code == "CE7").Id,
            CostPercentage = 100, MergeOffsetAccount = true
        });
        Context.SaveChanges();

        var result = await CreateBuilder(ResolverFor(AreaName, _employeeId)).BuildAsync(Input());

        var mergedLines = result.Lines.Where(l => l.IsMerged).ToList();
        mergedLines.Count.ShouldBe(1);
        mergedLines[0].Credit.ShouldBe(100);
        mergedLines[0].LedgerDimensionMainAccount.ShouldBe("2201");

        result.IsBalanced.ShouldBeTrue();
    }

    // -----------------------------------------------------------------------------------------------
    // PRECONDITIONS.
    // -----------------------------------------------------------------------------------------------

    [Fact]
    public async Task Should_Throw_When_No_AxBatch_Row_Exists()
    {
        Seed();
        Context.AXBatches.RemoveRange(Context.AXBatches);
        Context.SaveChanges();

        var builder = CreateBuilder(ResolverFor(AreaName, _employeeId));

        await Should.ThrowAsync<UserFriendlyException>(() => builder.BuildAsync(Input()));
    }

    [Fact]
    public async Task Should_Throw_When_Payroll_Area_Has_No_Fi_Bank()
    {
        Seed();
        var area = Context.PayrollAreas.First();
        area.FIBankId = null;
        Context.SaveChanges();

        var builder = CreateBuilder(ResolverFor(AreaName, _employeeId));

        await Should.ThrowAsync<UserFriendlyException>(() => builder.BuildAsync(Input()));
    }

    [Fact]
    public async Task Should_Exclude_Net_Pay_Wage_Code_9000_From_The_Register_Query()
    {
        Seed();
        var earning = AddWageType("1001", "Basic Salary", BillingType.Earning, 1);
        var netPayCode = AddWageType("9000", "Net Pay Wage", BillingType.Earning, 2);

        var register = AddCostRegister();
        AddDetail(register, earning, 700);
        AddDetail(register, netPayCode, 700);

        var result = await CreateBuilder(ResolverFor(AreaName, _employeeId)).BuildAsync(Input());

        // Legacy line 1790 filters WageType.Code != "9000"; the balancing line is synthesised instead.
        result.Lines.ShouldNotContain(l => l.Description == "Net Pay Wage");
        result.TotalDebit.ShouldBe(700);
        result.IsBalanced.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Return_Empty_When_No_Employee_Belongs_To_The_Payroll_Area()
    {
        Seed();
        var earning = AddWageType("1001", "Basic Salary", BillingType.Earning);
        AddDetail(AddCostRegister(), earning, 1000);

        var result = await CreateBuilder(ResolverFor(AreaName)).BuildAsync(Input());

        result.Lines.ShouldBeEmpty();
        result.IsBalanced.ShouldBeTrue();
    }
}
