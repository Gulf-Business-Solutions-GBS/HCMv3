using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SystemSecurity.Dto;

namespace HCMv3.SystemSecurity;

public interface IEmailTokensAppService : IApplicationService
{
    Task<PagedResultDto<EmailTokenDto>> GetAll(GetAllEmailTokensInput input);

    Task<GetEmailTokenForEditOutput> GetEmailTokenForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmailTokenDto input);

    Task Delete(EntityDto input);
}
