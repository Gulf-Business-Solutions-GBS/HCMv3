using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// The department overtime batch screen (legacy <c>OvertimeRequest</c>, raised from the HCM10.ESS Overtime
/// screen): pick a unit and dates, load its people's time-register days, enter approved hours, submit the lot
/// for approval as one request.
/// </summary>
public interface IOvertimeBatchesAppService : IApplicationService
{
    Task<PagedResultDto<OvertimeBatchListDto>> GetAll(GetAllOvertimeBatchesInput input);

    Task<GetOvertimeBatchForEditOutput> GetOvertimeBatchForEdit(EntityDto input);

    /// <summary>The units a batch can be raised for.</summary>
    Task<List<OvertimeBatchOrganizationUnitDto>> GetOrganizationUnits();

    /// <summary>The overtime types a line can show (the screen's own lookup, so no master-data permission is needed).</summary>
    Task<List<OvertimeBatchOvertimeTypeDto>> GetOvertimeTypes();

    /// <summary>The grid: every active employee in the unit (and the units below it), day by day.</summary>
    Task<List<OvertimeBatchLineDto>> GetOvertimeBatchCandidates(GetOvertimeBatchCandidatesInput input);

    /// <summary>Saves the batch and submits it for approval in one step. Returns the batch id.</summary>
    Task<int> CreateOvertimeBatch(CreateOrEditOvertimeBatchDto input);

    /// <summary>Rewrites a batch that is not in anybody's inbox (sent back, or never routed and not yet posted).</summary>
    Task UpdateOvertimeBatch(CreateOrEditOvertimeBatchDto input);

    /// <summary>Puts a sent-back batch forward again.</summary>
    Task ResubmitOvertimeBatch(EntityDto input);

    Task Delete(EntityDto input);
}
