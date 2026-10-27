using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing simulation salary register days-details.
/// </summary>
public class GetAllSimulationSalaryRegisterDaysDetailsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>
    /// Optional filter by parent SimulationSalaryRegisterDetail.
    /// </summary>
    public int? SimulationSalaryRegisterDetailId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Period ASC";
        }
    }
}
