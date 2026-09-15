using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.Localization;
using ClosedXML.Excel;
using HCMv3.GoLiveSetupV2;
using HCMv3.GoLiveSetupV2.Dto;
using HCMv3.Storage;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GoLiveSetupV2;

/// <summary>
/// The two workbooks V2 hands a customer: the employee sheet to fill in, and the comparison to check.
/// Made-up figures only.
/// </summary>
// ReSharper disable once InconsistentNaming
public class GoLiveV2ExcelExports_Tests : AppTestBase
{
    private readonly IGoLiveV2AppService _goLive;

    public GoLiveV2ExcelExports_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _goLive = Resolve<IGoLiveV2AppService>();
    }

    /// <summary>
    /// The sheet has the grid's columns in the grid's order, so it pastes back; a civil ID stays text however
    /// long; and an empty grid gives a header-only sheet to start from.
    /// </summary>
    [Fact]
    public async Task Should_Export_The_Employee_Sheet_In_The_Grids_Columns()
    {
        var file = await _goLive.ExportEmployeeSheet(new GoLiveV2EmployeeSheetInput
        {
            Employees = new List<GoLiveV2EmployeeRowDto>
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
        sheet.Cell(1, 1).GetString().ShouldBe(Localized("GoLiveV2ColCode"));
        sheet.Cell(1, 12).GetString().ShouldBe(Localized("GoLiveV2ColAnnualLeaveOpening"));
        sheet.Cell(1, 15).GetString().ShouldBe(Localized("GoLiveV2ColAccountNumber"));
        sheet.Cell(2, 4).Value.IsText.ShouldBeTrue();
        sheet.Cell(2, 4).GetString().ShouldBe("290011501234");
        sheet.Cell(2, 3).GetDateTime().ShouldBe(new DateTime(2020, 1, 15));
        sheet.Cell(2, 5).GetDouble().ShouldBe(500);
        sheet.Cell(2, 12).GetDouble().ShouldBe(12.5);

        using var empty = Open((await _goLive.ExportEmployeeSheet(new GoLiveV2EmployeeSheetInput())).FileToken);
        empty.Worksheet(1).LastRowUsed().RowNumber().ShouldBe(1);
    }

    /// <summary>
    /// Each legacy/HCMv3 pair is coloured by whether it agrees, and the Summary sheet counts an employee or wage
    /// type as matched only when all its lines are.
    /// </summary>
    [Fact]
    public async Task Should_Colour_The_Comparison_And_Count_What_Matched()
    {
        var file = await _goLive.ExportLegacyComparison(new GoLiveV2LegacyCompareExportInput
        {
            PayMonth = new DateTime(2026, 9, 30),
            Lines = new List<GoLiveV2LegacyCompareLineDto>
            {
                Line("E001", "1000", matched: true),
                Line("E001", "4015", matched: false),
                Line("E002", "1000", matched: true),
            },
        });

        using var book = Open(file.FileToken);
        var summary = book.Worksheet(1);
        Count(summary, "GoLiveV2CompareEmployeesMatched").ShouldBe(1);
        Count(summary, "GoLiveV2CompareEmployeesNotMatched").ShouldBe(1);
        summary.RowsUsed().First(r => r.Cell(1).GetString() == Localized("GoLiveV2CompareEmployeesMatchedPercent"))
            .Cell(2).GetDouble().ShouldBe(0.5);
        Count(summary, "GoLiveV2CompareWageTypesMatched").ShouldBe(1);
        Count(summary, "GoLiveV2CompareWageTypesNotMatched").ShouldBe(1);

        var lines = book.Worksheet(2);
        lines.Cell(1, 4).GetString().ShouldBe(Localized("GoLiveV2CompareColRateLegacy"));
        var differing = Enumerable.Range(2, 3).Single(r => lines.Cell(r, 2).GetString() == "4015");
        var agreeing = Enumerable.Range(2, 3).First(r => lines.Cell(r, 2).GetString() == "1000");
        lines.Cell(differing, 6).Style.Fill.BackgroundColor.ShouldNotBe(lines.Cell(agreeing, 6).Style.Fill.BackgroundColor);
        lines.Cell(differing, 10).GetString().ShouldBe(Localized("GoLiveV2CompareNotMatched"));
    }

    private static GoLiveV2LegacyCompareLineDto Line(string employee, string wage, bool matched) =>
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
            Status = matched ? GoLiveV2CompareStatus.Matched : GoLiveV2CompareStatus.Different,
            Cause = matched ? GoLiveV2CompareCause.None : GoLiveV2CompareCause.Days,
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
