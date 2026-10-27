using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeAbsAdjustmentsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeAbsAdjustmentDto>> GetAll(GetAllEmployeeAbsAdjustmentsInput input);

    Task<GetEmployeeAbsAdjustmentForEditOutput> GetEmployeeAbsAdjustmentForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeAbsAdjustmentDto input);

    Task Delete(EntityDto input);
}
