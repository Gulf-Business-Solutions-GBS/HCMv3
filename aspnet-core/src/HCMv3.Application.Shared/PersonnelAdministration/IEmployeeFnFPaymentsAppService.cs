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

    /// <summary>
    /// The balances and suggested indemnity for the EMPLOYEE whose id is <c>input.Id</c> (not an F&amp;F id) --
    /// what the screen shows before an entry is saved.
    /// </summary>
    Task<FullAndFinalBalancesDto> GetFullAndFinalBalances(EntityDto input);
}
