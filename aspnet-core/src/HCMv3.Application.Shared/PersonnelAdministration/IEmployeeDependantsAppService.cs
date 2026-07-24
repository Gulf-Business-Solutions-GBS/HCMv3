using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeDependantsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeDependantDto>> GetAll(GetAllEmployeeDependantsInput input);

    Task<GetEmployeeDependantForEditOutput> GetEmployeeDependantForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeDependantDto input);

    Task Delete(EntityDto input);
}
