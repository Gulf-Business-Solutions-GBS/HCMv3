using System;

namespace HCMv3.OrganizationManagement.Dto;

public class GetCompanyStructureNodeInput
{
    /// <summary>
    /// The node to focus on. Null means "start at the top": the service picks the first root unit, which
    /// is what the screen sends on first load before the user has chosen anything.
    /// </summary>
    public long? OrganizationUnitId { get; set; }

}
