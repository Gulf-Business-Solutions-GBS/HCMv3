using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeOvertimesAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeOvertimeDto>> GetAll(GetAllEmployeeOvertimesInput input);

    Task<ListResultDto<SimpleLookupDto>> GetEmployeesForLookup(GetEmployeeOvertimeEmployeesInput input);

    Task<GetEmployeeOvertimeForEditOutput> GetEmployeeOvertimeForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeOvertimeDto input);

    Task Delete(EntityDto input);

    /// <summary>
    /// The Expected Overtime Amount for what the form currently holds, recalculated whenever Employee,
    /// Effective Date, Overtime Type or Approved Hours change (legacy ImmediatePostData). Nothing is saved.
    /// </summary>
    Task<double> GetExpectedOvertimeAmount(GetExpectedOvertimeAmountInput input);
}
