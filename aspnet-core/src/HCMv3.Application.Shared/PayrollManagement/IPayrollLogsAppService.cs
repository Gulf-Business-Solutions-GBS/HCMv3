using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IPayrollLogsAppService : IApplicationService
{
    Task<PagedResultDto<PayrollLogDto>> GetAll(GetAllPayrollLogsInput input);

    Task<GetPayrollLogForEditOutput> GetPayrollLogForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditPayrollLogDto input);

    Task Delete(EntityDto input);
}
