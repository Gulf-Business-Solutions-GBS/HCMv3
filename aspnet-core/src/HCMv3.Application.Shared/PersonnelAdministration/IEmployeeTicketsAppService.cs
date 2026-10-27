using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeTicketsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeTicketDto>> GetAll(GetAllEmployeeTicketsInput input);

    Task<GetEmployeeTicketForEditOutput> GetEmployeeTicketForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeTicketDto input);

    Task Delete(EntityDto input);
}
