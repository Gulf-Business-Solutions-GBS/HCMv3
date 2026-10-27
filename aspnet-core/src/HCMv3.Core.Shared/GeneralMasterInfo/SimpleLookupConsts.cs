namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// Shared field-length constants for the ~29 pure bilingual lookup entities in GeneralMasterInfo
/// (Code/NameEn/NameAr only), e.g. Country, Title, Religion. Kept separate from
/// <see cref="AccessUnitConsts"/> since AccessUnit has its own extra fields.
/// </summary>
public static class SimpleLookupConsts
{
    public const int MaxCodeLength = 50;
    public const int MaxNameLength = 200;
}
