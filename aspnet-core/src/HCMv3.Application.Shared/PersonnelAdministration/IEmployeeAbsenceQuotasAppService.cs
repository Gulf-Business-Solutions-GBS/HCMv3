using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeAbsenceQuotasAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeAbsenceQuotaDto>> GetAll(GetAllEmployeeAbsenceQuotasInput input);

    Task<GetEmployeeAbsenceQuotaForEditOutput> GetEmployeeAbsenceQuotaForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeAbsenceQuotaDto input);

    Task Delete(EntityDto input);
}
