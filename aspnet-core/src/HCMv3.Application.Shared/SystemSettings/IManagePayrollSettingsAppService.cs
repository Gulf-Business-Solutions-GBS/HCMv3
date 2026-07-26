using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SystemSettings.Dto;

namespace HCMv3.SystemSettings;

public interface IManagePayrollSettingsAppService : IApplicationService
{
    Task<PagedResultDto<PayrollSettingsListDto>> GetPayrollSettingsList(PagedAndSortedResultRequestDto input);

    Task<PayrollSettingsEditDto> GetPayrollSettingsForEdit(EntityDto input);

    Task CreateOrEditPayrollSettings(PayrollSettingsEditDto input);

    Task DeletePayrollSettings(EntityDto input);
}
