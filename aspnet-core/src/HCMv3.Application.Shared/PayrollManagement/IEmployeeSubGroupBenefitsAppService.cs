using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IEmployeeSubGroupBenefitsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeSubGroupBenefitDto>> GetAll(GetAllEmployeeSubGroupBenefitsInput input);

    Task<GetEmployeeSubGroupBenefitForEditOutput> GetEmployeeSubGroupBenefitForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeSubGroupBenefitDto input);

    Task Delete(EntityDto input);
}
