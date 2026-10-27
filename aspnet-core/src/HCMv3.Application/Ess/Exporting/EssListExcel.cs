using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ClosedXML.Excel;
using HCMv3.Ess.Dto;
using HCMv3.Workflow;
using static HCMv3.Dashboards.DashboardExcel;

namespace HCMv3.Ess.Exporting;

/// <summary>
/// The workbooks the self-service request lists export.
///
/// <para>
/// Kept apart from the services that fetch the rows, and free of anything but the rows and a way to look up a
/// label, so that what ends up in each cell can be tested without a database or a file.
/// </para>
/// </summary>
public static class EssListExcel
{
    /// <summary>
    /// A leave list: ONE LINE PER REQUEST, with the leave it belongs to repeated on every line.
    ///
    /// <para>
    /// Repeated rather than indented or merged, because the first thing anyone does with an export is sort or
    /// filter it -- and then a child line has to still say which leave it belongs to. The lines of one leave
    /// are grouped (Excel's outline) with the first in bold, so the sheet still collapses to one line per
    /// leave and reads like the screen.
    /// </para>
    ///
    /// <para>
    /// Because the leave's days repeat down its lines, adding that column up would count a leave once per
    /// request. "Leave Days (total)" carries the figure on a leave's first line only, and is the one to total.
    /// </para>
    /// </summary>
    public static XLWorkbook BuildLeaves(IReadOnlyList<MyLeaveDto> leaves, Func<string, string> l, bool arabic)
    {
        var book = new XLWorkbook();
        var sheet = book.Worksheets.Add(Sheet(l("EssMyLeave"), book));

        var headers = new[]
        {
            l("RequestNumber"), l("LeaveType"), l("StartDate"), l("EndDate"), l("LeaveDays"), l("LeaveDaysTotal"),
            l("RequestLineNumber"), l("RequestType"), l("AskedFrom"), l("AskedTo"), l("AskedDays"), l("ReturnDate"),
            l("PaidLeaveStartDate"), l("PaidLeaveEndDate"), l("UnpaidLeaveStartDate"), l("UnpaidLeaveEndDate"),
            l("Reason"), l("Status"), l("DecidedBy"), l("DecidedOn")
        };

        Title(sheet, l("EssMyLeave"));
        Header(sheet, HeaderRow, headers);

        var row = HeaderRow;

        foreach (var leave in leaves)
        {
            var first = row + 1;

            // A leave nobody requested -- HR entered it -- or a draft not yet sent has no request of its own;
            // it is still a leave, so it gets its one line.
            var lines = leave.Requests is { Count: > 0 }
                ? leave.Requests
                : new List<MyLeaveHistoryItemDto> { null };

            foreach (var request in lines)
            {
                row++;

                var cells = new object[]
                {
                    Number(leave.RequestNumber),
                    Pick(arabic, leave.AbsenceTypeNameEn, leave.AbsenceTypeNameAr),
                    leave.StartDate.Date,
                    leave.EndDate.Date,
                    leave.TotalLeaveDays,
                    row == first ? leave.TotalLeaveDays : null,
                    Number(request?.RequestNumber),
                    l(ActionLabel(request?.Action ?? leave.RequestAction)),
                    request?.StartDate?.Date,
                    request?.EndDate?.Date,
                    request?.Days,
                    request?.ReturnDate?.Date,
                    request?.PaidStartDate?.Date,
                    request?.PaidEndDate?.Date,
                    request?.UnpaidStartDate?.Date,
                    request?.UnpaidEndDate?.Date,
                    request?.Reason,
                    request == null
                        ? l("WorkflowRequestStatus_" + leave.DisplayStatus)
                        : l("LeaveHistoryOutcome_" + request.Outcome),
                    request == null ? null : Pick(arabic, request.DecidedByNameEn, request.DecidedByNameAr),
                    request?.DecidedOn?.Date
                };

                for (var column = 0; column < cells.Length; column++)
                {
                    SetCell(sheet.Cell(row, column + 1), cells[column]);
                }
            }

            sheet.Range(first, 1, first, headers.Length).Style.Font.Bold = true;

            if (row > first)
            {
                sheet.Rows(first + 1, row).Group();
            }
        }

        // The +/- sits on the leave's own line, above the lines it folds away.
        sheet.Outline.SummaryVLocation = XLOutlineSummaryVLocation.Top;

        Finish(sheet, headers.Length, row);

        return book;
    }

    /// <summary>
    /// A plain list -- every other request screen: one line per row, the columns the screen shows.
    /// A cell that says it is a number or a date and does not read as one is written as the text it came as,
    /// rather than dropped.
    /// </summary>
    public static XLWorkbook BuildFlat(EssListExportInput input)
    {
        var book = new XLWorkbook();
        var sheet = book.Worksheets.Add(Sheet(input.Title, book));

        Title(sheet, input.Title);
        Header(sheet, HeaderRow, input.Columns.Select(c => c.Header).ToList());

        var row = HeaderRow;

        foreach (var line in input.Rows)
        {
            row++;

            for (var column = 0; column < input.Columns.Count && column < line.Count; column++)
            {
                SetCell(sheet.Cell(row, column + 1), Typed(line[column], input.Columns[column].Type));
            }
        }

        Finish(sheet, input.Columns.Count, row);

        return book;
    }

    private static object Typed(string value, EssListExportColumnType type)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        switch (type)
        {
            case EssListExportColumnType.Number
                when double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number):
                return number;

            case EssListExportColumnType.Date
                when DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date):
                return date.Date;

            default:
                return value;
        }
    }

    /// <summary>Request numbers are digits; written as a number they sort 9 before 10, as text they do not.</summary>
    private static object Number(string requestNumber)
    {
        return int.TryParse(requestNumber, NumberStyles.None, CultureInfo.InvariantCulture, out var number)
            ? number
            : requestNumber;
    }

    private static string ActionLabel(WorkflowRequestAction action)
    {
        return action switch
        {
            WorkflowRequestAction.Amendment => "AmendLeave",
            WorkflowRequestAction.Cancellation => "CancelLeave",
            WorkflowRequestAction.Return => "ReturnFromLeave",
            _ => "NewLeave"
        };
    }
}
