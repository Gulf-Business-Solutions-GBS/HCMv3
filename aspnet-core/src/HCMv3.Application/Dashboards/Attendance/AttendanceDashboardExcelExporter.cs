using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Abp;
using Abp.AspNetZeroCore.Net;
using Abp.Dependency;
using ClosedXML.Excel;
using HCMv3.Dashboards.Attendance.Dto;
using HCMv3.Dto;
using HCMv3.Storage;
using static HCMv3.Dashboards.DashboardExcel;

namespace HCMv3.Dashboards.Attendance;

public class AttendanceDashboardExportData
{
    public AttendanceDashboardExportPart Part { get; init; }

    public AttendanceDashboardDto Dashboard { get; init; }

    public IReadOnlyList<AttendanceExceptionRowDto> Exceptions { get; init; } = Array.Empty<AttendanceExceptionRowDto>();

    public IReadOnlyList<AttendancePunchRequestRowDto> PunchRequests { get; init; } = Array.Empty<AttendancePunchRequestRowDto>();

    public string ScopeLabel { get; init; }

    public bool UseArabic { get; init; }
}

/// <summary>The Time &amp; Attendance Dashboard as a workbook: a Summary sheet, then one sheet per tile.</summary>
public class AttendanceDashboardExcelExporter : AbpServiceBase, ITransientDependency
{
    private const string TimeFormat = "HH:mm";

    private readonly ITempFileCacheManager _tempFiles;

    public AttendanceDashboardExcelExporter(ITempFileCacheManager tempFiles)
    {
        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;
        _tempFiles = tempFiles;
    }

    public FileDto Export(AttendanceDashboardExportData data)
    {
        using var book = new XLWorkbook();
        var d = data.Dashboard;
        var all = data.Part == AttendanceDashboardExportPart.All;
        bool Wants(AttendanceDashboardExportPart part) => all || data.Part == part;

        if (all)
        {
            AddSummary(book, data);
        }

        if (Wants(AttendanceDashboardExportPart.Trend))
        {
            AddTrend(book, d);
        }

        if (Wants(AttendanceDashboardExportPart.ByOrgUnit))
        {
            AddByOrgUnit(book, d);
        }

        if (Wants(AttendanceDashboardExportPart.TopAbsent))
        {
            AddTop(book, L("AttendanceDashboard_TopAbsent"), L("AttendanceDashboard_AbsentDays"), d.TopAbsent, data.UseArabic);
        }

        if (Wants(AttendanceDashboardExportPart.TopLate))
        {
            AddTop(book, L("AttendanceDashboard_TopLate"), L("AttendanceDashboard_Hours"), d.TopLate, data.UseArabic);
        }

        if (Wants(AttendanceDashboardExportPart.TopEarly))
        {
            AddTop(book, L("AttendanceDashboard_TopEarly"), L("AttendanceDashboard_Hours"), d.TopEarly, data.UseArabic);
        }

        if (Wants(AttendanceDashboardExportPart.Weekday))
        {
            AddWeekdays(book, d);
        }

        if (Wants(AttendanceDashboardExportPart.Exceptions))
        {
            AddExceptions(book, data.Exceptions, data.UseArabic);
        }

        if (Wants(AttendanceDashboardExportPart.PunchRequests))
        {
            AddPunchRequests(book, data.PunchRequests, data.UseArabic);
        }

        var fileName = $"{L("AttendanceDashboard")} {d.FromDate:yyyy-MM-dd} - {d.ToDate:yyyy-MM-dd}.xlsx";
        var file = new FileDto(fileName, MimeTypeNames.ApplicationVndOpenxmlformatsOfficedocumentSpreadsheetmlSheet);
        using var stream = new MemoryStream();
        book.SaveAs(stream);
        _tempFiles.SetFile(file.FileToken, stream.ToArray());
        return file;
    }

    private void AddSummary(XLWorkbook book, AttendanceDashboardExportData data)
    {
        var d = data.Dashboard;
        var k = d.Kpis;
        var sheet = book.AddWorksheet(Sheet(L("Summary")));
        Title(sheet, L("AttendanceDashboard"));

        var unit = d.OrgUnitPath.Count > 0 ? string.Join(" › ", d.OrgUnitPath.Select(p => p.Name)) : L("LeaveDashboard_AllUnits");
        var rows = new List<(string, object)>
        {
            (L("Dashboard_Showing"), data.ScopeLabel),
            (L("FromDate"), d.FromDate),
            (L("ToDate"), d.ToDate),
            (L("OrganizationUnit"), unit),
            (L("Employee"), Pick(data.UseArabic, d.EmployeeNameEn, d.EmployeeNameAr)),
            (string.Empty, null),
            (L("AttendanceDashboard_AttendanceRate"), k.AttendanceRatePercent),
            (L("AttendanceDashboard_WorkingDays"), k.WorkingDays),
            (L("AttendanceDashboard_AbsentDays"), k.AbsentDays),
            (L("AttendanceDashboard_LateArrivals"), k.LateDays),
            (L("AttendanceDashboard_LateHours"), k.LateHours),
            (L("AttendanceDashboard_EarlyDepartures"), k.EarlyDays),
            (L("AttendanceDashboard_EarlyHours"), k.EarlyHours),
            (L("AttendanceDashboard_MissingPunches"), k.MissingPunchDays),
            (L("AttendanceDashboard_PlannedHours"), k.PlannedHours),
            (L("AttendanceDashboard_ActualHours"), k.ActualHours),
            (L("AttendanceDashboard_PunchRequests"), k.PunchRequests)
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

    private void AddTrend(XLWorkbook book, AttendanceDashboardDto d)
    {
        var daily = d.Granularity == AttendanceDashboardGranularity.Day;
        var title = L(daily ? "AttendanceDashboard_ByDay" : "AttendanceDashboard_ByMonth");
        var sheet = book.AddWorksheet(Sheet(title, book));
        Title(sheet, title);

        var headers = new List<string>
        {
            L("Period"), L("AttendanceDashboard_WorkingDays"), L("AttendanceDashboard_AbsentDays"),
            L("AttendanceDashboard_LateArrivals"), L("AttendanceDashboard_EarlyDepartures"), L("AttendanceDashboard_AttendanceRate")
        };
        Header(sheet, HeaderRow, headers);

        var r = HeaderRow + 1;
        foreach (var p in d.Trend)
        {
            sheet.Cell(r, 1).SetValue(daily
                ? p.PeriodStart.ToString(DateFormat, CultureInfo.InvariantCulture)
                : p.PeriodStart.ToString("MMM yyyy", CultureInfo.InvariantCulture));
            sheet.Cell(r, 2).SetValue(p.WorkingDays);
            sheet.Cell(r, 3).SetValue(p.AbsentDays);
            sheet.Cell(r, 4).SetValue(p.LateDays);
            sheet.Cell(r, 5).SetValue(p.EarlyDays);
            SetCell(sheet.Cell(r, 6), p.AttendanceRatePercent);
            r++;
        }

        Finish(sheet, headers.Count, r - 1);
    }

    private void AddByOrgUnit(XLWorkbook book, AttendanceDashboardDto d)
    {
        var title = L("AttendanceDashboard_ByDepartment");
        var sheet = book.AddWorksheet(Sheet(title, book));
        Title(sheet, title);

        var headers = new List<string>
        {
            L("OrganizationUnit"), L("AttendanceDashboard_WorkingDays"), L("AttendanceDashboard_AbsentDays"),
            L("AttendanceDashboard_LateArrivals"), L("AttendanceDashboard_EarlyDepartures"), L("AttendanceDashboard_AttendanceRate")
        };
        Header(sheet, HeaderRow, headers);

        var r = HeaderRow + 1;
        foreach (var b in d.ByOrgUnit)
        {
            var name = b.OrganizationUnitId == null
                ? L("LeaveDashboard_Unassigned")
                : b.IsDirectStaff ? L("LeaveDashboard_DirectStaff", b.Name) : b.Name;
            sheet.Cell(r, 1).SetValue(name);
            sheet.Cell(r, 2).SetValue(b.WorkingDays);
            sheet.Cell(r, 3).SetValue(b.AbsentDays);
            sheet.Cell(r, 4).SetValue(b.LateDays);
            sheet.Cell(r, 5).SetValue(b.EarlyDays);
            SetCell(sheet.Cell(r, 6), b.AttendanceRatePercent);
            r++;
        }

        Finish(sheet, headers.Count, r - 1);
    }

    private void AddTop(XLWorkbook book, string title, string valueHeader, IReadOnlyList<AttendanceTopEmployeeDto> rows, bool arabic)
    {
        var sheet = book.AddWorksheet(Sheet(title, book));
        Title(sheet, title);

        var headers = new List<string> { L("Code"), L("Employee"), valueHeader, L("AttendanceDashboard_Days") };
        Header(sheet, HeaderRow, headers);

        var r = HeaderRow + 1;
        foreach (var t in rows)
        {
            sheet.Cell(r, 1).SetValue(t.EmployeeCode);
            sheet.Cell(r, 2).SetValue(Pick(arabic, t.EmployeeNameEn, t.EmployeeNameAr));
            sheet.Cell(r, 3).SetValue(t.Value);
            sheet.Cell(r, 4).SetValue(t.Days);
            r++;
        }

        Finish(sheet, headers.Count, r - 1);
    }

    private void AddWeekdays(XLWorkbook book, AttendanceDashboardDto d)
    {
        var title = L("AttendanceDashboard_ByWeekday");
        var sheet = book.AddWorksheet(Sheet(title, book));
        Title(sheet, title);

        var headers = new List<string> { L("AttendanceDashboard_Weekday"), L("AttendanceDashboard_LateArrivals"), L("AttendanceDashboard_AbsentDays") };
        Header(sheet, HeaderRow, headers);

        var r = HeaderRow + 1;
        foreach (var w in d.Weekdays)
        {
            sheet.Cell(r, 1).SetValue(CultureInfo.CurrentUICulture.DateTimeFormat.GetDayName((DayOfWeek)w.DayOfWeek));
            sheet.Cell(r, 2).SetValue(w.LateDays);
            sheet.Cell(r, 3).SetValue(w.AbsentDays);
            r++;
        }

        Finish(sheet, headers.Count, r - 1);
    }

    private void AddExceptions(XLWorkbook book, IReadOnlyList<AttendanceExceptionRowDto> rows, bool arabic)
    {
        var title = L("AttendanceDashboard_Exceptions");
        var sheet = book.AddWorksheet(Sheet(title, book));
        Title(sheet, title);

        var headers = new List<string>
        {
            L("Date"), L("Code"), L("Employee"), L("OrganizationUnit"), L("AttendanceDashboard_PlannedIn"),
            L("AttendanceDashboard_PlannedOut"), L("AttendanceDashboard_ActualIn"), L("AttendanceDashboard_ActualOut"),
            L("AttendanceDashboard_LateMinutes"), L("AttendanceDashboard_EarlyMinutes"), L("AttendanceDashboard_Exception")
        };
        Header(sheet, HeaderRow, headers);

        var r = HeaderRow + 1;
        foreach (var e in rows)
        {
            SetCell(sheet.Cell(r, 1), e.DayDate);
            sheet.Cell(r, 2).SetValue(e.EmployeeCode);
            sheet.Cell(r, 3).SetValue(Pick(arabic, e.EmployeeNameEn, e.EmployeeNameAr));
            sheet.Cell(r, 4).SetValue(e.OrganizationUnitName);
            sheet.Cell(r, 5).SetValue(e.PlannedIn?.ToString(TimeFormat, CultureInfo.InvariantCulture) ?? string.Empty);
            sheet.Cell(r, 6).SetValue(e.PlannedOut?.ToString(TimeFormat, CultureInfo.InvariantCulture) ?? string.Empty);
            sheet.Cell(r, 7).SetValue(e.ActualIn?.ToString(TimeFormat, CultureInfo.InvariantCulture) ?? string.Empty);
            sheet.Cell(r, 8).SetValue(e.ActualOut?.ToString(TimeFormat, CultureInfo.InvariantCulture) ?? string.Empty);
            sheet.Cell(r, 9).SetValue(e.LateMinutes);
            sheet.Cell(r, 10).SetValue(e.EarlyMinutes);
            sheet.Cell(r, 11).SetValue(ExceptionLabel(e));
            r++;
        }

        Finish(sheet, headers.Count, r - 1);
    }

    private void AddPunchRequests(XLWorkbook book, IReadOnlyList<AttendancePunchRequestRowDto> rows, bool arabic)
    {
        var title = L("AttendanceDashboard_PunchRequests");
        var sheet = book.AddWorksheet(Sheet(title, book));
        Title(sheet, title);

        var headers = new List<string> { L("Date"), L("AttendanceDashboard_Time"), L("Code"), L("Employee"), L("AttendanceDashboard_PunchType"), L("Status") };
        Header(sheet, HeaderRow, headers);

        var r = HeaderRow + 1;
        foreach (var p in rows)
        {
            SetCell(sheet.Cell(r, 1), p.PunchDate);
            sheet.Cell(r, 2).SetValue(p.PunchTime.ToString(TimeFormat, CultureInfo.InvariantCulture));
            sheet.Cell(r, 3).SetValue(p.EmployeeCode);
            sheet.Cell(r, 4).SetValue(Pick(arabic, p.EmployeeNameEn, p.EmployeeNameAr));
            sheet.Cell(r, 5).SetValue(Pick(arabic, p.PunchTypeNameEn, p.PunchTypeNameAr));
            sheet.Cell(r, 6).SetValue(p.Status.HasValue ? L(p.Status.Value.ToString()) : string.Empty);
            r++;
        }

        Finish(sheet, headers.Count, r - 1);
    }

    private string ExceptionLabel(AttendanceExceptionRowDto e)
    {
        var parts = new List<string>();
        if (e.IsAbsent) parts.Add(L("AttendanceDashboard_Absent"));
        if (e.LateMinutes > 0) parts.Add(L("AttendanceDashboard_Late"));
        if (e.EarlyMinutes > 0) parts.Add(L("AttendanceDashboard_Early"));
        if (e.IsMissingIn) parts.Add(L("AttendanceDashboard_MissingIn"));
        if (e.IsMissingOut) parts.Add(L("AttendanceDashboard_MissingOut"));
        return string.Join(", ", parts);
    }
}
