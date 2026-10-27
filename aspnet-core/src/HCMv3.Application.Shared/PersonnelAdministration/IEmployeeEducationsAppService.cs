using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeEducationsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeEducationDto>> GetAll(GetAllEmployeeEducationsInput input);

    Task<GetEmployeeEducationForEditOutput> GetEmployeeEducationForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeEducationDto input);

    Task Delete(EntityDto input);
}
