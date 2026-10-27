using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeAbsencesAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeAbsenceDto>> GetAll(GetAllEmployeeAbsencesInput input);

    Task<GetEmployeeAbsenceForEditOutput> GetEmployeeAbsenceForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeAbsenceDto input);

    Task Delete(EntityDto input);

    /// <summary>
    /// The calculated, read-only figures for the leave as currently entered (legacy <c>EmployeeAbsence</c> detail
    /// view): calendar days, total leave days, current balance, paid / unpaid split and sick-leave deduction.
    /// </summary>
    Task<EmployeeAbsencePreviewDto> GetAbsencePreview(GetEmployeeAbsencePreviewInput input);
}
