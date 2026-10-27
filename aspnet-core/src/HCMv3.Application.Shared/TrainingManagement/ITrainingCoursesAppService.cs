using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TrainingManagement.Dto;

namespace HCMv3.TrainingManagement;

public interface ITrainingCoursesAppService : IApplicationService
{
    Task<PagedResultDto<TrainingCourseDto>> GetAll(GetAllTrainingCoursesInput input);

    Task<GetTrainingCourseForEditOutput> GetTrainingCourseForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditTrainingCourseDto input);

    Task Delete(EntityDto input);
}
