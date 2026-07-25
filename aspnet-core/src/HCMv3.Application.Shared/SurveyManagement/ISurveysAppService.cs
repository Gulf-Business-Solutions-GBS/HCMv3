using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SurveyManagement.Dto;

namespace HCMv3.SurveyManagement;

public interface ISurveysAppService : IApplicationService
{
    Task<PagedResultDto<SurveyDto>> GetAll(GetAllSurveysInput input);

    Task<GetSurveyForEditOutput> GetSurveyForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditSurveyDto input);

    Task Delete(EntityDto input);
}
