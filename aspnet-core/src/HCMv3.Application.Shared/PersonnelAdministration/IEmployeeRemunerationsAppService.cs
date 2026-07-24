using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeRemunerationsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeRemunerationDto>> GetAll(GetAllEmployeeRemunerationsInput input);

    Task<GetEmployeeRemunerationForEditOutput> GetEmployeeRemunerationForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeRemunerationDto input);

    Task Delete(EntityDto input);
}
