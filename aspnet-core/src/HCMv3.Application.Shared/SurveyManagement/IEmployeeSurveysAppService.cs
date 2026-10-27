using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SurveyManagement.Dto;

namespace HCMv3.SurveyManagement;

public interface IEmployeeSurveysAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeSurveyDto>> GetAll(GetAllEmployeeSurveysInput input);

    Task<GetEmployeeSurveyForEditOutput> GetEmployeeSurveyForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeSurveyDto input);

    Task Delete(EntityDto input);
}
