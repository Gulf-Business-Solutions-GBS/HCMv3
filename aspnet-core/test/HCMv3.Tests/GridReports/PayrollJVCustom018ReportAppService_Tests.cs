using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.PayrollReport;
using HCMv3.GridReports.PayrollReport.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Payroll JV (Custom 018) grid report (legacy <c>uscRptPayrollJV_Custom018.ascx</c>,
/// <c>Table1()</c> lines 530-911).
///
/// This report is a JOURNAL VOUCHER: a fixed list of 13 general-ledger lines whose amounts are
/// aggregates of the month's salary register. The tests therefore pin the LINE SET and the AMOUNT
/// FORMULAS rather than row-per-employee filtering, plus the usual month-boundary and export rules.
/// </summary>
// ReSharper disable once InconsistentNaming
public class PayrollJVCustom018ReportAppService_Tests : AppTestBase
{
    private readonly IPayrollJVCustom018ReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public PayrollJVCustom018ReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IPayrollJVCustom018ReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(string code)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Nationalities.Add(new Nationality
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
                FirstNameEn = "F" + code,
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "Full " + code,
                FullNameAr = "عربي " + code,
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                NationalityId = nationality.Id,
                Status = EmployeeStatus.Active
            }).Entity;
        });

        return employee;
    }

    private WageType CreateWageType(string code, BillingType billingType = BillingType.Earning)
    {
        WageType wageType = null;
        UsingDbContext(context =>
        {
            wageType = context.Set<WageType>().Add(new WageType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = "WT " + code,
                NameAr = "WT " + code,
                ShortNameEn = code,
                ShortNameAr = code,
                BillingType = billingType,
                PayrollOrder = 1,
                Status = true
            }).Entity;
        });

        return wageType;
    }

    private SalaryRegister CreateRegister(
        Employee employee,
        DateTime forPeriod,
        DateTime? inPeriod = null,
        PayrollRunType runType = PayrollRunType.Regular)
    {
        SalaryRegister register = null;
        UsingDbContext(context =>
        {
            register = context.Set<SalaryRegister>().Add(new SalaryRegister
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                ForPeriod = forPeriod,
                InPeriod = inPeriod ?? forPeriod,
                PayrollRunType = runType,
                PayrollResultType = PayrollResultType.Current,
                OffCyclePaymentDate = forPeriod
            }).Entity;
        });

        return register;
    }

    private void CreateDetail(SalaryRegister register, WageType wageType, double amount)
    {
        UsingDbContext(context =>
        {
            context.Set<SalaryRegisterDetail>().Add(new SalaryRegisterDetail
            {
                TenantId = TenantId,
                SalaryRegisterId = register.Id,
                WageTypeId = wageType.Id,
                Amount = amount,
                Rate = 0,
                Number = 0
            });
        });
    }

    private void CreateBank(Employee employee, DateTime start, DateTime end, PaymentType paymentType)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeBank>().Add(new EmployeeBank
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                StartDate = start,
                EndDate = end,
                PaymentType = paymentType,
                AccountNum = "ACC" + employee.Id
            });
        });
    }

    private static GetPayrollJVCustom018ReportInput Input(
        int month,
        int year,
        PayrollRunType? runType = null,
        int? payrollAreaId = null,
        int maxResultCount = 100,
        int skipCount = 0)
    {
        return new GetPayrollJVCustom018ReportInput
        {
            Month = month,
            Year = year,
            PayrollRunType = runType,
            PayrollAreaId = payrollAreaId,
            MaxResultCount = maxResultCount,
            SkipCount = skipCount
        };
    }

    /// <summary>
    /// Legacy <c>GetTable()</c> lines 460-461 build the period from month/year as
    /// [1st .. DaysInMonth], and <c>Table1()</c> matches <c>ForPeriod == EndDate</c> exactly -- so only
    /// a register stamped on the LAST day of the requested month contributes.
    /// </summary>
    [Fact]
    public async Task Should_Only_Aggregate_Registers_Stamped_On_The_Requested_Month_End()
    {
        var netPay = CreateWageType("9000");

        CreateDetail(CreateRegister(CreateEmployee("JV18-EOM"), new DateTime(2024, 6, 30)), netPay, 100);
        CreateDetail(CreateRegister(CreateEmployee("JV18-MID"), new DateTime(2024, 6, 15)), netPay, 500);
        CreateDetail(CreateRegister(CreateEmployee("JV18-JUL"), new DateTime(2024, 7, 31)), netPay, 700);

        var result = await _appService.GetPayrollJVCustom018Report(Input(6, 2024));

        // "Cash Salary" (15001) sums net pay of every employee with no bank record: only JV18-EOM is in
        // the requested period, so exactly its 100 shows up.
        var cashSalary = result.Items.Single(i => i.Description.StartsWith("Cash Salary"));
        cashSalary.Amount.ShouldBe(100);
        cashSalary.Date.ShouldBe(new DateTime(2024, 6, 30));
    }

    /// <summary>
    /// Legacy line 573: <c>ForPeriod == InPeriod</c>, so a retro run paid in a later month never enters
    /// the voucher.
    /// </summary>
    [Fact]
    public async Task Should_Exclude_Retro_Runs_Where_InPeriod_Differs_From_ForPeriod()
    {
        var netPay = CreateWageType("9000");

        CreateDetail(CreateRegister(CreateEmployee("JV18-IN"), new DateTime(2024, 6, 30)), netPay, 100);
        CreateDetail(
            CreateRegister(CreateEmployee("JV18-RETRO"), new DateTime(2024, 6, 30), new DateTime(2024, 7, 31)),
            netPay,
            900);

        var result = await _appService.GetPayrollJVCustom018Report(Input(6, 2024));

        result.Items.Single(i => i.Description.StartsWith("Cash Salary")).Amount.ShouldBe(100);
    }

    [Fact]
    public async Task Should_Filter_By_Payroll_Run_Type_When_Supplied()
    {
        var netPay = CreateWageType("9000");

        CreateDetail(CreateRegister(CreateEmployee("JV18-RT-REG"), new DateTime(2024, 6, 30)), netPay, 100);
        CreateDetail(
            CreateRegister(CreateEmployee("JV18-RT-OFF"), new DateTime(2024, 6, 30), runType: PayrollRunType.OffCycle),
            netPay,
            250);

        var result = await _appService.GetPayrollJVCustom018Report(
            Input(6, 2024, runType: PayrollRunType.OffCycle));

        result.Items.Single(i => i.Description.StartsWith("Cash Salary")).Amount.ShouldBe(250);
    }

    /// <summary>
    /// LEGACY DEFECT, REPRODUCED ON PURPOSE. The panel has a Payroll Area combo
    /// (uscRptPayrollJV_Custom018.ascx line 30) and legacy stores the pick in
    /// <c>Session[payrollArea]</c> and logs it in <c>GetParameterStr()</c> -- but <c>Table1()</c> never
    /// reads it back, so the combo filters nothing. This test exists so nobody "fixes" it into a real
    /// filter without a customer decision.
    /// </summary>
    [Fact]
    public async Task Should_Not_Filter_By_Payroll_Area_Because_Legacy_Never_Reads_The_Selection()
    {
        var netPay = CreateWageType("9000");
        CreateDetail(CreateRegister(CreateEmployee("JV18-PA"), new DateTime(2024, 6, 30)), netPay, 100);

        // A payroll area id that matches nothing at all.
        var result = await _appService.GetPayrollJVCustom018Report(Input(6, 2024, payrollAreaId: 999999));

        result.Items.Single(i => i.Description.StartsWith("Cash Salary")).Amount.ShouldBe(100);
    }

    /// <summary>
    /// The voucher is a FIXED list of 13 lines (legacy lines 676-887), emitted even when the period has
    /// no data at all -- legacy added every row unconditionally, with a zero amount.
    /// </summary>
    [Fact]
    public async Task Should_Always_Emit_The_Thirteen_Fixed_Voucher_Lines()
    {
        var result = await _appService.GetPayrollJVCustom018Report(Input(1, 2001));

        result.TotalCount.ShouldBe(13);

        result.Items.Select(i => i.GLAccount).ShouldBe(new[]
        {
            "60001", "60011", "60037", "60036", "15017", "15001", "15001", "15001", "15001",
            "60007", "15017", "60008", "15018"
        });

        result.Items.ShouldAllBe(i => i.Amount == 0);
        result.Items.ShouldAllBe(i => i.UsedForReimbursableExpenses == "FALSE");
        result.Items.ShouldAllBe(i => i.ConsolidatedTransaction == "FALSE");
    }

    /// <summary>Legacy line 680: Description is the caption plus the month end in "MMMM yyyy".</summary>
    [Fact]
    public async Task Should_Suffix_Every_Description_With_The_Month_End_In_MMMM_yyyy()
    {
        var result = await _appService.GetPayrollJVCustom018Report(Input(6, 2024));

        result.Items.First().Description.ShouldBe("Salary Clinic June 2024");
        result.Items.Single(i => i.GLAccount == "60008").Description.ShouldBe("Indemnity Pay Exp June 2024");
    }

    /// <summary>
    /// Legacy lines 748-761: the "Leave Salary" line (15017) credits ANNUAL LEAVE for every employee,
    /// summing wage types 1003, 7003, 1052 and 7052.
    /// </summary>
    [Fact]
    public async Task Should_Sum_All_Four_Annual_Leave_Wage_Types_Into_The_Leave_Salary_Line()
    {
        var register = CreateRegister(CreateEmployee("JV18-AL"), new DateTime(2024, 6, 30));

        CreateDetail(register, CreateWageType("1003"), 10);
        CreateDetail(register, CreateWageType("7003"), 20);
        CreateDetail(register, CreateWageType("1052"), 30);
        CreateDetail(register, CreateWageType("7052"), 40);
        // Not an annual-leave wage type -- must not be counted.
        CreateDetail(register, CreateWageType("9000"), 500);

        var result = await _appService.GetPayrollJVCustom018Report(Input(6, 2024));

        // The first 15017 line is "Leave Salary"; the second is the Leave Pay Exp credit.
        result.Items.Single(i => i.Description.StartsWith("Leave Salary")).Amount.ShouldBe(100);
    }

    /// <summary>
    /// Legacy lines 827-887: the two provision pairs each post the same amount twice with opposite
    /// signs (60007/15017 for the leave provision, 60008/15018 for the indemnity provision).
    /// </summary>
    [Fact]
    public async Task Should_Post_Each_Provision_As_An_Offsetting_Debit_And_Credit_Pair()
    {
        var register = CreateRegister(CreateEmployee("JV18-PROV"), new DateTime(2024, 6, 30));

        CreateDetail(register, CreateWageType("3001", BillingType.Provision), 60);
        CreateDetail(register, CreateWageType("3000", BillingType.Provision), 80);

        var result = await _appService.GetPayrollJVCustom018Report(Input(6, 2024));

        result.Items.Single(i => i.GLAccount == "60007").Amount.ShouldBe(60);
        result.Items.Single(i => i.GLAccount == "15017" && i.Description.StartsWith("Leave Pay Exp"))
            .Amount.ShouldBe(-60);
        result.Items.Single(i => i.GLAccount == "60008").Amount.ShouldBe(80);
        result.Items.Single(i => i.GLAccount == "15018").Amount.ShouldBe(-80);
    }

    /// <summary>
    /// Legacy lines 661-672: an employee is "bank paid" when an <c>EmployeeBank</c> row of payment type
    /// BANK covers the month end; every OTHER employee counts as cash paid, including one whose only
    /// bank row is of type CASH.
    /// </summary>
    [Fact]
    public async Task Should_Split_Net_Pay_Between_Bank_Paid_And_Cash_Paid_Employees()
    {
        var netPay = CreateWageType("9000");

        var bankEmployee = CreateEmployee("JV18-BANK");
        CreateBank(bankEmployee, new DateTime(2024, 1, 1), new DateTime(2099, 12, 31), PaymentType.Bank);
        CreateDetail(CreateRegister(bankEmployee, new DateTime(2024, 6, 30)), netPay, 300);

        var cashEmployee = CreateEmployee("JV18-CASH");
        CreateBank(cashEmployee, new DateTime(2024, 1, 1), new DateTime(2099, 12, 31), PaymentType.Cash);
        CreateDetail(CreateRegister(cashEmployee, new DateTime(2024, 6, 30)), netPay, 70);

        var result = await _appService.GetPayrollJVCustom018Report(Input(6, 2024));

        result.Items.Single(i => i.Description.StartsWith("Paid Salary")).Amount.ShouldBe(300);
        result.Items.Single(i => i.Description.StartsWith("KSEC Salary")).Amount.ShouldBe(-300);
        result.Items.Single(i => i.Description.StartsWith("Cash Salary")).Amount.ShouldBe(70);
        result.Items.Single(i => i.Description.StartsWith("Cash KSEC Salary")).Amount.ShouldBe(-70);
    }

    /// <summary>
    /// ACCOUNTING DEFECT PINNED, NOT FIXED. The four 15001 lines are two exact self-cancelling pairs, so
    /// account 15001 always nets to zero however much salary was paid. Reproduced from legacy lines
    /// 763-825 -- see the app service remarks; it needs a customer decision before this voucher is used
    /// to post.
    /// </summary>
    [Fact]
    public async Task Should_Reproduce_The_Legacy_Defect_Where_Account_15001_Always_Nets_To_Zero()
    {
        var netPay = CreateWageType("9000");

        var bankEmployee = CreateEmployee("JV18-BAL-BANK");
        CreateBank(bankEmployee, new DateTime(2024, 1, 1), new DateTime(2099, 12, 31), PaymentType.Bank);
        CreateDetail(CreateRegister(bankEmployee, new DateTime(2024, 6, 30)), netPay, 1000);

        CreateDetail(CreateRegister(CreateEmployee("JV18-BAL-CASH"), new DateTime(2024, 6, 30)), netPay, 400);

        var result = await _appService.GetPayrollJVCustom018Report(Input(6, 2024));

        result.Items.Where(i => i.GLAccount == "15001").Sum(i => i.Amount).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Page_The_Voucher_Lines()
    {
        var secondPage = await _appService.GetPayrollJVCustom018Report(
            Input(6, 2024, maxResultCount: 2, skipCount: 2));

        secondPage.TotalCount.ShouldBe(13);
        secondPage.Items.Count.ShouldBe(2);
        secondPage.Items.Select(i => i.LineNumber).ShouldBe(new[] { 3, 4 });
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var netPay = CreateWageType("9000");
        CreateDetail(CreateRegister(CreateEmployee("JV18-EXP"), new DateTime(2024, 6, 30)), netPay, 100);

        // Page size of 1 -- the export must ignore it and still return every voucher line.
        var file = await _appService.GetPayrollJVCustom018ReportToExcel(Input(6, 2024, maxResultCount: 1));

        file.FileName.ShouldBe("PayrollJVCustom018.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(13);
    }

    /// <summary>
    /// Every exported header must be a RESOLVED caption. A localization key with no XML entry renders as
    /// "[SomeKey]" in the xlsx -- visible to users, invisible to the compiler, and grep alone has proved
    /// unreliable at catching it.
    /// </summary>
    [Fact]
    public async Task Should_Resolve_Every_Exported_Column_Header()
    {
        var file = await _appService.GetPayrollJVCustom018ReportToExcel(Input(6, 2024));

        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        var unresolved = exported.First().Keys
            .Where(k => k.StartsWith("[") && k.EndsWith("]"))
            .ToList();

        unresolved.ShouldBeEmpty();
    }
}
