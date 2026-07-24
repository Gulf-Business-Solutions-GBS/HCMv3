using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeDisciplinariesAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeDisciplinaryDto>> GetAll(GetAllEmployeeDisciplinariesInput input);

    Task<GetEmployeeDisciplinaryForEditOutput> GetEmployeeDisciplinaryForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeDisciplinaryDto input);

    Task Delete(EntityDto input);
}
