using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IAbsenceDedRuleDetailsAppService : IApplicationService
{
    Task<PagedResultDto<AbsenceDedRuleDetailDto>> GetAll(GetAllAbsenceDedRuleDetailsInput input);

    Task<GetAbsenceDedRuleDetailForEditOutput> GetAbsenceDedRuleDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditAbsenceDedRuleDetailDto input);

    Task Delete(EntityDto input);
}
