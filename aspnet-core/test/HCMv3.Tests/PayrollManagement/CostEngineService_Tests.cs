using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Costing;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// Cost-split allocation coverage for the cost engine. Legacy reference:
/// <c>HCM10.Web/UserControls/PayrollManagement/uscCostingFile.ascx.cs</c> <c>CreateCostRegister</c>,
/// lines 397-703 (the allocation itself is line 590).
/// </summary>
// ReSharper disable once InconsistentNaming
public class CostEngineService_Tests : CostingTestBase
{
    private const string AreaName = "GBSIT";

    /// <summary>Legacy normalises the period to the last day of the month before matching registers.</summary>
    private static readonly DateTime PayInDate = new DateTime(2026, 4, 30);

    private int _tenantId;
    private int _employeeId;
    private int _accountInfoId;
    private int _payrollReasonTypeId;

    private void Seed()
    {
        _tenantId = AbpSession.TenantId ?? 1;

        var accountType = new AccountType
        { TenantId = _tenantId, Code = "L", NameEn = "Ledger", NameAr = "Ledger" };
        Context.AccountTypes.Add(accountType);
        Context.SaveChanges();

        var accountInfo = new AccountInfo
        {
            TenantId = _tenantId, Code = "7101", NameEn = "Salary Expense", NameAr = "Salary",
            AccountTypeId = accountType.Id
        };
        Context.AccountInfos.Add(accountInfo);

        for (var slot = 1; slot <= 7; slot++)
        {
            Context.CostElements.Add(new CostElement
            { TenantId = _tenantId, Code = "CE" + slot, NameEn = "CE" + slot, NameAr = "CE" + slot });
        }

        // Employee.NationalityId and SalaryRegister.PayrollReasonTypeId are non-nullable FKs, so their
        // referenced rows must exist before the Sqlite FK constraints will accept the inserts.
        var nationality = new Nationality
        { TenantId = _tenantId, Code = "KW", NameEn = "Kuwaiti", NameAr = "كويتي" };
        Context.Nationalities.Add(nationality);

        var reasonType = new PayrollReasonType
        { TenantId = _tenantId, Code = "R1", NameEn = "Regular", NameAr = "Regular" };
        Context.PayrollReasonTypes.Add(reasonType);
        Context.SaveChanges();

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

        _accountInfoId = accountInfo.Id;
        _employeeId = employee.Id;
        _payrollReasonTypeId = reasonType.Id;
    }

    private int AddWageType(string code, string name, BillingType billingType,
                            bool includeInFiPosting = true, bool includeInArrearPay = false,
                            int? arrearSourceWageTypeId = null)
    {
        var wageType = new WageType
        {
            TenantId = _tenantId, Code = code, NameEn = name, NameAr = name,
            BillingType = billingType, PayrollOrder = 1, Status = true,
            IncludeInFIPosting = includeInFiPosting,
            IncludeInArrearPay = includeInArrearPay,
            ArrearSourceWageTypeId = arrearSourceWageTypeId
        };
        Context.WageTypes.Add(wageType);
        Context.SaveChanges();
        return wageType.Id;
    }

    private int AddSalaryRegister(params (int WageTypeId, double Amount)[] lines)
    {
        var register = new SalaryRegister
        {
            TenantId = _tenantId, EmployeeId = _employeeId,
            ForPeriod = PayInDate, InPeriod = PayInDate,
            PayrollRunType = PayrollRunType.Regular, PayrollReasonTypeId = _payrollReasonTypeId
        };
        Context.SalaryRegisters.Add(register);
        Context.SaveChanges();

        foreach (var line in lines)
        {
            Context.SalaryRegisterDetails.Add(new SalaryRegisterDetail
            {
                TenantId = _tenantId, SalaryRegisterId = register.Id,
                WageTypeId = line.WageTypeId, Amount = line.Amount, Rate = 0, Number = 0
            });
        }

        Context.SaveChanges();
        return register.Id;
    }

    private void AddWageAccount(int wageTypeId, double costPercentage, string costElementCodeForSlot2 = null)
    {
        Context.EmployeeWageAccounts.Add(new EmployeeWageAccount
        {
            TenantId = _tenantId, EmployeeId = _employeeId, WageTypeId = wageTypeId,
            StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2099, 12, 31),
            AccountInfoId = _accountInfoId,
            CostElement1Id = Context.CostElements.First(c => c.Code == "CE1").Id,
            CostElement2Id = costElementCodeForSlot2 == null
                ? null
                : Context.CostElements.First(c => c.Code == costElementCodeForSlot2).Id,
            CostPercentage = costPercentage
        });
        Context.SaveChanges();
    }

    private static CostingRunInput Input() => new CostingRunInput
    {
        PayrollAreaName = AreaName,
        EmployeeCode = "",
        PayInDate = PayInDate,
        PostingDate = new DateTime(2026, 5, 5),
        PayrollRunType = PayrollRunType.Regular
    };

    [Fact]
    public async Task Should_Split_Amount_Across_Cost_Percentages()
    {
        Seed();
        var wageTypeId = AddWageType("1001", "Basic Salary", BillingType.Earning);
        AddSalaryRegister((wageTypeId, 1000));

        // 60/40 split across two different cost-element combinations.
        AddWageAccount(wageTypeId, 60, "CE2");
        AddWageAccount(wageTypeId, 40, "CE3");

        var result = await CreateCostEngine(ResolverFor(AreaName, _employeeId)).RunAsync(Input());

        result.SuccessCount.ShouldBe(1);
        result.FailedCount.ShouldBe(0);

        var details = Context.CostRegisterDetails.OrderBy(d => d.Id).ToList();
        details.Count.ShouldBe(2);
        details[0].Amount.ShouldBe(600);
        details[1].Amount.ShouldBe(400);

        // The split must sum back to the register amount when percentages total 100.
        details.Sum(d => d.Amount).ShouldBe(1000);
    }

    [Fact]
    public async Task Should_Split_Across_All_Seven_Cost_Element_Slots()
    {
        Seed();
        var wageTypeId = AddWageType("1001", "Basic Salary", BillingType.Earning);
        AddSalaryRegister((wageTypeId, 700));

        Context.EmployeeWageAccounts.Add(new EmployeeWageAccount
        {
            TenantId = _tenantId, EmployeeId = _employeeId, WageTypeId = wageTypeId,
            StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2099, 12, 31),
            AccountInfoId = _accountInfoId,
            CostElement1Id = Context.CostElements.First(c => c.Code == "CE1").Id,
            CostElement2Id = Context.CostElements.First(c => c.Code == "CE2").Id,
            CostElement3Id = Context.CostElements.First(c => c.Code == "CE3").Id,
            CostElement4Id = Context.CostElements.First(c => c.Code == "CE4").Id,
            CostElement5Id = Context.CostElements.First(c => c.Code == "CE5").Id,
            CostElement6Id = Context.CostElements.First(c => c.Code == "CE6").Id,
            CostElement7Id = Context.CostElements.First(c => c.Code == "CE7").Id,
            CostPercentage = 100
        });
        Context.SaveChanges();

        await CreateCostEngine(ResolverFor(AreaName, _employeeId)).RunAsync(Input());

        var detail = Context.CostRegisterDetails.Single();
        detail.Amount.ShouldBe(700);

        // All seven slots must be carried through onto the cost detail, not just the first few.
        detail.CostElement1Id.ShouldNotBeNull();
        detail.CostElement2Id.ShouldNotBeNull();
        detail.CostElement3Id.ShouldNotBeNull();
        detail.CostElement4Id.ShouldNotBeNull();
        detail.CostElement5Id.ShouldNotBeNull();
        detail.CostElement6Id.ShouldNotBeNull();
        detail.CostElement7Id.ShouldNotBeNull();
    }

    [Fact]
    public async Task Should_Leave_Rounding_Remainder_Unredistributed_Matching_Legacy()
    {
        Seed();
        var wageTypeId = AddWageType("1001", "Basic Salary", BillingType.Earning);
        AddSalaryRegister((wageTypeId, 100));

        // Three equal thirds: 33.333...% each -> 99.999% of the amount.
        AddWageAccount(wageTypeId, 33.333, "CE2");
        AddWageAccount(wageTypeId, 33.333, "CE3");
        AddWageAccount(wageTypeId, 33.333, "CE4");

        await CreateCostEngine(ResolverFor(AreaName, _employeeId)).RunAsync(Input());

        var total = Context.CostRegisterDetails.Sum(d => d.Amount);

        // Legacy line 590 performs a bare multiply/divide with NO rounding and NO largest-remainder
        // correction, so the shortfall is preserved rather than silently absorbed.
        total.ShouldBe(99.999, 0.0001);
        total.ShouldBeLessThan(100);
    }

    [Fact]
    public async Task Should_Not_Round_The_Split_Amount()
    {
        Seed();
        var wageTypeId = AddWageType("1001", "Basic Salary", BillingType.Earning);
        AddSalaryRegister((wageTypeId, 1000));
        AddWageAccount(wageTypeId, 33.3333, "CE2");

        await CreateCostEngine(ResolverFor(AreaName, _employeeId)).RunAsync(Input());

        // 1000 * 33.3333 / 100 = 333.333 exactly; the engine stores full precision (rounding to 3dp
        // happens later, in the JV builder).
        Context.CostRegisterDetails.Single().Amount.ShouldBe(333.333, 0.000001);
    }

    [Fact]
    public async Task Should_Fold_Arrear_Wage_Type_Onto_Its_Source_And_Sum_Both_Amounts()
    {
        Seed();
        var baseWageTypeId = AddWageType("1001", "Basic Salary", BillingType.Earning);
        var arrearWageTypeId = AddWageType("7001", "Basic Salary Arrears", BillingType.Earning,
                                           includeInArrearPay: true,
                                           arrearSourceWageTypeId: baseWageTypeId);

        AddSalaryRegister((baseWageTypeId, 800), (arrearWageTypeId, 200));

        // Only the SOURCE wage type is configured -- that is the point of the fold.
        AddWageAccount(baseWageTypeId, 100, "CE2");

        var result = await CreateCostEngine(ResolverFor(AreaName, _employeeId)).RunAsync(Input());

        result.FailedCount.ShouldBe(0);

        var detail = Context.CostRegisterDetails.Single();
        detail.WageTypeId.ShouldBe(baseWageTypeId);
        detail.Amount.ShouldBe(1000);
    }

    [Fact]
    public async Task Should_Ignore_Wage_Types_Not_Marked_For_Fi_Posting()
    {
        Seed();
        var posted = AddWageType("1001", "Basic Salary", BillingType.Earning);
        var notPosted = AddWageType("1002", "Informational", BillingType.Earning, includeInFiPosting: false);

        AddSalaryRegister((posted, 500), (notPosted, 999));
        AddWageAccount(posted, 100, "CE2");

        var result = await CreateCostEngine(ResolverFor(AreaName, _employeeId)).RunAsync(Input());

        result.FailedCount.ShouldBe(0);
        Context.CostRegisterDetails.Single().Amount.ShouldBe(500);
    }

    [Fact]
    public async Task Should_Fail_The_Employee_When_A_Wage_Type_Has_No_Account_Configuration()
    {
        Seed();
        var wageTypeId = AddWageType("1001", "Basic Salary", BillingType.Earning);
        AddSalaryRegister((wageTypeId, 500));
        // Deliberately no EmployeeWageAccount row.

        var result = await CreateCostEngine(ResolverFor(AreaName, _employeeId)).RunAsync(Input());

        // Legacy line 604 fails the EMPLOYEE, not the whole run.
        result.TotalEmployees.ShouldBe(1);
        result.SuccessCount.ShouldBe(0);
        result.FailedCount.ShouldBe(1);
        result.Entries.Single().Status.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Delete_And_Rebuild_An_Existing_Cost_Register()
    {
        Seed();
        var wageTypeId = AddWageType("1001", "Basic Salary", BillingType.Earning);
        AddSalaryRegister((wageTypeId, 500));
        AddWageAccount(wageTypeId, 100, "CE2");

        var engine = CreateCostEngine(ResolverFor(AreaName, _employeeId));
        await engine.RunAsync(Input());
        await engine.RunAsync(Input());

        // Re-running must overwrite, never duplicate (legacy lines 486-507).
        Context.CostRegisters.Count().ShouldBe(1);
        Context.CostRegisterDetails.Count().ShouldBe(1);
        Context.CostRegisterDetails.Single().Amount.ShouldBe(500);
    }

    [Fact]
    public async Task Should_Succeed_Silently_When_The_Employee_Has_No_Salary_Register()
    {
        Seed();
        AddWageType("1001", "Basic Salary", BillingType.Earning);
        // No SalaryRegister for the period.

        var result = await CreateCostEngine(ResolverFor(AreaName, _employeeId)).RunAsync(Input());

        // Legacy line 515: nothing to cost is not an error.
        result.SuccessCount.ShouldBe(1);
        result.FailedCount.ShouldBe(0);
        Context.CostRegisters.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Abort_When_A_Stop_Flag_Is_Raised()
    {
        Seed();
        var wageTypeId = AddWageType("1001", "Basic Salary", BillingType.Earning);
        AddSalaryRegister((wageTypeId, 500));
        AddWageAccount(wageTypeId, 100, "CE2");

        // ProjectLog.EmployeeId is a non-nullable FK in this port, so the stop marker must hang off a real
        // employee -- which matches legacy, where the stop button flags an EXISTING per-employee log row
        // (uscCostingFile.ascx.cs lines 2287-2293) rather than inserting a standalone marker.
        Context.ProjectLogs.Add(new HCMv3.ProjectManagement.ProjectLog
        {
            TenantId = _tenantId, Message = "stop", RunTimeStamp = DateTime.Now,
            EmployeeId = _employeeId, Status = 1, InPeriod = PayInDate, StopFlag = true, NoOfEmployee = 0
        });
        Context.SaveChanges();

        var result = await CreateCostEngine(ResolverFor(AreaName, _employeeId)).RunAsync(Input());

        result.Aborted.ShouldBeTrue();
        result.SuccessCount.ShouldBe(0);
        Context.CostRegisters.ShouldBeEmpty();
    }

    /// <summary>
    /// Legacy wrote each employee's ProjectLog row inside the loop (lines 604-654); the observer is where the
    /// background job now does that, so it must hear about every employee, success and failure alike.
    /// </summary>
    [Fact]
    public async Task Should_Report_Each_Employee_To_The_Observer_As_It_Finishes()
    {
        Seed();
        var wageTypeId = AddWageType("1001", "Basic Salary", BillingType.Earning);
        AddSalaryRegister((wageTypeId, 500));
        AddWageAccount(wageTypeId, 100, "CE2");

        var observer = new RecordingObserver();

        var result = await CreateCostEngine(ResolverFor(AreaName, _employeeId)).RunAsync(Input(), observer);

        result.SuccessCount.ShouldBe(1);
        observer.Completed.Count.ShouldBe(1);
        observer.Completed[0].Entry.EmployeeId.ShouldBe(_employeeId);
        observer.Completed[0].Entry.Status.ShouldBe(0);
        observer.Completed[0].Entry.Message.ShouldBe(CostEngineService.SuccessMessage);
        observer.Completed[0].Total.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Report_A_Failed_Employee_To_The_Observer_Too()
    {
        Seed();
        var wageTypeId = AddWageType("1001", "Basic Salary", BillingType.Earning);
        AddSalaryRegister((wageTypeId, 500));

        // No wage account: legacy line 604 fails the employee.
        var observer = new RecordingObserver();

        var result = await CreateCostEngine(ResolverFor(AreaName, _employeeId)).RunAsync(Input(), observer);

        result.FailedCount.ShouldBe(1);
        observer.Completed.Count.ShouldBe(1);
        observer.Completed[0].Entry.Status.ShouldBe(1);
    }

    /// <summary>
    /// A Stop pressed before any ProjectLog row exists: legacy's button had nothing to flag and the run went
    /// on. The observer's stop request ends the run before the first employee.
    /// </summary>
    [Fact]
    public async Task Should_Stop_When_The_Observer_Asks_Even_With_No_Log_Row_To_Flag()
    {
        Seed();
        var wageTypeId = AddWageType("1001", "Basic Salary", BillingType.Earning);
        AddSalaryRegister((wageTypeId, 500));
        AddWageAccount(wageTypeId, 100, "CE2");

        var observer = new RecordingObserver { StopRequested = true };

        var result = await CreateCostEngine(ResolverFor(AreaName, _employeeId)).RunAsync(Input(), observer);

        result.Aborted.ShouldBeTrue();
        result.SuccessCount.ShouldBe(0);
        observer.Completed.ShouldBeEmpty();
        Context.CostRegisters.ShouldBeEmpty();
    }

    private class RecordingObserver : ICostingRunObserver
    {
        public bool StopRequested { get; set; }

        public System.Collections.Generic.List<(CostingRunLogEntry Entry, int Total)> Completed { get; } = new();

        public Task<bool> IsStopRequestedAsync() => Task.FromResult(StopRequested);

        public Task OnEmployeeCompletedAsync(CostingRunLogEntry entry, int totalEmployees)
        {
            Completed.Add((entry, totalEmployees));
            return Task.CompletedTask;
        }
    }

    [Fact]
    public void Should_Normalize_Period_To_End_Of_Month()
    {
        // Legacy line 442.
        CostEngineService.NormalizeToEndOfMonth(new DateTime(2026, 2, 3))
            .ShouldBe(new DateTime(2026, 2, 28));
        CostEngineService.NormalizeToEndOfMonth(new DateTime(2024, 2, 10))
            .ShouldBe(new DateTime(2024, 2, 29));
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("E1", 1)]
    [InlineData("E1,E2 , E3", 3)]
    [InlineData("E1,,E2", 2)]
    public void Should_Parse_Comma_Separated_Employee_Codes(string input, int expectedCount)
    {
        // Legacy GetEmpCodeList, uscCostingFile.ascx.cs lines 2489-2498.
        JournalVoucherBuilder.ParseEmployeeCodes(input).Count.ShouldBe(expectedCount);
    }
}
