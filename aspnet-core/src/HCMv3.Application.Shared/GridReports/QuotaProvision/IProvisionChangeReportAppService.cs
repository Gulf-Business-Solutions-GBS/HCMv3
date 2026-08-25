using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.QuotaProvision.Dto;

namespace HCMv3.GridReports.QuotaProvision;

public interface IProvisionChangeReportAppService : IApplicationService
{
    Task<PagedResultDto<ProvisionChangeReportRowDto>> GetProvisionChangeReport(GetProvisionChangeReportInput input);

    Task<FileDto> GetProvisionChangeReportToExcel(GetProvisionChangeReportInput input);
}
