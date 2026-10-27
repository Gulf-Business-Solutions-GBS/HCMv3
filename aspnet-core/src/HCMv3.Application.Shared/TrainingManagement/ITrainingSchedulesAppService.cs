using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TrainingManagement.Dto;

namespace HCMv3.TrainingManagement;

public interface ITrainingSchedulesAppService : IApplicationService
{
    Task<PagedResultDto<TrainingScheduleDto>> GetAll(GetAllTrainingSchedulesInput input);

    Task<GetTrainingScheduleForEditOutput> GetTrainingScheduleForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditTrainingScheduleDto input);

    Task Delete(EntityDto input);
}
