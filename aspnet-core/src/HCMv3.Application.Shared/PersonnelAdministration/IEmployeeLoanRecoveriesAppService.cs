using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeLoanRecoveriesAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeLoanRecoveryDto>> GetAll(GetAllEmployeeLoanRecoveriesInput input);

    Task<GetEmployeeLoanRecoveryForEditOutput> GetEmployeeLoanRecoveryForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeLoanRecoveryDto input);

    Task Delete(EntityDto input);
}
