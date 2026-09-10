using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GoLiveSetup.ReferenceData;
using HCMv3.PayrollManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GoLiveSetup;

/// <summary>
/// The <c>payroll</c> reference pack: the wage types and leave types the time and payroll engines look up by
/// fixed code, taken from the legacy production catalogue. A tenant without them cannot produce a payslip --
/// there is no Net Pay without wage code 9000, and no leave accrual without absence code 1.
/// </summary>
// ReSharper disable once InconsistentNaming
public class PayrollCatalogue_Tests : AppTestBase
{
    /// <summary>
    /// Every wage code the engines reference in code. The day counters and their arrear twins are listed
    /// without the ones legacy commented out (4012, 4013, 4019 and eight 5xxx), which the engine never writes.
    /// </summary>
    private static readonly string[] EngineWageCodes = new[]
        {
            "1000", "9000", "3000", "3001", "3002", "3003", "3004", "3005", "2007", "2000", "2010",
            "1010", "1006", "2001", "2005", "2011", "1", "3", "5",
        }
        .Concat(Enumerable.Range(4000, 32).Select(c => c.ToString())
            .Where(c => c != "4012" && c != "4013" && c != "4018" && c != "4019"))
        .Concat(Enumerable.Range(5000, 32).Select(c => c.ToString())
            .Where(c => !new[] { "5005", "5006", "5012", "5013", "5018", "5019", "5025", "5026", "5031" }.Contains(c)))
        .ToArray();

    /// <summary>Annual, Unpaid, Sick paid, Sick unpaid, Unapproved absence -- found by code in the time engine.</summary>
    private static readonly string[] EngineAbsenceCodes = { "1", "2", "3", "5", "15" };

    private readonly IReferenceDataSeeder _seeder;

    public PayrollCatalogue_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _seeder = Resolve<IReferenceDataSeeder>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private ReferenceDataPack Pack => _seeder.GetPack("payroll");

    private T Row<T>(Func<HCMv3.EntityFrameworkCore.HCMv3DbContext, T> query) => UsingDbContext(query);

    // ---------------------------------------------------------------- the pack itself

    [Fact]
    public void Should_Carry_The_Whole_Legacy_Catalogue()
    {
        Pack.WageTypes.Count.ShouldBe(325);
        Pack.AbsenceTypes.Count.ShouldBe(20);
    }

    [Fact]
    public void Should_Be_Seeded_With_Every_New_Tenant()
    {
        _seeder.GetAvailablePackIds().ShouldContain("payroll");
    }

    [Fact]
    public void Should_Carry_Every_Wage_Code_The_Engines_Look_Up()
    {
        var codes = Pack.WageTypes.Select(w => w.Code).ToHashSet();

        EngineWageCodes.Where(c => !codes.Contains(c)).ShouldBeEmpty();
    }

    [Fact]
    public void Should_Carry_Every_Leave_Code_The_Time_Engine_Looks_Up()
    {
        var codes = Pack.AbsenceTypes.Select(a => a.Code).ToHashSet();

        EngineAbsenceCodes.Where(c => !codes.Contains(c)).ShouldBeEmpty();
    }

    [Fact]
    public void Should_Mark_Every_Day_Counter_As_A_Time_Type()
    {
        // The wage-type loop skips time types; a day counter billed as an earning would be paid out as money.
        Pack.WageTypes
            .Where(w => w.Code.Length == 4 && (w.Code[0] == '4' || w.Code[0] == '5'))
            .Where(w => w.BillingType != BillingType.TimeType)
            .Select(w => w.Code)
            .ShouldBeEmpty();
    }

    [Fact]
    public void Should_Calculate_Net_Pay_Last()
    {
        // Net Pay sums every line above it, so anything ordered after it would be left off the payslip total.
        var netPay = Pack.WageTypes.Single(w => w.Code == "9000");

        Pack.WageTypes.Where(w => w.Code != "9000").Max(w => w.PayrollOrder).ShouldBeLessThan(netPay.PayrollOrder);
    }

    [Fact]
    public void Should_Not_Repeat_A_Code()
    {
        Pack.WageTypes.GroupBy(w => w.Code).Where(g => g.Count() > 1).Select(g => g.Key).ShouldBeEmpty();
        Pack.AbsenceTypes.GroupBy(a => a.Code).Where(g => g.Count() > 1).Select(g => g.Key).ShouldBeEmpty();
    }

    [Fact]
    public void Should_Give_Every_Row_A_Code_And_Both_Names()
    {
        foreach (var w in Pack.WageTypes)
        {
            w.Code.ShouldNotBeNullOrWhiteSpace();
            w.NameEn.ShouldNotBeNullOrWhiteSpace($"wage type {w.Code}");
            w.NameAr.ShouldNotBeNullOrWhiteSpace($"wage type {w.Code}");
        }

        foreach (var a in Pack.AbsenceTypes)
        {
            a.Code.ShouldNotBeNullOrWhiteSpace();
            a.NameEn.ShouldNotBeNullOrWhiteSpace($"absence type {a.Code}");
            a.NameAr.ShouldNotBeNullOrWhiteSpace($"absence type {a.Code}");
        }
    }

    /// <summary>
    /// The pack carries enums as the legacy integers. An integer the port's enum does not define would
    /// deserialise without complaint and then match no branch in the engine -- so check every one.
    /// </summary>
    [Fact]
    public void Should_Only_Carry_Enum_Values_The_Port_Defines()
    {
        UndefinedEnumValues(Pack.WageTypes, w => w.Code).ShouldBeEmpty();
        UndefinedEnumValues(Pack.AbsenceTypes, a => a.Code).ShouldBeEmpty();
    }

    [Fact]
    public void Should_Resolve_Every_Link_Inside_The_Pack()
    {
        var codes = Pack.WageTypes.Select(w => w.Code).ToHashSet();

        Pack.WageTypes
            .Where(w => w.ArrearSourceCode != null && !codes.Contains(w.ArrearSourceCode))
            .Select(w => w.Code)
            .ShouldBeEmpty();

        Pack.AbsenceTypes
            .Where(a => a.WageTypeCode != null && !codes.Contains(a.WageTypeCode))
            .Select(a => a.Code)
            .ShouldBeEmpty();
    }

    // ---------------------------------------------------------------- seeding

    [Fact]
    public async Task Should_Seed_The_Catalogue_Into_A_Tenant()
    {
        var result = await _seeder.SeedPackAsync("payroll", TenantId);

        result.TotalInserted.ShouldBeGreaterThan(0);

        Row(c => c.WageTypes.Count(w => w.TenantId == TenantId && w.Code == "9000")).ShouldBe(1);
        Row(c => c.WageTypes.Single(w => w.TenantId == TenantId && w.Code == "1000").BillingType)
            .ShouldBe(BillingType.Earning);
        Row(c => c.AbsenceTypes.Count(a => a.TenantId == TenantId && a.Code == "1")).ShouldBe(1);
    }

    [Fact]
    public async Task Should_Link_Each_Arrear_To_Its_Source()
    {
        // Without the link the arrear calculation returns early and a retro correction is never paid.
        await _seeder.SeedPackAsync("payroll", TenantId);

        UsingDbContext(context =>
        {
            var pifss = context.WageTypes.Single(w => w.TenantId == TenantId && w.Code == "1");
            var pifssArrear = context.WageTypes.Single(w => w.TenantId == TenantId && w.Code == "2");

            pifssArrear.ArrearSourceWageTypeId.ShouldBe(pifss.Id);

            var linked = context.WageTypes.Count(w => w.TenantId == TenantId && w.ArrearSourceWageTypeId != null);
            linked.ShouldBe(Pack.WageTypes.Count(w => w.ArrearSourceCode != null));
        });
    }

    [Fact]
    public async Task Should_Link_Each_Leave_Type_To_The_Wage_Type_It_Is_Paid_Through()
    {
        await _seeder.SeedPackAsync("payroll", TenantId);

        UsingDbContext(context =>
        {
            var annual = context.AbsenceTypes.Single(a => a.TenantId == TenantId && a.Code == "1");
            var leavePay = context.WageTypes.Single(w => w.TenantId == TenantId && w.Code == "1003");

            annual.WageTypeId.ShouldBe(leavePay.Id);
            annual.HaveQuotaAccrual.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task Should_Leave_A_Wage_Type_The_Tenant_Already_Has_Untouched()
    {
        // A tenant may have renamed or re-ordered its own Basic Salary; the seeder must not undo that.
        UsingDbContext(context =>
        {
            context.WageTypes.Add(new WageType
            {
                TenantId = TenantId,
                Code = "1000",
                NameEn = "Basic Pay",
                NameAr = "الراتب",
                BillingType = BillingType.Earning,
                PayrollOrder = 5,
                Status = true,
            });
        });

        var result = await _seeder.SeedPackAsync("payroll", TenantId);

        result.Counts.Single(c => c.Lookup == nameof(WageType)).Skipped.ShouldBeGreaterThanOrEqualTo(1);

        UsingDbContext(context =>
        {
            var basic = context.WageTypes.Single(w => w.TenantId == TenantId && w.Code == "1000");
            basic.NameEn.ShouldBe("Basic Pay");
            basic.PayrollOrder.ShouldBe(5);

            // A new row that points at an existing one still gets its link.
            context.WageTypes.Single(w => w.TenantId == TenantId && w.Code == "7000")
                .ArrearSourceWageTypeId.ShouldBe(basic.Id);
        });
    }

    [Fact]
    public async Task Should_Insert_Nothing_On_A_Second_Run()
    {
        var first = await _seeder.SeedPackAsync("payroll", TenantId);
        var second = await _seeder.SeedPackAsync("payroll", TenantId);

        first.TotalInserted.ShouldBeGreaterThan(0);
        second.TotalInserted.ShouldBe(0);
        Row(c => c.WageTypes.Count(w => w.TenantId == TenantId && w.Code == "9000")).ShouldBe(1);
    }

    [Fact]
    public async Task Should_Write_Nothing_On_Preview()
    {
        var before = Row(c => c.WageTypes.Count(w => w.TenantId == TenantId));

        var preview = await _seeder.PreviewPackAsync("payroll");

        preview.TotalInserted.ShouldBeGreaterThan(0);
        Row(c => c.WageTypes.Count(w => w.TenantId == TenantId)).ShouldBe(before);
    }

    // ---------------------------------------------------------------- helpers

    private static List<string> UndefinedEnumValues<T>(IEnumerable<T> rows, Func<T, string> code)
    {
        var enumProperties = typeof(T).GetProperties().Where(p => p.PropertyType.IsEnum).ToList();

        return rows
            .SelectMany(row => enumProperties
                .Where(p => !Enum.IsDefined(p.PropertyType, p.GetValue(row)!))
                .Select(p => $"{code(row)}.{p.Name}={Convert.ToInt32(p.GetValue(row))}"))
            .ToList();
    }
}
