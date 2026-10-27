using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface ISocialSecuritiesAppService : IApplicationService
{
    Task<PagedResultDto<SocialSecurityDto>> GetAll(GetAllSocialSecuritiesInput input);

    Task<GetSocialSecurityForEditOutput> GetSocialSecurityForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditSocialSecurityDto input);

    Task Delete(EntityDto input);
}
