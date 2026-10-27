namespace HCMv3.OrganizationManagement;

/// <summary>Limits for the HR organization unit profile (<c>OrgUnitProfile</c>).</summary>
public static class OrgUnitProfileConsts
{
    /// <summary>Length of <c>OrgUnitProfile.LogoPath</c>, the logo's path in the file store.</summary>
    public const int MaxLogoPathLength = 512;

    /// <summary>
    /// Largest logo accepted, in bytes (1 MB). The logo is printed on reports, so it only needs to be
    /// sharp at letterhead size; ASP.NET Zero's own tenant logo upload stops at 100 KB.
    /// </summary>
    public const int MaxLogoBytes = 1024 * 1024;

    /// <summary>
    /// Largest logo width/height in pixels. Only there so a corrupt or absurd file is refused at upload,
    /// not when a report tries to draw it.
    /// </summary>
    public const int MaxLogoDimension = 4096;
}
