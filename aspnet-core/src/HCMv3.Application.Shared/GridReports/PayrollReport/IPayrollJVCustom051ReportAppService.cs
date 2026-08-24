using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.Dto;
using HCMv3.GridReports.PayrollReport.Dto;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Payroll JV (Custom 051) grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollJV_Custom051.ascx</c>).
///
/// The read returns <see cref="PayrollJVCustom051ReportOutput"/> rather than a bare
/// <c>PagedResultDto</c> so the grid and the export are driven by one server-side column list.
/// </summary>
public interface IPayrollJVCustom051ReportAppService : IApplicationService
{
    Task<PayrollJVCustom051ReportOutput> GetPayrollJVCustom051Report(GetPayrollJVCustom051ReportInput input);

    Task<FileDto> GetPayrollJVCustom051ReportToExcel(GetPayrollJVCustom051ReportInput input);
}
