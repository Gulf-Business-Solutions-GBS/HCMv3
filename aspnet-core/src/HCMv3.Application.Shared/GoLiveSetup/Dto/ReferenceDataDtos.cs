using System.Collections.Generic;

namespace HCMv3.GoLiveSetup.Dto;

/// <summary>One shipped reference pack, as offered on the Go-Live Setup screen.</summary>
public class ReferenceDataPackDto
{
    public string Id { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public string Description { get; set; }

    /// <summary>Rows the pack would add, per lookup, before de-duplication against the tenant.</summary>
    public List<ReferenceDataPackLineDto> Lines { get; set; } = new();

    public int TotalRows { get; set; }
}

public class ReferenceDataPackLineDto
{
    public string Lookup { get; set; }

    /// <summary>Rows in the pack.</summary>
    public int Available { get; set; }

    /// <summary>Rows the tenant already has under the same English name.</summary>
    public int AlreadyPresent { get; set; }
}

public class SeedReferenceDataInput
{
    /// <summary>Pack to apply. Leave null to apply every default pack.</summary>
    public string PackId { get; set; }
}

public class SeedReferenceDataOutput
{
    public List<SeedReferenceDataPackResultDto> Packs { get; set; } = new();

    public int TotalInserted { get; set; }

    public int TotalSkipped { get; set; }
}

public class SeedReferenceDataPackResultDto
{
    public string PackId { get; set; }

    public List<ReferenceDataPackLineDto> Lines { get; set; } = new();

    public int Inserted { get; set; }

    public int Skipped { get; set; }
}
