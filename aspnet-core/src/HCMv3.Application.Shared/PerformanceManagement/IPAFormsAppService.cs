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

    Task Delete(EntityDto input);
}
