using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeDatesAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeDateDto>> GetAll(GetAllEmployeeDatesInput input);

    Task<GetEmployeeDateForEditOutput> GetEmployeeDateForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeDateDto input);

    Task Delete(EntityDto input);
}
