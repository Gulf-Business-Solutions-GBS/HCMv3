using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeDelegationsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeDelegationDto>> GetAll(GetAllEmployeeDelegationsInput input);

    Task<GetEmployeeDelegationForEditOutput> GetEmployeeDelegationForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeDelegationDto input);

    Task Delete(EntityDto input);
}
