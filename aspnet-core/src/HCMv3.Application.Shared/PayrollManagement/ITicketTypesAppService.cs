using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface ITicketTypesAppService : IApplicationService
{
    Task<PagedResultDto<TicketTypeDto>> GetAll(GetAllTicketTypesInput input);

    Task<GetTicketTypeForEditOutput> GetTicketTypeForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditTicketTypeDto input);

    Task Delete(EntityDto input);
}
