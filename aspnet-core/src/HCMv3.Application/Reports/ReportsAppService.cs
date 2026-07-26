using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Timing;
using FastReport;
using FastReport.Export.PdfSimple;
using FastReport.Utils;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Reports.Dto;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Reports;

/// <summary>
/// FastReport.OpenSource pilot app service. Renders two sample reports (Salary Certificate, Payslip)
/// as PDFs for a given employee.
///
/// This is a reporting-evaluation spike, not production payroll code:
/// <list type="bullet">
/// <item><description>Employee header data (name, code, job title, department) is pulled from real
/// PersonnelAdministration/OrganizationManagement data.</description></item>
/// <item><description>Salary data is pulled from the employee's most recent <see cref="EmployeeBasicPay"/>
/// header + <see cref="EmployeeBasicPayDetail"/> rows (joined to <see cref="WageType"/> for the
/// Earning/Deduction split shown on the payslip). If no basic-pay record exists for the employee, the
/// report degrades gracefully to placeholder ("N/A"/0) values rather than throwing.</description></item>
/// </list>
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Preview)]
public class ReportsAppService : HCMv3AppServiceBase, IReportsAppService
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly IRepository<EmployeeBasicPay> _employeeBasicPayRepository;
    private readonly IRepository<EmployeeBasicPayDetail> _employeeBasicPayDetailRepository;
    private readonly IRepository<EmployeeBank> _employeeBankRepository;

    public ReportsAppService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        IRepository<EmployeeBasicPay> employeeBasicPayRepository,
        IRepository<EmployeeBasicPayDetail> employeeBasicPayDetailRepository,
        IRepository<EmployeeBank> employeeBankRepository)
    {
        _employeeRepository = employeeRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _employeeBasicPayRepository = employeeBasicPayRepository;
        _employeeBasicPayDetailRepository = employeeBasicPayDetailRepository;
        _employeeBankRepository = employeeBankRepository;
    }

    [AbpAuthorize(AppPermissions.Pages_Reports_Preview)]
    public async Task<ReportPdfOutput> GetSalaryCertificatePdf(EntityDto input)
    {
        var employee = await _employeeRepository.GetAsync(input.Id);
        var orgAssignment = await GetLatestOrgAssignmentAsync(input.Id);
        var basicPay = await GetLatestBasicPayAsync(input.Id);
        var currencyName = await GetBasicPayCurrencyNameAsync(basicPay);

        var table = new DataTable("Employees");
        table.Columns.Add("FullName", typeof(string));
        table.Columns.Add("JobTitle", typeof(string));
        table.Columns.Add("JoiningDate", typeof(string));
        table.Columns.Add("BasicSalary", typeof(string));
        table.Columns.Add("Currency", typeof(string));
        table.Columns.Add("IssueDate", typeof(string));

        table.Rows.Add(
            employee.FullNameEn.IsNullOrEmpty() ? BuildFullName(employee) : employee.FullNameEn,
            orgAssignment?.JobTitle?.NameEn ?? "N/A",
            employee.JoiningDate.ToString("dd MMM yyyy"),
            (basicPay?.Amount ?? 0).ToString("N2"),
            currencyName,
            Clock.Now.ToString("dd MMM yyyy")
        );

        var pdfBytes = BuildSalaryCertificateReport(table);
        return new ReportPdfOutput($"SalaryCertificate_{employee.Code ?? employee.Id.ToString()}.pdf", Convert.ToBase64String(pdfBytes));
    }

    [AbpAuthorize(AppPermissions.Pages_Reports_Preview)]
    public async Task<ReportPdfOutput> GetPayslipPdf(EntityDto input)
    {
        var employee = await _employeeRepository.GetAsync(input.Id);
        var orgAssignment = await GetLatestOrgAssignmentAsync(input.Id);
        var basicPay = await GetLatestBasicPayAsync(input.Id);

        var employeeTable = new DataTable("Employee");
        employeeTable.Columns.Add("FullName", typeof(string));
        employeeTable.Columns.Add("EmployeeCode", typeof(string));
        employeeTable.Columns.Add("JobTitle", typeof(string));
        employeeTable.Columns.Add("Department", typeof(string));
        employeeTable.Columns.Add("PayPeriod", typeof(string));
        employeeTable.Columns.Add("BankAccount", typeof(string));

        var bankAccount = await GetLatestBankAccountDescriptionAsync(input.Id);

        employeeTable.Rows.Add(
            employee.FullNameEn.IsNullOrEmpty() ? BuildFullName(employee) : employee.FullNameEn,
            employee.Code ?? employee.Id.ToString(),
            orgAssignment?.JobTitle?.NameEn ?? "N/A",
            orgAssignment?.OrganizationUnit?.DisplayName ?? "N/A",
            basicPay != null ? basicPay.StartDate.ToString("MMMM yyyy") : "N/A",
            bankAccount
        );

        var payItemsTable = new DataTable("PayItems");
        payItemsTable.Columns.Add("Section", typeof(string));
        payItemsTable.Columns.Add("Description", typeof(string));
        payItemsTable.Columns.Add("Amount", typeof(string));

        double totalEarnings = 0;
        double totalDeductions = 0;

        if (basicPay != null)
        {
            var details = await _employeeBasicPayDetailRepository
                .GetAll()
                .Include(d => d.WageType)
                .Where(d => d.EmployeeBasicPayId == basicPay.Id)
                .ToListAsync();

            foreach (var detail in details)
            {
                var isDeduction = detail.WageType?.BillingType == BillingType.Deduction;
                var section = isDeduction ? "Deductions" : "Earnings";
                var description = detail.WageType?.NameEn ?? "Wage Item";

                payItemsTable.Rows.Add(section, description, detail.Amount.ToString("N2"));

                if (isDeduction)
                {
                    totalDeductions += detail.Amount;
                }
                else
                {
                    totalEarnings += detail.Amount;
                }
            }
        }

        if (payItemsTable.Rows.Count == 0)
        {
            // Graceful degradation: no real basic-pay breakdown available for this employee -- show
            // clearly-labeled illustrative line items instead of an empty payslip (pilot only).
            payItemsTable.Rows.Add("Earnings", "Basic Salary (illustrative)", "0.00");
            payItemsTable.Rows.Add("Deductions", "Social Security (illustrative)", "0.00");
        }

        var totalsTable = new DataTable("Totals");
        totalsTable.Columns.Add("TotalEarnings", typeof(string));
        totalsTable.Columns.Add("TotalDeductions", typeof(string));
        totalsTable.Columns.Add("NetPay", typeof(string));
        totalsTable.Rows.Add(
            totalEarnings.ToString("N2"),
            totalDeductions.ToString("N2"),
            (totalEarnings - totalDeductions).ToString("N2")
        );

        var pdfBytes = BuildPayslipReport(employeeTable, payItemsTable, totalsTable);
        return new ReportPdfOutput($"Payslip_{employee.Code ?? employee.Id.ToString()}.pdf", Convert.ToBase64String(pdfBytes));
    }

    private async Task<EmployeeOrgAssignment> GetLatestOrgAssignmentAsync(int employeeId)
    {
        return await _employeeOrgAssignmentRepository
            .GetAll()
            .Include(a => a.JobTitle)
            .Include(a => a.OrganizationUnit)
            .Where(a => a.EmployeeId == employeeId)
            .OrderByDescending(a => a.StartDate)
            .FirstOrDefaultAsync();
    }

    private async Task<EmployeeBasicPay> GetLatestBasicPayAsync(int employeeId)
    {
        return await _employeeBasicPayRepository
            .GetAll()
            .Where(p => p.EmployeeId == employeeId)
            .OrderByDescending(p => p.StartDate)
            .FirstOrDefaultAsync();
    }

    private async Task<string> GetBasicPayCurrencyNameAsync(EmployeeBasicPay basicPay)
    {
        if (basicPay == null)
        {
            return "N/A";
        }

        var firstDetail = await _employeeBasicPayDetailRepository
            .GetAll()
            .Include(d => d.Currency)
            .Where(d => d.EmployeeBasicPayId == basicPay.Id)
            .FirstOrDefaultAsync();

        return firstDetail?.Currency?.NameEn ?? "N/A";
    }

    private async Task<string> GetLatestBankAccountDescriptionAsync(int employeeId)
    {
        var employeeBank = await _employeeBankRepository
            .GetAll()
            .Include(b => b.Bank)
            .Where(b => b.EmployeeId == employeeId)
            .OrderByDescending(b => b.StartDate)
            .FirstOrDefaultAsync();

        if (employeeBank == null)
        {
            return "N/A";
        }

        var bankName = employeeBank.Bank?.NameEn ?? "N/A";
        var accountNum = employeeBank.IBAN.IsNullOrEmpty() ? employeeBank.AccountNum : employeeBank.IBAN;

        return accountNum.IsNullOrEmpty() ? bankName : $"{bankName} - {accountNum}";
    }

    private static string BuildFullName(Employee employee)
    {
        var parts = new[] { employee.FirstNameEn, employee.SecondNameEn, employee.ThirdNameEn, employee.LastNameEn }
            .Where(p => !p.IsNullOrEmpty());
        return string.Join(" ", parts);
    }

    /// <summary>
    /// Report 1 -- Salary Certificate (single-record document, no repeating rows).
    /// </summary>
    private static byte[] BuildSalaryCertificateReport(DataTable table)
    {
        var report = new Report();
        var page = new ReportPage { Name = "Page1" };
        report.Pages.Add(page);

        var titleBand = new ReportTitleBand { Name = "ReportTitle1", Height = Units.Millimeters * 45 };
        page.ReportTitle = titleBand;

        titleBand.Objects.Add(new TextObject
        {
            Name = "txtCompany",
            Bounds = new RectangleF(0, 0, Units.Millimeters * 190, Units.Millimeters * 10),
            Text = "GBS Holding Company",
            Font = new Font("Arial", 14, FontStyle.Bold),
            HorzAlign = HorzAlign.Center
        });
        titleBand.Objects.Add(new TextObject
        {
            Name = "txtCertTitle",
            Bounds = new RectangleF(0, Units.Millimeters * 18, Units.Millimeters * 190, Units.Millimeters * 10),
            Text = "SALARY CERTIFICATE",
            Font = new Font("Arial", 13, FontStyle.Bold | FontStyle.Underline),
            HorzAlign = HorzAlign.Center
        });
        titleBand.Objects.Add(new TextObject
        {
            Name = "txtIssueDate",
            Bounds = new RectangleF(Units.Millimeters * 130, Units.Millimeters * 32, Units.Millimeters * 60, Units.Millimeters * 8),
            Text = "Date: [Employees.IssueDate]",
            Font = new Font("Arial", 9),
            HorzAlign = HorzAlign.Right
        });

        var dataBand = new DataBand { Name = "Data1", Height = Units.Millimeters * 70 };
        page.Bands.Add(dataBand);
        dataBand.Objects.Add(new TextObject
        {
            Name = "txtSalutation",
            Bounds = new RectangleF(0, 0, Units.Millimeters * 190, Units.Millimeters * 8),
            Text = "TO WHOM IT MAY CONCERN",
            Font = new Font("Arial", 11, FontStyle.Bold),
            HorzAlign = HorzAlign.Center
        });
        dataBand.Objects.Add(new TextObject
        {
            Name = "txtBody",
            Bounds = new RectangleF(0, Units.Millimeters * 15, Units.Millimeters * 190, Units.Millimeters * 45),
            Text = "This is to certify that [Employees.FullName] is a permanent employee of " +
                   "GBS Holding Company, working as [Employees.JobTitle], since [Employees.JoiningDate].\n\n" +
                   "The employee's current basic monthly salary is [Employees.BasicSalary] [Employees.Currency].\n\n" +
                   "This certificate is issued upon the employee's request for whatever legal purpose it may serve.",
            Font = new Font("Arial", 10.5f),
            WordWrap = true
        });

        var footerBand = new PageFooterBand { Name = "PageFooter1", Height = Units.Millimeters * 20 };
        page.PageFooter = footerBand;
        footerBand.Objects.Add(new TextObject
        {
            Name = "txtSignature",
            Bounds = new RectangleF(Units.Millimeters * 10, Units.Millimeters * 8, Units.Millimeters * 80, Units.Millimeters * 8),
            Text = "___________________________\nHR Manager, GBS Holding Company",
            Font = new Font("Arial", 9)
        });

        report.RegisterData(table, "Employees");
        report.GetDataSource("Employees")!.Enabled = true;
        report.Prepare();

        using var pdfExport = new PDFSimpleExport();
        using var stream = new System.IO.MemoryStream();
        report.Export(pdfExport, stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Report 2 -- Payslip (header + repeating earnings/deductions rows + computed totals footer).
    /// Three data sources: Employee (1 row), PayItems (N rows), Totals (1 row, computed by the caller).
    /// </summary>
    private static byte[] BuildPayslipReport(DataTable employeeTable, DataTable payItemsTable, DataTable totalsTable)
    {
        var report = new Report();
        var page = new ReportPage { Name = "Page1" };
        report.Pages.Add(page);

        var titleBand = new ReportTitleBand { Name = "ReportTitle1", Height = Units.Millimeters * 45 };
        page.ReportTitle = titleBand;

        titleBand.Objects.Add(new TextObject
        {
            Name = "txtCompany",
            Bounds = new RectangleF(0, 0, Units.Millimeters * 190, Units.Millimeters * 10),
            Text = "GBS Holding Company",
            Font = new Font("Arial", 14, FontStyle.Bold),
            HorzAlign = HorzAlign.Center
        });
        titleBand.Objects.Add(new TextObject
        {
            Name = "txtPayslipTitle",
            Bounds = new RectangleF(0, Units.Millimeters * 14, Units.Millimeters * 190, Units.Millimeters * 10),
            Text = "PAYSLIP",
            Font = new Font("Arial", 13, FontStyle.Bold | FontStyle.Underline),
            HorzAlign = HorzAlign.Center
        });

        var headerBand = new DataBand { Name = "EmployeeHeader", Height = Units.Millimeters * 25 };
        page.Bands.Add(headerBand);
        headerBand.Objects.Add(new TextObject
        {
            Name = "txtEmployeeInfo",
            Bounds = new RectangleF(0, 0, Units.Millimeters * 190, Units.Millimeters * 25),
            Text = "Employee: [Employee.FullName] ([Employee.EmployeeCode])\n" +
                   "Job Title: [Employee.JobTitle]     Department: [Employee.Department]\n" +
                   "Pay Period: [Employee.PayPeriod]     Bank Account: [Employee.BankAccount]",
            Font = new Font("Arial", 10)
        });

        var payItemsBand = new DataBand { Name = "PayItemsBand", Height = Units.Millimeters * 8 };
        page.Bands.Add(payItemsBand);
        payItemsBand.Objects.Add(new TextObject
        {
            Name = "txtPayItemDescription",
            Bounds = new RectangleF(0, 0, Units.Millimeters * 140, Units.Millimeters * 8),
            Text = "[PayItems.Section]: [PayItems.Description]",
            Font = new Font("Arial", 9.5f)
        });
        payItemsBand.Objects.Add(new TextObject
        {
            Name = "txtPayItemAmount",
            Bounds = new RectangleF(Units.Millimeters * 140, 0, Units.Millimeters * 50, Units.Millimeters * 8),
            Text = "[PayItems.Amount]",
            Font = new Font("Arial", 9.5f),
            HorzAlign = HorzAlign.Right
        });

        var totalsBand = new DataBand { Name = "TotalsBand", Height = Units.Millimeters * 28 };
        page.Bands.Add(totalsBand);
        totalsBand.Objects.Add(new TextObject
        {
            Name = "txtTotalEarnings",
            Bounds = new RectangleF(0, 0, Units.Millimeters * 190, Units.Millimeters * 8),
            Text = "Total Earnings: [Totals.TotalEarnings]",
            Font = new Font("Arial", 10),
            HorzAlign = HorzAlign.Right
        });
        totalsBand.Objects.Add(new TextObject
        {
            Name = "txtTotalDeductions",
            Bounds = new RectangleF(0, Units.Millimeters * 8, Units.Millimeters * 190, Units.Millimeters * 8),
            Text = "Total Deductions: [Totals.TotalDeductions]",
            Font = new Font("Arial", 10),
            HorzAlign = HorzAlign.Right
        });
        totalsBand.Objects.Add(new TextObject
        {
            Name = "txtNetPay",
            Bounds = new RectangleF(0, Units.Millimeters * 18, Units.Millimeters * 190, Units.Millimeters * 10),
            Text = "NET PAY: [Totals.NetPay]",
            Font = new Font("Arial", 11, FontStyle.Bold),
            HorzAlign = HorzAlign.Right,
            Border = { Lines = FastReport.BorderLines.Top }
        });

        report.RegisterData(employeeTable, "Employee");
        report.GetDataSource("Employee")!.Enabled = true;
        headerBand.DataSource = report.GetDataSource("Employee");

        report.RegisterData(payItemsTable, "PayItems");
        report.GetDataSource("PayItems")!.Enabled = true;
        payItemsBand.DataSource = report.GetDataSource("PayItems");

        report.RegisterData(totalsTable, "Totals");
        report.GetDataSource("Totals")!.Enabled = true;
        totalsBand.DataSource = report.GetDataSource("Totals");

        report.Prepare();

        using var pdfExport = new PDFSimpleExport();
        using var stream = new System.IO.MemoryStream();
        report.Export(pdfExport, stream);
        return stream.ToArray();
    }
}
