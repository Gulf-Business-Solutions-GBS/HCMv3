namespace HCMv3.Navigation;

/// <summary>
/// Shape of a screen code (the S-code), e.g. <c>S-PA-EMP</c>.
///
/// Lives in Core.Shared because BOTH the entity in Core and the DTOs in Application.Shared need the
/// length, and Application.Shared may not reference Core -- DTOs there must stay free of domain types.
/// Core.Shared is the layer both may see.
/// </summary>
public static class ScreenCodeConsts
{
    /// <summary>
    /// Generous for a code that is meant to be typed. Long enough that a deep module path never has to
    /// be abbreviated into something unreadable, short enough to stay indexable.
    /// </summary>
    public const int MaxCodeLength = 64;
}
