using System.Collections.Generic;

namespace HCMv3.GoLiveSetup.ReferenceData;

/// <summary>
/// One shipped set of reference lists, deserialised from an embedded <c>Packs/*.json</c> file.
///
/// <para>Packs are split by scope rather than shipped as one blob so that adding a second country is a
/// new pack rather than a fork: <c>universal</c> carries the lists every tenant needs (countries,
/// nationalities, titles, currencies…), <c>kuwait</c> carries geography, banks and labour-law contract
/// types that only apply there.</para>
/// </summary>
public class ReferenceDataPack
{
    /// <summary>Stable identifier, e.g. <c>universal</c>. Matches the file name.</summary>
    public string Id { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public string Description { get; set; }

    /// <summary>
    /// Entity short name (<c>Country</c>, <c>Nationality</c>…) to its rows. Keyed by name rather than
    /// typed per entity because all eleven targets share the same Code/NameEn/NameAr shape; the seeder
    /// resolves the name to a repository. An unknown key is a hard error, not a silent skip.
    /// </summary>
    public Dictionary<string, List<ReferenceDataEntry>> Lookups { get; set; } = new();

    /// <summary>
    /// Currencies are separate because <c>Currency</c> carries <c>DecimalCount</c> and the short forms,
    /// and getting the decimal count wrong silently mis-rounds every payslip.
    /// </summary>
    public List<ReferenceCurrencyEntry> Currencies { get; set; } = new();

    /// <summary>
    /// The standard payroll catalogue. Wage types carry some thirty engine flags and are matched on
    /// <c>Code</c> rather than name, so they cannot share the three-column lookup shape above.
    /// </summary>
    public List<ReferenceWageTypeEntry> WageTypes { get; set; } = new();

    /// <summary>Leave types, matched on <c>Code</c> for the same reason. Seeded after the wage types they pay through.</summary>
    public List<ReferenceAbsenceTypeEntry> AbsenceTypes { get; set; } = new();
}

/// <summary>A single bilingual lookup row.</summary>
public class ReferenceDataEntry
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }
}

/// <summary>A currency row. <see cref="Code"/> is the ISO 4217 code.</summary>
public class ReferenceCurrencyEntry
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    /// <summary>Fils/cents places. 3 for the Gulf dinars, 2 for most others.</summary>
    public int DecimalCount { get; set; }

    public string ShortFormEn { get; set; }

    public string ShortFormAr { get; set; }
}

/// <summary>What a seeding run did, per list. Returned so the caller can log or show it.</summary>
public class ReferenceDataSeedResult
{
    public string PackId { get; set; }

    public List<ReferenceDataSeedCount> Counts { get; set; } = new();

    public int TotalInserted { get; set; }

    public int TotalSkipped { get; set; }
}

public class ReferenceDataSeedCount
{
    public string Lookup { get; set; }

    public int Inserted { get; set; }

    /// <summary>Rows already present under the same name, left untouched.</summary>
    public int Skipped { get; set; }
}
