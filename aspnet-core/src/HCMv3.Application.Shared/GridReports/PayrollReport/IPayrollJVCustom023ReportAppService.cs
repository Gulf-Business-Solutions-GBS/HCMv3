using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.Dto;
using HCMv3.GridReports.PayrollReport.Dto;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Payroll JV (Custom 023) analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollJV_Custom023.ascx(.cs)</c>.
/// </summary>
public interface IPayrollJVCustom023ReportAppService : IApplicationService
{
    Task<PayrollJVCustom023ReportOutput> GetPayrollJVCustom023Report(GetPayrollJVCustom023ReportInput input);

    Task<FileDto> GetPayrollJVCustom023ReportToExcel(GetPayrollJVCustom023ReportInput input);
}
