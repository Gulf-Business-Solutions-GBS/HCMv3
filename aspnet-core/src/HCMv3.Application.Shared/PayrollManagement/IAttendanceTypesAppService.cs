using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IAttendanceTypesAppService : IApplicationService
{
    Task<PagedResultDto<AttendanceTypeDto>> GetAll(GetAllAttendanceTypesInput input);

    Task<GetAttendanceTypeForEditOutput> GetAttendanceTypeForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditAttendanceTypeDto input);

    Task Delete(EntityDto input);
}
