namespace HCMv3.Recruitment;

/// <summary>
/// Ported from legacy XAF <c>HCM10.Module.BusinessObjects.Recruitment.EmploymentType</c> (FullTime/PartTime,
/// used by <see cref="Vacancy"/>). Renamed from <c>EmploymentType</c> to <c>VacancyEmploymentType</c> because
/// <c>HCMv3.PersonnelAdministration.EmploymentType</c> (Permanent/Temporary/ALL, a distinct legacy enum with
/// the same bare name) already exists in this codebase -- a duplicate enum name across modules builds fine
/// but breaks NSwag schema generation.
/// </summary>
public enum VacancyEmploymentType
{
    FullTime = 0,
    PartTime = 1
}
