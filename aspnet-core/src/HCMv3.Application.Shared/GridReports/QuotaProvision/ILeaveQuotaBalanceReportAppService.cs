using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.QuotaProvision.Dto;

namespace HCMv3.GridReports.QuotaProvision;

public interface ILeaveQuotaBalanceReportAppService : IApplicationService
{
    Task<PagedResultDto<LeaveQuotaBalanceReportRowDto>> GetLeaveQuotaBalanceReport(GetLeaveQuotaBalanceReportInput input);

    Task<FileDto> GetLeaveQuotaBalanceReportToExcel(GetLeaveQuotaBalanceReportInput input);
}
