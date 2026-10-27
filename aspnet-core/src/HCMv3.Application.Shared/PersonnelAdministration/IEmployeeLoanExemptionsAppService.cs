using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeLoanExemptionsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeLoanExemptionDto>> GetAll(GetAllEmployeeLoanExemptionsInput input);

    Task<GetEmployeeLoanExemptionForEditOutput> GetEmployeeLoanExemptionForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeLoanExemptionDto input);

    Task Delete(EntityDto input);
}
