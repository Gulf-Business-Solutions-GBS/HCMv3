using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeRelationshipsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeRelationshipDto>> GetAll(GetAllEmployeeRelationshipsInput input);

    Task<GetEmployeeRelationshipForEditOutput> GetEmployeeRelationshipForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeRelationshipDto input);

    Task Delete(EntityDto input);
}
