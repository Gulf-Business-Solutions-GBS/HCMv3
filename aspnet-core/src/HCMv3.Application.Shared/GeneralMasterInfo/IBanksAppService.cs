using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo.Dto;

namespace HCMv3.GeneralMasterInfo;

public interface IBanksAppService : IApplicationService
{
    Task<PagedResultDto<BankDto>> GetAll(GetAllBanksInput input);

    Task<GetBankForEditOutput> GetBankForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditBankDto input);

    Task Delete(EntityDto input);
}
