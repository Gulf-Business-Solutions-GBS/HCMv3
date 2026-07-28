using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Reports.Dto;

namespace HCMv3.Reports;

/// <summary>
/// FastReport.OpenSource report surface. Renders the Salary Certificate (pilot) and the ported
/// Payslip (legacy <c>uscPreviewPaySlip</c> / <c>uscRptPaySlip</c> + DevExpress
/// <c>rptPayslipEn</c>/<c>rptPayslipAr</c>) as PDFs.
/// </summary>
public interface IReportsAppService : IApplicationService
{
    Task<ReportPdfOutput> GetSalaryCertificatePdf(EntityDto input);

    /// <summary>
    /// PREVIEW path (legacy <c>uscPreviewPaySlip</c>): one employee, one period, rendered on screen.
    /// </summary>
    Task<ReportPdfOutput> GetPayslipPdf(PayslipInput input);

    /// <summary>
    /// REPORT path, step 1 (legacy <c>uscRptPaySlip</c> employee-list SQL): resolve who would be mailed.
    /// </summary>
    Task<List<PayslipRecipientDto>> GetPayslipRecipients(GetPayslipRecipientsInput input);

    /// <summary>
    /// REPORT path, step 2 (legacy <c>uscRptPaySlip.Send_PaySlip</c>): render + e-mail each payslip.
    /// </summary>
    Task<SendPayslipEmailsOutput> SendPayslipEmails(SendPayslipEmailsInput input);
}
