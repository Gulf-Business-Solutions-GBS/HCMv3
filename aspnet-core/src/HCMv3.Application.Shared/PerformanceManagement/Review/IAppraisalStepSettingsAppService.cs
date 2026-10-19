using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PerformanceManagement.Review.Dto;

namespace HCMv3.PerformanceManagement.Review;

/// <summary>
/// The "Appraisal WF Setup" screen: per request master and step, what the people on that step of an
/// appraisal's chain may do (legacy <c>PARequest</c>).
/// </summary>
public interface IAppraisalStepSettingsAppService : IApplicationService
{
    Task<PagedResultDto<AppraisalStepSettingDto>> GetAll(GetAllAppraisalStepSettingsInput input);

    Task<GetAppraisalStepSettingForEditOutput> GetAppraisalStepSettingForEdit(EntityDto input);

    /// <summary>The request masters (with their steps) and relationship types the dialog offers.</summary>
    Task<AppraisalStepSettingLookupsDto> GetLookups();

    Task CreateOrEdit(CreateOrEditAppraisalStepSettingDto input);

    Task Delete(EntityDto input);
}
