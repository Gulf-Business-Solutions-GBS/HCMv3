using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeDottedLineManagersAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeDottedLineManagerDto>> GetAll(GetAllEmployeeDottedLineManagersInput input);

    Task<GetEmployeeDottedLineManagerForEditOutput> GetEmployeeDottedLineManagerForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeDottedLineManagerDto input);

    Task Delete(EntityDto input);
}
