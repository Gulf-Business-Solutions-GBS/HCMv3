using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface ISocialSecurityRulesAppService : IApplicationService
{
    Task<PagedResultDto<SocialSecurityRuleDto>> GetAll(GetAllSocialSecurityRulesInput input);

    Task<GetSocialSecurityRuleForEditOutput> GetSocialSecurityRuleForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditSocialSecurityRuleDto input);

    Task Delete(EntityDto input);
}
