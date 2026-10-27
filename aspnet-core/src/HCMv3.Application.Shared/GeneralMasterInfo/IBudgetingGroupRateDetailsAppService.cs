using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo.Dto;

namespace HCMv3.GeneralMasterInfo;

public interface IBudgetingGroupRateDetailsAppService : IApplicationService
{
    Task<PagedResultDto<BudgetingGroupRateDetailDto>> GetAll(GetAllBudgetingGroupRateDetailsInput input);

    Task<GetBudgetingGroupRateDetailForEditOutput> GetBudgetingGroupRateDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditBudgetingGroupRateDetailDto input);

    Task Delete(EntityDto input);
}
