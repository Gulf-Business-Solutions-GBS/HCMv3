using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IEmployeeSubGroupTicketsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeSubGroupTicketDto>> GetAll(GetAllEmployeeSubGroupTicketsInput input);

    Task<GetEmployeeSubGroupTicketForEditOutput> GetEmployeeSubGroupTicketForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeSubGroupTicketDto input);

    Task Delete(EntityDto input);
}
