using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IAbsenceViolationRuleExceptionsAppService : IApplicationService
{
    Task<PagedResultDto<AbsenceViolationRuleExceptionDto>> GetAll(GetAllAbsenceViolationRuleExceptionsInput input);

    Task<GetAbsenceViolationRuleExceptionForEditOutput> GetAbsenceViolationRuleExceptionForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditAbsenceViolationRuleExceptionDto input);

    Task Delete(EntityDto input);
}
