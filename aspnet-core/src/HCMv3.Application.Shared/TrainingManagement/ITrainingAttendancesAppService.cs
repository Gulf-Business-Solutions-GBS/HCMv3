using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TrainingManagement.Dto;

namespace HCMv3.TrainingManagement;

public interface ITrainingAttendancesAppService : IApplicationService
{
    Task<PagedResultDto<TrainingAttendanceDto>> GetAll(GetAllTrainingAttendancesInput input);

    Task<GetTrainingAttendanceForEditOutput> GetTrainingAttendanceForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditTrainingAttendanceDto input);

    Task Delete(EntityDto input);
}
