using System;
using System.Linq;
using HCMv3.Recruitment;

namespace HCMv3.Careers;

/// <summary>
/// THE single definition of "publicly visible vacancy" for the whole careers portal.
///
/// Extracted verbatim from wave R1's <c>CareersAppService.PublishedVacancies</c> when wave R3a needed the
/// same rule on the apply path. It is deliberately a shared helper rather than a copy: an apply endpoint
/// whose notion of "published" drifts from the browse endpoint's is exactly how a candidate ends up able to
/// apply to a closed or unpublished requisition by posting its id.
///
/// Ported from legacy <c>HCM10.Recruiting/Controllers/VacancyController.GetVacancyList</c>
/// (<c>VacancyController.cs:148-150</c>): <c>VacancyStatus == 0</c> (<see cref="VacancyStatus.Open"/>) AND
/// <c>ClosingDate &gt;= DateTime.Now</c>. See <c>CareersAppService</c> for the two legacy-fidelity notes
/// (legacy's filtered list dropped the closing-date check; legacy never gated on <c>PublishOn</c>).
///
/// The explicit <c>TenantId</c> predicate is part of this method so no caller can forget it -- the public
/// browse surface runs with a null session tenant (= host = <c>MustHaveTenant</c> filter OFF), so the
/// ambient filter is not the isolation guarantee there.
/// </summary>
public static class CareersVacancyQueries
{
    public static IQueryable<Vacancy> Published(IQueryable<Vacancy> query, int tenantId, DateTime now)
    {
        return query
            .Where(v => v.TenantId == tenantId)
            .Where(v => v.VacancyStatus == VacancyStatus.Open)
            .Where(v => v.ClosingDate >= now);
    }
}
