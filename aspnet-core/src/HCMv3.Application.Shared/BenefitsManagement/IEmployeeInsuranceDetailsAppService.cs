using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.BenefitsManagement.Dto;

namespace HCMv3.BenefitsManagement;

public interface IEmployeeInsuranceDetailsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeInsuranceDetailDto>> GetAll(GetAllEmployeeInsuranceDetailsInput input);

    Task<GetEmployeeInsuranceDetailForEditOutput> GetEmployeeInsuranceDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeInsuranceDetailDto input);

    Task Delete(EntityDto input);
}
