using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IPayrollAreasAppService : IApplicationService
{
    Task<PagedResultDto<PayrollAreaDto>> GetAll(GetAllPayrollAreasInput input);

    Task<GetPayrollAreaForEditOutput> GetPayrollAreaForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditPayrollAreaDto input);

    Task Delete(EntityDto input);
}
