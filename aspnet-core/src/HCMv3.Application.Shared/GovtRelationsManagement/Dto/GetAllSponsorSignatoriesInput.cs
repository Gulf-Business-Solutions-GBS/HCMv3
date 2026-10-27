using Abp.Application.Services.Dto;

namespace HCMv3.GovtRelationsManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing sponsor signatories.
/// </summary>
public class GetAllSponsorSignatoriesInput : PagedAndSortedResultRequestDto
{
    /// <summary>Optional filter by parent Sponsor.</summary>
    public int? SponsorId { get; set; }
}
