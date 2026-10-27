using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IWageAccGroupDetailsAppService : IApplicationService
{
    Task<PagedResultDto<WageAccGroupDetailDto>> GetAll(GetAllWageAccGroupDetailsInput input);

    Task<GetWageAccGroupDetailForEditOutput> GetWageAccGroupDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditWageAccGroupDetailDto input);

    Task Delete(EntityDto input);
}
