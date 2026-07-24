using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeBanksAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeBankDto>> GetAll(GetAllEmployeeBanksInput input);

    Task<GetEmployeeBankForEditOutput> GetEmployeeBankForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeBankDto input);

    Task Delete(EntityDto input);
}
