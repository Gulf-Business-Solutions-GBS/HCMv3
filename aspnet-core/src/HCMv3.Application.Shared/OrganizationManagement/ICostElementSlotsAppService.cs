using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.OrganizationManagement.Dto;

namespace HCMv3.OrganizationManagement;

/// <summary>
/// Read-only lookup for the seven cost element slots shown on wage account group details, cost register
/// details and employee wage accounts. Returns only the slots that have a cost element type configured.
/// </summary>
public interface ICostElementSlotsAppService : IApplicationService
{
    Task<ListResultDto<CostElementSlotDto>> GetSlots();
}
