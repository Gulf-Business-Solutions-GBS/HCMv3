using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeTicketRoutesAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeTicketRouteDto>> GetAll(GetAllEmployeeTicketRoutesInput input);

    Task<GetEmployeeTicketRouteForEditOutput> GetEmployeeTicketRouteForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeTicketRouteDto input);

    Task Delete(EntityDto input);
}
