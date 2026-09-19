using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.Localization;
using ClosedXML.Excel;
using HCMv3.GoLiveSetup;
using HCMv3.GoLiveSetup.Dto;
using HCMv3.Storage;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GoLiveSetup;

/// <summary>
/// The two workbooks Go-Live Setup hands a customer: the employee sheet to fill in, and the comparison to check.
/// Made-up figures only.
/// </summary>
// ReSharper disable once InconsistentNaming
public class GoLiveExcelExports_Tests : AppTestBase
{
    private readonly IGoLiveSetupAppService _goLive;

    public GoLiveExcelExports_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _goLive = Resolve<IGoLiveSetupAppService>();
    }

    /// <summary>
    /// The sheet has the grid's columns in the grid's order, so it pastes back; a civil ID stays text however
    /// long; and an empty grid gives a header-only sheet to start from.
    /// </summary>
    [Fact]
    public async Task Should_Export_The_Employee_Sheet_In_The_Grids_Columns()
    {
        var file = await _goLive.ExportEmployeeSheet(new GoLiveEmployeeSheetInput
        {
            Employees = new List<GoLiveEmployeeRowDto>
            {
                new()
                {
                    Code = "E001",
                    FullName = "Employee One",
                    JoiningDate = new DateTime(2020, 1, 15),
                    CivilId = "290011501234",
                    BasicSalary = 500,
                    AnnualLeaveOpeningBalance = 12.5,
                    Iban = "KW81CBKU0000000000001234560101",
                },
            },
        });

        using var book = Open(file.FileToken);
        var sheet = book.Worksheet(1);
        sheet.Cell(1, 1).GetString().ShouldBe(Localized("GoLiveColCode"));
        sheet.Cell(1, 12).GetString().ShouldBe(Localized("GoLiveColAnnualLeaveOpening"));
        sheet.Cell(1, 15).GetString().ShouldBe(Localized("GoLiveColAccountNumber"));
        sheet.Cell(2, 4).Value.IsText.ShouldBeTrue();
        sheet.Cell(2, 4).GetString().ShouldBe("290011501234");
        sheet.Cell(2, 3).GetDateTime().ShouldBe(new DateTime(2020, 1, 15));
        sheet.Cell(2, 5).GetDouble().ShouldBe(500);
        sheet.Cell(2, 12).GetDouble().ShouldBe(12.5);
        sheet.Cell(2, 5).Style.NumberFormat.Format.ShouldBe(GoLiveExcelExports.AmountFormat);

        using var empty = Open((await _goLive.ExportEmployeeSheet(new GoLiveEmployeeSheetInput())).FileToken);
        empty.Worksheet(1).LastRowUsed().RowNumber().ShouldBe(1);
    }

    /// <summary>
    /// Each legacy/HCMv3 pair is coloured by whether it agrees, and the Summary sheet counts an employee or wage
    /// type as matched only when all its lines are.
    /// </summary>
    [Fact]
    public async Task Should_Colour_The_Comparison_And_Count_What_Matched()
    {
        var file = await _goLive.ExportLegacyComparison(new GoLiveLegacyCompareExportInput
        {
            PayMonth = new DateTime(2026, 9, 30),
            Lines = new List<GoLiveLegacyCompareLineDto>
            {
                Line("E001", "1000", matched: true),
                Line("E001", "4015", matched: false),
                Line("E002", "1000", matched: true),
            },
        });

        using var book = Open(file.FileToken);
        var summary = book.Worksheet(1);
        Count(summary, "GoLiveCompareEmployeesMatched").ShouldBe(1);
        Count(summary, "GoLiveCompareEmployeesNotMatched").ShouldBe(1);
        summary.RowsUsed().First(r => r.Cell(1).GetString() == Localized("GoLiveCompareEmployeesMatchedPercent"))
            .Cell(2).GetDouble().ShouldBe(0.5);
        Count(summary, "GoLiveCompareWageTypesMatched").ShouldBe(1);
        Count(summary, "GoLiveCompareWageTypesNotMatched").ShouldBe(1);

        var lines = book.Worksheet(2);
        lines.Cell(1, 4).GetString().ShouldBe(Localized("GoLiveCompareColRateLegacy"));
        var differing = Enumerable.Range(2, 3).Single(r => lines.Cell(r, 2).GetString() == "4015");
        var agreeing = Enumerable.Range(2, 3).First(r => lines.Cell(r, 2).GetString() == "1000");
        lines.Cell(differing, 6).Style.Fill.BackgroundColor.ShouldNotBe(lines.Cell(agreeing, 6).Style.Fill.BackgroundColor);
        lines.Cell(differing, 10).GetString().ShouldBe(Localized("GoLiveCompareNotMatched"));

        // No stray decimal point after a whole number and no thousand separators.
        lines.Cell(agreeing, 4).Style.NumberFormat.Format.ShouldBe(GoLiveExcelExports.NumberAsIsFormat);
        lines.Cell(agreeing, 8).Style.NumberFormat.Format.ShouldBe(GoLiveExcelExports.AmountFormat);
        GoLiveExcelExports.AmountFormat.ShouldNotContain(",");
    }

    private static GoLiveLegacyCompareLineDto Line(string employee, string wage, bool matched) =>
        new()
        {
            EmployeeCode = employee,
            WageTypeCode = wage,
            WageTypeName = "Wage " + wage,
            LegacyRate = 10,
            Hcmv3Rate = 10,
            LegacyNumber = 26,
            Hcmv3Number = matched ? 26 : 31,
            LegacyAmount = 10,
            Hcmv3Amount = 10,
            RateMatches = true,
            NumberMatches = matched,
            AmountMatches = true,
            Status = matched ? GoLiveCompareStatus.Matched : GoLiveCompareStatus.Different,
            Cause = matched ? GoLiveCompareCause.None : GoLiveCompareCause.Days,
            Reason = matched ? "Matches." : "Days differ.",
        };

    private int Count(IXLWorksheet summary, string labelKey)
    {
        var label = Localized(labelKey);
        var row = summary.RowsUsed().First(r => r.Cell(1).GetString() == label);
        return (int)row.Cell(2).GetDouble();
    }

    private XLWorkbook Open(string token) =>
        new(new MemoryStream(Resolve<ITempFileCacheManager>().GetFile(token)));

    private string Localized(string key) =>
        Resolve<ILocalizationManager>().GetString(HCMv3Consts.LocalizationSourceName, key);
}
