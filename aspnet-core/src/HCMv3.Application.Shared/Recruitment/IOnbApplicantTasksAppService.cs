using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Recruitment.Dto;

namespace HCMv3.Recruitment;

public interface IOnbApplicantTasksAppService : IApplicationService
{
    Task<PagedResultDto<OnbApplicantTasksDto>> GetAll(GetAllOnbApplicantTasksInput input);

    Task<GetOnbApplicantTasksForEditOutput> GetOnbApplicantTasksForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditOnbApplicantTasksDto input);

    Task Delete(EntityDto input);
}
