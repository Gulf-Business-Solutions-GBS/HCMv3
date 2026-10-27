using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SystemSecurity.Dto;

namespace HCMv3.SystemSecurity;

public interface INotificationMastersAppService : IApplicationService
{
    Task<PagedResultDto<NotificationMasterDto>> GetAll(GetAllNotificationMastersInput input);

    Task<GetNotificationMasterForEditOutput> GetNotificationMasterForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditNotificationMasterDto input);

    Task Delete(EntityDto input);
}
