using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.Dto;
using HCMv3.GridReports.PayrollReport.Dto;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Payroll JV (Custom 035) grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollJV_Custom035.ascx</c>).
///
/// The read returns <see cref="PayrollJVCustom035ReportOutput"/> rather than a bare
/// <c>PagedResultDto</c> because the column set depends on the selected
/// <see cref="Dto.PayrollJVCustom035ReportType"/>.
/// </summary>
public interface IPayrollJVCustom035ReportAppService : IApplicationService
{
    Task<PayrollJVCustom035ReportOutput> GetPayrollJVCustom035Report(GetPayrollJVCustom035ReportInput input);

    Task<FileDto> GetPayrollJVCustom035ReportToExcel(GetPayrollJVCustom035ReportInput input);
}
