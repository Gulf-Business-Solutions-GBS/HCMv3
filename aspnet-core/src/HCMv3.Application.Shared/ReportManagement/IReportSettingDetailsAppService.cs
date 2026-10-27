using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.ReportManagement.Dto;

namespace HCMv3.ReportManagement;

public interface IReportSettingDetailsAppService : IApplicationService
{
    Task<PagedResultDto<ReportSettingDetailDto>> GetAll(GetAllReportSettingDetailsInput input);

    Task<GetReportSettingDetailForEditOutput> GetReportSettingDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditReportSettingDetailDto input);

    Task Delete(EntityDto input);
}
