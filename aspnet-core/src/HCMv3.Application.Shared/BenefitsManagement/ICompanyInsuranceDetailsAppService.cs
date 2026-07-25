using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.BenefitsManagement.Dto;

namespace HCMv3.BenefitsManagement;

public interface ICompanyInsuranceDetailsAppService : IApplicationService
{
    Task<PagedResultDto<CompanyInsuranceDetailDto>> GetAll(GetAllCompanyInsuranceDetailsInput input);

    Task<GetCompanyInsuranceDetailForEditOutput> GetCompanyInsuranceDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditCompanyInsuranceDetailDto input);

    Task Delete(EntityDto input);
}
