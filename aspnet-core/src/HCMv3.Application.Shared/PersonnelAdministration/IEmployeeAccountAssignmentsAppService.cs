using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeAccountAssignmentsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeAccountAssignmentDto>> GetAll(GetAllEmployeeAccountAssignmentsInput input);

    Task<GetEmployeeAccountAssignmentForEditOutput> GetEmployeeAccountAssignmentForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeAccountAssignmentDto input);

    Task Delete(EntityDto input);
}
