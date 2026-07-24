using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeAttendanceQuotasAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeAttendanceQuotaDto>> GetAll(GetAllEmployeeAttendanceQuotasInput input);

    Task<GetEmployeeAttendanceQuotaForEditOutput> GetEmployeeAttendanceQuotaForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeAttendanceQuotaDto input);

    Task Delete(EntityDto input);
}
