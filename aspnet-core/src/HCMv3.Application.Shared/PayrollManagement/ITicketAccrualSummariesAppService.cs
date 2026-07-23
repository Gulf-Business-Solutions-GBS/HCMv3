using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface ITicketAccrualSummariesAppService : IApplicationService
{
    Task<PagedResultDto<TicketAccrualSummaryDto>> GetAll(GetAllTicketAccrualSummariesInput input);

    Task<GetTicketAccrualSummaryForEditOutput> GetTicketAccrualSummaryForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditTicketAccrualSummaryDto input);

    Task Delete(EntityDto input);
}
