using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface ITicketPaxTypesAppService : IApplicationService
{
    Task<PagedResultDto<TicketPaxTypeDto>> GetAll(GetAllTicketPaxTypesInput input);

    Task<GetTicketPaxTypeForEditOutput> GetTicketPaxTypeForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditTicketPaxTypeDto input);

    Task Delete(EntityDto input);
}
