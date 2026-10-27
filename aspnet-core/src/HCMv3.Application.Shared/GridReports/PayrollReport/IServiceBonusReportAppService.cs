using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.PayrollReport.Dto;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Service Bonus grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptServiceBonus.ascx</c>).
/// </summary>
public interface IServiceBonusReportAppService : IApplicationService
{
    Task<PagedResultDto<ServiceBonusReportRowDto>> GetServiceBonusReport(GetServiceBonusReportInput input);

    Task<FileDto> GetServiceBonusReportToExcel(GetServiceBonusReportInput input);
}
