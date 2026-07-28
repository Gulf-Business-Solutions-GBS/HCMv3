using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.Net.Mail;
using Abp.Runtime.Validation;
using Abp.Timing;
using Abp.UI;
using FastReport;
using FastReport.Export.PdfSimple;
using FastReport.Utils;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Reports.Dto;
using HCMv3.SystemSecurity;
using HCMv3.SystemSettings;
using Microsoft.AspNetCore.Mvc;
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
    private readonly IRepository<SalaryRegister> _salaryRegisterRepository;
    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<ManagePayrollSettings> _payrollSettingsRepository;
    private readonly IRepository<PayslipConfig> _payslipConfigRepository;
    private readonly IEmailSender _emailSender;

    public ReportsAppService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        IRepository<EmployeeBasicPay> employeeBasicPayRepository,
        IRepository<EmployeeBasicPayDetail> employeeBasicPayDetailRepository,
        IRepository<EmployeeBank> employeeBankRepository,
        IRepository<SalaryRegister> salaryRegisterRepository,
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<ManagePayrollSettings> payrollSettingsRepository,
        IRepository<PayslipConfig> payslipConfigRepository,
        IEmailSender emailSender)
    {
        _employeeRepository = employeeRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _employeeBasicPayRepository = employeeBasicPayRepository;
        _employeeBasicPayDetailRepository = employeeBasicPayDetailRepository;
        _employeeBankRepository = employeeBankRepository;
        _salaryRegisterRepository = salaryRegisterRepository;
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _payrollSettingsRepository = payrollSettingsRepository;
        _payslipConfigRepository = payslipConfigRepository;
        _emailSender = emailSender;
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

    #region Payslip -- PREVIEW path (legacy uscPreviewPaySlip)

    /// <summary>
    /// Port of legacy <c>uscPreviewPaySlip.dxShowSlip_Click</c>: renders ONE employee's payslip for ONE
    /// period straight to the browser. Data comes from the posted <see cref="SalaryRegister"/>, matching
    /// the legacy DevExpress <c>rptPayslipEn</c> master query and its two subreport queries
    /// (<c>srptPaySlipEarningEn</c> / <c>srptPaySlipDeductionEn</c>).
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_Reports_Preview)]
    public async Task<ReportPdfOutput> GetPayslipPdf(PayslipInput input)
    {
        var data = await BuildPayslipDataAsync(
            input.EmployeeId,
            input.Month,
            input.Year,
            input.PayrollRunType,
            input.EmployeeStatus);

        if (data == null)
        {
            throw new UserFriendlyException(L("NoPayslipFoundForSelectedPeriod"));
        }

        var pdfBytes = RenderPayslipPdf(data, input.Language);
        return new ReportPdfOutput($"Payslip_{data.EmployeeCode}_{input.Year:0000}{input.Month:00}.pdf", Convert.ToBase64String(pdfBytes));
    }

    #endregion

    #region Payslip -- REPORT path (legacy uscRptPaySlip: bulk resolve + e-mail)

    /// <summary>
    /// Port of the inline employee-list SQL in <c>uscRptPaySlip.dxButton1_Click</c>:
    /// <c>Where IsNull(A.Email,'') != '' and A.[Status] = @status and B.PayrollArea = @area</c>, restricted
    /// to employees that actually have a matching <see cref="SalaryRegister"/> for the period/run type
    /// (legacy relied on the report itself producing nothing for employees without a register; resolving it
    /// up front lets the operator see the real recipient list before money documents are mailed).
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_Reports_Payslip_Email)]
    [HttpPost]
    public async Task<List<PayslipRecipientDto>> GetPayslipRecipients(GetPayslipRecipientsInput input)
    {
        GuardPayrollAreaFilterNotSupported(input.PayrollAreaId);

        var registers = await BuildPeriodRegisterQuery(input.Month, input.Year, input.PayrollRunType)
            .Include(r => r.Employee)
            .Where(r => r.Employee.Email != null && r.Employee.Email != "")
            .WhereIf(input.EmployeeStatus.HasValue, r => r.Employee.Status == input.EmployeeStatus.Value)
            .ToListAsync();

        var result = new List<PayslipRecipientDto>();
        foreach (var register in registers)
        {
            result.Add(new PayslipRecipientDto
            {
                EmployeeId = register.EmployeeId,
                EmployeeCode = register.Employee?.Code,
                EmployeeName = register.Employee == null
                    ? string.Empty
                    : (register.Employee.FullNameEn.IsNullOrEmpty() ? BuildFullName(register.Employee) : register.Employee.FullNameEn),
                Email = register.Employee?.Email,
                Status = register.Employee?.Status ?? EmployeeStatus.Inactive,
                NetPay = Math.Round(await GetNetPayAsync(register.Id), PayslipDecimals, MidpointRounding.AwayFromZero)
            });
        }

        // Legacy ORDER BY: numeric codes right-aligned/zero-padded to 20, non-numeric left-aligned.
        return result
            .OrderBy(r => LegacyEmployeeCodeSortKey(r.EmployeeCode), StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Port of <c>uscRptPaySlip.dxButton1_Click</c> + <c>Send_PaySlip</c>: renders each payslip and mails it
    /// as a PDF attachment. Legacy swallowed every per-employee exception and only counted successes; this
    /// port keeps that behaviour (a single bad address must not abort a payroll-wide mail run) but returns
    /// the failure count too.
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_Reports_Payslip_Email)]
    public async Task<SendPayslipEmailsOutput> SendPayslipEmails(SendPayslipEmailsInput input)
    {
        GuardPayrollAreaFilterNotSupported(input.PayrollAreaId);

        List<PayslipRecipientDto> recipients;
        if (input.EmployeeId.HasValue)
        {
            // Legacy: a typed employee code short-circuits the list query entirely.
            var employee = await _employeeRepository.GetAsync(input.EmployeeId.Value);
            recipients = new List<PayslipRecipientDto>
            {
                new PayslipRecipientDto
                {
                    EmployeeId = employee.Id,
                    EmployeeCode = employee.Code,
                    EmployeeName = employee.FullNameEn.IsNullOrEmpty() ? BuildFullName(employee) : employee.FullNameEn,
                    Email = employee.Email,
                    Status = employee.Status
                }
            };
        }
        else
        {
            recipients = await GetPayslipRecipients(input);
        }

        var subject = await GetPayslipEmailSubjectAsync();
        var output = new SendPayslipEmailsOutput { TotalCount = recipients.Count };

        foreach (var recipient in recipients)
        {
            try
            {
                if (recipient.Email.IsNullOrWhiteSpace())
                {
                    output.FailureCount++;
                    continue;
                }

                var data = await BuildPayslipDataAsync(
                    recipient.EmployeeId,
                    input.Month,
                    input.Year,
                    input.PayrollRunType,
                    input.EmployeeStatus);

                if (data == null)
                {
                    output.FailureCount++;
                    continue;
                }

                var pdfBytes = RenderPayslipPdf(data, input.Language);

                using var mail = new MailMessage { Subject = subject, IsBodyHtml = true };
                mail.To.Add(recipient.Email);
                mail.Body = BuildPayslipEmailBodyHtml(data);
                using var attachmentStream = new System.IO.MemoryStream(pdfBytes);
                mail.Attachments.Add(new Attachment(attachmentStream, "payslip.pdf", "application/pdf"));

                await _emailSender.SendAsync(mail);
                output.SuccessCount++;
            }
            catch
            {
                // Legacy parity: per-employee failures are swallowed so the run continues.
                output.FailureCount++;
            }
        }

        return output;
    }

    /// <summary>
    /// Legacy narrowed the bulk recipient list by PayrollArea via
    /// <c>EmployeeOrgAssignment.PayrollArea</c>. That column is explicitly DEFERRED in this port -- see the
    /// class remarks on <c>HCMv3.PersonnelAdministration.EmployeeOrgAssignment</c> ("DEFERRED (not ported,
    /// see Wave B scope notes): Position/EmployeeGroup/EmployeeSubGroup/PayrollArea/WorkflowGroup"), and no
    /// other Employee-to-PayrollArea link exists in HCMv3. Silently dropping the filter would mail payslips
    /// to employees outside the selected payroll area, so it is refused outright.
    /// Legacy pointer: uscRptPaySlip.ascx.cs lines 150-153 (<c>B.PayrollArea = '@ParamPayrollArea'</c>).
    /// </summary>
    private static void GuardPayrollAreaFilterNotSupported(int? payrollAreaId)
    {
        if (payrollAreaId.HasValue)
        {
            throw new NotImplementedException(
                "Payroll-area filtering of payslip recipients is not portable: EmployeeOrgAssignment.PayrollArea " +
                "is deferred in HCMv3 and no other Employee-to-PayrollArea link exists. " +
                "Legacy pointer: HCM10.Web/UserControls/PayrollManagement/uscRptPaySlip.ascx.cs lines 150-153.");
        }
    }

    private async Task<string> GetPayslipEmailSubjectAsync()
    {
        // PayslipConfig is the ported legacy SysSecurity.PayslipConfig -> EmailTemplate pointer; legacy
        // rptPayslipEn used this.ExportOptions.Email.Subject (a DevExpress report property) for the subject.
        var config = await _payslipConfigRepository
            .GetAll()
            .Include(c => c.EmailTemplate)
            .FirstOrDefaultAsync();

        return config?.EmailTemplate?.Subject.IsNullOrWhiteSpace() == false
            ? config.EmailTemplate.Subject
            : "Payslip";
    }

    /// <summary>
    /// Port of <c>rptPayslipEn.BodyTemplateHtml</c>, minus the embedded company-logo LinkedResource (the
    /// legacy body inlined <c>Employee.CurrentCompany.Logo</c>; HCMv3 has no CurrentCompany navigation on
    /// Employee, so the logo is omitted -- reported as a deviation).
    /// </summary>
    private static string BuildPayslipEmailBodyHtml(PayslipData data)
    {
        return
            "<div><table>" +
            $"<thead><tr><td><strong><h3>{data.CompanyName}</h3></strong></td></tr>" +
            $"<tr><td><br /><u><b>Salary Release For Month {data.PeriodLabel}</b></u></td></tr></thead>" +
            $"<tbody><tr><td><br /><strong>Dear Mr /Mrs/ Miss </strong><u>{data.EmployeeName}</u></td></tr>" +
            $"<tr><td><br />We would like to inform you that the salaries have been released for month {data.PeriodLabel}." +
            "<br />To view your own salary slip in detail please follow attach slip.</td></tr>" +
            "<tr><td><strong><br />Finance Department.</strong></td></tr>" +
            "<tr><td><br />This is an auto generated message .Please do not try to reply to this e-mail.</td></tr>" +
            "</tbody></table></div>";
    }

    #endregion

    #region Payslip -- data assembly

    /// <summary>
    /// Number of decimals used for every monetary/quantity figure on the payslip. Legacy prints amounts,
    /// Number and Rate with the DevExpress format string <c>{0:#0.000}</c>
    /// (srptPaySlipEarningEn.Designer.cs line 119/160/362) and the subreport SQL applies
    /// <c>ROUND(Number, 3)</c> / <c>ROUND(Rate, 3)</c>. 3 decimals is also the KWD minor unit.
    /// </summary>
    private const int PayslipDecimals = 3;

    private const string PayslipAmountFormat = "#0.000";

    /// <summary>Legacy net-pay carrier wage type (<c>WageType.Code = 9000</c>).</summary>
    private const string NetPayWageTypeCode = "9000";

    /// <summary>
    /// Legacy master-query period predicate:
    /// <c>InPeriod = ForPeriod AND Month(InPeriod) = @Month AND Year(InPeriod) = @Year AND PayrollRunType = @RunType</c>.
    /// </summary>
    private IQueryable<SalaryRegister> BuildPeriodRegisterQuery(int month, int year, PayrollRunType runType)
    {
        return _salaryRegisterRepository
            .GetAll()
            .Where(r => r.InPeriod == r.ForPeriod
                        && r.InPeriod.Month == month
                        && r.InPeriod.Year == year
                        && r.PayrollRunType == runType);
    }

    private async Task<PayslipData> BuildPayslipDataAsync(
        int employeeId,
        int month,
        int year,
        PayrollRunType runType,
        EmployeeStatus? employeeStatus)
    {
        var register = await BuildPeriodRegisterQuery(month, year, runType)
            .Include(r => r.Employee)
            .Include(r => r.PayrollReasonType)
            .Where(r => r.EmployeeId == employeeId)
            .WhereIf(employeeStatus.HasValue, r => r.Employee.Status == employeeStatus.Value)
            .FirstOrDefaultAsync();

        if (register == null)
        {
            return null;
        }

        var employee = register.Employee ?? await _employeeRepository.GetAsync(employeeId);
        var orgAssignment = await GetOrgAssignmentAtAsync(employeeId, register.ForPeriod);
        var bankDescription = await GetLatestBankAccountDescriptionAsync(employeeId);
        var basicPay = await GetLatestBasicPayAsync(employeeId);
        var currency = await GetBasicPayCurrencyShortFormAsync(basicPay);

        var details = await _salaryRegisterDetailRepository
            .GetAll()
            .Include(d => d.WageType)
            .Where(d => d.SalaryRegisterId == register.Id && d.Amount != 0)
            .ToListAsync();

        // srptPaySlipEarningEn: BillingType = 0 (Earning), DontImpactNetPay = 0, WageType.Code <> 9000.
        var earnings = details
            .Where(d => d.WageType != null
                        && d.WageType.BillingType == BillingType.Earning
                        && !d.WageType.DontImpactNetPay
                        && d.WageType.Code != NetPayWageTypeCode)
            .Select(ToPayslipLine)
            .ToList();

        // srptPaySlipDeductionEn: BillingType = 1 (Deduction), DontImpactNetPay = 0.
        // DEVIATION: legacy also excluded the hard-coded XPO row id `SRdetail.WageType <> 65`. That is a raw
        // primary key of the legacy database with no stable meaning in HCMv3, so it is NOT ported.
        var deductions = details
            .Where(d => d.WageType != null
                        && d.WageType.BillingType == BillingType.Deduction
                        && !d.WageType.DontImpactNetPay)
            .Select(ToPayslipLine)
            .ToList();

        // Net pay is NOT recomputed as (earnings - deductions): legacy prints the payroll engine's own
        // wage type 9000 amount, and the payslip must reconcile with the register exactly.
        var netPay = await GetNetPayAsync(register.Id);

        var payrollSettings = await _payrollSettingsRepository.GetAll().FirstOrDefaultAsync();

        return new PayslipData
        {
            EmployeeCode = employee.Code ?? employee.Id.ToString(),
            EmployeeName = employee.FullNameEn.IsNullOrEmpty() ? BuildFullName(employee) : employee.FullNameEn,
            EmployeeNameAr = employee.FullNameAr,
            CivilId = employee.CivilID,
            JobTitleEn = orgAssignment?.JobTitle?.NameEn ?? "N/A",
            JobTitleAr = orgAssignment?.JobTitle?.NameAr ?? "N/A",
            OrgUnit = orgAssignment?.OrganizationUnit?.DisplayName ?? "N/A",
            CompanyName = "GBS Holding Company",
            JoiningDate = employee.JoiningDate,
            BankDescription = bankDescription,
            Currency = currency,
            ForPeriod = register.ForPeriod,
            InPeriod = register.InPeriod,
            PeriodLabel = new DateTime(year, month, 1).ToString("MMM-yyyy"),
            HeaderText = BuildHeaderText(register, year, month),
            Earnings = earnings,
            Deductions = deductions,
            NetPay = Math.Round(netPay, PayslipDecimals, MidpointRounding.AwayFromZero),
            ShowSignature = payrollSettings?.PayslipSignature ?? false
        };
    }

    /// <summary>
    /// Port of the legacy master query's <c>HeaderText</c> CASE expression. The <c>@PrmEncashment</c> branch
    /// ("LEAVE ENCASHMENT PAYSLIP...") is unreachable from both ported screens -- neither
    /// <c>uscPreviewPaySlip</c> nor <c>uscRptPaySlip</c> ever sets <c>PrmEncashment</c> -- so it is not ported.
    /// </summary>
    private static string BuildHeaderText(SalaryRegister register, int year, int month)
    {
        if (register.PayrollRunType == PayrollRunType.Regular)
        {
            return $"REGULAR PAYSLIP FOR THE MONTH : {register.ForPeriod.ToString("MMMM").ToUpperInvariant()}-{year}";
        }

        var reason = register.PayrollReasonType?.NameEn ?? string.Empty;
        if (reason == "Advance Leave")
        {
            return "LEAVE SALARY PAYSLIP";
        }

        if (reason == "End Of Service")
        {
            return "END OF SERVICE SETTLEMENT";
        }

        // Legacy CASE has no ELSE -> NULL header. Fall back to a neutral label rather than a blank banner.
        return $"PAYSLIP FOR THE MONTH : {new DateTime(year, month, 1).ToString("MMMM").ToUpperInvariant()}-{year}";
    }

    private static PayslipLine ToPayslipLine(SalaryRegisterDetail detail)
    {
        return new PayslipLine
        {
            DescriptionEn = detail.WageType?.NameEn ?? "Wage Item",
            DescriptionAr = detail.WageType?.NameAr ?? detail.WageType?.NameEn ?? "Wage Item",
            Number = Math.Round(detail.Number, PayslipDecimals, MidpointRounding.AwayFromZero),
            Rate = Math.Round(detail.Rate, PayslipDecimals, MidpointRounding.AwayFromZero),
            Amount = Math.Round(detail.Amount, PayslipDecimals, MidpointRounding.AwayFromZero)
        };
    }

    /// <summary>Legacy: <c>SalaryRegisterDetail.Amount</c> where <c>WageType.Code = 9000</c>.</summary>
    private async Task<double> GetNetPayAsync(int salaryRegisterId)
    {
        var netPayDetail = await _salaryRegisterDetailRepository
            .GetAll()
            .Include(d => d.WageType)
            .Where(d => d.SalaryRegisterId == salaryRegisterId && d.WageType.Code == NetPayWageTypeCode)
            .FirstOrDefaultAsync();

        return netPayDetail?.Amount ?? 0;
    }

    /// <summary>
    /// Legacy joined <c>EmployeeOrgAssignment</c> on the payslip date, i.e. the LAST day of the selected
    /// month: <c>Employee.PaySlipDate BETWEEN StartDate AND EndDate</c>.
    /// </summary>
    private async Task<EmployeeOrgAssignment> GetOrgAssignmentAtAsync(int employeeId, DateTime forPeriod)
    {
        var paySlipDate = new DateTime(forPeriod.Year, forPeriod.Month, 1).AddMonths(1).AddDays(-1);

        var assignment = await _employeeOrgAssignmentRepository
            .GetAll()
            .Include(a => a.JobTitle)
            .Include(a => a.OrganizationUnit)
            .Where(a => a.EmployeeId == employeeId && a.StartDate <= paySlipDate && a.EndDate >= paySlipDate)
            .FirstOrDefaultAsync();

        return assignment ?? await GetLatestOrgAssignmentAsync(employeeId);
    }

    private async Task<string> GetBasicPayCurrencyShortFormAsync(EmployeeBasicPay basicPay)
    {
        if (basicPay == null)
        {
            return string.Empty;
        }

        var firstDetail = await _employeeBasicPayDetailRepository
            .GetAll()
            .Include(d => d.Currency)
            .Where(d => d.EmployeeBasicPayId == basicPay.Id)
            .FirstOrDefaultAsync();

        return firstDetail?.Currency?.CurrencyShortFormEn ?? string.Empty;
    }

    /// <summary>
    /// Port of the legacy ORDER BY: numeric employee codes are right-aligned into a 20-char zero-padded
    /// string so they sort numerically; non-numeric codes sort as-is.
    /// </summary>
    private static string LegacyEmployeeCodeSortKey(string code)
    {
        if (code.IsNullOrWhiteSpace())
        {
            return string.Empty;
        }

        return double.TryParse(code, out _) ? code.PadLeft(20, '0') : code;
    }

    private sealed class PayslipLine
    {
        public string DescriptionEn { get; set; }
        public string DescriptionAr { get; set; }
        public double Number { get; set; }
        public double Rate { get; set; }
        public double Amount { get; set; }
    }

    private sealed class PayslipData
    {
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public string EmployeeNameAr { get; set; }
        public long CivilId { get; set; }
        public string JobTitleEn { get; set; }
        public string JobTitleAr { get; set; }
        public string OrgUnit { get; set; }
        public string CompanyName { get; set; }
        public DateTime JoiningDate { get; set; }
        public string BankDescription { get; set; }
        public string Currency { get; set; }
        public DateTime ForPeriod { get; set; }
        public DateTime InPeriod { get; set; }
        public string PeriodLabel { get; set; }
        public string HeaderText { get; set; }
        public List<PayslipLine> Earnings { get; set; } = new List<PayslipLine>();
        public List<PayslipLine> Deductions { get; set; } = new List<PayslipLine>();
        public double NetPay { get; set; }
        public bool ShowSignature { get; set; }

        public double TotalEarnings => Math.Round(Earnings.Sum(e => e.Amount), PayslipDecimals, MidpointRounding.AwayFromZero);

        public double TotalDeductions => Math.Round(Deductions.Sum(d => d.Amount), PayslipDecimals, MidpointRounding.AwayFromZero);
    }

    #endregion

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


    #region Payslip -- rendering

    /// <summary>
    /// Renders the payslip document. This replaces the legacy DevExpress <c>rptPayslipEn</c> /
    /// <c>rptPayslipAr</c> XtraReports with a programmatic FastReport.OpenSource layout.
    ///
    /// KNOWN GAPS vs. the DevExpress originals (reported, NOT worked around -- the reporting-tool decision
    /// is open):
    /// <list type="bullet">
    /// <item><description>The legacy payslip is a master report with ~10 SUBREPORTS (earnings, deductions,
    /// working-days summary, attendance summary, indemnity days, leave info, leave encashment, overtime
    /// hours, gratuity). FastReport.OpenSource has no XRSubreport equivalent; only the earnings and
    /// deductions sections are reproduced here.</description></item>
    /// <item><description>The legacy header prints the company logo from
    /// <c>OrganizationUnit.Logo</c> (a byte[] picked from OrgLevel1..7 by OrganizationUnitType =
    /// 'Company'). HCMv3's EmployeeOrgAssignment models OrgLevel1..7 as a single OrganizationUnitId, so the
    /// company/logo resolution has no equivalent and is omitted.</description></item>
    /// <item><description>Arabic (<c>rptPayslipAr</c>) relied on the DevExpress RTL layout
    /// (<c>RightToLeft</c>/<c>RightToLeftLayout</c>) and embedded Arabic fonts. FastReport.OpenSource's
    /// PDFSimpleExport does no bidi shaping and embeds no fonts, so the Arabic variant here only swaps the
    /// NameAr text and right-aligns; correct Arabic rendering is a genuine tool gap.</description></item>
    /// </list>
    /// </summary>
    private static byte[] RenderPayslipPdf(PayslipData data, PayslipReportLanguage language)
    {
        var report = new Report();

        if (language == PayslipReportLanguage.English || language == PayslipReportLanguage.EnglishAndArabic)
        {
            AddPayslipPage(report, data, arabic: false, suffix: "En");
        }

        if (language == PayslipReportLanguage.Arabic || language == PayslipReportLanguage.EnglishAndArabic)
        {
            // DEVIATION: legacy "English/Arabic" produced TWO separate PDFs and mailed both
            // (uscRptPaySlip.Send_PaySlip runs the English branch then the Arabic branch). A single
            // PDF response cannot carry two documents, so both variants are emitted as pages of one PDF.
            AddPayslipPage(report, data, arabic: true, suffix: "Ar");
        }

        report.Prepare();

        using var pdfExport = new PDFSimpleExport();
        using var stream = new System.IO.MemoryStream();
        report.Export(pdfExport, stream);
        return stream.ToArray();
    }

    private static void AddPayslipPage(Report report, PayslipData data, bool arabic, string suffix)
    {
        var headerTable = new DataTable("Header" + suffix);
        headerTable.Columns.Add("Company", typeof(string));
        headerTable.Columns.Add("HeaderText", typeof(string));
        headerTable.Columns.Add("EmployeeName", typeof(string));
        headerTable.Columns.Add("EmployeeCode", typeof(string));
        headerTable.Columns.Add("JobTitle", typeof(string));
        headerTable.Columns.Add("OrgUnit", typeof(string));
        headerTable.Columns.Add("JoiningDate", typeof(string));
        headerTable.Columns.Add("BankAccount", typeof(string));
        headerTable.Columns.Add("CivilId", typeof(string));
        headerTable.Columns.Add("Period", typeof(string));
        headerTable.Columns.Add("Currency", typeof(string));

        headerTable.Rows.Add(
            data.CompanyName,
            data.HeaderText,
            arabic ? (data.EmployeeNameAr.IsNullOrWhiteSpace() ? data.EmployeeName : data.EmployeeNameAr) : data.EmployeeName,
            data.EmployeeCode,
            arabic ? data.JobTitleAr : data.JobTitleEn,
            data.OrgUnit,
            // Legacy date format on the payslip labels is {0:dd-MM-yyyy} (rptPayslipEn.Designer.cs line 377).
            data.JoiningDate.ToString("dd-MM-yyyy"),
            data.BankDescription,
            data.CivilId == 0 ? string.Empty : data.CivilId.ToString(),
            data.PeriodLabel,
            data.Currency);

        var linesTable = new DataTable("Lines" + suffix);
        linesTable.Columns.Add("Section", typeof(string));
        linesTable.Columns.Add("Description", typeof(string));
        linesTable.Columns.Add("Number", typeof(string));
        linesTable.Columns.Add("Rate", typeof(string));
        linesTable.Columns.Add("Amount", typeof(string));

        var earningsLabel = arabic ? "الاستحقاقات" : "EARNINGS";
        var deductionsLabel = arabic ? "الاستقطاعات" : "DEDUCTIONS";

        foreach (var line in data.Earnings)
        {
            AddPayslipLineRow(linesTable, earningsLabel, line, arabic);
        }

        foreach (var line in data.Deductions)
        {
            AddPayslipLineRow(linesTable, deductionsLabel, line, arabic);
        }

        var totalsTable = new DataTable("Totals" + suffix);
        totalsTable.Columns.Add("TotalEarnings", typeof(string));
        totalsTable.Columns.Add("TotalDeductions", typeof(string));
        totalsTable.Columns.Add("NetPay", typeof(string));
        totalsTable.Rows.Add(
            data.TotalEarnings.ToString(PayslipAmountFormat),
            data.TotalDeductions.ToString(PayslipAmountFormat),
            data.NetPay.ToString(PayslipAmountFormat));

        var page = new ReportPage { Name = "Page" + suffix };
        report.Pages.Add(page);

        var align = arabic ? HorzAlign.Right : HorzAlign.Left;

        var titleBand = new ReportTitleBand { Name = "Title" + suffix, Height = Units.Millimeters * 24 };
        page.ReportTitle = titleBand;
        titleBand.Objects.Add(new TextObject
        {
            Name = "txtCompany" + suffix,
            Bounds = new RectangleF(0, 0, Units.Millimeters * 190, Units.Millimeters * 10),
            Text = "[Header" + suffix + ".Company]",
            Font = new Font("Arial", 14, FontStyle.Bold),
            HorzAlign = HorzAlign.Center
        });
        titleBand.Objects.Add(new TextObject
        {
            Name = "txtHeaderText" + suffix,
            Bounds = new RectangleF(0, Units.Millimeters * 12, Units.Millimeters * 190, Units.Millimeters * 10),
            Text = "[Header" + suffix + ".HeaderText]",
            Font = new Font("Arial", 11, FontStyle.Bold | FontStyle.Underline),
            HorzAlign = HorzAlign.Center
        });

        var headerBand = new DataBand { Name = "HeaderBand" + suffix, Height = Units.Millimeters * 39 };
        page.Bands.Add(headerBand);
        headerBand.Objects.Add(new TextObject
        {
            Name = "txtEmployeeInfo" + suffix,
            Bounds = new RectangleF(0, 0, Units.Millimeters * 190, Units.Millimeters * 30),
            Text = "Employee: [Header" + suffix + ".EmployeeName] ([Header" + suffix + ".EmployeeCode])\n" +
                   "Job Title: [Header" + suffix + ".JobTitle]     Org Unit: [Header" + suffix + ".OrgUnit]\n" +
                   "Joining Date: [Header" + suffix + ".JoiningDate]     Civil ID: [Header" + suffix + ".CivilId]\n" +
                   "Period: [Header" + suffix + ".Period]     Currency: [Header" + suffix + ".Currency]\n" +
                   "Bank: [Header" + suffix + ".BankAccount]",
            Font = new Font("Arial", 10),
            HorzAlign = align
        });

        // Column captions live on the (single-row) header band rather than on a DataHeaderBand:
        // FastReport.OpenSource rejects a DataHeaderBand added directly to page.Bands, and attaching one via
        // DataBand.Header destabilises Prepare() for this layout. Reported as a layout gap.
        headerBand.Objects.Add(BuildPayslipCaption("hdrSection" + suffix, 0, 34, 30, "Type"));
        headerBand.Objects.Add(BuildPayslipCaption("hdrDescription" + suffix, 34, 76, 30, "Description"));
        headerBand.Objects.Add(BuildPayslipCaption("hdrNumber" + suffix, 110, 25, 30, "Number"));
        headerBand.Objects.Add(BuildPayslipCaption("hdrRate" + suffix, 135, 25, 30, "Rate"));
        headerBand.Objects.Add(BuildPayslipCaption("hdrAmount" + suffix, 160, 30, 30, "Amount"));

        var linesBand = new DataBand { Name = "LinesBand" + suffix, Height = Units.Millimeters * 7 };
        page.Bands.Add(linesBand);
        linesBand.Objects.Add(BuildPayslipCell("colSection" + suffix, 0, 34, "[Lines" + suffix + ".Section]", false, HorzAlign.Left));
        linesBand.Objects.Add(BuildPayslipCell("colDescription" + suffix, 34, 76, "[Lines" + suffix + ".Description]", false, HorzAlign.Left));
        linesBand.Objects.Add(BuildPayslipCell("colNumber" + suffix, 110, 25, "[Lines" + suffix + ".Number]", false, HorzAlign.Right));
        linesBand.Objects.Add(BuildPayslipCell("colRate" + suffix, 135, 25, "[Lines" + suffix + ".Rate]", false, HorzAlign.Right));
        linesBand.Objects.Add(BuildPayslipCell("colAmount" + suffix, 160, 30, "[Lines" + suffix + ".Amount]", false, HorzAlign.Right));

        var totalsBand = new DataBand { Name = "TotalsBand" + suffix, Height = Units.Millimeters * 30 };
        page.Bands.Add(totalsBand);
        totalsBand.Objects.Add(new TextObject
        {
            Name = "txtTotalEarnings" + suffix,
            Bounds = new RectangleF(0, Units.Millimeters * 2, Units.Millimeters * 190, Units.Millimeters * 7),
            Text = "Total Earnings: [Totals" + suffix + ".TotalEarnings]",
            Font = new Font("Arial", 10),
            HorzAlign = HorzAlign.Right
        });
        totalsBand.Objects.Add(new TextObject
        {
            Name = "txtTotalDeductions" + suffix,
            Bounds = new RectangleF(0, Units.Millimeters * 10, Units.Millimeters * 190, Units.Millimeters * 7),
            Text = "Total Deductions: [Totals" + suffix + ".TotalDeductions]",
            Font = new Font("Arial", 10),
            HorzAlign = HorzAlign.Right
        });
        totalsBand.Objects.Add(new TextObject
        {
            Name = "txtNetPay" + suffix,
            Bounds = new RectangleF(0, Units.Millimeters * 19, Units.Millimeters * 190, Units.Millimeters * 9),
            Text = "NET PAY: [Totals" + suffix + ".NetPay]",
            Font = new Font("Arial", 11, FontStyle.Bold),
            HorzAlign = HorzAlign.Right,
            Border = { Lines = FastReport.BorderLines.Top }
        });

        // Legacy: PanelSignature.Visible = SysService.GetPayrollSettings(...).PayslipSignature
        // (rptPayslipEn.cs line 58). Ported to ManagePayrollSettings.PayslipSignature.
        if (data.ShowSignature)
        {
            var footerBand = new PageFooterBand { Name = "Footer" + suffix, Height = Units.Millimeters * 20 };
            page.PageFooter = footerBand;
            footerBand.Objects.Add(new TextObject
            {
                Name = "txtSignature" + suffix,
                Bounds = new RectangleF(Units.Millimeters * 10, Units.Millimeters * 8, Units.Millimeters * 90, Units.Millimeters * 10),
                Text = "___________________________\nAuthorised Signatory",
                Font = new Font("Arial", 9)
            });
        }

        report.RegisterData(headerTable, headerTable.TableName);
        report.GetDataSource(headerTable.TableName)!.Enabled = true;
        headerBand.DataSource = report.GetDataSource(headerTable.TableName);

        report.RegisterData(linesTable, linesTable.TableName);
        report.GetDataSource(linesTable.TableName)!.Enabled = true;
        linesBand.DataSource = report.GetDataSource(linesTable.TableName);

        report.RegisterData(totalsTable, totalsTable.TableName);
        report.GetDataSource(totalsTable.TableName)!.Enabled = true;
        totalsBand.DataSource = report.GetDataSource(totalsTable.TableName);
    }

    private static void AddPayslipLineRow(DataTable table, string sectionLabel, PayslipLine line, bool arabic)
    {
        table.Rows.Add(
            sectionLabel,
            arabic ? line.DescriptionAr : line.DescriptionEn,
            line.Number.ToString(PayslipAmountFormat),
            line.Rate.ToString(PayslipAmountFormat),
            line.Amount.ToString(PayslipAmountFormat));
    }

    /// <summary>
    /// Bold column caption placed at an explicit offset inside the employee-header band.
    ///
    /// FastReport.OpenSource 2026.2.3 GAP: setting <c>Border = { Lines = BorderLines.Bottom }</c> on these
    /// caption objects HARD-CRASHES the process during <c>Report.Prepare()</c> (no managed exception; the
    /// test host dies). The underline is therefore omitted. The DevExpress original ruled the column header
    /// with a real line.
    /// </summary>
    private static TextObject BuildPayslipCaption(string name, float leftMm, float widthMm, float topMm, string text)
    {
        return new TextObject
        {
            Name = name,
            Bounds = new RectangleF(Units.Millimeters * leftMm, Units.Millimeters * topMm, Units.Millimeters * widthMm, Units.Millimeters * 7),
            Text = text,
            Font = new Font("Arial", 9.5f, FontStyle.Bold),
            HorzAlign = leftMm >= 110 ? HorzAlign.Right : HorzAlign.Left
        };
    }

    private static TextObject BuildPayslipCell(string name, float leftMm, float widthMm, string text, bool bold, HorzAlign align)
    {
        return new TextObject
        {
            Name = name,
            Bounds = new RectangleF(Units.Millimeters * leftMm, 0, Units.Millimeters * widthMm, Units.Millimeters * 7),
            Text = text,
            Font = new Font("Arial", 9.5f, bold ? FontStyle.Bold : FontStyle.Regular),
            HorzAlign = align
        };
    }

    #endregion
}
