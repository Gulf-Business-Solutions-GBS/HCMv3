using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Abp;
using Abp.AspNetZeroCore.Net;
using Abp.Dependency;
using ClosedXML.Excel;
using HCMv3.Dashboards.Workflow.Dto;
using HCMv3.Dto;
using HCMv3.Storage;
using HCMv3.Workflow;
using static HCMv3.Dashboards.DashboardExcel;

namespace HCMv3.Dashboards.Workflow;

public class WorkflowDashboardExportData
{
    public WorkflowDashboardExportPart Part { get; init; }

    public WorkflowDashboardDto Dashboard { get; init; }

    public IReadOnlyList<WorkflowRequestRowDto> Requests { get; init; } = Array.Empty<WorkflowRequestRowDto>();

    /// <summary>Request type names by code, in the reader's language.</summary>
    public IReadOnlyDictionary<string, string> TypeNames { get; init; } = new Dictionary<string, string>();

    public string ScopeLabel { get; init; }

    public bool UseArabic { get; init; }
}

/// <summary>The Workflow Requests Dashboard as a workbook: a Summary sheet, then one sheet per tile.</summary>
public class WorkflowDashboardExcelExporter : AbpServiceBase, ITransientDependency
{
    private readonly ITempFileCacheManager _tempFiles;

    public WorkflowDashboardExcelExporter(ITempFileCacheManager tempFiles)
    {
        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;
        _tempFiles = tempFiles;
    }

    public FileDto Export(WorkflowDashboardExportData data)
    {
        using var book = new XLWorkbook();
        var d = data.Dashboard;
        var all = data.Part == WorkflowDashboardExportPart.All;
        bool Wants(WorkflowDashboardExportPart part) => all || data.Part == part;
        string TypeName(string code) => code != null && data.TypeNames.TryGetValue(code, out var n) ? n : code;

        if (all)
        {
            AddSummary(book, data);
        }

        if (Wants(WorkflowDashboardExportPart.Trend))
        {
            var statuses = new[] { RequestStatus.Open, RequestStatus.Approved, RequestStatus.Rejected, RequestStatus.Closed, RequestStatus.Transferred };
            var headers = new List<string> { L("WorkflowDashboard_Month") };
            headers.AddRange(statuses.Select(StatusLabel));
            headers.Add(L("Total"));
            AddTable(book, L("WorkflowDashboard_ByMonth"), headers,
                d.Trend.Select(p =>
                {
                    var row = new List<object> { p.MonthStart.ToString("MMM yyyy", CultureInfo.InvariantCulture) };
                    row.AddRange(statuses.Select(s => (object)(p.Counts.FirstOrDefault(c => c.Status == s)?.Count ?? 0)));
                    row.Add(p.Counts.Sum(c => c.Count));
                    return row.ToArray();
                }));
        }

        if (Wants(WorkflowDashboardExportPart.ByType))
        {
            AddTable(book, L("WorkflowDashboard_ByType"),
                new[]
                {
                    L("WorkflowDashboard_RequestType"), L("WorkflowDashboard_Raised"), L("Open"), L("Approved"), L("Rejected"),
                    L("WorkflowDashboard_Overdue"), L("WorkflowDashboard_DaysToDecide")
                },
                d.ByType.Select(b => new object[] { TypeName(b.RequestType), b.Raised, b.Open, b.Approved, b.Rejected, b.Overdue, b.AverageDaysToDecide }));
        }

        if (Wants(WorkflowDashboardExportPart.ByOrgUnit))
        {
            AddTable(book, L("WorkflowDashboard_ByDepartment"),
                new[] { L("OrganizationUnit"), L("WorkflowDashboard_Raised"), L("Open"), L("WorkflowDashboard_Overdue"), L("WorkflowDashboard_DaysToDecide") },
                d.ByOrgUnit.Select(b => new object[]
                {
                    b.OrganizationUnitId == null ? L("LeaveDashboard_Unassigned") : b.IsDirectStaff ? L("LeaveDashboard_DirectStaff", b.Name) : b.Name,
                    b.Raised, b.Open, b.Overdue, b.AverageDaysToDecide
                }));
        }

        if (Wants(WorkflowDashboardExportPart.Waiting))
        {
            AddTable(book, L("WorkflowDashboard_WaitingWith"),
                new[] { L("Code"), L("WorkflowDashboard_Approver"), L("WorkflowDashboard_Waiting"), L("WorkflowDashboard_Overdue"), L("WorkflowDashboard_OldestDays") },
                d.Waiting.Select(w => new object[] { w.ApproverCode, Pick(data.UseArabic, w.ApproverNameEn, w.ApproverNameAr), w.Waiting, w.Overdue, w.OldestDays }));
        }

        if (Wants(WorkflowDashboardExportPart.WaitingBands))
        {
            AddTable(book, L("WorkflowDashboard_WaitingBands"),
                new[] { L("WorkflowDashboard_DaysWaiting"), L("WorkflowDashboard_Requests") },
                d.WaitingBands.Select(b => new object[] { BandLabel(b), b.Requests }));
        }

        if (Wants(WorkflowDashboardExportPart.Requests))
        {
            AddTable(book, L("WorkflowDashboard_Requests"),
                new[]
                {
                    L("WorkflowDashboard_RequestNumber"), L("WorkflowDashboard_RequestType"), L("Code"), L("Employee"), L("OrganizationUnit"),
                    L("WorkflowDashboard_RaisedOn"), L("Status"), L("WorkflowDashboard_Step"), L("WorkflowDashboard_WaitingWith"),
                    L("WorkflowDashboard_Days"), L("WorkflowDashboard_DueDate")
                },
                data.Requests.Select(r => new object[]
                {
                    r.RequestNumber, TypeName(r.RequestType), r.EmployeeCode, Pick(data.UseArabic, r.EmployeeNameEn, r.EmployeeNameAr),
                    r.OrganizationUnitName, r.CreationTime, StatusLabel(r.Status), r.WorkflowStep,
                    Pick(data.UseArabic, r.WaitingWithEn, r.WaitingWithAr), r.Days, r.DueDate
                }));
        }

        var fileName = $"{L("WorkflowDashboard")} {d.FromDate:yyyy-MM-dd} - {d.ToDate:yyyy-MM-dd}.xlsx";
        var file = new FileDto(fileName, MimeTypeNames.ApplicationVndOpenxmlformatsOfficedocumentSpreadsheetmlSheet);
        using var stream = new MemoryStream();
        book.SaveAs(stream);
        _tempFiles.SetFile(file.FileToken, stream.ToArray());
        return file;
    }

    /// <summary>Closed is how a request the employee withdrew ends up.</summary>
    private string StatusLabel(RequestStatus status) =>
        status == RequestStatus.Closed ? L("WorkflowDashboard_Withdrawn") : L(status.ToString());

    private string BandLabel(WorkflowWaitingBandDto band) =>
        band.ToDays.HasValue
            ? L("WorkflowDashboard_BandRange", band.FromDays, band.ToDays.Value)
            : L("WorkflowDashboard_BandFrom", band.FromDays);

    private static void AddTable(XLWorkbook book, string title, IReadOnlyList<string> headers, IEnumerable<object[]> rows)
    {
        var sheet = book.AddWorksheet(Sheet(title, book));
        Title(sheet, title);
        Header(sheet, HeaderRow, headers.ToList());

        var r = HeaderRow + 1;
        foreach (var row in rows)
        {
            for (var c = 0; c < row.Length; c++)
            {
                SetCell(sheet.Cell(r, c + 1), row[c]);
            }

            r++;
        }

        Finish(sheet, headers.Count, r - 1);
    }

    private void AddSummary(XLWorkbook book, WorkflowDashboardExportData data)
    {
        var d = data.Dashboard;
        var k = d.Kpis;
        var sheet = book.AddWorksheet(Sheet(L("Summary")));
        Title(sheet, L("WorkflowDashboard"));

        var unit = d.OrgUnitPath.Count > 0 ? string.Join(" › ", d.OrgUnitPath.Select(p => p.Name)) : L("LeaveDashboard_AllUnits");
        var rows = new List<(string, object)>
        {
            (L("Dashboard_Showing"), data.ScopeLabel),
            (L("FromDate"), d.FromDate),
            (L("ToDate"), d.ToDate),
            (L("OrganizationUnit"), unit),
            (string.Empty, null),
            (L("WorkflowDashboard_Raised"), k.Raised),
            (L("Open"), k.Open),
            (L("Approved"), k.Approved),
            (L("Rejected"), k.Rejected),
            (L("WorkflowDashboard_Withdrawn"), k.Withdrawn),
            (L("Transferred"), k.Transferred),
            (L("WorkflowDashboard_ApprovalRate"), k.ApprovalRatePercent),
            (L("WorkflowDashboard_Overdue"), k.Overdue),
            (L("WorkflowDashboard_DaysToDecide"), k.AverageDaysToDecide),
            (L("WorkflowDashboard_LongestWait"), k.LongestWaitDays)
        };

        var r = HeaderRow;
        foreach (var (label, value) in rows)
        {
            sheet.Cell(r, 1).SetValue(label);
            sheet.Cell(r, 1).Style.Font.Bold = true;
            SetCell(sheet.Cell(r, 2), value);
            r++;
        }

        sheet.Columns(1, 2).AdjustToContents();
    }
}
