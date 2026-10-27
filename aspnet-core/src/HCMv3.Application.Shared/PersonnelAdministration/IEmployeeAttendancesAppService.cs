using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeAttendancesAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeAttendanceDto>> GetAll(GetAllEmployeeAttendancesInput input);

    Task<GetEmployeeAttendanceForEditOutput> GetEmployeeAttendanceForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeAttendanceDto input);

    Task Delete(EntityDto input);
}
