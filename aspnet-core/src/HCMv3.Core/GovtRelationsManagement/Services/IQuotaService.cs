using System.Threading.Tasks;

namespace HCMv3.GovtRelationsManagement.Services;

/// <summary>
/// Ported from legacy XAF <c>HCM10.Module.BusinessObjects.Services.QuotaService</c>.
/// </summary>
public interface IQuotaService
{
    /// <summary>
    /// Ported from legacy <c>QuotaService.CalculateSponsorJobTitleQuota</c>. Recomputes the
    /// citizen/non-citizen "quota used" counters on every <see cref="SponsorJobTitle"/> row (one per
    /// Sponsor x GovtJobTitle combination, created on first run with the same hard-coded defaults as
    /// legacy) from currently-active <see cref="HCMv3.PersonnelAdministration.EmployeeSponsor"/>
    /// assignments. No-op unless <c>ManagePersonnelSettings.EnableSponsorGovtJobTitleQuota</c> is enabled.
    /// </summary>
    Task CalculateSponsorJobTitleQuotaAsync();
}
