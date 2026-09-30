using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Abp;
using Abp.AspNetZeroCore.Net;
using Abp.Dependency;
using ClosedXML.Excel;
using HCMv3.Dashboards.Leave.Dto;
using HCMv3.Dto;
using HCMv3.Storage;

namespace HCMv3.Dashboards.Leave;

/// <summary>Everything one Leave Dashboard export needs, already scoped and filtered by the app service.</summary>
public class LeaveDashboardExportData
{
    public LeaveDashboardExportPart Part { get; init; }

    public LeaveDashboardDto Dashboard { get; init; }

    /// <summary>Only for <see cref="LeaveDashboardExportPart.All"/> and <see cref="LeaveDashboardExportPart.LeavesInPeriod"/>.</summary>
    public IReadOnlyList<LeaveDashboardLeaveRowDto> PeriodLeaves { get; init; } = Array.Empty<LeaveDashboardLeaveRowDto>();

    public DateTime? Day { get; init; }

    public string ScopeLabel { get; init; }

    public string AbsenceTypeLabel { get; init; }

    /// <summary>Leave type names by id, in the reader's language, for the trend columns.</summary>
    public IReadOnlyDictionary<int, string> TypeNames { get; init; } = new Dictionary<int, string>();

    public bool UseArabic { get; init; }
}

/// <summary>
/// The Leave Dashboard as a workbook. The whole dashboard is a Summary sheet (filters and headline numbers) plus one
/// sheet per tile; a single tile is just its own sheet. Built in memory and handed to the temporary file store the
/// download reads from.
/// </summary>
public class LeaveDashboardExcelExporter : AbpServiceBase, ITransientDependency
{
    private static readonly XLColor HeaderFill = XLColor.FromHtml("#E9EEF6");
    private static readonly XLColor AlertFont = XLColor.FromHtml("#C0392B");
    private const string DateFormat = "dd/MM/yyyy";

    private readonly ITempFileCacheManager _tempFiles;

    public LeaveDashboardExcelExporter(ITempFileCacheManager tempFiles)
    {
        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;
        _tempFiles = tempFiles;
    }

    public FileDto Export(LeaveDashboardExportData data)
    {
        using var book = new XLWorkbook();
        var d = data.Dashboard;

        var all = data.Part == LeaveDashboardExportPart.All;

        if (all)
        {
            AddSummary(book, data);
        }

        if (all || data.Part == LeaveDashboardExportPart.OnLeaveToday)
        {
            AddLeaveSheet(book, L("LeaveDashboard_OnLeaveToday"), d.OnLeaveToday, data, overdue: false);
        }

        if (all || data.Part == LeaveDashboardExportPart.NotReturned)
        {
            AddLeaveSheet(book, L("LeaveDashboard_NotReturned"), d.NotReturned, data, overdue: true);
        }

        if (all || data.Part == LeaveDashboardExportPart.StartingSoon)
        {
            AddLeaveSheet(book, L("LeaveDashboard_StartingSoon"), d.StartingSoon, data, overdue: false);
        }

        if (all || data.Part == LeaveDashboardExportPart.ReturningSoon)
        {
            AddLeaveSheet(book, L("LeaveDashboard_ReturningSoon"), d.ReturningSoon, data, overdue: false);
        }

        if (all || data.Part == LeaveDashboardExportPart.PendingApproval)
        {
            AddLeaveSheet(book, L("LeaveDashboard_PendingApproval"), d.PendingApproval, data, overdue: false);
        }

        if (all || data.Part == LeaveDashboardExportPart.Trend)
        {
            AddTrend(book, data);
        }

        if (all || data.Part == LeaveDashboardExportPart.ByOrgUnit)
        {
            AddByOrgUnit(book, d);
        }

        if (all || data.Part == LeaveDashboardExportPart.ByType)
        {
            AddByType(book, data);
        }

        if (all || data.Part == LeaveDashboardExportPart.LeavesInPeriod)
        {
            var title = data.Day.HasValue
                ? L("LeaveDashboard_OutOnDay", data.Day.Value.ToString(DateFormat, CultureInfo.InvariantCulture))
                : L("LeaveDashboard_LeaveInPeriod");
            AddLeaveSheet(book, title, data.PeriodLeaves, data, overdue: false);
        }

        var fileName = $"{L("LeaveDashboard")} {d.FromDate:yyyy-MM-dd} - {d.ToDate:yyyy-MM-dd}.xlsx";
        var file = new FileDto(fileName, MimeTypeNames.ApplicationVndOpenxmlformatsOfficedocumentSpreadsheetmlSheet);

        using var stream = new MemoryStream();
        book.SaveAs(stream);
        _tempFiles.SetFile(file.FileToken, stream.ToArray());
        return file;
    }

    // ---------------------------------------------------------------- sheets

    private void AddSummary(XLWorkbook book, LeaveDashboardExportData data)
    {
        var d = data.Dashboard;
        var sheet = book.AddWorksheet(Sheet(L("Summary")));
        Title(sheet, L("LeaveDashboard"));

        var unit = d.OrgUnitPath.Count > 0 ? string.Join(" › ", d.OrgUnitPath.Select(p => p.Name)) : L("LeaveDashboard_AllUnits");

        var rows = new List<(string Label, object Value)>
        {
            (L("Dashboard_Showing"), data.ScopeLabel),
            (L("FromDate"), d.FromDate),
            (L("ToDate"), d.ToDate),
            (L("OrganizationUnit"), unit),
            (L("LeaveType"), data.AbsenceTypeLabel),
            (string.Empty, null),
            (L("LeaveDashboard_OnLeaveToday"), d.Kpis.OnLeaveToday),
            (L("LeaveDashboard_NotReturned"), d.Kpis.NotReturned),
            (L("LeaveDashboard_StartingSoon") + " (" + L("LeaveDashboard_Next7Days") + ")", d.Kpis.StartingSoon),
            (L("LeaveDashboard_ReturningSoon") + " (" + L("LeaveDashboard_Next7Days") + ")", d.Kpis.ReturningSoon),
            (L("LeaveDashboard_PendingApproval"), d.Kpis.PendingApproval),
            (L("LeaveDashboard_LeaveDaysInPeriod"), d.Kpis.LeaveDaysInPeriod),
            (L("Employees"), d.Kpis.EmployeesInScope),
            (L("LeaveDashboard_AsOf", d.AsOfDate.ToString(DateFormat, CultureInfo.InvariantCulture)), null)
        };

        var r = 3;
        foreach (var (label, value) in rows)
        {
            sheet.Cell(r, 1).SetValue(label);
            sheet.Cell(r, 1).Style.Font.Bold = true;
            SetCell(sheet.Cell(r, 2), value);
            r++;
        }

        sheet.Columns(1, 2).AdjustToContents();
    }

    private void AddLeaveSheet(
        XLWorkbook book,
        string title,
        IReadOnlyList<LeaveDashboardLeaveRowDto> rows,
        LeaveDashboardExportData data,
        bool overdue)
    {
        var sheet = book.AddWorksheet(Sheet(title, book));
        Title(sheet, title);

        var headers = new List<string>
        {
            L("Code"), L("Employee"), L("OrganizationUnit"), L("LeaveType"), L("StartDate"), L("EndDate"),
            L("TotalLeaveDays")
        };
        if (overdue)
        {
            headers.Add(L("DaysOverdue"));
        }

        Header(sheet, 3, headers);

        var r = 4;
        foreach (var row in rows)
        {
            sheet.Cell(r, 1).SetValue(row.EmployeeCode);
            sheet.Cell(r, 2).SetValue(Pick(data.UseArabic, row.EmployeeNameEn, row.EmployeeNameAr));
            sheet.Cell(r, 3).SetValue(row.OrganizationUnitName);
            sheet.Cell(r, 4).SetValue(Pick(data.UseArabic, row.AbsenceTypeNameEn, row.AbsenceTypeNameAr));
            SetCell(sheet.Cell(r, 5), row.StartDate);
            SetCell(sheet.Cell(r, 6), row.EndDate);
            sheet.Cell(r, 7).SetValue(row.TotalLeaveDays);
            if (overdue)
            {
                sheet.Cell(r, 8).SetValue(row.DaysOverdue);
                sheet.Cell(r, 8).Style.Font.FontColor = AlertFont;
            }

            r++;
        }

        Finish(sheet, headers.Count, r - 1);
    }

    private void AddTrend(XLWorkbook book, LeaveDashboardExportData data)
    {
        var d = data.Dashboard;
        var daily = d.Granularity == LeaveDashboardGranularity.Day;
        var title = L(daily ? "LeaveDashboard_ByDay" : "LeaveDashboard_ByMonth");
        var sheet = book.AddWorksheet(Sheet(title, book));
        Title(sheet, title);
        sheet.Cell(2, 1).SetValue(L(daily ? "LeaveDashboard_ByDayHint" : "LeaveDashboard_ByMonthHint"));

        var typeIds = d.Trend.SelectMany(p => p.Values).Select(v => v.AbsenceTypeId).Distinct().OrderBy(id => id).ToList();

        var headers = new List<string> { L("Period") };
        headers.AddRange(typeIds.Select(id => data.TypeNames.TryGetValue(id, out var name) ? name : "#" + id));
        headers.Add(L("Total"));
        Header(sheet, 3, headers);

        var r = 4;
        foreach (var point in d.Trend)
        {
            sheet.Cell(r, 1).SetValue(daily
                ? point.PeriodStart.ToString(DateFormat, CultureInfo.InvariantCulture)
                : point.PeriodStart.ToString("MMM yyyy", CultureInfo.InvariantCulture));

            for (var i = 0; i < typeIds.Count; i++)
            {
                var value = point.Values.FirstOrDefault(v => v.AbsenceTypeId == typeIds[i])?.Value ?? 0;
                sheet.Cell(r, i + 2).SetValue(value);
            }

            sheet.Cell(r, typeIds.Count + 2).SetValue(point.Values.Sum(v => v.Value));
            sheet.Cell(r, typeIds.Count + 2).Style.Font.Bold = true;
            r++;
        }

        Finish(sheet, headers.Count, r - 1);
    }

    private void AddByOrgUnit(XLWorkbook book, LeaveDashboardDto d)
    {
        var title = L("LeaveDashboard_ByDepartment");
        var sheet = book.AddWorksheet(Sheet(title, book));
        Title(sheet, title);

        var headers = new List<string> { L("OrganizationUnit"), L("LeaveDashboard_EmployeesOnLeave"), L("LeaveDays") };
        Header(sheet, 3, headers);

        var r = 4;
        foreach (var bucket in d.ByOrgUnit)
        {
            var name = bucket.OrganizationUnitId == null
                ? L("LeaveDashboard_Unassigned")
                : bucket.IsDirectStaff ? L("LeaveDashboard_DirectStaff", bucket.Name) : bucket.Name;

            sheet.Cell(r, 1).SetValue(name);
            sheet.Cell(r, 2).SetValue(bucket.Employees);
            sheet.Cell(r, 3).SetValue(bucket.LeaveDays);
            r++;
        }

        Finish(sheet, headers.Count, r - 1);
    }

    private void AddByType(XLWorkbook book, LeaveDashboardExportData data)
    {
        var title = L("LeaveDashboard_ByType");
        var sheet = book.AddWorksheet(Sheet(title, book));
        Title(sheet, title);

        var headers = new List<string> { L("LeaveType"), L("LeaveDashboard_EmployeesOnLeave"), L("LeaveDays") };
        Header(sheet, 3, headers);

        var r = 4;
        foreach (var bucket in data.Dashboard.ByType)
        {
            sheet.Cell(r, 1).SetValue(Pick(data.UseArabic, bucket.NameEn, bucket.NameAr));
            sheet.Cell(r, 2).SetValue(bucket.Employees);
            sheet.Cell(r, 3).SetValue(bucket.LeaveDays);
            r++;
        }

        Finish(sheet, headers.Count, r - 1);
    }

    // ---------------------------------------------------------------- helpers

    private static void Title(IXLWorksheet sheet, string title)
    {
        sheet.Cell(1, 1).SetValue(title);
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;
    }

    private static void Header(IXLWorksheet sheet, int row, IReadOnlyList<string> headers)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            sheet.Cell(row, i + 1).SetValue(headers[i]);
        }

        var range = sheet.Range(row, 1, row, headers.Count);
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = HeaderFill;
    }

    private static void Finish(IXLWorksheet sheet, int columns, int lastRow)
    {
        if (lastRow >= 3)
        {
            sheet.Range(3, 1, lastRow, columns).SetAutoFilter();
        }

        sheet.SheetView.FreezeRows(3);
        sheet.Columns(1, columns).AdjustToContents(3, Math.Min(lastRow, 500));
    }

    private static void SetCell(IXLCell cell, object value)
    {
        switch (value)
        {
            case null:
                return;
            case DateTime date:
                cell.SetValue(date);
                cell.Style.DateFormat.Format = DateFormat;
                return;
            case int number:
                cell.SetValue(number);
                return;
            case double number:
                cell.SetValue(number);
                return;
            default:
                cell.SetValue(value.ToString());
                return;
        }
    }

    private static string Pick(bool arabic, string en, string ar) =>
        arabic ? (string.IsNullOrWhiteSpace(ar) ? en : ar) : (string.IsNullOrWhiteSpace(en) ? ar : en);

    /// <summary>Excel refuses sheet names over 31 characters, repeated names, or any of : \ / ? * [ ].</summary>
    private static string Sheet(string name, XLWorkbook book = null)
    {
        var clean = new string((name ?? "Sheet").Where(c => !":\\/?*[]".Contains(c)).ToArray()).Trim();
        if (clean.Length == 0)
        {
            clean = "Sheet";
        }

        clean = clean.Length > 31 ? clean[..31] : clean;

        if (book == null)
        {
            return clean;
        }

        var candidate = clean;
        for (var i = 2; book.Worksheets.Any(w => string.Equals(w.Name, candidate, StringComparison.OrdinalIgnoreCase)); i++)
        {
            var suffix = " " + i;
            candidate = (clean.Length + suffix.Length > 31 ? clean[..(31 - suffix.Length)] : clean) + suffix;
        }

        return candidate;
    }
}
