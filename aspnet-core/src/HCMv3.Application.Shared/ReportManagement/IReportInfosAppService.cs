using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.ReportManagement.Dto;

namespace HCMv3.ReportManagement;

public interface IReportInfosAppService : IApplicationService
{
    Task<PagedResultDto<ReportInfoDto>> GetAll(GetAllReportInfosInput input);

    Task<GetReportInfoForEditOutput> GetReportInfoForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditReportInfoDto input);

    Task Delete(EntityDto input);
}
