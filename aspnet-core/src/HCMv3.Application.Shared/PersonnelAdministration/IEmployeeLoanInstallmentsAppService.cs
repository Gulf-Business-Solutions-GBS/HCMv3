using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeLoanInstallmentsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeLoanInstallmentDto>> GetAll(GetAllEmployeeLoanInstallmentsInput input);

    Task<GetEmployeeLoanInstallmentForEditOutput> GetEmployeeLoanInstallmentForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeLoanInstallmentDto input);

    Task Delete(EntityDto input);
}
