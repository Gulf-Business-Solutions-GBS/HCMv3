using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IEmployeeSubGroupPaymentsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeSubGroupPaymentDto>> GetAll(GetAllEmployeeSubGroupPaymentsInput input);

    Task<GetEmployeeSubGroupPaymentForEditOutput> GetEmployeeSubGroupPaymentForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeSubGroupPaymentDto input);

    Task Delete(EntityDto input);
}
