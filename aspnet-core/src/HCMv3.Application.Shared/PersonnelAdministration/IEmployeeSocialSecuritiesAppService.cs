using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeSocialSecuritiesAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeSocialSecurityDto>> GetAll(GetAllEmployeeSocialSecuritiesInput input);

    Task<GetEmployeeSocialSecurityForEditOutput> GetEmployeeSocialSecurityForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeSocialSecurityDto input);

    Task Delete(EntityDto input);
}
