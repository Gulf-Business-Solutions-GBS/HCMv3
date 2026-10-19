using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PerformanceManagement.Dto;

namespace HCMv3.PerformanceManagement;

public interface IPAFormsAppService : IApplicationService
{
    Task<PagedResultDto<PAFormDto>> GetAll(GetAllPAFormsInput input);

    Task<GetPAFormForEditOutput> GetPAFormForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditPAFormDto input);

    /// <summary>The appraisal settings the PA form screens read; see <see cref="AppraisalFormSettingsDto"/>.</summary>
    Task<AppraisalFormSettingsDto> GetAppraisalFormSettings();

    /// <summary>
    /// Which of the form's twelve boxes the signed-in employee may see and tick now. Pass no id for a form
    /// that is still being created.
    /// </summary>
    Task<PAFormTickRulesDto> GetPAFormTickRules(NullableIdDto input);

    /// <summary>
    /// A nominated peer marks their peer review of this form's employee as submitted (legacy
    /// <c>PAForm.Submitted</c>), during End-of-Year only.
    /// </summary>
    Task SubmitPeerReview(EntityDto input);

    Task Delete(EntityDto input);
}
