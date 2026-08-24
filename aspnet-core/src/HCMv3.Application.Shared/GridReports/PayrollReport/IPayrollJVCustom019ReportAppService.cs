using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.Dto;
using HCMv3.GridReports.PayrollReport.Dto;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Payroll JV (Custom 019) analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollJV_Custom019.ascx(.cs)</c>.
/// </summary>
public interface IPayrollJVCustom019ReportAppService : IApplicationService
{
    Task<PayrollJVCustom019ReportOutput> GetPayrollJVCustom019Report(GetPayrollJVCustom019ReportInput input);

    Task<FileDto> GetPayrollJVCustom019ReportToExcel(GetPayrollJVCustom019ReportInput input);
}
