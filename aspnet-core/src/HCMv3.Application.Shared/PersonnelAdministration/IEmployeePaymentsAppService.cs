using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeePaymentsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeePaymentDto>> GetAll(GetAllEmployeePaymentsInput input);

    Task<GetEmployeePaymentForEditOutput> GetEmployeePaymentForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeePaymentDto input);

    Task Delete(EntityDto input);

    /// <summary>Legacy non-persistent <c>ExpectedAmount</c>, worked out live while the form is edited.</summary>
    Task<double> GetExpectedAmount(GetExpectedPaymentAmountInput input);

    /// <summary>Employees a payment may be keyed in for (legacy <c>GetAllowedEmployeesForPayment</c>).</summary>
    Task<ListResultDto<SimpleLookupDto>> GetEmployeesForLookup(GetEmployeePaymentEmployeesInput input);

    /// <summary>Wage types a payment may use (legacy detail-view wage type criteria).</summary>
    Task<ListResultDto<EmployeePaymentWageTypeLookupDto>> GetWageTypesForLookup();

    Task<EmployeePaymentScreenSettingsDto> GetScreenSettings();
}
