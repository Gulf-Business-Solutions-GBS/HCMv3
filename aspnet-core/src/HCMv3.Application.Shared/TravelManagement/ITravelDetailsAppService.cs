using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TravelManagement.Dto;

namespace HCMv3.TravelManagement;

public interface ITravelDetailsAppService : IApplicationService
{
    Task<PagedResultDto<TravelDetailDto>> GetAll(GetAllTravelDetailsInput input);

    Task<GetTravelDetailForEditOutput> GetTravelDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditTravelDetailDto input);

    Task Delete(EntityDto input);

    /// <summary>
    /// Computes the on-demand expected per-diem rate/amount for a persisted <see cref="TravelDetail"/> by
    /// querying <see cref="TravelPerDiem"/> for the given EmployeeGroup/EmployeeSubGroup (caller-supplied --
    /// see <see cref="Dto.CalculateExpectedPerDiemInput"/> remarks for why this can't be auto-derived from
    /// OrgAssignment in this codebase) -- see the implementation's XML doc for details/simplifications made
    /// vs. the legacy XAF non-persistent properties.
    /// </summary>
    Task<CalculateExpectedPerDiemOutput> CalculateExpectedPerDiemAsync(CalculateExpectedPerDiemInput input);
}
