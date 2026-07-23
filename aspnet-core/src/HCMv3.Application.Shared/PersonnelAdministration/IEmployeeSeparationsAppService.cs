using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeSeparationsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeSeparationDto>> GetAll(GetAllEmployeeSeparationsInput input);

    Task<GetEmployeeSeparationForEditOutput> GetEmployeeSeparationForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeSeparationDto input);

    Task Delete(EntityDto input);
}
