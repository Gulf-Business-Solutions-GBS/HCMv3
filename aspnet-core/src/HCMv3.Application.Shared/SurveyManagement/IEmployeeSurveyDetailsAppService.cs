using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SurveyManagement.Dto;

namespace HCMv3.SurveyManagement;

public interface IEmployeeSurveyDetailsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeSurveyDetailDto>> GetAll(GetAllEmployeeSurveyDetailsInput input);

    Task<GetEmployeeSurveyDetailForEditOutput> GetEmployeeSurveyDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeSurveyDetailDto input);

    Task Delete(EntityDto input);
}
