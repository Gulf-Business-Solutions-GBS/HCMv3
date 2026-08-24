using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.Dto;
using HCMv3.GridReports.PayrollReport.Dto;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Payroll Sheet (Custom 023) grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollSheet_Custom023.ascx</c>).
///
/// The read returns <see cref="PayrollSheetCustom023ReportOutput"/> rather than a bare
/// <c>PagedResultDto</c> so the grid and the export are driven by one server-side column list.
/// </summary>
public interface IPayrollSheetCustom023ReportAppService : IApplicationService
{
    Task<PayrollSheetCustom023ReportOutput> GetPayrollSheetCustom023Report(
        GetPayrollSheetCustom023ReportInput input);

    Task<FileDto> GetPayrollSheetCustom023ReportToExcel(GetPayrollSheetCustom023ReportInput input);
}
