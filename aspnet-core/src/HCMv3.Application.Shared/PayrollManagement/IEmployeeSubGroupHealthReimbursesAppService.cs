using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IEmployeeSubGroupHealthReimbursesAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeSubGroupHealthReimburseDto>> GetAll(GetAllEmployeeSubGroupHealthReimbursesInput input);

    Task<GetEmployeeSubGroupHealthReimburseForEditOutput> GetEmployeeSubGroupHealthReimburseForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeSubGroupHealthReimburseDto input);

    Task Delete(EntityDto input);
}
