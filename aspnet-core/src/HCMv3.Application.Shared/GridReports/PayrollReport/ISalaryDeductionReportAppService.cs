using System.Threading.Tasks;
using HCMv3.Dto;
using HCMv3.GridReports.PayrollReport.Dto;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Salary Deduction grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptSalaryDeduction.ascx</c>). Not a plain
/// <c>IApplicationService</c> paged-result method because the row shape has DATA-DRIVEN columns -- see
/// <see cref="SalaryDeductionReportOutput"/>.
/// </summary>
public interface ISalaryDeductionReportAppService : Abp.Application.Services.IApplicationService
{
    Task<SalaryDeductionReportOutput> GetSalaryDeductionReport(GetSalaryDeductionReportInput input);

    Task<FileDto> GetSalaryDeductionReportToExcel(GetSalaryDeductionReportInput input);
}
