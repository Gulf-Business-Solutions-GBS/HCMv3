using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GovtRelationsManagement.Dto;

namespace HCMv3.GovtRelationsManagement;

/// <summary>
/// HR-side maintenance of exit permits. The self-service counterpart is
/// <c>IMyExitPermitAppService</c>: same entity, same workflow handler, different surface -- this one may
/// record a permit for any employee, that one only for the signed-in employee.
/// </summary>
public interface IEmployeeExitPermitsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeExitPermitDto>> GetAll(GetAllEmployeeExitPermitsInput input);

    Task<GetEmployeeExitPermitForEditOutput> GetEmployeeExitPermitForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeExitPermitDto input);

    Task Delete(EntityDto input);
}
