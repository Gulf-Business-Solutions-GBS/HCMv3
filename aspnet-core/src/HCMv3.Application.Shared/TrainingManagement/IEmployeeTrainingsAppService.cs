using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TrainingManagement.Dto;

namespace HCMv3.TrainingManagement;

public interface IEmployeeTrainingsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeTrainingDto>> GetAll(GetAllEmployeeTrainingsInput input);

    Task<GetEmployeeTrainingForEditOutput> GetEmployeeTrainingForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeTrainingDto input);

    Task Delete(EntityDto input);
}
