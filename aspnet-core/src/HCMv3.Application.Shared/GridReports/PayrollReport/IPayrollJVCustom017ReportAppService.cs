using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.Dto;
using HCMv3.GridReports.PayrollReport.Dto;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Payroll JV (Custom 017) analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollJV_Custom017.ascx(.cs)</c>.
/// </summary>
public interface IPayrollJVCustom017ReportAppService : IApplicationService
{
    Task<PayrollJVCustom017ReportOutput> GetPayrollJVCustom017Report(GetPayrollJVCustom017ReportInput input);

    Task<FileDto> GetPayrollJVCustom017ReportToExcel(GetPayrollJVCustom017ReportInput input);
}
