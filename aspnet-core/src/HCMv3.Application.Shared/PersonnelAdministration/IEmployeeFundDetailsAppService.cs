using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeFundDetailsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeFundDetailDto>> GetAll(GetAllEmployeeFundDetailsInput input);

    Task<GetEmployeeFundDetailForEditOutput> GetEmployeeFundDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeFundDetailDto input);

    Task Delete(EntityDto input);
}
