using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SystemSecurity.Dto;

namespace HCMv3.SystemSecurity;

public interface IEmployeesNotificationsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeesNotificationDto>> GetAll(GetAllEmployeesNotificationsInput input);

    Task<GetEmployeesNotificationForEditOutput> GetEmployeesNotificationForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeesNotificationDto input);

    Task Delete(EntityDto input);
}
