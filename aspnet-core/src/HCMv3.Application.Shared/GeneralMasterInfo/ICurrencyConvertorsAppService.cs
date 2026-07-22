using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo.Dto;

namespace HCMv3.GeneralMasterInfo;

public interface ICurrencyConvertorsAppService : IApplicationService
{
    Task<PagedResultDto<CurrencyConvertorDto>> GetAll(GetAllCurrencyConvertorsInput input);

    Task<GetCurrencyConvertorForEditOutput> GetCurrencyConvertorForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditCurrencyConvertorDto input);

    Task Delete(EntityDto input);
}
