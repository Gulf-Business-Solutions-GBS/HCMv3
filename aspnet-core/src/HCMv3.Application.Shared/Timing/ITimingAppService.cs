using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Timing.Dto;

namespace HCMv3.Timing;

public interface ITimingAppService : IApplicationService
{
    Task<ListResultDto<NameValueDto>> GetTimezones(GetTimezonesInput input);

    Task<List<ComboboxItemDto>> GetTimezoneComboboxItems(GetTimezoneComboboxItemsInput input);
}

