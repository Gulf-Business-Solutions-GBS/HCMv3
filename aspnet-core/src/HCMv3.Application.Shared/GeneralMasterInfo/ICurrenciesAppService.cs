using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo.Dto;

namespace HCMv3.GeneralMasterInfo;

public interface ICurrenciesAppService : IApplicationService
{
    Task<PagedResultDto<CurrencyDto>> GetAll(GetAllCurrenciesInput input);

    Task<GetCurrencyForEditOutput> GetCurrencyForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditCurrencyDto input);

    Task Delete(EntityDto input);
}
