using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SystemSecurity.Dto;

namespace HCMv3.SystemSecurity;

public interface IEmailTemplatesAppService : IApplicationService
{
    Task<PagedResultDto<EmailTemplateDto>> GetAll(GetAllEmailTemplatesInput input);

    Task<GetEmailTemplateForEditOutput> GetEmailTemplateForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmailTemplateDto input);

    Task Delete(EntityDto input);
}
