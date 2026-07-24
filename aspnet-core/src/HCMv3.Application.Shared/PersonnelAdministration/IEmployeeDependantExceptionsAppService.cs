using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeDependantExceptionsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeDependantExceptionDto>> GetAll(GetAllEmployeeDependantExceptionsInput input);

    Task<GetEmployeeDependantExceptionForEditOutput> GetEmployeeDependantExceptionForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeDependantExceptionDto input);

    Task Delete(EntityDto input);
}
