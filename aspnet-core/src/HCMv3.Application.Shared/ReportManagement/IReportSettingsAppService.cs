using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.ReportManagement.Dto;

namespace HCMv3.ReportManagement;

public interface IReportSettingsAppService : IApplicationService
{
    Task<PagedResultDto<ReportSettingDto>> GetAll(GetAllReportSettingsInput input);

    Task<GetReportSettingForEditOutput> GetReportSettingForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditReportSettingDto input);

    Task Delete(EntityDto input);
}
