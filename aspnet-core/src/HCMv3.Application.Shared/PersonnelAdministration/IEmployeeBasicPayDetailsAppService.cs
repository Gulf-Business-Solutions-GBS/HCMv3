using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeBasicPayDetailsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeBasicPayDetailDto>> GetAll(GetAllEmployeeBasicPayDetailsInput input);

    Task<GetEmployeeBasicPayDetailForEditOutput> GetEmployeeBasicPayDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeBasicPayDetailDto input);

    Task Delete(EntityDto input);
}
