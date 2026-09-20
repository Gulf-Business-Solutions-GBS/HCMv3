using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IPayrollControlRecordsAppService : IApplicationService
{
    Task<PagedResultDto<PayrollControlRecordDto>> GetAll(GetAllPayrollControlRecordsInput input);

    Task<GetPayrollControlRecordForEditOutput> GetPayrollControlRecordForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditPayrollControlRecordDto input);

    Task Delete(EntityDto input);

    /// <summary>
    /// Moves the period to another state: release it for a payroll run (which locks master data), release it so data
    /// can be corrected, close it when payroll for the month is finished, or set it back to open.
    /// </summary>
    Task SetState(SetPayrollPeriodStateInput input);
}
