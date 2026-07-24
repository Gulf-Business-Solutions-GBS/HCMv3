using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeGrievancesAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeGrievanceDto>> GetAll(GetAllEmployeeGrievancesInput input);

    Task<GetEmployeeGrievanceForEditOutput> GetEmployeeGrievanceForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeGrievanceDto input);

    Task Delete(EntityDto input);
}
