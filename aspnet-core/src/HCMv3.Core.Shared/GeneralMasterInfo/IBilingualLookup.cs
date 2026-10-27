namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// The Code/NameEn/NameAr shape shared by every reference lookup.
///
/// <para>Most of these entities derive from <c>SimpleLookupEntity</c>, but four cannot because they
/// carry extra columns -- <c>Nationality</c> (group FK), <c>City</c> (state FK), <c>Bank</c> (SWIFT and
/// file FK) and <c>Currency</c> (decimal count and short forms). Before this interface the fact that
/// all twelve share a shape was true but invisible to the compiler, so anything wanting to treat them
/// uniformly had to fall back on reflection.</para>
///
/// <para>Purely additive: every implementer already declared these three properties, and it changes no
/// schema.</para>
/// </summary>
public interface IBilingualLookup
{
    string Code { get; set; }

    string NameEn { get; set; }

    string NameAr { get; set; }
}
