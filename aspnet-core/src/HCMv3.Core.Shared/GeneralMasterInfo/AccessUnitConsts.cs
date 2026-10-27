namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// Field length / validation constants for <c>AccessUnit</c>.
/// Kept in Core.Shared so both the domain layer and DTO validation can reference them.
/// </summary>
public static class AccessUnitConsts
{
    public const int MaxCodeLength = 50;
    public const int MaxNameEnLength = 200;
    public const int MaxNameArLength = 200;
    public const int MaxIPAddressLength = 50;
}
