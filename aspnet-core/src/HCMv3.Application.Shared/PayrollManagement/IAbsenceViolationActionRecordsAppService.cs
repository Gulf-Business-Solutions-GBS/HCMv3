using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IAbsenceViolationActionRecordsAppService : IApplicationService
{
    Task<PagedResultDto<AbsenceViolationActionRecordDto>> GetAll(GetAllAbsenceViolationActionRecordsInput input);

    Task<GetAbsenceViolationActionRecordForEditOutput> GetAbsenceViolationActionRecordForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditAbsenceViolationActionRecordDto input);

    Task Delete(EntityDto input);
}
