using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Reports.Dto;

namespace HCMv3.Reports;

/// <summary>
/// FastReport.OpenSource pilot: renders two sample reports (Salary Certificate, Payslip) as PDFs
/// for a given employee, pulling real data from PersonnelAdministration/PayrollManagement.
/// </summary>
public interface IReportsAppService : IApplicationService
{
    Task<ReportPdfOutput> GetSalaryCertificatePdf(EntityDto input);

    Task<ReportPdfOutput> GetPayslipPdf(EntityDto input);
}
