using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeSponsorsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeSponsorDto>> GetAll(GetAllEmployeeSponsorsInput input);

    Task<GetEmployeeSponsorForEditOutput> GetEmployeeSponsorForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeSponsorDto input);

    Task Delete(EntityDto input);
}
