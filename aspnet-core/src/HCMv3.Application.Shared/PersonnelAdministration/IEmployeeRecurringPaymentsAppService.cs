using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeRecurringPaymentsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeRecurringPaymentDto>> GetAll(GetAllEmployeeRecurringPaymentsInput input);

    Task<GetEmployeeRecurringPaymentForEditOutput> GetEmployeeRecurringPaymentForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeRecurringPaymentDto input);

    Task Delete(EntityDto input);
}
