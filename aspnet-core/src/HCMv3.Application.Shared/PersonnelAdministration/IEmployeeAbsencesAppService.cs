using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeAbsencesAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeAbsenceDto>> GetAll(GetAllEmployeeAbsencesInput input);

    /// <summary>Every employee, "code - name", for the Employee Leaves list's dialog.</summary>
    Task<ListResultDto<SimpleLookupDto>> GetEmployeesForLookup(GetEmployeeAbsenceEmployeesInput input);

    /// <summary>The leave types HR may book for this employee (legacy <c>EmpAbsenceType</c>).</summary>
    Task<ListResultDto<EmployeeAbsenceLeaveTypeDto>> GetLeaveTypesForEmployee(EntityDto input);

    /// <summary>The settings that show or hide fields on HR's leave form.</summary>
    Task<EmployeeAbsenceFormSettingsDto> GetLeaveFormSettings();

    Task<GetEmployeeAbsenceForEditOutput> GetEmployeeAbsenceForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeAbsenceDto input);

    Task Delete(EntityDto input);

    /// <summary>
    /// The calculated, read-only figures for the leave as currently entered (legacy <c>EmployeeAbsence</c> detail
    /// view): calendar days, total leave days, current balance, paid / unpaid split and sick-leave deduction.
    /// </summary>
    Task<EmployeeAbsencePreviewDto> GetAbsencePreview(GetEmployeeAbsencePreviewInput input);

    /// <summary>
    /// Asked before saving: whether the leave exceeds what is available and, if so, how legacy would split it into
    /// a paid part and additional unpaid leave (legacy <c>CheckQuota</c> / <c>SetAdditionalUnpaidLeave</c>).
    /// </summary>
    Task<EmployeeAbsenceUnpaidSplitDto> GetUnpaidSplitProposal(GetEmployeeAbsencePreviewInput input);
}
