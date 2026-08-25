using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.QuotaProvision.Dto;

namespace HCMv3.GridReports.QuotaProvision;

public interface IYearlyLeaveIndemnityDetailCustom016ReportAppService : IApplicationService
{
    Task<PagedResultDto<YearlyLeaveIndemnityDetailReportRowDto>> GetYearlyLeaveIndemnityDetailCustom016Report(GetYearlyLeaveIndemnityDetailReportInput input);

    Task<FileDto> GetYearlyLeaveIndemnityDetailCustom016ReportToExcel(GetYearlyLeaveIndemnityDetailReportInput input);
}
