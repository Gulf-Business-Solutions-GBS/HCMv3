using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IAbsenceDeductionRulesAppService : IApplicationService
{
    Task<PagedResultDto<AbsenceDeductionRuleDto>> GetAll(GetAllAbsenceDeductionRulesInput input);

    Task<GetAbsenceDeductionRuleForEditOutput> GetAbsenceDeductionRuleForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditAbsenceDeductionRuleDto input);

    Task Delete(EntityDto input);
}
