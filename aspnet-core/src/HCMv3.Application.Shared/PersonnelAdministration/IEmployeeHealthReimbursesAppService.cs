using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeHealthReimbursesAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeHealthReimburseDto>> GetAll(GetAllEmployeeHealthReimbursesInput input);

    Task<GetEmployeeHealthReimburseForEditOutput> GetEmployeeHealthReimburseForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeHealthReimburseDto input);

    Task Delete(EntityDto input);
}
