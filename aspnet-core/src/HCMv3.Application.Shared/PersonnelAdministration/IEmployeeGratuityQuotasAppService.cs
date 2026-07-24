using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeGratuityQuotasAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeGratuityQuotaDto>> GetAll(GetAllEmployeeGratuityQuotasInput input);

    Task<GetEmployeeGratuityQuotaForEditOutput> GetEmployeeGratuityQuotaForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeGratuityQuotaDto input);

    Task Delete(EntityDto input);
}
