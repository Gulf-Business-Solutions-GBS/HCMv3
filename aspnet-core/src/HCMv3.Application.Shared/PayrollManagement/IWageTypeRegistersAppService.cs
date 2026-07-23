using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IWageTypeRegistersAppService : IApplicationService
{
    Task<PagedResultDto<WageTypeRegisterDto>> GetAll(GetAllWageTypeRegistersInput input);

    Task<GetWageTypeRegisterForEditOutput> GetWageTypeRegisterForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditWageTypeRegisterDto input);

    Task Delete(EntityDto input);
}
