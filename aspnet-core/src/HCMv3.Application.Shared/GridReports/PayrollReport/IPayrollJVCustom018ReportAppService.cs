using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.Dto;
using HCMv3.GridReports.PayrollReport.Dto;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Payroll JV (Custom 018) analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollJV_Custom018.ascx(.cs)</c>.
/// </summary>
public interface IPayrollJVCustom018ReportAppService : IApplicationService
{
    Task<PayrollJVCustom018ReportOutput> GetPayrollJVCustom018Report(GetPayrollJVCustom018ReportInput input);

    Task<FileDto> GetPayrollJVCustom018ReportToExcel(GetPayrollJVCustom018ReportInput input);
}
