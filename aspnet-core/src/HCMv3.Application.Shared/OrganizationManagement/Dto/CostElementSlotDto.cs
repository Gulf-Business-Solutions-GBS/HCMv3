using System.Collections.Generic;

namespace HCMv3.OrganizationManagement.Dto;

/// <summary>
/// One configured cost element slot (CostElement1..CostElement7 on wage account rows).
/// A slot is configured when a <c>CostElementType</c> exists whose Code is "CostElement{Slot}";
/// its names become the slot's caption and only cost elements of that type are offered in it
/// (legacy <c>CostController</c> + the <c>CostElementNList</c> data sources).
/// </summary>
public class CostElementSlotDto
{
    /// <summary>1..7: which CostElement{N}Id field this slot fills.</summary>
    public int Slot { get; set; }

    public int CostElementTypeId { get; set; }

    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    /// <summary>Cost elements of this slot's type, ordered by code.</summary>
    public List<CostElementSlotOptionDto> Options { get; set; } = new();
}

public class CostElementSlotOptionDto
{
    public int Id { get; set; }

    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }
}
