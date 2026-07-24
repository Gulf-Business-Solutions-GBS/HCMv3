using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeFnFPaymentsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeFnFPaymentDto>> GetAll(GetAllEmployeeFnFPaymentsInput input);

    Task<GetEmployeeFnFPaymentForEditOutput> GetEmployeeFnFPaymentForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeFnFPaymentDto input);

    Task Delete(EntityDto input);
}
