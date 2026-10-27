using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeesAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeDto>> GetAll(GetAllEmployeesInput input);

    Task<GetEmployeeForEditOutput> GetEmployeeForEdit(EntityDto input);

    /// <summary>
    /// The employee's particulars with every lookup resolved to a name, for the read view on the
    /// employee record. See <see cref="EmployeeProfileDto"/> for why the edit DTO cannot serve this.
    /// </summary>
    Task<EmployeeProfileDto> GetEmployeeProfile(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeDto input);

    Task Delete(EntityDto input);
}
