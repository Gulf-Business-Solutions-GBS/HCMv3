using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeTicketQuotasAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeTicketQuotaDto>> GetAll(GetAllEmployeeTicketQuotasInput input);

    Task<GetEmployeeTicketQuotaForEditOutput> GetEmployeeTicketQuotaForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeTicketQuotaDto input);

    Task Delete(EntityDto input);
}
