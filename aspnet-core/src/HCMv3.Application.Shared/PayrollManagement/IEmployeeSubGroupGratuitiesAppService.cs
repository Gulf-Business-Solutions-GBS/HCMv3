using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IEmployeeSubGroupGratuitiesAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeSubGroupGratuityDto>> GetAll(GetAllEmployeeSubGroupGratuitiesInput input);

    Task<GetEmployeeSubGroupGratuityForEditOutput> GetEmployeeSubGroupGratuityForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeSubGroupGratuityDto input);

    Task Delete(EntityDto input);
}
