using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeContractsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeContractDto>> GetAll(GetAllEmployeeContractsInput input);

    Task<GetEmployeeContractForEditOutput> GetEmployeeContractForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeContractDto input);

    Task Delete(EntityDto input);
}
