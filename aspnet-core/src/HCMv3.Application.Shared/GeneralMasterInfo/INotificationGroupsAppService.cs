using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo.Dto;

namespace HCMv3.GeneralMasterInfo;

public interface INotificationGroupsAppService : IApplicationService
{
    Task<PagedResultDto<NotificationGroupDto>> GetAll(GetAllNotificationGroupsInput input);

    Task<GetNotificationGroupForEditOutput> GetNotificationGroupForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditNotificationGroupDto input);

    Task Delete(EntityDto input);
}
