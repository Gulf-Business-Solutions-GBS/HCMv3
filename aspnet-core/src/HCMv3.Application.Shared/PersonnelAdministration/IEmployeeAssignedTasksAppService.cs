using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// The ONLY way work is assigned to an employee (legacy <c>TaskRequest</c>). There is no self-service
/// counterpart and there should not be one: a task is handed to somebody, not asked for, so creation here
/// routes it for approval itself through <c>IAdminTransactionSubmitter</c>.
///
/// <para>
/// Not to be confused with <c>IEmployeeTasksAppService</c>, which manages the HR reminder/monitoring record.
/// See <c>EmployeeAssignedTask</c> for why the two are separate.
/// </para>
/// </summary>
public interface IEmployeeAssignedTasksAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeAssignedTaskDto>> GetAll(GetAllEmployeeAssignedTasksInput input);

    Task<GetEmployeeAssignedTaskForEditOutput> GetEmployeeAssignedTaskForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeAssignedTaskDto input);

    Task Delete(EntityDto input);
}
