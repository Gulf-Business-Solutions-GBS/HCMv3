using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeRejoinsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeRejoinDto>> GetAll(GetAllEmployeeRejoinsInput input);

    Task<GetEmployeeRejoinForEditOutput> GetEmployeeRejoinForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeRejoinDto input);

    Task Delete(EntityDto input);
}
