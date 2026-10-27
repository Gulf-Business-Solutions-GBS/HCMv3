using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IEmployeePayrollStatusesAppService : IApplicationService
{
    Task<PagedResultDto<EmployeePayrollStatusDto>> GetAll(GetAllEmployeePayrollStatusesInput input);

    Task<GetEmployeePayrollStatusForEditOutput> GetEmployeePayrollStatusForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeePayrollStatusDto input);

    Task Delete(EntityDto input);
}
