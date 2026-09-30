using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Abp;
using Abp.AspNetZeroCore.Net;
using Abp.Dependency;
using ClosedXML.Excel;
using HCMv3.Dashboards.Workforce.Dto;
using HCMv3.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using static HCMv3.Dashboards.DashboardExcel;

namespace HCMv3.Dashboards.Workforce;

/// <summary>Everything one Workforce Dashboard export needs, already scoped and filtered by the app service.</summary>
public class WorkforceDashboardExportData
{
    public WorkforceDashboardExportPart Part { get; init; }

    public WorkforceDashboardDto Dashboard { get; init; }

    public IReadOnlyList<WorkforceEmployeeRowDto> HeadcountRows { get; init; } = Array.Empty<WorkforceEmployeeRowDto>();

    public IReadOnlyList<WorkforceEmployeeRowDto> JoinerRows { get; init; } = Array.Empty<WorkforceEmployeeRowDto>();

    public IReadOnlyList<WorkforceEmployeeRowDto> LeaverRows { get; init; } = Array.Empty<WorkforceEmployeeRowDto>();

    public string ScopeLabel { get; init; }

    public bool UseArabic { get; init; }
}

/// <summary>
/// The Workforce Dashboard as a workbook: a Summary sheet (whose data, the period, the headline figures) then one
/// sheet per tile, or a single tile on its own. Built in memory and handed to the temporary file store.
/// </summary>
public class WorkforceDashboardExcelExporter : AbpServiceBase, ITransientDependency
{
    private readonly ITempFileCacheManager _tempFiles;

    public WorkforceDashboardExcelExporter(ITempFileCacheManager tempFiles)
    {
        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;
        _tempFiles = tempFiles;
    }

    public FileDto Export(WorkforceDashboardExportData data)
    {
        using var book = new XLWorkbook();
        var d = data.Dashboard;
        var all = data.Part == WorkforceDashboardExportPart.All;
        bool Wants(WorkforceDashboardExportPart part) => all || data.Part == part;

        if (all)
        {
            AddSummary(book, data);
        }

        if (Wants(WorkforceDashboardExportPart.Trend))
        {
            AddTrend(book, d);
        }

        if (Wants(WorkforceDashboardExportPart.Age))
        {
            AddCounts(book, L("WorkforceDashboard_ByAge"), L("Age"),
                d.AgeBands.Select(b => (L("WorkforceAgeBand_" + (WorkforceAgeBand)b.Band), b.Count)));
        }

        if (Wants(WorkforceDashboardExportPart.Gender))
        {
            AddCounts(book, L("WorkforceDashboard_ByGender"), L("Gender"),
                d.Genders.Select(g => (GenderLabel(g.Gender), g.Count)));
        }

        if (Wants(WorkforceDashboardExportPart.Service))
        {
            AddCounts(book, L("WorkforceDashboard_ByService"), L("WorkforceDashboard_YearsOfService"),
                d.ServiceBands.Select(b => (L("WorkforceServiceBand_" + (WorkforceServiceBand)b.Band), b.Count)));
        }

        if (Wants(WorkforceDashboardExportPart.Nationality))
        {
            AddCounts(book, L("WorkforceDashboard_ByNationality"), L("Nationality"),
                d.Nationalities.Select(n => (NamedLabel(n, data.UseArabic), n.Count)));
        }

        if (Wants(WorkforceDashboardExportPart.Qualification))
        {
            AddCounts(book, L("WorkforceDashboard_ByQualification"), L("Qualification"),
                d.Qualifications.Select(n => (NamedLabel(n, data.UseArabic), n.Count)));
        }

        if (Wants(WorkforceDashboardExportPart.JobTitle))
        {
            AddCounts(book, L("WorkforceDashboard_ByJobTitle"), L("JobTitle"),
                d.JobTitles.Select(n => (NamedLabel(n, data.UseArabic), n.Count)));
        }

        if (Wants(WorkforceDashboardExportPart.ByOrgUnit))
        {
            AddByOrgUnit(book, d);
        }

        if (Wants(WorkforceDashboardExportPart.Headcount))
        {
            AddEmployees(book, L("WorkforceDashboard_Headcount"), data.HeadcountRows, data.UseArabic, leavers: false);
        }

        if (Wants(WorkforceDashboardExportPart.Joiners))
        {
            AddEmployees(book, L("WorkforceDashboard_Joiners"), data.JoinerRows, data.UseArabic, leavers: false);
        }

        if (Wants(WorkforceDashboardExportPart.Leavers))
        {
            AddEmployees(book, L("WorkforceDashboard_Leavers"), data.LeaverRows, data.UseArabic, leavers: true);
        }

        var fileName = $"{L("WorkforceDashboard")} {d.FromDate:yyyy-MM-dd} - {d.ToDate:yyyy-MM-dd}.xlsx";
        var file = new FileDto(fileName, MimeTypeNames.ApplicationVndOpenxmlformatsOfficedocumentSpreadsheetmlSheet);

        using var stream = new MemoryStream();
        book.SaveAs(stream);
        _tempFiles.SetFile(file.FileToken, stream.ToArray());
        return file;
    }

    // ---------------------------------------------------------------- sheets

    private void AddSummary(XLWorkbook book, WorkforceDashboardExportData data)
    {
        var d = data.Dashboard;
        var k = d.Kpis;
        var sheet = book.AddWorksheet(Sheet(L("Summary")));
        Title(sheet, L("WorkforceDashboard"));

        var unit = d.OrgUnitPath.Count > 0 ? string.Join(" › ", d.OrgUnitPath.Select(p => p.Name)) : L("LeaveDashboard_AllUnits");

        var rows = new List<(string Label, object Value)>
        {
            (L("Dashboard_Showing"), data.ScopeLabel),
            (L("FromDate"), d.FromDate),
            (L("ToDate"), d.ToDate),
            (L("WorkforceDashboard_AsOfDate"), d.AsOfDate),
            (L("OrganizationUnit"), unit),
            (string.Empty, null),
            (L("WorkforceDashboard_Headcount"), k.Headcount),
            (L("Male"), k.Male),
            (L("Female"), k.Female),
            (L("WorkforceDashboard_AverageAge"), k.AverageAge),
            (L("WorkforceDashboard_AverageService"), k.AverageServiceYears),
            (L("WorkforceDashboard_Nationalities"), k.Nationalities),
            (L("WorkforceDashboard_Joiners"), k.Joiners),
            (L("WorkforceDashboard_Leavers"), k.Leavers),
            (L("Resignation"), k.Resignations),
            (L("Termination"), k.Terminations),
            (L("WorkforceDashboard_AverageHeadcount"), k.AverageHeadcount),
            (L("WorkforceDashboard_Turnover"), k.TurnoverPercent)
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

    private void AddTrend(XLWorkbook book, WorkforceDashboardDto d)
    {
        var title = L("WorkforceDashboard_ByMonth");
        var sheet = book.AddWorksheet(Sheet(title, book));
        Title(sheet, title);

        var headers = new List<string>
        {
            L("Month"), L("WorkforceDashboard_Headcount"), L("WorkforceDashboard_Joiners"), L("WorkforceDashboard_Leavers"),
            L("WorkforceDashboard_Turnover")
        };
        Header(sheet, HeaderRow, headers);

        var r = HeaderRow + 1;
        foreach (var point in d.Trend)
        {
            sheet.Cell(r, 1).SetValue(point.MonthStart.ToString("MMM yyyy", CultureInfo.InvariantCulture));
            sheet.Cell(r, 2).SetValue(point.Headcount);
            sheet.Cell(r, 3).SetValue(point.Joiners);
            sheet.Cell(r, 4).SetValue(point.Leavers);
            SetCell(sheet.Cell(r, 5), point.TurnoverPercent);
            r++;
        }

        Finish(sheet, headers.Count, r - 1);
    }

    private void AddCounts(XLWorkbook book, string title, string groupHeader, IEnumerable<(string Label, int Count)> rows)
    {
        var sheet = book.AddWorksheet(Sheet(title, book));
        Title(sheet, title);

        var list = rows.ToList();
        var total = list.Sum(x => x.Count);
        var headers = new List<string> { groupHeader, L("Employees"), "%" };
        Header(sheet, HeaderRow, headers);

        var r = HeaderRow + 1;
        foreach (var (label, count) in list)
        {
            sheet.Cell(r, 1).SetValue(label);
            sheet.Cell(r, 2).SetValue(count);
            sheet.Cell(r, 3).SetValue(total == 0 ? 0 : Math.Round(count * 100.0 / total, 1));
            r++;
        }

        Finish(sheet, headers.Count, r - 1);
    }

    private void AddByOrgUnit(XLWorkbook book, WorkforceDashboardDto d)
    {
        var title = L("WorkforceDashboard_ByDepartment");
        var sheet = book.AddWorksheet(Sheet(title, book));
        Title(sheet, title);

        var headers = new List<string>
        {
            L("OrganizationUnit"), L("WorkforceDashboard_Headcount"), L("WorkforceDashboard_Leavers"), L("WorkforceDashboard_Turnover")
        };
        Header(sheet, HeaderRow, headers);

        var r = HeaderRow + 1;
        foreach (var bucket in d.ByOrgUnit)
        {
            var name = bucket.OrganizationUnitId == null
                ? L("LeaveDashboard_Unassigned")
                : bucket.IsDirectStaff ? L("LeaveDashboard_DirectStaff", bucket.Name) : bucket.Name;

            sheet.Cell(r, 1).SetValue(name);
            sheet.Cell(r, 2).SetValue(bucket.Headcount);
            sheet.Cell(r, 3).SetValue(bucket.Leavers);
            SetCell(sheet.Cell(r, 4), bucket.TurnoverPercent);
            r++;
        }

        Finish(sheet, headers.Count, r - 1);
    }

    private void AddEmployees(XLWorkbook book, string title, IReadOnlyList<WorkforceEmployeeRowDto> rows, bool arabic, bool leavers)
    {
        var sheet = book.AddWorksheet(Sheet(title, book));
        Title(sheet, title);

        var headers = new List<string>
        {
            L("Code"), L("Employee"), L("OrganizationUnit"), L("JobTitle"), L("Nationality"), L("Gender"), L("Age"),
            L("WorkforceDashboard_YearsOfService"), L("JoiningDate")
        };
        if (leavers)
        {
            headers.Add(L("LastDayInService"));
            headers.Add(L("SeparationType"));
        }

        Header(sheet, HeaderRow, headers);

        var r = HeaderRow + 1;
        foreach (var row in rows)
        {
            sheet.Cell(r, 1).SetValue(row.EmployeeCode);
            sheet.Cell(r, 2).SetValue(Pick(arabic, row.EmployeeNameEn, row.EmployeeNameAr));
            sheet.Cell(r, 3).SetValue(row.OrganizationUnitName);
            sheet.Cell(r, 4).SetValue(Pick(arabic, row.JobTitleNameEn, row.JobTitleNameAr));
            sheet.Cell(r, 5).SetValue(Pick(arabic, row.NationalityNameEn, row.NationalityNameAr));
            sheet.Cell(r, 6).SetValue(GenderLabel(row.Gender));
            SetCell(sheet.Cell(r, 7), row.Age);
            sheet.Cell(r, 8).SetValue(row.ServiceYears);
            SetCell(sheet.Cell(r, 9), row.JoiningDate);
            if (leavers)
            {
                SetCell(sheet.Cell(r, 10), row.LastDayInService);
                sheet.Cell(r, 11).SetValue(row.SeparationType.HasValue ? L(row.SeparationType.Value.ToString()) : string.Empty);
            }

            r++;
        }

        Finish(sheet, headers.Count, r - 1);
    }

    // ---------------------------------------------------------------- labels

    private string GenderLabel(Gender gender) => gender switch
    {
        Gender.Male => L("Male"),
        Gender.Female => L("Female"),
        _ => L("WorkforceDashboard_GenderNotSpecified")
    };

    private string NamedLabel(WorkforceNamedCountDto item, bool arabic)
    {
        if (item.IsOthers)
        {
            return L("Others");
        }

        return item.Id == 0 ? L("WorkforceDashboard_NotRecorded") : Pick(arabic, item.NameEn, item.NameAr);
    }
}
