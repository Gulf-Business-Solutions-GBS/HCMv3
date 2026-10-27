using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Recruitment.Dto;

namespace HCMv3.Recruitment;

public interface ICVLanguagesAppService : IApplicationService
{
    Task<PagedResultDto<CVLanguageDto>> GetAll(GetAllCVLanguagesInput input);

    Task<GetCVLanguageForEditOutput> GetCVLanguageForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditCVLanguageDto input);

    Task Delete(EntityDto input);
}
