using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.GovtRelationsManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing organization documents.
/// </summary>
public class GetAllOrganizationDocumentsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>Optional filter by OrganizationUnit.</summary>
    public long? OrganizationUnitId { get; set; }

    /// <summary>Optional filter by parent Sponsor.</summary>
    public int? SponsorId { get; set; }

    /// <summary>Free-text filter matched against DocumentNum / DocumentPlace.</summary>
    public string Filter { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "ExpiryDate ASC";
        }
    }
}
