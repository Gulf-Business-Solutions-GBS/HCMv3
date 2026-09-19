using System.IO;
using System.Text;
using ClosedXML.Excel;
using HCMv3.GoLiveSetup;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GoLiveSetup;

/// <summary>
/// The legacy export, however SQL Server Management Studio or Excel saved it. Made-up values only.
/// </summary>
// ReSharper disable once InconsistentNaming
public class GoLiveLegacyFileReader_Tests
{
    private readonly GoLiveLegacyFileReader _reader = new();

    /// <summary>
    /// "Save Results As" leaves the headers out and writes blanks as NULL: the columns are taken in the query's
    /// order, and NULL reads as nothing.
    /// </summary>
    [Fact]
    public void Should_Read_A_Csv_Without_Headers_In_The_Query_Order()
    {
        var file = Read("legacy.csv",
            "E001,2026-09-30,1000,Basic Salary,Earning,12.5,30,12.5\n" +
            "E001,2026-09-30,4015,Paid Days,TimeType,NULL,30,NULL\n");

        file.Rows.Count.ShouldBe(2);
        file.Rows[0].Rate.ShouldBe(12.5m);
        file.Rows[0].PayMonth.ShouldBe(new System.DateTime(2026, 9, 30));
        file.Rows[1].Rate.ShouldBeNull();
        file.Rows[1].Number.ShouldBe(30m);
    }

    /// <summary>With headers the columns are found by name, in any order, and a quoted name keeps its comma.</summary>
    [Fact]
    public void Should_Find_Columns_By_Header_Name_In_Any_Order()
    {
        var file = Read("legacy.txt",
            "Amount\tWageTypeCode\tEmployeeCode\tWageTypeName\n" +
            "7.25\t1000\tE002\t\"Basic, Salary\"\n");

        file.Rows.ShouldHaveSingleItem();
        file.Rows[0].EmployeeCode.ShouldBe("E002");
        file.Rows[0].WageTypeName.ShouldBe("Basic, Salary");
        file.Rows[0].Amount.ShouldBe(7.25m);
    }

    /// <summary>A row without an employee or wage type code is counted and left out.</summary>
    [Fact]
    public void Should_Count_Rows_Without_Codes()
    {
        var file = Read("legacy.csv", "EmployeeCode,WageTypeCode,Amount\nE001,,1\n,1000,1\nE001,1000,1\n");

        file.Rows.ShouldHaveSingleItem();
        file.Unreadable.ShouldBe(2);
    }

    /// <summary>A workbook's numbers are read at full precision, not as the sheet formats them.</summary>
    [Fact]
    public void Should_Read_A_Workbook_At_Full_Precision()
    {
        using var book = new XLWorkbook();
        var sheet = book.AddWorksheet("Result");
        sheet.Cell(1, 1).Value = "EmployeeCode";
        sheet.Cell(1, 2).Value = "WageTypeCode";
        sheet.Cell(1, 3).Value = "Amount";
        sheet.Cell(2, 1).Value = "E001";
        sheet.Cell(2, 2).Value = "1000";
        sheet.Cell(2, 3).Value = 123.4567;
        sheet.Cell(2, 3).Style.NumberFormat.Format = "0.0";

        using var stream = new MemoryStream();
        book.SaveAs(stream);

        var file = _reader.Read("legacy.xlsx", stream.ToArray());

        file.Rows.ShouldHaveSingleItem();
        file.Rows[0].Amount.ShouldBe(123.4567m);
    }

    private GoLiveLegacyFile Read(string name, string text) => _reader.Read(name, Encoding.UTF8.GetBytes(text));
}
