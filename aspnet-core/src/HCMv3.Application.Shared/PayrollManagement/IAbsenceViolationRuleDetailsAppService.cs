using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IAbsenceViolationRuleDetailsAppService : IApplicationService
{
    Task<PagedResultDto<AbsenceViolationRuleDetailDto>> GetAll(GetAllAbsenceViolationRuleDetailsInput input);

    Task<GetAbsenceViolationRuleDetailForEditOutput> GetAbsenceViolationRuleDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditAbsenceViolationRuleDetailDto input);

    Task Delete(EntityDto input);
}
