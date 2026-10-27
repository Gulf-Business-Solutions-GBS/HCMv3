using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface ISocialSecurityMaritalAllowancesAppService : IApplicationService
{
    Task<PagedResultDto<SocialSecurityMaritalAllowanceDto>> GetAll(GetAllSocialSecurityMaritalAllowancesInput input);

    Task<GetSocialSecurityMaritalAllowanceForEditOutput> GetSocialSecurityMaritalAllowanceForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditSocialSecurityMaritalAllowanceDto input);

    Task Delete(EntityDto input);
}
