using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface ISocialSecurityEducationAllowancesAppService : IApplicationService
{
    Task<PagedResultDto<SocialSecurityEducationAllowanceDto>> GetAll(GetAllSocialSecurityEducationAllowancesInput input);

    Task<GetSocialSecurityEducationAllowanceForEditOutput> GetSocialSecurityEducationAllowanceForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditSocialSecurityEducationAllowanceDto input);

    Task Delete(EntityDto input);
}
