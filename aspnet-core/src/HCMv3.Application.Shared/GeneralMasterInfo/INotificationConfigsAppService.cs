using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo.Dto;

namespace HCMv3.GeneralMasterInfo;

public interface INotificationConfigsAppService : IApplicationService
{
    Task<PagedResultDto<NotificationConfigDto>> GetAll(GetAllNotificationConfigsInput input);

    Task<GetNotificationConfigForEditOutput> GetNotificationConfigForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditNotificationConfigDto input);

    Task Delete(EntityDto input);
}
