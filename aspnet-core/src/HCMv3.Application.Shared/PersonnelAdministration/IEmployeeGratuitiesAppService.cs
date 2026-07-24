using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeGratuitiesAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeGratuityDto>> GetAll(GetAllEmployeeGratuitiesInput input);

    Task<GetEmployeeGratuityForEditOutput> GetEmployeeGratuityForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeGratuityDto input);

    Task Delete(EntityDto input);
}
