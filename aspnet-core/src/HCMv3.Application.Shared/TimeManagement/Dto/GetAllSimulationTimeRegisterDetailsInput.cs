using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing simulation time register details.
/// </summary>
public class GetAllSimulationTimeRegisterDetailsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>
    /// Optional filter by parent SimulationTimeRegister.
    /// </summary>
    public int? SimulationTimeRegisterId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id ASC";
        }
    }
}
