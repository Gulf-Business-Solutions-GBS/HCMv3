using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.JobProfileManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing job description acknowledgements.
/// </summary>
public class GetAllJobDescriptionAcknowledgementsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    public int? JobDescriptionId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id";
        }
    }
}
