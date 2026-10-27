using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeSocialSecurityClaimsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeSocialSecurityClaimDto>> GetAll(GetAllEmployeeSocialSecurityClaimsInput input);

    Task<GetEmployeeSocialSecurityClaimForEditOutput> GetEmployeeSocialSecurityClaimForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeSocialSecurityClaimDto input);

    Task Delete(EntityDto input);
}
