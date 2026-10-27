using Abp.Application.Services.Dto;

namespace HCMv3.GovtRelationsManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing sponsor bank records.
/// </summary>
public class GetAllSponsorBanksInput : PagedAndSortedResultRequestDto
{
    /// <summary>Optional filter by parent Sponsor.</summary>
    public int? SponsorId { get; set; }
}
