using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IWageAccountsAppService : IApplicationService
{
    Task<PagedResultDto<WageAccountDto>> GetAll(GetAllWageAccountsInput input);

    Task<GetWageAccountForEditOutput> GetWageAccountForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditWageAccountDto input);

    Task Delete(EntityDto input);
}
