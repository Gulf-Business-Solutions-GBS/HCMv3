using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.Employees;
using HCMv3.GridReports.Employees.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Employee Action Change grid report (legacy <c>uscRptEmployeeActionChange.ascx</c>,
/// <c>Table1()</c>).
///
/// This report's whole point is the per-action-code branch table, so the tests pin the two branches
/// that produce derived values (CHANGE IN PAY, which emits one row per wage type, and CONTRACT RENEWAL)
/// plus the boundary-date and "export ignores page size" tests every report in this family needs.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeActionChangeReportAppService_Tests : AppTestBase
{
    private readonly IEmployeeActionChangeReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeActionChangeReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeActionChangeReportAppService>();
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

    /// <summary>
    /// The legacy branch table keys on <c>PersonalAction.Code</c>, so tests must create the action with
    /// the exact legacy code ("4" = change in pay, "9" = contract renewal, ...).
    /// </summary>
    private (PersonalAction Action, PersonalActionReason Reason) CreatePersonalAction(string legacyCode)
    {
        PersonalAction action = null;
        PersonalActionReason reason = null;

        UsingDbContext(context =>
        {
            action = context.Set<PersonalAction>().FirstOrDefault(a => a.Code == legacyCode);

            if (action == null)
            {
                action = context.Set<PersonalAction>().Add(new PersonalAction
                {
                    TenantId = TenantId,
                    Code = legacyCode,
                    NameEn = "Action-" + legacyCode,
                    NameAr = "Action-" + legacyCode + "-Ar"
                }).Entity;

                context.SaveChanges();
            }

            reason = context.Set<PersonalActionReason>().Add(new PersonalActionReason
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Reason",
                NameAr = "Reason",
                PersonalActionId = action.Id
            }).Entity;
        });

        return (action, reason);
    }

    private void CreateAction(Employee employee, PersonalAction action, PersonalActionReason reason, DateTime start)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeAction>().Add(new EmployeeAction
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                PersonalActionId = action.Id,
                ActionReasonId = reason.Id,
                StartDate = start
            });
        });
    }

    private WageType CreateWageType(string code)
    {
        WageType wageType = null;
        UsingDbContext(context =>
        {
            wageType = context.Set<WageType>().Add(new WageType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code + "-En",
                NameAr = code + "-Ar",
                PayrollOrder = 1
            }).Entity;
        });

        return wageType;
    }

    private void CreateBasicPay(Employee employee, WageType wageType, DateTime start, DateTime end, double amount)
    {
        UsingDbContext(context =>
        {
            var currency = context.Set<Currency>().Add(new Currency
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 3),
                NameEn = "Cur",
                NameAr = "Cur",
                CurrencyShortFormEn = "KWD",
                CurrencyShortFormAr = "د.ك"
            }).Entity;

            context.SaveChanges();

            var basicPay = context.Set<EmployeeBasicPay>().Add(new EmployeeBasicPay
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                StartDate = start,
                EndDate = end,
                Amount = amount
            }).Entity;

            context.SaveChanges();

            context.Set<EmployeeBasicPayDetail>().Add(new EmployeeBasicPayDetail
            {
                TenantId = TenantId,
                EmployeeBasicPayId = basicPay.Id,
                WageTypeId = wageType.Id,
                CurrencyId = currency.Id,
                Amount = amount
            });
        });
    }

    private void CreateContract(Employee employee, DateTime start, DateTime end)
    {
        UsingDbContext(context =>
        {
            // EmployeeContract.ContractTypeId is non-nullable, so a contract type has to exist first.
            var contractType = context.Set<ContractType>().Add(new ContractType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "CT",
                NameAr = "CT"
            }).Entity;

            context.SaveChanges();

            context.Set<EmployeeContract>().Add(new EmployeeContract
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                ContractTypeId = contractType.Id,
                StartDate = start,
                EndDate = end
            });
        });
    }

    private static GetEmployeeActionChangeReportInput Input(
        DateTime from,
        DateTime to,
        string personalActionCode = null,
        int maxResultCount = 100)
    {
        return new GetEmployeeActionChangeReportInput
        {
            FromDate = from,
            ToDate = to,
            PersonalActionCode = personalActionCode,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Include_Actions_On_The_Window_Boundary()
    {
        var (action, reason) = CreatePersonalAction("7"); // SEPARATION: exactly one row, no lookups.
        var onFrom = CreateEmployee("CEDGE-FROM");
        var onTo = CreateEmployee("CEDGE-TO");
        var outside = CreateEmployee("CEDGE-OUT");

        CreateAction(onFrom, action, reason, new DateTime(2024, 1, 1));
        CreateAction(onTo, action, reason, new DateTime(2024, 12, 31));
        CreateAction(outside, action, reason, new DateTime(2025, 1, 1));

        var result = await _appService.GetEmployeeActionChangeReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), personalActionCode: "7"));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("CEDGE-FROM");
        codes.ShouldContain("CEDGE-TO");
        codes.ShouldNotContain("CEDGE-OUT");
    }

    [Fact]
    public async Task Should_Emit_One_Row_Per_Wage_Type_With_The_Delta_For_A_Change_In_Pay()
    {
        var (action, reason) = CreatePersonalAction("4"); // CHANGE IN PAY
        var employee = CreateEmployee("CPAY-1");
        var wageType = CreateWageType("WT-" + Guid.NewGuid().ToString("N").Substring(0, 6));

        // Latest pay starting BEFORE the action.
        CreateBasicPay(employee, wageType, new DateTime(2023, 1, 1), new DateTime(2024, 5, 31), 1000);
        // Pay starting in the action's calendar month.
        CreateBasicPay(employee, wageType, new DateTime(2024, 6, 1), new DateTime(2024, 12, 31), 1250);

        CreateAction(employee, action, reason, new DateTime(2024, 6, 1));

        var result = await _appService.GetEmployeeActionChangeReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), personalActionCode: "4"));

        var row = result.Items.Single(i => i.EmployeeCode == "CPAY-1");
        row.Component.ShouldBe(wageType.Code + "-En");
        row.OriginalValue.ShouldBe("1000");
        // Legacy's AdjustedValue is the DELTA, not the new amount.
        row.AdjustedValue.ShouldBe("250");
        row.FinalValue.ShouldBe("1250");
    }

    [Fact]
    public async Task Should_Compare_Contract_End_Dates_For_A_Contract_Renewal()
    {
        var (action, reason) = CreatePersonalAction("9"); // CONTRACT RENEWAL
        var employee = CreateEmployee("CCON-1");

        CreateContract(employee, new DateTime(2022, 1, 1), new DateTime(2024, 5, 31));
        CreateContract(employee, new DateTime(2024, 6, 1), new DateTime(2026, 5, 31));

        CreateAction(employee, action, reason, new DateTime(2024, 6, 1));

        var result = await _appService.GetEmployeeActionChangeReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), personalActionCode: "9"));

        var row = result.Items.Single(i => i.EmployeeCode == "CCON-1");
        row.Component.ShouldBe("Contract");
        row.OriginalValue.ShouldBe("31/05/2024");
        row.FinalValue.ShouldBe("31/05/2026");
    }

    [Fact]
    public async Task Should_Leave_Separation_Rows_Without_Modification_Audit_Values()
    {
        var (action, reason) = CreatePersonalAction("7"); // SEPARATION
        var employee = CreateEmployee("CSEP-1");

        CreateAction(employee, action, reason, new DateTime(2024, 7, 1));

        var result = await _appService.GetEmployeeActionChangeReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), personalActionCode: "7"));

        var row = result.Items.Single(i => i.EmployeeCode == "CSEP-1");
        row.Component.ShouldBe("Separation");
        // Legacy lines 617-630 set only CreatedBy on this branch.
        row.CreatedOn.ShouldBeNull();
        row.ModifiedOn.ShouldBeNull();
        row.ModifiedBy.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var (action, reason) = CreatePersonalAction("7"); // SEPARATION: one row per action.
        var first = CreateEmployee("CEXP-A");
        var second = CreateEmployee("CEXP-B");
        var third = CreateEmployee("CEXP-C");

        CreateAction(first, action, reason, new DateTime(2024, 8, 1));
        CreateAction(second, action, reason, new DateTime(2024, 8, 1));
        CreateAction(third, action, reason, new DateTime(2024, 8, 1));

        // Page size of 1 - the export must ignore it and still return every filtered row.
        var input = Input(new DateTime(2024, 8, 1), new DateTime(2024, 8, 1), personalActionCode: "7", maxResultCount: 1);

        var page = await _appService.GetEmployeeActionChangeReport(input);
        page.Items.Count.ShouldBe(1);

        var file = await _appService.GetEmployeeActionChangeReportToExcel(input);

        file.FileName.ShouldBe("EmployeeActionChange.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(page.TotalCount);
        var exportedCodes = exported.Select(r => r["Employee Code"]?.ToString()).OrderBy(c => c).ToList();
        exportedCodes.ShouldBe(new List<string> { "CEXP-A", "CEXP-B", "CEXP-C" });
        exported.First().Keys.ShouldContain("Component");
    }
}
