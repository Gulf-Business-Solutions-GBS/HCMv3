using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeFundExemptionsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeFundExemptionDto>> GetAll(GetAllEmployeeFundExemptionsInput input);

    Task<GetEmployeeFundExemptionForEditOutput> GetEmployeeFundExemptionForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeFundExemptionDto input);

    Task Delete(EntityDto input);
}
