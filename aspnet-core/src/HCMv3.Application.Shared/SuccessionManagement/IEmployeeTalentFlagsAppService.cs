using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SuccessionManagement.Dto;

namespace HCMv3.SuccessionManagement;

public interface IEmployeeTalentFlagsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeTalentFlagDto>> GetAll(GetAllEmployeeTalentFlagsInput input);

    Task<GetEmployeeTalentFlagForEditOutput> GetEmployeeTalentFlagForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeTalentFlagDto input);

    Task Delete(EntityDto input);
}
