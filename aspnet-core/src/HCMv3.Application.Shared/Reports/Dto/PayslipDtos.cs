using System.ComponentModel.DataAnnotations;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;

namespace HCMv3.Reports.Dto;

/// <summary>
/// Language variant of the payslip document. Ported from the legacy "Report Type" combo bound in
/// <c>uscPreviewPaySlip.BindReportType</c> ("English"/"Arabic") and
/// <c>uscRptPaySlip.BindReportType</c> ("English/Arabic"/"English"/"Arabic").
///
/// NOTE: <see cref="EnglishAndArabic"/> exists only on the bulk (report) screen in legacy — the preview
/// screen deliberately commented it out (<c>uscPreviewPaySlip.ascx.cs</c> line 116). In legacy it means
/// "render and mail BOTH documents"; see <c>uscRptPaySlip.Send_PaySlip</c> which runs the English branch
/// and then the Arabic branch.
/// </summary>
public enum PayslipReportLanguage
{
    English = 0,
    Arabic = 1,
    EnglishAndArabic = 2
}

/// <summary>
/// Single-employee payslip request. This is the faithful port of the legacy
/// <c>uscPreviewPaySlip</c> parameter set (ParamEmpCode / ParamMonth / ParamYear / ParamPayrollArea /
/// ParamPayrollRunType / PrmempStatus / ReportType) that was pushed into the DevExpress
/// <c>rptPayslipEn</c>/<c>rptPayslipAr</c> report parameters.
/// </summary>
public class PayslipInput
{
    /// <summary>
    /// Legacy <c>@PrmEmpCode</c>. Legacy passed the employee CODE; this port takes the surrogate
    /// <c>Employee.Id</c> instead (codes are not unique-by-contract in HCMv3).
    /// </summary>
    [Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }

    /// <summary>Legacy <c>@PrmMonth</c> — matched against <c>SalaryRegister.InPeriod</c>'s month.</summary>
    [Range(1, 12)]
    public int Month { get; set; }

    /// <summary>Legacy <c>@PrmYear</c> — matched against <c>SalaryRegister.InPeriod</c>'s year.</summary>
    [Range(1900, 2999)]
    public int Year { get; set; }

    /// <summary>Legacy <c>@PrmPayrollRunType</c> (combo "Regular"=0 / "OffCycle"=1).</summary>
    public PayrollRunType PayrollRunType { get; set; } = PayrollRunType.Regular;

    /// <summary>
    /// Legacy <c>@PrmEmployeeStatus</c>. Legacy uses the sentinel -1 for "any status"; this port models
    /// that as <c>null</c>.
    /// </summary>
    public EmployeeStatus? EmployeeStatus { get; set; }

    /// <summary>
    /// Legacy <c>@PrmPayrollArea</c>. On the SINGLE-employee path this is deliberately IGNORED, exactly
    /// as the legacy SQL does: the WHERE clause is
    /// <c>(@PrmEmpCode != '' AND Employee.Code = @PrmEmpCode) OR (@PrmEmpCode = '' AND @PrmPayrollArea != '' AND ...)</c>
    /// — the employee-code branch short-circuits the payroll-area branch. Kept on the DTO for screen
    /// parity/validation only.
    /// </summary>
    public int? PayrollAreaId { get; set; }

    /// <summary>Legacy "Report Type" combo.</summary>
    public PayslipReportLanguage Language { get; set; } = PayslipReportLanguage.English;
}

/// <summary>
/// Bulk recipient-resolution request — the port of the employee-list SQL inside
/// <c>uscRptPaySlip.dxButton1_Click</c> (the "else" branch when no employee code was typed).
/// </summary>
public class GetPayslipRecipientsInput
{
    [Range(1, 12)]
    public int Month { get; set; }

    [Range(1900, 2999)]
    public int Year { get; set; }

    public PayrollRunType PayrollRunType { get; set; } = PayrollRunType.Regular;

    /// <summary>
    /// Legacy hard-codes <c>A.[Status] = (PrmempStatus == "Active" ? 1 : 0)</c> — the bulk screen always
    /// applies a status filter (no "any" sentinel). <c>null</c> here means "any", which legacy could not
    /// express on this screen.
    /// </summary>
    public EmployeeStatus? EmployeeStatus { get; set; }

    /// <summary>
    /// Legacy <c>B.PayrollArea = '...'</c>. NOT SUPPORTED in this port — see
    /// <c>ReportsAppService.GuardPayrollAreaFilterNotSupported</c>.
    /// </summary>
    public int? PayrollAreaId { get; set; }
}

/// <summary>
/// One resolved bulk recipient. Mirrors the columns the legacy employee-list SQL selected
/// (<c>A.OID, A.Code, A.Email, PayrollArea Code, Status</c>) plus the net pay that will be printed, so the
/// operator can eyeball the run before mailing money documents.
/// </summary>
public class PayslipRecipientDto
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public string Email { get; set; }

    public EmployeeStatus Status { get; set; }

    /// <summary>Wage type 9000 amount on the matched <c>SalaryRegister</c>, rounded to 3 decimals.</summary>
    public double NetPay { get; set; }
}

/// <summary>
/// Bulk mail request — the port of <c>uscRptPaySlip.dxButton1_Click</c> + <c>Send_PaySlip</c>.
/// </summary>
public class SendPayslipEmailsInput : GetPayslipRecipientsInput
{
    /// <summary>
    /// Legacy: when the operator typed an employee code the screen mailed only that one employee and
    /// skipped the list query entirely. <c>null</c> = mail everyone the recipient query resolves.
    /// </summary>
    public int? EmployeeId { get; set; }

    public PayslipReportLanguage Language { get; set; } = PayslipReportLanguage.English;
}

/// <summary>
/// Result of a bulk mail run. Legacy reported only a success count
/// ("Mail with Pay slip Successfully to {n} Employee").
/// </summary>
public class SendPayslipEmailsOutput
{
    public int TotalCount { get; set; }

    public int SuccessCount { get; set; }

    public int FailureCount { get; set; }
}
