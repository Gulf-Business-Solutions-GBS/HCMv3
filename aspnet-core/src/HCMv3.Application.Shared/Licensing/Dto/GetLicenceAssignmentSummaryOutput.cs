using System.Collections.Generic;

namespace HCMv3.Licensing.Dto;

/// <summary>The header of the Licence Assignment screen.</summary>
public class GetLicenceAssignmentSummaryOutput
{
    /// <summary>
    /// Per-user licences are switched on in the tenant's edition. While off, assignments can be prepared but nobody
    /// is refused anything.
    /// </summary>
    public bool IsEnforced { get; set; }

    public List<LicenceModuleUsageDto> Modules { get; set; } = new();
}
