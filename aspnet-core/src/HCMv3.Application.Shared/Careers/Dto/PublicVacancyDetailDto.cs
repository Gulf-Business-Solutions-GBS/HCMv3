namespace HCMv3.Careers.Dto;

/// <summary>
/// A vacancy as shown on the PUBLIC careers portal detail page. Adds the long-form fields legacy's
/// <c>VacancyController.VacancyDetails</c> rendered on top of the list fields. See
/// <see cref="PublicVacancyListDto"/> for the full list of deliberately excluded internal fields.
/// </summary>
public class PublicVacancyDetailDto : PublicVacancyListDto
{
    public string JobDescription { get; set; }

    public int? QualificationId { get; set; }

    /// <summary>Legacy <c>VacancyViewModel.Qualification</c>; "Unspecified" when not set.</summary>
    public string QualificationName { get; set; }

    public int YearsOfExperience { get; set; }
}
