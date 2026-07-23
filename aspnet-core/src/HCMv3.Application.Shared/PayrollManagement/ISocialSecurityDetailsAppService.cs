using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface ISocialSecurityDetailsAppService : IApplicationService
{
    Task<PagedResultDto<SocialSecurityDetailDto>> GetAll(GetAllSocialSecurityDetailsInput input);

    Task<GetSocialSecurityDetailForEditOutput> GetSocialSecurityDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditSocialSecurityDetailDto input);

    Task Delete(EntityDto input);
}
