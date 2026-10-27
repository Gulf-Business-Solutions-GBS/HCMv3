using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IAccountInfosAppService : IApplicationService
{
    Task<PagedResultDto<AccountInfoDto>> GetAll(GetAllAccountInfosInput input);

    Task<GetAccountInfoForEditOutput> GetAccountInfoForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditAccountInfoDto input);

    Task Delete(EntityDto input);
}
