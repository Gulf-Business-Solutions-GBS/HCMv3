using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.TimeManagement;

public interface IEmployeeSubstituteDutiesAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeSubstituteDutyDto>> GetAll(GetAllEmployeeSubstituteDutiesInput input);

    Task<GetEmployeeSubstituteDutyForEditOutput> GetEmployeeSubstituteDutyForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeSubstituteDutyDto input);

    Task Delete(EntityDto input);
}
