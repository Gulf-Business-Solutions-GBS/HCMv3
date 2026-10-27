namespace HCMv3.Careers.Dto;

/// <summary>
/// One facet option on the public careers list, with the number of currently-published vacancies behind it.
/// Ported from legacy <c>VacancyController.VacancyFilterObj</c> (Oid/NameEn/Count), which legacy built by
/// grouping the already-published vacancy list -- the same approach is used here, so a facet never reveals
/// a job title or location that has no publicly visible vacancy.
/// </summary>
public class PublicVacancyFilterOptionDto
{
    public int Id { get; set; }

    public string Name { get; set; }

    public int Count { get; set; }
}
