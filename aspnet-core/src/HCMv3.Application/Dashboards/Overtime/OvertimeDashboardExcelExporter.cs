using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Abp;
using Abp.AspNetZeroCore.Net;
using Abp.Dependency;
using ClosedXML.Excel;
using HCMv3.Dashboards.Overtime.Dto;
using HCMv3.Dto;
using HCMv3.Storage;
using static HCMv3.Dashboards.DashboardExcel;

namespace HCMv3.Dashboards.Overtime;

public class OvertimeDashboardExportData
{
    public OvertimeDashboardExportPart Part { get; init; }

    public OvertimeDashboardDto Dashboard { get; init; }

    public IReadOnlyList<OvertimeEntryRowDto> Entries { get; init; } = Array.Empty<OvertimeEntryRowDto>();

    /// <summary>Overtime type names by id, in the reader's language.</summary>
    public IReadOnlyDictionary<int, string> TypeNames { get; init; } = new Dictionary<int, string>();

    public string ScopeLabel { get; init; }

    public bool UseArabic { get; init; }
}

/// <summary>The Overtime Dashboard as a workbook: a Summary sheet, then one sheet per tile.</summary>
public class OvertimeDashboardExcelExporter : AbpServiceBase, ITransientDependency
{
    private const string MoneyFormat = "#,##0.000";

    private readonly ITempFileCacheManager _tempFiles;

    public OvertimeDashboardExcelExporter(ITempFileCacheManager tempFiles)
    {
        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;
        _tempFiles = tempFiles;
    }

    public FileDto Export(OvertimeDashboardExportData data)
    {
        using var book = new XLWorkbook();
        var d = data.Dashboard;
        var all = data.Part == OvertimeDashboardExportPart.All;
        bool Wants(OvertimeDashboardExportPart part) => all || data.Part == part;
        string TypeName(int id) => data.TypeNames.TryGetValue(id, out var n) ? n : "#" + id;

        if (all)
        {
            AddSummary(book, data);
        }

        if (Wants(OvertimeDashboardExportPart.Trend) || Wants(OvertimeDashboardExportPart.CostTrend))
        {
            AddTrend(book, d, TypeName);
        }

        if (Wants(OvertimeDashboardExportPart.ByType))
        {
            var sheet = NewSheet(book, L("OvertimeDashboard_ByType"));
            var headers = new List<string> { L("OvertimeDashboard_OvertimeType"), L("OvertimeDashboard_Hours"), L("Employees"), L("OvertimeDashboard_Cost") };
            Header(sheet, HeaderRow, headers);
            var r = HeaderRow + 1;
            foreach (var b in d.ByType)
            {
                sheet.Cell(r, 1).SetValue(TypeName(b.OvertimeTypeId));
                sheet.Cell(r, 2).SetValue(b.Hours);
                sheet.Cell(r, 3).SetValue(b.Employees);
                SetMoney(sheet.Cell(r, 4), b.Cost);
                r++;
            }

            Finish(sheet, headers.Count, r - 1);
        }

        if (Wants(OvertimeDashboardExportPart.ByOrgUnit))
        {
            var sheet = NewSheet(book, L("OvertimeDashboard_ByDepartment"));
            var headers = new List<string> { L("OrganizationUnit"), L("OvertimeDashboard_Hours"), L("Employees"), L("OvertimeDashboard_Cost") };
            Header(sheet, HeaderRow, headers);
            var r = HeaderRow + 1;
            foreach (var b in d.ByOrgUnit)
            {
                sheet.Cell(r, 1).SetValue(b.OrganizationUnitId == null
                    ? L("LeaveDashboard_Unassigned")
                    : b.IsDirectStaff ? L("LeaveDashboard_DirectStaff", b.Name) : b.Name);
                sheet.Cell(r, 2).SetValue(b.Hours);
                sheet.Cell(r, 3).SetValue(b.Employees);
                SetMoney(sheet.Cell(r, 4), b.Cost);
                r++;
            }

            Finish(sheet, headers.Count, r - 1);
        }

        if (Wants(OvertimeDashboardExportPart.TopEmployees))
        {
            var sheet = NewSheet(book, L("OvertimeDashboard_TopEmployees"));
            var headers = new List<string> { L("Code"), L("Employee"), L("OvertimeDashboard_Hours"), L("OvertimeDashboard_Cost") };
            Header(sheet, HeaderRow, headers);
            var r = HeaderRow + 1;
            foreach (var t in d.TopEmployees)
            {
                sheet.Cell(r, 1).SetValue(t.EmployeeCode);
                sheet.Cell(r, 2).SetValue(Pick(data.UseArabic, t.EmployeeNameEn, t.EmployeeNameAr));
                sheet.Cell(r, 3).SetValue(t.Hours);
                SetMoney(sheet.Cell(r, 4), t.Cost);
                r++;
            }

            Finish(sheet, headers.Count, r - 1);
        }

        if (Wants(OvertimeDashboardExportPart.Entries))
        {
            var sheet = NewSheet(book, L("OvertimeDashboard_Entries"));
            var headers = new List<string>
            {
                L("OvertimeDashboard_Month"), L("Code"), L("Employee"), L("OrganizationUnit"), L("OvertimeDashboard_OvertimeType"),
                L("OvertimeDashboard_ClaimedHours"), L("OvertimeDashboard_ActualHours"), L("OvertimeDashboard_ApprovedHours"), L("Status")
            };
            Header(sheet, HeaderRow, headers);
            var r = HeaderRow + 1;
            foreach (var e in data.Entries)
            {
                sheet.Cell(r, 1).SetValue(e.EffectiveDate.ToString("MMM yyyy", CultureInfo.InvariantCulture));
                sheet.Cell(r, 2).SetValue(e.EmployeeCode);
                sheet.Cell(r, 3).SetValue(Pick(data.UseArabic, e.EmployeeNameEn, e.EmployeeNameAr));
                sheet.Cell(r, 4).SetValue(e.OrganizationUnitName);
                sheet.Cell(r, 5).SetValue(TypeName(e.OvertimeTypeId));
                sheet.Cell(r, 6).SetValue(e.ClaimedHours);
                sheet.Cell(r, 7).SetValue(e.ActualHours);
                sheet.Cell(r, 8).SetValue(e.ApprovedHours);
                sheet.Cell(r, 9).SetValue(e.Status.HasValue ? L(e.Status.Value.ToString()) : string.Empty);
                r++;
            }

            Finish(sheet, headers.Count, r - 1);
        }

        var fileName = $"{L("OvertimeDashboard")} {d.FromDate:yyyy-MM-dd} - {d.ToDate:yyyy-MM-dd}.xlsx";
        var file = new FileDto(fileName, MimeTypeNames.ApplicationVndOpenxmlformatsOfficedocumentSpreadsheetmlSheet);
        using var stream = new MemoryStream();
        book.SaveAs(stream);
        _tempFiles.SetFile(file.FileToken, stream.ToArray());
        return file;
    }

    private static IXLWorksheet NewSheet(XLWorkbook book, string title)
    {
        var sheet = book.AddWorksheet(Sheet(title, book));
        Title(sheet, title);
        return sheet;
    }

    private static void SetMoney(IXLCell cell, double value)
    {
        cell.SetValue(value);
        cell.Style.NumberFormat.Format = MoneyFormat;
    }

    private void AddSummary(XLWorkbook book, OvertimeDashboardExportData data)
    {
        var d = data.Dashboard;
        var k = d.Kpis;
        var sheet = book.AddWorksheet(Sheet(L("Summary")));
        Title(sheet, L("OvertimeDashboard"));

        var unit = d.OrgUnitPath.Count > 0 ? string.Join(" › ", d.OrgUnitPath.Select(p => p.Name)) : L("LeaveDashboard_AllUnits");
        var rows = new List<(string, object)>
        {
            (L("Dashboard_Showing"), data.ScopeLabel),
            (L("FromDate"), d.FromDate),
            (L("ToDate"), d.ToDate),
            (L("OrganizationUnit"), unit),
            (L("Employee"), Pick(data.UseArabic, d.EmployeeNameEn, d.EmployeeNameAr)),
            (string.Empty, null),
            (L("OvertimeDashboard_ApprovedHours"), k.ApprovedHours),
            (L("OvertimeDashboard_ClaimedHours"), k.ClaimedHours),
            (L("OvertimeDashboard_ApprovedOfClaimed"), k.ApprovedOfClaimedPercent),
            (L("OvertimeDashboard_Employees"), k.Employees),
            (L("OvertimeDashboard_AveragePerEmployee"), k.AverageHoursPerEmployee),
            (L("OvertimeDashboard_Cost"), k.Cost),
            (L("OvertimeDashboard_Pending"), k.PendingEntries)
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

    private void AddTrend(XLWorkbook book, OvertimeDashboardDto d, Func<int, string> typeName)
    {
        var sheet = NewSheet(book, L("OvertimeDashboard_ByMonth"));
        var typeIds = d.Trend.SelectMany(p => p.Hours).Select(v => v.OvertimeTypeId).Distinct().OrderBy(x => x).ToList();

        var headers = new List<string> { L("OvertimeDashboard_Month") };
        headers.AddRange(typeIds.Select(typeName));
        headers.Add(L("Total"));
        headers.Add(L("OvertimeDashboard_Cost"));
        Header(sheet, HeaderRow, headers);

        var r = HeaderRow + 1;
        foreach (var p in d.Trend)
        {
            sheet.Cell(r, 1).SetValue(p.MonthStart.ToString("MMM yyyy", CultureInfo.InvariantCulture));
            for (var i = 0; i < typeIds.Count; i++)
            {
                sheet.Cell(r, i + 2).SetValue(p.Hours.FirstOrDefault(v => v.OvertimeTypeId == typeIds[i])?.Hours ?? 0);
            }

            sheet.Cell(r, typeIds.Count + 2).SetValue(p.Hours.Sum(v => v.Hours));
            SetMoney(sheet.Cell(r, typeIds.Count + 3), p.Cost);
            r++;
        }

        Finish(sheet, headers.Count, r - 1);
    }
}
