using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface ISalaryRegistersAppService : IApplicationService
{
    Task<PagedResultDto<SalaryRegisterDto>> GetAll(GetAllSalaryRegistersInput input);

    Task<GetSalaryRegisterForEditOutput> GetSalaryRegisterForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditSalaryRegisterDto input);

    Task Delete(EntityDto input);
}
