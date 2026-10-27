using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeFundsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeFundDto>> GetAll(GetAllEmployeeFundsInput input);

    Task<GetEmployeeFundForEditOutput> GetEmployeeFundForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeFundDto input);

    Task Delete(EntityDto input);
}
