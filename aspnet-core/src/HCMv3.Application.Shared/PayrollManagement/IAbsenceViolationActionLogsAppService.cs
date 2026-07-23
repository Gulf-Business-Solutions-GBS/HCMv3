using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IAbsenceViolationActionLogsAppService : IApplicationService
{
    Task<PagedResultDto<AbsenceViolationActionLogDto>> GetAll(GetAllAbsenceViolationActionLogsInput input);

    Task<GetAbsenceViolationActionLogForEditOutput> GetAbsenceViolationActionLogForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditAbsenceViolationActionLogDto input);

    Task Delete(EntityDto input);
}
