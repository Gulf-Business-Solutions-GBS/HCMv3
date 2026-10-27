using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeAbsQuotaForfeitsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeAbsQuotaForfeitDto>> GetAll(GetAllEmployeeAbsQuotaForfeitsInput input);

    Task<GetEmployeeAbsQuotaForfeitForEditOutput> GetEmployeeAbsQuotaForfeitForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeAbsQuotaForfeitDto input);

    Task Delete(EntityDto input);
}
