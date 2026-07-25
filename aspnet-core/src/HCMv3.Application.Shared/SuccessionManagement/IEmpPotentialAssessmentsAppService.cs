using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SuccessionManagement.Dto;

namespace HCMv3.SuccessionManagement;

public interface IEmpPotentialAssessmentsAppService : IApplicationService
{
    Task<PagedResultDto<EmpPotentialAssessmentDto>> GetAll(GetAllEmpPotentialAssessmentsInput input);

    Task<GetEmpPotentialAssessmentForEditOutput> GetEmpPotentialAssessmentForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmpPotentialAssessmentDto input);

    Task Delete(EntityDto input);
}
