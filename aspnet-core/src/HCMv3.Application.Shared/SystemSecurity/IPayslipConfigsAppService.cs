using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SystemSecurity.Dto;

namespace HCMv3.SystemSecurity;

public interface IPayslipConfigsAppService : IApplicationService
{
    Task<PagedResultDto<PayslipConfigDto>> GetAll(GetAllPayslipConfigsInput input);

    Task<GetPayslipConfigForEditOutput> GetPayslipConfigForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditPayslipConfigDto input);

    Task Delete(EntityDto input);
}
