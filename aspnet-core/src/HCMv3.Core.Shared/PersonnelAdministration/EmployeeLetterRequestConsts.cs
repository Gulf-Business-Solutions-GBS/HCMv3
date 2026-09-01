namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Field limits for <c>EmployeeLetterRequest</c>, in Core.Shared so the entity and the DTOs state the same
/// number rather than two literals that drift apart -- the failure mode being a DTO that accepts 500
/// characters into a column that stores fewer, which surfaces as a database error at save time rather than
/// as a validation message on the form.
/// </summary>
public static class EmployeeLetterRequestConsts
{
    /// <summary>Addressee and comment fields, matching legacy <c>[Size(500)]</c>.</summary>
    public const int MaxTextLength = 500;
}
