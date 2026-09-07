using System;

namespace HCMv3.OrganizationManagement.Dto;

public class GetCompanyStructureNodeInput
{
    /// <summary>
    /// The node to focus on. Null means "start at the top": the service picks the first root unit, which
    /// is what the screen sends on first load before the user has chosen anything.
    /// </summary>
    public long? OrganizationUnitId { get; set; }

    /// <summary>
    /// The "As Of" date driving every headcount on the returned cards. Null means today. Time-of-day is
    /// ignored -- the service takes <c>.Date</c>, since org assignments are whole-day ranges.
    /// </summary>
    public DateTime? AsOfDate { get; set; }
}
