using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SystemSettings.Dto;

namespace HCMv3.SystemSettings;

public interface IPayrollDriverAppService : IApplicationService
{
    Task<PagedResultDto<PayrollDriverListDto>> GetPayrollDrivers(GetPayrollDriversInput input);

    Task<PayrollDriverEditDto> GetPayrollDriverForEdit(EntityDto input);

    Task CreateOrEditPayrollDriver(PayrollDriverEditDto input);

    Task DeletePayrollDriver(EntityDto input);
}
