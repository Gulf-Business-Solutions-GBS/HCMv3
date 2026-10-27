using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.Dto;
using HCMv3.GridReports.PayrollReport.Dto;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Payroll JV (Custom 030) grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollJV_Custom030.ascx</c>).
///
/// The read returns <see cref="PayrollJVCustom030ReportOutput"/> rather than a bare
/// <c>PagedResultDto</c> because the column set depends on the selected
/// <see cref="Dto.PayrollJVCustom030ReportType"/>.
/// </summary>
public interface IPayrollJVCustom030ReportAppService : IApplicationService
{
    Task<PayrollJVCustom030ReportOutput> GetPayrollJVCustom030Report(GetPayrollJVCustom030ReportInput input);

    Task<FileDto> GetPayrollJVCustom030ReportToExcel(GetPayrollJVCustom030ReportInput input);
}
