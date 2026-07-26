using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SystemSecurity.Dto;

namespace HCMv3.SystemSecurity;

public interface IEmailNotificationLogsAppService : IApplicationService
{
    Task<PagedResultDto<EmailNotificationLogDto>> GetAll(GetAllEmailNotificationLogsInput input);

    Task<GetEmailNotificationLogForEditOutput> GetEmailNotificationLogForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmailNotificationLogDto input);

    Task Delete(EntityDto input);
}
