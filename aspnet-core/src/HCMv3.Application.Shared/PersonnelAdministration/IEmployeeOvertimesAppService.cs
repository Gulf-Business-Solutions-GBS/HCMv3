using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeOvertimesAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeOvertimeDto>> GetAll(GetAllEmployeeOvertimesInput input);

    Task<GetEmployeeOvertimeForEditOutput> GetEmployeeOvertimeForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeOvertimeDto input);

    Task Delete(EntityDto input);
}
