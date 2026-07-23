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
}
