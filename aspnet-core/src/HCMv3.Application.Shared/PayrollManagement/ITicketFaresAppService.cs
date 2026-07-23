using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface ITicketFaresAppService : IApplicationService
{
    Task<PagedResultDto<TicketFareDto>> GetAll(GetAllTicketFaresInput input);

    Task<GetTicketFareForEditOutput> GetTicketFareForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditTicketFareDto input);

    Task Delete(EntityDto input);
}
