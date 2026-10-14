using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Ess.Dto;

namespace HCMv3.Ess;

/// <summary>
/// Self-service personnel-file update (legacy <c>PersonalDataUpdateRequest</c>): the employee proposes
/// changes to their own master record and an approver agrees them.
///
/// <para>
/// The highest-consequence self-service type there is -- approving one writes to the employee master record
/// that payroll, government reporting and every letter template read from. That is why the proposal lives in
/// its own table (<c>EmployeePersonalDataChange</c>) instead of on the employee row: an in-flight edit to
/// somebody's phone number must never make them invisible to payroll, which is exactly what putting the
/// <c>ApprovedOnly</c> filter on <c>Employee</c> would do.
/// </para>
/// </summary>
public interface IMyPersonalDataChangeAppService : IApplicationService
{
    Task<PagedResultDto<MyPersonalDataChangeDto>> GetMyPersonalDataChanges(GetMyRequestsInput input);

    /// <summary>
    /// The form, pre-filled with the employee's current values, so they edit what is there instead of
    /// retyping their identity to change one field.
    /// </summary>
    Task<MyPersonalDataForEditDto> GetMyPersonalDataForEdit();

    /// <summary>Creates the proposal and, when the type is workflow-enabled, submits it in one step.</summary>
    Task<MyRequestSubmissionDto> RequestPersonalDataChange(RequestPersonalDataChangeInput input);

    /// <summary>The choices behind the form's pickers: title, nationality, religion, marital status, birth
    /// place, governorate and country.</summary>
    Task<PersonalDataLookupsDto> GetMyPersonalDataLookups();

    /// <summary>
    /// Save: files the form as a draft only the employee can see, or changes a draft (or a request an
    /// approver sent back) already filed. Nothing is sent to anybody.
    /// </summary>
    Task<MyRequestSubmissionDto> SaveMyPersonalDataChangeDraft(SavePersonalDataChangeDraftInput input);

    /// <summary>One of the employee's own drafts, or a request sent back to them, to reopen the form on.</summary>
    Task<MyPersonalDataChangeDto> GetMyPersonalDataChangeDraftForEdit(EntityDto input);

    /// <summary>Sends a saved draft for approval, or sends a request that was sent back again.</summary>
    Task<MyRequestSubmissionDto> SubmitMyPersonalDataChangeDraft(EntityDto input);

    /// <summary>Withdraws a proposal while it is still theirs to withdraw.</summary>
    Task CancelMyPersonalDataChange(EntityDto input);
}
