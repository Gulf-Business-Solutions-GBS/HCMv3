using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SystemSecurity.Dto;

namespace HCMv3.SystemSecurity;

public interface IEmployeesNotificationLogsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeesNotificationLogDto>> GetAll(GetAllEmployeesNotificationLogsInput input);

    Task<GetEmployeesNotificationLogForEditOutput> GetEmployeesNotificationLogForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeesNotificationLogDto input);

    Task Delete(EntityDto input);
}
