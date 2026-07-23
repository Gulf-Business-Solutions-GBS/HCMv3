using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface ISalaryRegisterDetailsAppService : IApplicationService
{
    Task<PagedResultDto<SalaryRegisterDetailDto>> GetAll(GetAllSalaryRegisterDetailsInput input);

    Task<GetSalaryRegisterDetailForEditOutput> GetSalaryRegisterDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditSalaryRegisterDetailDto input);

    Task Delete(EntityDto input);
}
