using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SurveyManagement.Dto;

namespace HCMv3.SurveyManagement;

public interface ISurveyDetailsAppService : IApplicationService
{
    Task<PagedResultDto<SurveyDetailDto>> GetAll(GetAllSurveyDetailsInput input);

    Task<GetSurveyDetailForEditOutput> GetSurveyDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditSurveyDetailDto input);

    Task Delete(EntityDto input);
}
