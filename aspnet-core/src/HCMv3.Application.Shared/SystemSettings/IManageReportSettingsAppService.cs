using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SystemSettings.Dto;

namespace HCMv3.SystemSettings;

public interface IManageReportSettingsAppService : IApplicationService
{
    Task<PagedResultDto<ReportSettingsListDto>> GetReportSettingsList(PagedAndSortedResultRequestDto input);

    Task<ReportSettingsEditDto> GetReportSettingsForEdit(EntityDto input);

    Task CreateOrEditReportSettings(ReportSettingsEditDto input);

    Task DeleteReportSettings(EntityDto input);
}
