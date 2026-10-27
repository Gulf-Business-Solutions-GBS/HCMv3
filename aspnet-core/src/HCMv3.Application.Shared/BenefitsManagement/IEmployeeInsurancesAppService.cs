using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.BenefitsManagement.Dto;

namespace HCMv3.BenefitsManagement;

public interface IEmployeeInsurancesAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeInsuranceDto>> GetAll(GetAllEmployeeInsurancesInput input);

    Task<GetEmployeeInsuranceForEditOutput> GetEmployeeInsuranceForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeInsuranceDto input);

    Task Delete(EntityDto input);
}
