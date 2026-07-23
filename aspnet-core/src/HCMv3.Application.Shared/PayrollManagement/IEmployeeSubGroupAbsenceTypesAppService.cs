using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IEmployeeSubGroupAbsenceTypesAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeSubGroupAbsenceTypeDto>> GetAll(GetAllEmployeeSubGroupAbsenceTypesInput input);

    Task<GetEmployeeSubGroupAbsenceTypeForEditOutput> GetEmployeeSubGroupAbsenceTypeForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeSubGroupAbsenceTypeDto input);

    Task Delete(EntityDto input);
}
