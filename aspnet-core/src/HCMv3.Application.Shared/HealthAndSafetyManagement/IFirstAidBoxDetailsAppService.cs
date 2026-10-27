using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.HealthAndSafetyManagement.Dto;

namespace HCMv3.HealthAndSafetyManagement;

public interface IFirstAidBoxDetailsAppService : IApplicationService
{
    Task<PagedResultDto<FirstAidBoxDetailDto>> GetAll(GetAllFirstAidBoxDetailsInput input);

    Task<GetFirstAidBoxDetailForEditOutput> GetFirstAidBoxDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditFirstAidBoxDetailDto input);

    Task Delete(EntityDto input);
}
