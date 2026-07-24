using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeCostDistributionsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeCostDistributionDto>> GetAll(GetAllEmployeeCostDistributionsInput input);

    Task<GetEmployeeCostDistributionForEditOutput> GetEmployeeCostDistributionForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeCostDistributionDto input);

    Task Delete(EntityDto input);
}
