using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// The ONLY way an IT service request is raised. There is no self-service counterpart: legacy has no ESS
/// surface for this type, so creation here routes it for approval itself through
/// <c>IAdminTransactionSubmitter</c> rather than deferring to an ESS service that does not exist.
/// </summary>
public interface IEmployeeITServicesAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeITServiceDto>> GetAll(GetAllEmployeeITServicesInput input);

    Task<GetEmployeeITServiceForEditOutput> GetEmployeeITServiceForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeITServiceDto input);

    Task Delete(EntityDto input);
}
