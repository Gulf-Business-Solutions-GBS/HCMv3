using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IEmployeeSubGroupWageTypesAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeSubGroupWageTypeDto>> GetAll(GetAllEmployeeSubGroupWageTypesInput input);

    Task<GetEmployeeSubGroupWageTypeForEditOutput> GetEmployeeSubGroupWageTypeForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeSubGroupWageTypeDto input);

    Task Delete(EntityDto input);
}
