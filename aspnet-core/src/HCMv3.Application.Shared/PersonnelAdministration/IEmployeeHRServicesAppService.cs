using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Back-office maintenance of HR service records. The self-service counterpart is
/// <c>IMyHRServiceAppService</c>; this one lets HR pick any employee, which is exactly why the two are
/// separate services rather than one with a filter.
/// </summary>
public interface IEmployeeHRServicesAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeHRServiceDto>> GetAll(GetAllEmployeeHRServicesInput input);

    Task<GetEmployeeHRServiceForEditOutput> GetEmployeeHRServiceForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeHRServiceDto input);

    Task Delete(EntityDto input);
}
