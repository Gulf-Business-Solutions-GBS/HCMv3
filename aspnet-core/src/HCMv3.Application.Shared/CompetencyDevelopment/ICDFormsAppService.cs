using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.CompetencyDevelopment.Dto;

namespace HCMv3.CompetencyDevelopment;

public interface ICDFormsAppService : IApplicationService
{
    Task<PagedResultDto<CDFormDto>> GetAll(GetAllCDFormsInput input);

    Task<GetCDFormForEditOutput> GetCDFormForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditCDFormDto input);

    Task Delete(EntityDto input);
}
