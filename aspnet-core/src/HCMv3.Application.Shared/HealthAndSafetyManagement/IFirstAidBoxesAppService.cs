using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.HealthAndSafetyManagement.Dto;

namespace HCMv3.HealthAndSafetyManagement;

public interface IFirstAidBoxesAppService : IApplicationService
{
    Task<PagedResultDto<FirstAidBoxDto>> GetAll(GetAllFirstAidBoxesInput input);

    Task<GetFirstAidBoxForEditOutput> GetFirstAidBoxForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditFirstAidBoxDto input);

    Task Delete(EntityDto input);
}
