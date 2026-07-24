using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeTktAdjustmentsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeTktAdjustmentDto>> GetAll(GetAllEmployeeTktAdjustmentsInput input);

    Task<GetEmployeeTktAdjustmentForEditOutput> GetEmployeeTktAdjustmentForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeTktAdjustmentDto input);

    Task Delete(EntityDto input);
}
