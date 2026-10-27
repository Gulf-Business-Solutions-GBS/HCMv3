using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeExtdWorkLocationsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeExtdWorkLocationDto>> GetAll(GetAllEmployeeExtdWorkLocationsInput input);

    Task<GetEmployeeExtdWorkLocationForEditOutput> GetEmployeeExtdWorkLocationForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeExtdWorkLocationDto input);

    Task Delete(EntityDto input);
}
