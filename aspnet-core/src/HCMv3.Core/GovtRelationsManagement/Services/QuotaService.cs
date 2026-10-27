using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings.Services;

namespace HCMv3.GovtRelationsManagement.Services;

/// <inheritdoc cref="IQuotaService"/>
/// <remarks>
/// Ported from legacy XAF <c>HCM10.Module.BusinessObjects.Services.QuotaService</c>. The legacy method
/// created its own <c>XPObjectSpaceProvider</c>/raw connection-string session and manually committed/rolled
/// back a single ObjectSpace transaction, swallowing per-Sponsor/GovtJobTitle exceptions into an
/// accumulated error string it then discarded (dead-end <c>catch</c> at the very end that just resets the
/// string to empty). None of that manual session/transaction/error-swallowing plumbing is ported: this is
/// a normal ABP domain service method using injected <c>IRepository&lt;T&gt;</c> instances, and any failure
/// simply propagates to the caller (App layer callers should wrap this in their own error handling if a
/// "best effort across all sponsors" semantics is desired).
///
/// The legacy <c>SysService.userSysJobAdmin</c> stamping of <c>SysCreator</c>/<c>SysCreatedOn</c>/
/// <c>SysModifier</c>/<c>SysModifiedOn</c> is NOT ported -- this port's entities inherit ABP's own
/// <c>FullAuditedEntity</c> auditing, applied automatically by the ABP auditing interceptor (same rationale
/// documented in <c>AccrualService</c>).
///
/// The legacy <c>Employee.Nationality != null</c> guard is dropped: <see cref="EmployeeSponsor.EmployeeId"/>
/// and <see cref="Employee.NationalityId"/> are both required (non-nullable) FKs in this port, so any
/// resolvable <see cref="Employee"/> always has a Nationality -- the guard is structurally always true here.
///
/// <see cref="UnitOfWorkAttribute"/> at the class level, matching <c>SysSettingsAccessor</c>/<c>AccrualService</c>
/// (this domain service is independently callable, opening its own unit of work when not already inside one).
/// </remarks>
[UnitOfWork]
public class QuotaService : HCMv3DomainServiceBase, IQuotaService
{
    private readonly IRepository<Sponsor> _sponsorRepository;
    private readonly IRepository<GovtJobTitle> _govtJobTitleRepository;
    private readonly IRepository<SponsorJobTitle> _sponsorJobTitleRepository;
    private readonly IRepository<EmployeeSponsor> _employeeSponsorRepository;
    private readonly IRepository<Nationality> _nationalityRepository;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly IAbpSession _abpSession;

    public QuotaService(
        IRepository<Sponsor> sponsorRepository,
        IRepository<GovtJobTitle> govtJobTitleRepository,
        IRepository<SponsorJobTitle> sponsorJobTitleRepository,
        IRepository<EmployeeSponsor> employeeSponsorRepository,
        IRepository<Nationality> nationalityRepository,
        ISysSettingsAccessor sysSettingsAccessor,
        IAbpSession abpSession)
    {
        _sponsorRepository = sponsorRepository;
        _govtJobTitleRepository = govtJobTitleRepository;
        _sponsorJobTitleRepository = sponsorJobTitleRepository;
        _employeeSponsorRepository = employeeSponsorRepository;
        _nationalityRepository = nationalityRepository;
        _sysSettingsAccessor = sysSettingsAccessor;
        _abpSession = abpSession;
    }

    public async Task CalculateSponsorJobTitleQuotaAsync()
    {
        var personnelSettings = await _sysSettingsAccessor.GetPersonnelSettingsAsync();
        if (personnelSettings == null || !personnelSettings.EnableSponsorGovtJobTitleQuota)
        {
            return;
        }

        var today = DateTime.Now.Date;
        var tenantId = _abpSession.GetTenantId();

        var govtJobTitles = _govtJobTitleRepository.GetAll().ToList();
        var sponsors = _sponsorRepository.GetAll().ToList();

        var citizenNationalityIds = _nationalityRepository.GetAll()
            .Where(r => r.NationalityGroup != null && r.NationalityGroup.NameEn == "Citizen")
            .Select(r => r.Id)
            .ToList();

        var nonCitizenNationalityIds = _nationalityRepository.GetAll()
            .Where(r => r.NationalityGroup != null && r.NationalityGroup.NameEn != "Citizen")
            .Select(r => r.Id)
            .ToList();

        foreach (var sponsor in sponsors)
        {
            var empSponsorList = _employeeSponsorRepository.GetAll()
                .Where(r => r.SponsorId == sponsor.Id
                         && r.StartDate <= today
                         && r.EndDate >= today)
                .Select(r => new { r.GovtJobTitleId, r.Employee.NationalityId })
                .ToList();

            foreach (var govtJobTitle in govtJobTitles)
            {
                var sponsorJobTitle = _sponsorJobTitleRepository.GetAll()
                    .FirstOrDefault(r => r.SponsorId == sponsor.Id && r.GovtJobTitleId == govtJobTitle.Id);

                var isNew = sponsorJobTitle == null;
                if (isNew)
                {
                    sponsorJobTitle = new SponsorJobTitle
                    {
                        TenantId = tenantId,
                        SponsorId = sponsor.Id,
                        GovtJobTitleId = govtJobTitle.Id,
                        StartDate = new DateTime(2023, 1, 1),
                        EndDate = new DateTime(9999, 12, 31),
                        NoofPosition = 999,
                        CitizenPercent = 10,
                        CitizenQuotaAccrued = 100,
                        NonCitizenPercent = 90,
                        NonCitizenQuotaAccrued = 899
                    };
                }

                sponsorJobTitle.CitizenQuotaUsed = empSponsorList.Count(r =>
                    r.GovtJobTitleId == govtJobTitle.Id && citizenNationalityIds.Contains(r.NationalityId));

                sponsorJobTitle.NonCitizenQuotaUsed = empSponsorList.Count(r =>
                    r.GovtJobTitleId == govtJobTitle.Id && nonCitizenNationalityIds.Contains(r.NationalityId));

                if (isNew)
                {
                    await _sponsorJobTitleRepository.InsertAsync(sponsorJobTitle);
                }
                else
                {
                    await _sponsorJobTitleRepository.UpdateAsync(sponsorJobTitle);
                }
            }
        }
    }
}
