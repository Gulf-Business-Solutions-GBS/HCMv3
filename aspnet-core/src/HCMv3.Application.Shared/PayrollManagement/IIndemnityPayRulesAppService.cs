using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IIndemnityPayRulesAppService : IApplicationService
{
    Task<PagedResultDto<IndemnityPayRuleDto>> GetAll(GetAllIndemnityPayRulesInput input);

    Task<GetIndemnityPayRuleForEditOutput> GetIndemnityPayRuleForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditIndemnityPayRuleDto input);

    Task Delete(EntityDto input);
}
