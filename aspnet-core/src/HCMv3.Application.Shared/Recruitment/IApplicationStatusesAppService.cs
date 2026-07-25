using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Recruitment.Dto;

namespace HCMv3.Recruitment;

public interface IApplicationStatusesAppService : IApplicationService
{
    Task<PagedResultDto<ApplicationStatusDto>> GetAll(GetAllApplicationStatusesInput input);

    Task<GetApplicationStatusForEditOutput> GetApplicationStatusForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditApplicationStatusDto input);

    Task Delete(EntityDto input);
}
