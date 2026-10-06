using System;
using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;
using HCMv3.Ess.Dto;
using HCMv3.Ess.Exporting;
using HCMv3.Workflow;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Ess;

/// <summary>
/// What the self-service lists put in an Excel file. The workbook is built from rows alone, so these need no
/// database: they check the sheet a person would open.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EssListExcel_Tests
{
    // Row 1 is the title, row 3 the headings; data starts on row 4.
    private const int FirstDataRow = 4;

    private const int RequestNumber = 1;
    private const int StartDate = 3;
    private const int LeaveDays = 5;
    private const int LeaveDaysTotal = 6;
    private const int RequestLineNumber = 7;
    private const int RequestType = 8;
    private const int AskedTo = 10;
    private const int Status = 18;

    [Fact]
    public void Should_Write_One_Line_For_A_Leave_With_A_Single_Request()
    {
        var leave = Leave(12, days: 10, Request(12, WorkflowRequestAction.New, MyLeaveHistoryOutcome.Approved));

        using var book = EssListExcel.BuildLeaves(new[] { leave }, Label, arabic: false);
        var sheet = book.Worksheets.Single();

        DataRows(sheet).ShouldBe(1);
        sheet.Cell(FirstDataRow, RequestNumber).GetValue<int>().ShouldBe(12);
        sheet.Cell(FirstDataRow, RequestLineNumber).GetValue<int>().ShouldBe(12);
        sheet.Cell(FirstDataRow, RequestType).GetString().ShouldBe("NewLeave");
        sheet.Cell(FirstDataRow, Status).GetString().ShouldBe("LeaveHistoryOutcome_Approved");
    }

    [Fact]
    public void Should_Repeat_The_Leave_On_Every_One_Of_Its_Request_Lines()
    {
        var leave = Leave(
            9,
            days: 4,
            Request(9, WorkflowRequestAction.New, MyLeaveHistoryOutcome.Approved),
            Request(11, WorkflowRequestAction.Cancellation, MyLeaveHistoryOutcome.Rejected),
            Request(13, WorkflowRequestAction.Return, MyLeaveHistoryOutcome.Approved));

        using var book = EssListExcel.BuildLeaves(new[] { leave }, Label, arabic: false);
        var sheet = book.Worksheets.Single();

        // As many lines as requests, each still saying which leave it belongs to after any sort or filter.
        DataRows(sheet).ShouldBe(3);

        for (var row = FirstDataRow; row < FirstDataRow + 3; row++)
        {
            sheet.Cell(row, RequestNumber).GetValue<int>().ShouldBe(9);
            sheet.Cell(row, LeaveDays).GetValue<double>().ShouldBe(4);
        }

        Enumerable.Range(FirstDataRow, 3)
            .Select(row => sheet.Cell(row, RequestLineNumber).GetValue<int>())
            .ShouldBe(new[] { 9, 11, 13 });

        sheet.Cell(FirstDataRow + 1, RequestType).GetString().ShouldBe("CancelLeave");
        sheet.Cell(FirstDataRow + 2, RequestType).GetString().ShouldBe("ReturnFromLeave");
    }

    [Fact]
    public void Should_Carry_The_Total_Days_On_A_Leaves_First_Line_Only()
    {
        var leaves = new[]
        {
            Leave(
                9,
                days: 4,
                Request(9, WorkflowRequestAction.New, MyLeaveHistoryOutcome.Approved),
                Request(13, WorkflowRequestAction.Return, MyLeaveHistoryOutcome.Approved)),
            Leave(12, days: 10, Request(12, WorkflowRequestAction.New, MyLeaveHistoryOutcome.Approved))
        };

        using var book = EssListExcel.BuildLeaves(leaves, Label, arabic: false);
        var sheet = book.Worksheets.Single();

        // Adding the column up gives 14, not 4 + 4 + 10.
        sheet.Cell(FirstDataRow, LeaveDaysTotal).GetValue<double>().ShouldBe(4);
        sheet.Cell(FirstDataRow + 1, LeaveDaysTotal).IsEmpty().ShouldBeTrue();
        sheet.Cell(FirstDataRow + 2, LeaveDaysTotal).GetValue<double>().ShouldBe(10);
    }

    [Fact]
    public void Should_Group_The_Later_Lines_Under_The_Leaves_First_Line()
    {
        var leave = Leave(
            9,
            days: 4,
            Request(9, WorkflowRequestAction.New, MyLeaveHistoryOutcome.Approved),
            Request(13, WorkflowRequestAction.Return, MyLeaveHistoryOutcome.Approved));

        using var book = EssListExcel.BuildLeaves(new[] { leave }, Label, arabic: false);
        var sheet = book.Worksheets.Single();

        sheet.Row(FirstDataRow).OutlineLevel.ShouldBe(0);
        sheet.Row(FirstDataRow + 1).OutlineLevel.ShouldBe(1);
        sheet.Cell(FirstDataRow, RequestNumber).Style.Font.Bold.ShouldBeTrue();
        sheet.Cell(FirstDataRow + 1, RequestNumber).Style.Font.Bold.ShouldBeFalse();
    }

    [Fact]
    public void Should_Write_Dates_As_Dates_Not_Text()
    {
        var leave = Leave(12, days: 10, Request(12, WorkflowRequestAction.New, MyLeaveHistoryOutcome.Approved));

        using var book = EssListExcel.BuildLeaves(new[] { leave }, Label, arabic: false);
        var sheet = book.Worksheets.Single();

        sheet.Cell(FirstDataRow, StartDate).DataType.ShouldBe(XLDataType.DateTime);
        sheet.Cell(FirstDataRow, StartDate).GetDateTime().ShouldBe(new DateTime(2026, 10, 1));
        sheet.Cell(FirstDataRow, AskedTo).GetDateTime().ShouldBe(new DateTime(2026, 10, 10));
    }

    [Fact]
    public void Should_Still_Write_A_Line_For_A_Leave_That_Has_No_Request()
    {
        // Entered by HR, or a draft not yet sent.
        var leave = Leave(0, days: 2);
        leave.RequestNumber = null;
        leave.DisplayStatus = MyLeaveRequestStatus.Draft;

        using var book = EssListExcel.BuildLeaves(new[] { leave }, Label, arabic: false);
        var sheet = book.Worksheets.Single();

        DataRows(sheet).ShouldBe(1);
        sheet.Cell(FirstDataRow, RequestLineNumber).IsEmpty().ShouldBeTrue();
        sheet.Cell(FirstDataRow, Status).GetString().ShouldBe("WorkflowRequestStatus_Draft");
    }

    [Fact]
    public void Should_Use_The_Arabic_Leave_Type_Name_For_An_Arabic_Reader()
    {
        var leave = Leave(12, days: 10, Request(12, WorkflowRequestAction.New, MyLeaveHistoryOutcome.Approved));

        using var book = EssListExcel.BuildLeaves(new[] { leave }, Label, arabic: true);

        book.Worksheets.Single().Cell(FirstDataRow, 2).GetString().ShouldBe("إجازة سنوية");
    }

    [Fact]
    public void A_Plain_List_Should_Write_Numbers_And_Dates_As_Such_And_Keep_Text_As_Text()
    {
        var input = new EssListExportInput
        {
            Title = "Bank Details",
            Columns = new List<EssListExportColumnDto>
            {
                new() { Header = "Account Number", Type = EssListExportColumnType.Text },
                new() { Header = "Amount", Type = EssListExportColumnType.Number },
                new() { Header = "Start Date", Type = EssListExportColumnType.Date }
            },
            Rows = new List<List<string>>
            {
                // An account number is digits but must not become a number: it would lose its leading zeros.
                new() { "0012345678", "1234.5", "2026-10-05" },
                new() { "0099", "", "not a date" }
            }
        };

        using var book = EssListExcel.BuildFlat(input);
        var sheet = book.Worksheets.Single();

        sheet.Cell(3, 1).GetString().ShouldBe("Account Number");
        sheet.Cell(FirstDataRow, 1).GetString().ShouldBe("0012345678");
        sheet.Cell(FirstDataRow, 1).DataType.ShouldBe(XLDataType.Text);
        sheet.Cell(FirstDataRow, 2).GetValue<double>().ShouldBe(1234.5);
        sheet.Cell(FirstDataRow, 3).GetDateTime().ShouldBe(new DateTime(2026, 10, 5));

        // Nothing is dropped: what does not read as a date is kept as the text it came as.
        sheet.Cell(FirstDataRow + 1, 2).IsEmpty().ShouldBeTrue();
        sheet.Cell(FirstDataRow + 1, 3).GetString().ShouldBe("not a date");
    }

    private static int DataRows(IXLWorksheet sheet) => sheet.LastRowUsed().RowNumber() - FirstDataRow + 1;

    /// <summary>The key itself, so a test can say which label a cell was given without a language file.</summary>
    private static string Label(string key) => key;

    private static MyLeaveDto Leave(int number, double days, params MyLeaveHistoryItemDto[] requests)
    {
        return new MyLeaveDto
        {
            Id = number,
            RequestNumber = number.ToString(),
            AbsenceTypeNameEn = "Annual Paid Leave",
            AbsenceTypeNameAr = "إجازة سنوية",
            StartDate = new DateTime(2026, 10, 1),
            EndDate = new DateTime(2026, 10, 10),
            TotalLeaveDays = days,
            DisplayStatus = MyLeaveRequestStatus.Approved,
            Requests = requests.ToList()
        };
    }

    private static MyLeaveHistoryItemDto Request(int number, WorkflowRequestAction action, MyLeaveHistoryOutcome outcome)
    {
        return new MyLeaveHistoryItemDto
        {
            WorkflowRequestId = number,
            RequestNumber = number.ToString(),
            Action = action,
            Outcome = outcome,
            StartDate = new DateTime(2026, 10, 1),
            EndDate = new DateTime(2026, 10, 10),
            Days = 10
        };
    }
}
