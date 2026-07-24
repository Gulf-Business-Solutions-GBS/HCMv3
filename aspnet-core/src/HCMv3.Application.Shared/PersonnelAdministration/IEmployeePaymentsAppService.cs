using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeePaymentsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeePaymentDto>> GetAll(GetAllEmployeePaymentsInput input);

    Task<GetEmployeePaymentForEditOutput> GetEmployeePaymentForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeePaymentDto input);

    Task Delete(EntityDto input);
}
