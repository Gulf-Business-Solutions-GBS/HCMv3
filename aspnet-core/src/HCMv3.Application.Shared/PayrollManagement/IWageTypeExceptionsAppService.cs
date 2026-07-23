using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IWageTypeExceptionsAppService : IApplicationService
{
    Task<PagedResultDto<WageTypeExceptionDto>> GetAll(GetAllWageTypeExceptionsInput input);

    Task<GetWageTypeExceptionForEditOutput> GetWageTypeExceptionForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditWageTypeExceptionDto input);

    Task Delete(EntityDto input);
}
