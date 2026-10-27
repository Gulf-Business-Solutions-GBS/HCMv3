using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeWorkExperiencesAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeWorkExperienceDto>> GetAll(GetAllEmployeeWorkExperiencesInput input);

    Task<GetEmployeeWorkExperienceForEditOutput> GetEmployeeWorkExperienceForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeWorkExperienceDto input);

    Task Delete(EntityDto input);
}
