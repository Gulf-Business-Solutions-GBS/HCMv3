using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IIndemnityPayRuleDetailsAppService : IApplicationService
{
    Task<PagedResultDto<IndemnityPayRuleDetailDto>> GetAll(GetAllIndemnityPayRuleDetailsInput input);

    Task<GetIndemnityPayRuleDetailForEditOutput> GetIndemnityPayRuleDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditIndemnityPayRuleDetailDto input);

    Task Delete(EntityDto input);
}
