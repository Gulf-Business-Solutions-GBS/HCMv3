using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Recruitment.Dto;

namespace HCMv3.Recruitment;

public interface ICVSkillsAppService : IApplicationService
{
    Task<PagedResultDto<CVSkillDto>> GetAll(GetAllCVSkillsInput input);

    Task<GetCVSkillForEditOutput> GetCVSkillForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditCVSkillDto input);

    Task Delete(EntityDto input);
}
