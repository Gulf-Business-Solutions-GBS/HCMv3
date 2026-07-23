using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IEmployeeSubGroupBenefitDetailsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeSubGroupBenefitDetailDto>> GetAll(GetAllEmployeeSubGroupBenefitDetailsInput input);

    Task<GetEmployeeSubGroupBenefitDetailForEditOutput> GetEmployeeSubGroupBenefitDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeSubGroupBenefitDetailDto input);

    Task Delete(EntityDto input);
}
