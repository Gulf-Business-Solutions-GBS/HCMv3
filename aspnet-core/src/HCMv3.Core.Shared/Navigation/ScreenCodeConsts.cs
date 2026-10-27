namespace HCMv3.Navigation;

/// <summary>
/// Field sizes for the screen catalogue.
///
/// Lives in Core.Shared because the entities in Core and the DTOs in Application.Shared both need them,
/// and Application.Shared may not reference Core -- DTOs there must stay free of domain types.
/// </summary>
public static class ScreenCodeConsts
{
    /// <summary>
    /// The Screen Code, e.g. <c>SPA001</c>. Generous for something meant to be typed: the assigned codes
    /// are six characters, and the slack is for a customer who re-maps them to their own vocabulary.
    /// </summary>
    public const int MaxCodeLength = 32;

    /// <summary>
    /// The immutable key, e.g. <c>app.admin.employee-basic-pay-details</c>. Seeded from the route, so it
    /// has to fit the longest route in the application with room to spare.
    /// </summary>
    public const int MaxKeyLength = 128;

    public const int MaxModuleLength = 64;

    /// <summary>Localization key of the menu name, e.g. <c>EmployeeBasicPayDetails</c>.</summary>
    public const int MaxNameKeyLength = 128;

    public const int MaxRouteLength = 256;

    /// <summary>e.g. <c>Pages.PersonnelAdministration.Employees</c>.</summary>
    public const int MaxPermissionLength = 128;
}
