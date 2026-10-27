using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeVisaInfosAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeVisaInfoDto>> GetAll(GetAllEmployeeVisaInfosInput input);

    Task<GetEmployeeVisaInfoForEditOutput> GetEmployeeVisaInfoForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeVisaInfoDto input);

    Task Delete(EntityDto input);
}
