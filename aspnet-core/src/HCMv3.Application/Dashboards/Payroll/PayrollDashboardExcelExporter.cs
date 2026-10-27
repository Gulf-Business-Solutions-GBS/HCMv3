using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Abp;
using Abp.AspNetZeroCore.Net;
using Abp.Dependency;
using ClosedXML.Excel;
using HCMv3.Dashboards.Payroll.Dto;
using HCMv3.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using static HCMv3.Dashboards.DashboardExcel;

namespace HCMv3.Dashboards.Payroll;

public class PayrollDashboardExportData
{
    public PayrollDashboardExportPart Part { get; init; }

    public PayrollDashboardDto Dashboard { get; init; }

    public IReadOnlyList<PayrollEmployeeRowDto> Employees { get; init; } = Array.Empty<PayrollEmployeeRowDto>();

    public string ScopeLabel { get; init; }

    public bool UseArabic { get; init; }
}

/// <summary>The Payroll Dashboard as a workbook: a Summary sheet, then one sheet per tile.</summary>
public class PayrollDashboardExcelExporter : AbpServiceBase, ITransientDependency
{
    private const string MoneyFormat = "#,##0.000";

    private readonly ITempFileCacheManager _tempFiles;

    public PayrollDashboardExcelExporter(ITempFileCacheManager tempFiles)
    {
        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;
        _tempFiles = tempFiles;
    }

    public FileDto Export(PayrollDashboardExportData data)
    {
        using var book = new XLWorkbook();
        var d = data.Dashboard;
        var all = data.Part == PayrollDashboardExportPart.All;
        bool Wants(PayrollDashboardExportPart part) => all || data.Part == part;

        if (all)
        {
            AddSummary(book, data);
        }

        if (Wants(PayrollDashboardExportPart.Trend))
        {
            AddTable(book, L("PayrollDashboard_ByMonth"),
                new[] { L("PayrollDashboard_Month"), L("Employees"), L("PayrollDashboard_Gross"), L("PayrollDashboard_Deductions"), L("PayrollDashboard_Net"), L("PayrollDashboard_EmployerCost"), L("PayrollDashboard_CostToCompany") },
                d.Trend.Select(p => new object[]
                {
                    p.MonthStart.ToString("MMM yyyy", CultureInfo.InvariantCulture), p.Employees, Money(p.Gross), Money(p.Deductions), Money(p.Net), Money(p.EmployerCost), Money(p.CostToCompany)
                }));
        }

        if (Wants(PayrollDashboardExportPart.CostMix))
        {
            AddTable(book, L("PayrollDashboard_CostMix"),
                new[] { L("PayrollDashboard_Component"), L("PayrollDashboard_Amount") },
                d.CostMix.Select(c => new object[] { L("PayrollDashboard_Component_" + c.Component), Money(c.Amount) }));
        }

        if (Wants(PayrollDashboardExportPart.ByOrgUnit))
        {
            AddTable(book, L("PayrollDashboard_ByDepartment"),
                new[] { L("OrganizationUnit"), L("Employees"), L("PayrollDashboard_Gross"), L("PayrollDashboard_Net"), L("PayrollDashboard_CostToCompany") },
                d.ByOrgUnit.Select(b => new object[]
                {
                    b.OrganizationUnitId == null ? L("LeaveDashboard_Unassigned") : b.IsDirectStaff ? L("LeaveDashboard_DirectStaff", b.Name) : b.Name,
                    b.Employees, Money(b.Gross), Money(b.Net), Money(b.CostToCompany)
                }));
        }

        if (Wants(PayrollDashboardExportPart.BasicBands))
        {
            AddTable(book, L("PayrollDashboard_BasicBands"),
                new[] { L("PayrollDashboard_BasicSalary"), L("Employees") },
                d.BasicBands.Select(b => new object[] { BandLabel(b), b.Employees }));
        }

        if (Wants(PayrollDashboardExportPart.Earnings))
        {
            AddWageTypes(book, L("PayrollDashboard_Earnings"), d.Earnings, data.UseArabic);
        }

        if (Wants(PayrollDashboardExportPart.Deductions))
        {
            AddWageTypes(book, L("PayrollDashboard_DeductionTypes"), d.Deductions, data.UseArabic);
        }

        if (Wants(PayrollDashboardExportPart.PaymentMethods))
        {
            AddTable(book, L("PayrollDashboard_PaymentMethods"),
                new[] { L("PayrollDashboard_PaymentMethod"), L("Employees"), L("PayrollDashboard_Net") },
                d.PaymentMethods.Select(b => new object[] { PaymentLabel(b.PaymentType), b.Employees, Money(b.Net) }));
        }

        if (Wants(PayrollDashboardExportPart.RunTypes))
        {
            AddTable(book, L("PayrollDashboard_RunTypes"),
                new[] { L("PayrollDashboard_RunType"), L("PayrollDashboard_Payslips"), L("Employees"), L("PayrollDashboard_Net") },
                d.RunTypes.Select(b => new object[] { RunTypeLabel(b.RunType), b.Payslips, b.Employees, Money(b.Net) }));
        }

        if (Wants(PayrollDashboardExportPart.Provisions))
        {
            var p = d.Provisions ?? new PayrollProvisionsDto();
            AddTable(book, L("PayrollDashboard_Provisions"),
                new[] { L("PayrollDashboard_Component"), L("PayrollDashboard_Amount") },
                new[]
                {
                    new object[] { L("PayrollDashboard_IndemnityBalance"), Money(p.Indemnity) },
                    new object[] { L("PayrollDashboard_LeaveBalance"), Money(p.Leave) },
                    new object[] { L("PayrollDashboard_TicketBalance"), Money(p.Ticket) },
                    new object[] { L("Total"), Money(p.Total) }
                });
        }

        if (Wants(PayrollDashboardExportPart.Employees))
        {
            AddTable(book, L("PayrollDashboard_Payslips"),
                new[]
                {
                    L("PayrollDashboard_Month"), L("Code"), L("Employee"), L("OrganizationUnit"), L("PayrollDashboard_RunType"), L("PayrollDashboard_BasicSalary"),
                    L("PayrollDashboard_Gross"), L("PayrollDashboard_Deductions"), L("PayrollDashboard_Net"), L("PayrollDashboard_EmployerCost"),
                    L("PayrollDashboard_CostToCompany"), L("PayrollDashboard_PaymentMethod")
                },
                data.Employees.Select(e => new object[]
                {
                    e.Month.ToString("MMM yyyy", CultureInfo.InvariantCulture), e.EmployeeCode, Pick(data.UseArabic, e.EmployeeNameEn, e.EmployeeNameAr),
                    e.OrganizationUnitName, RunTypeLabel(e.RunType), Money(e.Basic), Money(e.Gross), Money(e.Deductions), Money(e.Net),
                    Money(e.EmployerCost), Money(e.CostToCompany), PaymentLabel(e.PaymentType)
                }));
        }

        var fileName = $"{L("PayrollDashboard")} {d.FromDate:yyyy-MM-dd} - {d.ToDate:yyyy-MM-dd}.xlsx";
        var file = new FileDto(fileName, MimeTypeNames.ApplicationVndOpenxmlformatsOfficedocumentSpreadsheetmlSheet);
        using var stream = new MemoryStream();
        book.SaveAs(stream);
        _tempFiles.SetFile(file.FileToken, stream.ToArray());
        return file;
    }

    /// <summary>Marks a value for the money number format.</summary>
    private sealed record MoneyValue(double Value);

    private static MoneyValue Money(double value) => new(value);

    private string BandLabel(PayrollBasicBandDto band) =>
        band.To.HasValue
            ? L("PayrollDashboard_BandRange", band.From.ToString("#,##0", CultureInfo.InvariantCulture), band.To.Value.ToString("#,##0", CultureInfo.InvariantCulture))
            : L("PayrollDashboard_BandFrom", band.From.ToString("#,##0", CultureInfo.InvariantCulture));

    private string PaymentLabel(PaymentType? type) => type switch
    {
        PaymentType.Bank => L("PayrollDashboard_PaymentBank"),
        PaymentType.Cash => L("PayrollDashboard_PaymentCash"),
        _ => L("PayrollDashboard_PaymentNotSet")
    };

    private string RunTypeLabel(PayrollRunType type) =>
        type == PayrollRunType.OffCycle ? L("PayrollDashboard_OffCycle") : L("PayrollDashboard_Regular");

    private void AddWageTypes(XLWorkbook book, string title, IEnumerable<PayrollWageTypeAmountDto> rows, bool useArabic)
    {
        AddTable(book, title,
            new[] { L("Code"), L("PayrollDashboard_WageType"), L("Employees"), L("PayrollDashboard_Amount") },
            rows.Select(w => new object[] { w.Code, Pick(useArabic, w.NameEn, w.NameAr), w.Employees, Money(w.Amount) }));
    }

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
                var cell = sheet.Cell(r, c + 1);
                if (row[c] is MoneyValue money)
                {
                    cell.SetValue(money.Value);
                    cell.Style.NumberFormat.Format = MoneyFormat;
                }
                else
                {
                    SetCell(cell, row[c]);
                }
            }

            r++;
        }

        Finish(sheet, headers.Count, r - 1);
    }

    private void AddSummary(XLWorkbook book, PayrollDashboardExportData data)
    {
        var d = data.Dashboard;
        var k = d.Kpis;
        var sheet = book.AddWorksheet(Sheet(L("Summary")));
        Title(sheet, L("PayrollDashboard"));

        var unit = d.OrgUnitPath.Count > 0 ? string.Join(" › ", d.OrgUnitPath.Select(p => p.Name)) : L("LeaveDashboard_AllUnits");
        var rows = new List<(string, object)>
        {
            (L("Dashboard_Showing"), data.ScopeLabel),
            (L("FromDate"), d.FromDate),
            (L("ToDate"), d.ToDate),
            (L("OrganizationUnit"), unit),
            (L("Employee"), Pick(data.UseArabic, d.EmployeeNameEn, d.EmployeeNameAr)),
            (L("PayrollDashboard_LatestMonth"), d.LatestMonth?.ToString("MMM yyyy", CultureInfo.InvariantCulture)),
            (string.Empty, null),
            (L("PayrollDashboard_EmployeesPaid"), k.EmployeesPaid),
            (L("PayrollDashboard_Gross"), Money(k.Gross)),
            (L("PayrollDashboard_Deductions"), Money(k.Deductions)),
            (L("PayrollDashboard_Net"), Money(k.Net)),
            (L("PayrollDashboard_EmployerCost"), Money(k.EmployerCost)),
            (L("PayrollDashboard_CostToCompany"), Money(k.CostToCompany)),
            (L("PayrollDashboard_AveragePerEmployee"), k.AverageMonthlyCostPerEmployee.HasValue ? Money(k.AverageMonthlyCostPerEmployee.Value) : null),
            (L("PayrollDashboard_OffCyclePayments"), k.OffCyclePayments)
        };

        var r = HeaderRow;
        foreach (var (label, value) in rows)
        {
            sheet.Cell(r, 1).SetValue(label);
            sheet.Cell(r, 1).Style.Font.Bold = true;
            if (value is MoneyValue money)
            {
                sheet.Cell(r, 2).SetValue(money.Value);
                sheet.Cell(r, 2).Style.NumberFormat.Format = MoneyFormat;
            }
            else
            {
                SetCell(sheet.Cell(r, 2), value);
            }

            r++;
        }

        sheet.Columns(1, 2).AdjustToContents();
    }
}
