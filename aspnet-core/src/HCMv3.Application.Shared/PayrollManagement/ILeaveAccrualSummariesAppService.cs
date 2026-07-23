using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface ILeaveAccrualSummariesAppService : IApplicationService
{
    Task<PagedResultDto<LeaveAccrualSummaryDto>> GetAll(GetAllLeaveAccrualSummariesInput input);

    Task<GetLeaveAccrualSummaryForEditOutput> GetLeaveAccrualSummaryForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditLeaveAccrualSummaryDto input);

    Task Delete(EntityDto input);
}
