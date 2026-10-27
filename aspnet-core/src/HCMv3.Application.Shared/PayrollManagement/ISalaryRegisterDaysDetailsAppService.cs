using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface ISalaryRegisterDaysDetailsAppService : IApplicationService
{
    Task<PagedResultDto<SalaryRegisterDaysDetailDto>> GetAll(GetAllSalaryRegisterDaysDetailsInput input);

    Task<GetSalaryRegisterDaysDetailForEditOutput> GetSalaryRegisterDaysDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditSalaryRegisterDaysDetailDto input);

    Task Delete(EntityDto input);
}
