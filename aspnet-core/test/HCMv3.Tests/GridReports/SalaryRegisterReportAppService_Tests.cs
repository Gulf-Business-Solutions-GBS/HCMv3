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
/// Tests for the Salary Register grid report (legacy <c>uscSalaryRegister.ascx</c>,
/// <c>Table1()</c> lines 368-795).
///
/// This screen's filter panel is From/To date ONLY -- no run type, no employee code, no status --
/// so the boundary test and the export test carry most of the weight here, alongside the
/// wage-type-code mapping and the reproduced OT-on-public-holiday defect.
/// </summary>
// ReSharper disable once InconsistentNaming
public class SalaryRegisterReportAppService_Tests : AppTestBase
{
    private readonly ISalaryRegisterReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public SalaryRegisterReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<ISalaryRegisterReportAppService>();
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

    private WageType CreateWageType(string code)
    {
        WageType wageType = null;
        UsingDbContext(context =>
        {
            wageType = context.Set<WageType>().Add(new WageType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = "WT-" + code,
                NameAr = "WT-" + code,
                ShortNameEn = code,
                ShortNameAr = code,
                Status = true
            }).Entity;
        });

        return wageType;
    }

    private SalaryRegister CreateRegister(Employee employee, DateTime inPeriod)
    {
        SalaryRegister register = null;
        UsingDbContext(context =>
        {
            register = context.Set<SalaryRegister>().Add(new SalaryRegister
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                ForPeriod = inPeriod,
                InPeriod = inPeriod,
                PayrollRunType = PayrollRunType.Regular,
                PayrollResultType = PayrollResultType.Current,
                OffCyclePaymentDate = inPeriod
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

    private static GetSalaryRegisterReportInput Input(
        DateTime from,
        DateTime to,
        int maxResultCount = 100,
        int skipCount = 0)
    {
        return new GetSalaryRegisterReportInput
        {
            FromDate = from,
            ToDate = to,
            MaxResultCount = maxResultCount,
            SkipCount = skipCount
        };
    }

    [Fact]
    public async Task Should_Return_Only_Registers_Whose_InPeriod_Falls_In_The_Window()
    {
        var wageType = CreateWageType("1001");
        var inRange = CreateEmployee("SR-IN");
        var before = CreateEmployee("SR-BEFORE");
        var after = CreateEmployee("SR-AFTER");

        CreateDetail(CreateRegister(inRange, new DateTime(2024, 6, 30)), wageType, 100);
        CreateDetail(CreateRegister(before, new DateTime(2023, 12, 31)), wageType, 100);
        CreateDetail(CreateRegister(after, new DateTime(2025, 1, 31)), wageType, 100);

        var result = await _appService.GetSalaryRegisterReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("SR-IN");
        codes.ShouldNotContain("SR-BEFORE");
        codes.ShouldNotContain("SR-AFTER");
    }

    [Fact]
    public async Task Should_Include_Registers_Sitting_Exactly_On_The_Window_Boundaries()
    {
        var wageType = CreateWageType("1001");
        var onFrom = CreateEmployee("SR-EDGE-FROM");
        var onTo = CreateEmployee("SR-EDGE-TO");

        // Legacy predicate is InPeriod >= dtFrom && InPeriod <= dtTo -- BOTH ends inclusive.
        CreateDetail(CreateRegister(onFrom, new DateTime(2024, 1, 1)), wageType, 100);
        CreateDetail(CreateRegister(onTo, new DateTime(2024, 12, 31)), wageType, 100);

        var result = await _appService.GetSalaryRegisterReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("SR-EDGE-FROM");
        codes.ShouldContain("SR-EDGE-TO");
    }

    [Fact]
    public async Task Should_Expose_The_Period_As_A_Column_Unlike_The_Basic_Sheet()
    {
        var employee = CreateEmployee("SR-PERIOD");
        CreateDetail(CreateRegister(employee, new DateTime(2024, 6, 30)), CreateWageType("1001"), 50);

        var result = await _appService.GetSalaryRegisterReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var row = result.Items.Single(i => i.EmployeeCode == "SR-PERIOD");
        row.InPeriod.ShouldBe(new DateTime(2024, 6, 30));
    }

    [Fact]
    public async Task Should_Map_Current_Month_And_Arrear_Wage_Type_Codes_To_Their_Columns()
    {
        var employee = CreateEmployee("SR-MAP");
        var register = CreateRegister(employee, new DateTime(2024, 6, 30));

        CreateDetail(register, CreateWageType("1001"), 1001);  // HousingAllowance
        CreateDetail(register, CreateWageType("1002"), 1002);  // TransportAllowance
        CreateDetail(register, CreateWageType("1007"), 1007);  // OTonNormalDay
        CreateDetail(register, CreateWageType("7001"), 7001);  // HousingAllowanceArrear
        CreateDetail(register, CreateWageType("7002"), 7002);  // TransportAllowanceArrear

        var result = await _appService.GetSalaryRegisterReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var row = result.Items.Single(i => i.EmployeeCode == "SR-MAP");
        row.HousingAllowance.ShouldBe(1001);
        row.TransportAllowance.ShouldBe(1002);
        row.OTonNormalDay.ShouldBe(1007);
        row.HousingAllowanceArrear.ShouldBe(7001);
        row.TransportAllowanceArrear.ShouldBe(7002);
        // An absent wage type reads as 0, matching legacy's `!= null ? .Amount : 0`.
        row.Commission.ShouldBe(0);
    }

    /// <summary>
    /// LEGACY DEFECT, DELIBERATELY REPRODUCED (uscSalaryRegister.ascx.cs lines 681-682): the
    /// OTonPublicHoliday and OTonPublicHolidayArr cells are guarded on their OWN wage type (1009 / 7009)
    /// but assign the WEEKEND line's amount --
    /// <c>rec.OTonPublicHoliday != null ? rec.OTonWeekend.Amount : 0</c>. Both columns therefore report
    /// wage types 1008 / 7008. Legacy fidelity is the standing rule, so the port reproduces it rather
    /// than reading 1009 / 7009.
    /// </summary>
    [Fact]
    public async Task Should_Report_Weekend_Overtime_Under_PublicHoliday_Reproducing_Legacy_Defect()
    {
        var employee = CreateEmployee("SR-OTBUG");
        var register = CreateRegister(employee, new DateTime(2024, 6, 30));

        CreateDetail(register, CreateWageType("1008"), 808);   // OT on weekend
        CreateDetail(register, CreateWageType("1009"), 909);   // OT on public holiday (IGNORED by legacy)
        CreateDetail(register, CreateWageType("7008"), 7808);  // OT on weekend, arrear
        CreateDetail(register, CreateWageType("7009"), 7909);  // OT on public holiday, arrear (IGNORED)

        var result = await _appService.GetSalaryRegisterReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var row = result.Items.Single(i => i.EmployeeCode == "SR-OTBUG");
        row.OTonWeekend.ShouldBe(808);
        // The bug: the public-holiday column shows the WEEKEND amount, not 909.
        row.OTonPublicHoliday.ShouldBe(808);
        row.OTonWeekendArr.ShouldBe(7808);
        row.OTonPublicHolidayArr.ShouldBe(7808);
    }

    [Fact]
    public async Task Should_Page_Results()
    {
        var wageType = CreateWageType("1001");

        foreach (var code in new[] { "SR-PAGE-A", "SR-PAGE-B" })
        {
            CreateDetail(CreateRegister(CreateEmployee(code), new DateTime(2024, 6, 30)), wageType, 10);
        }

        var page = await _appService.GetSalaryRegisterReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), maxResultCount: 1));

        page.TotalCount.ShouldBe(2);
        page.Items.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var wageType = CreateWageType("1001");

        foreach (var code in new[] { "SR-EXP-A", "SR-EXP-B", "SR-EXP-C" })
        {
            CreateDetail(CreateRegister(CreateEmployee(code), new DateTime(2024, 6, 30)), wageType, 10);
        }

        // Out of the window -- must not reach the export.
        CreateDetail(CreateRegister(CreateEmployee("SR-EXP-OUT"), new DateTime(2025, 6, 30)), wageType, 10);

        // Page size of 1 -- the export must ignore it and still return every filtered row.
        var file = await _appService.GetSalaryRegisterReportToExcel(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), maxResultCount: 1));

        file.FileName.ShouldBe("SalaryRegister.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(3);
        exported.Select(r => r["Code"]?.ToString()).OrderBy(c => c)
            .ShouldBe(new List<string> { "SR-EXP-A", "SR-EXP-B", "SR-EXP-C" });
    }

    /// <summary>
    /// Every exported header must be a RESOLVED caption. A localization key with no XML entry renders as
    /// "[SomeKey]" in the xlsx -- visible to users but invisible to the compiler and to every other test.
    /// This report has the widest column set of the six (108 columns), so it is the natural place to gate.
    /// </summary>
    [Fact]
    public async Task Should_Resolve_Every_Exported_Column_Header()
    {
        CreateDetail(
            CreateRegister(CreateEmployee("SR-HDR"), new DateTime(2024, 6, 30)),
            CreateWageType("1001"),
            10);

        var file = await _appService.GetSalaryRegisterReportToExcel(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        var unresolved = exported.First().Keys
            .Where(k => k.StartsWith("[") && k.EndsWith("]"))
            .ToList();

        unresolved.ShouldBeEmpty();
    }
}
