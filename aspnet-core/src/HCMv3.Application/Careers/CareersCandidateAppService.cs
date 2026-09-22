using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Organizations;
using Abp.Runtime.Session;
using Abp.Timing;
using Abp.UI;
using HCMv3.Careers.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;
using HCMv3.Recruitment;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Careers;

/// <summary>
/// AUTHENTICATED candidate surface of the careers portal -- wave R3a. Ported from legacy
/// <c>HCM10.Recruiting/Controllers/UserAccountController</c> and the apply path of
/// <c>VacancyController.QuickApplyForJob</c> (<c>VacancyController.cs:572-880</c>).
///
/// SECURITY MODEL -- read this before changing anything here.
///
/// R1's risk was CROSS-TENANT reads and the control there was an explicit <c>TenantId</c> predicate on a
/// tenant-less (anonymous) request. R3a's risk is different and the tenant filter does nothing about it:
/// CROSS-CANDIDATE access WITHIN one tenant. Candidate A and candidate B are both authenticated, both on
/// tenant 5, and ABP's <c>MustHaveTenant</c> filter admits both to every row of tenant 5. Nothing in the
/// framework distinguishes them. So:
///
/// 1. THE APPLICANT IS NEVER TAKEN FROM INPUT. Every method starts at
///    <see cref="GetCurrentCandidateAsync"/>, which resolves the row by
///    <c>Applicant.UserId == AbpSession.UserId AND Applicant.TenantId == AbpSession.TenantId</c>. No DTO on
///    this service carries an applicant id.
/// 2. EVERY CLIENT-SUPPLIED CV ID IS RE-READ UNDER THE OWNER PREDICATE by
///    <see cref="GetOwnedCvAsync"/> (<c>Id == x AND ApplicantId == mine AND TenantId == mine</c>) before it
///    is read, written, deleted or attached to an application. The "belongs to someone else" and "does not
///    exist" cases throw the SAME exception, so the endpoint cannot be used to enumerate which CV ids exist.
/// 3. CV CHILD ROWS ARE NOT CLIENT-ADDRESSABLE. Qualifications/experience/skills/languages are replaced
///    wholesale via their parent CV, so there is no id surface to attack; the child writes are scoped by the
///    already-verified <c>ApplicantCVId</c>.
/// 4. THE SESSION TENANT IS MANDATORY. A host-session caller (<c>AbpSession.TenantId == null</c>) is
///    REJECTED, not treated as "all tenants" -- a null tenant in ABP switches the tenant filter OFF, which
///    on this service would mean every applicant row in the installation.
/// 5. WHEN THE REQUEST ALSO CARRIES A CAREERS SUBDOMAIN, IT MUST AGREE WITH THE TOKEN. Checked via the
///    existing <see cref="ICareersTenantResolver"/> (wave R2) -- this is a cross-check of the session, NOT a
///    second resolution path; the session tenant remains authoritative and a subdomain that resolves to
///    nothing is not a failure (the SPA may legitimately call the API on a non-careers host).
///
/// <c>[AbpAuthorize]</c> is present on every method but is deliberately PERMISSION-FREE: the
/// <c>Candidate</c> role is permissionless by design, so requiring a permission here would lock candidates
/// out of their own data. Authorization here means "authenticated"; authorization to a specific ROW is the
/// ownership predicate above and nothing else.
/// </summary>
[AbpAuthorize]
public class CareersCandidateAppService : HCMv3AppServiceBase, ICareersCandidateAppService
{
    private readonly IRepository<Applicant> _applicantRepository;
    private readonly IRepository<ApplicantCV> _cvRepository;
    private readonly IRepository<CVQualification> _cvQualificationRepository;
    private readonly IRepository<CVWorkExp> _cvWorkExpRepository;
    private readonly IRepository<CVSkill> _cvSkillRepository;
    private readonly IRepository<CVLanguage> _cvLanguageRepository;
    private readonly IRepository<ApplicationStatus> _applicationStatusRepository;
    private readonly IRepository<Vacancy> _vacancyRepository;
    private readonly IRepository<OrganizationUnit, long> _organizationUnitRepository;
    private readonly IRepository<Nationality> _nationalityRepository;
    private readonly IRepository<JobTitle> _jobTitleRepository;
    private readonly IRepository<Qualification> _qualificationRepository;
    private readonly IRepository<LanguageInfo> _languageRepository;
    private readonly ICareersTenantResolver _tenantResolver;

    public CareersCandidateAppService(
        IRepository<Applicant> applicantRepository,
        IRepository<ApplicantCV> cvRepository,
        IRepository<CVQualification> cvQualificationRepository,
        IRepository<CVWorkExp> cvWorkExpRepository,
        IRepository<CVSkill> cvSkillRepository,
        IRepository<CVLanguage> cvLanguageRepository,
        IRepository<ApplicationStatus> applicationStatusRepository,
        IRepository<Vacancy> vacancyRepository,
        IRepository<OrganizationUnit, long> organizationUnitRepository,
        IRepository<Nationality> nationalityRepository,
        IRepository<JobTitle> jobTitleRepository,
        IRepository<Qualification> qualificationRepository,
        IRepository<LanguageInfo> languageRepository,
        ICareersTenantResolver tenantResolver)
    {
        _applicantRepository = applicantRepository;
        _cvRepository = cvRepository;
        _cvQualificationRepository = cvQualificationRepository;
        _cvWorkExpRepository = cvWorkExpRepository;
        _cvSkillRepository = cvSkillRepository;
        _cvLanguageRepository = cvLanguageRepository;
        _applicationStatusRepository = applicationStatusRepository;
        _vacancyRepository = vacancyRepository;
        _organizationUnitRepository = organizationUnitRepository;
        _nationalityRepository = nationalityRepository;
        _jobTitleRepository = jobTitleRepository;
        _qualificationRepository = qualificationRepository;
        _languageRepository = languageRepository;
        _tenantResolver = tenantResolver;
    }

    // =============================================================================================
    // Profile
    // =============================================================================================

    public async Task<CareersProfileDto> GetMyProfile()
    {
        var applicant = await GetCurrentCandidateAsync();
        return await ToProfileDtoAsync(applicant);
    }

    /// <summary>
    /// Ported from legacy <c>UserAccountController.UserProfile</c> POST
    /// (<c>UserAccountController.cs:128-180</c>), minus the email/login rewrite -- see
    /// <see cref="UpdateCareersProfileInput"/>.
    ///
    /// Legacy swallowed every exception and returned <c>false</c> (<c>:175-179</c>), so a candidate whose
    /// save failed got a silent no-op. Here the failure propagates.
    /// </summary>
    public async Task<CareersProfileDto> UpdateMyProfile(UpdateCareersProfileInput input)
    {
        var applicant = await GetCurrentCandidateAsync();

        applicant.FirstNameEn = input.FirstNameEn;
        applicant.SecondNameEn = input.SecondNameEn;
        applicant.ThirdNameEn = input.ThirdNameEn;
        applicant.LastNameEn = input.LastNameEn;
        applicant.FirstNameAr = input.FirstNameAr;
        applicant.SecondNameAr = input.SecondNameAr;
        applicant.ThirdNameAr = input.ThirdNameAr;
        applicant.LastNameAr = input.LastNameAr;
        applicant.NationalityId = input.NationalityId;
        applicant.Gender = input.Gender;
        applicant.BirthDate = input.BirthDate;
        applicant.CivilID = input.CivilID;
        applicant.Mobile = input.Mobile;
        applicant.Telephone = input.Telephone;
        applicant.CurrentAddress = input.CurrentAddress;
        applicant.PermanentAddress = input.PermanentAddress;

        // TenantId and UserId are intentionally NOT assignable from input: the row was fetched by them.
        await _applicantRepository.UpdateAsync(applicant);

        applicant.CompletionPoints = await GetCompletionPointsAsync(applicant);

        return await ToProfileDtoAsync(applicant);
    }

    // =============================================================================================
    // CVs
    // =============================================================================================

    public async Task<ListResultDto<CareersCvListDto>> GetMyCvs()
    {
        var applicant = await GetCurrentCandidateAsync();

        var cvs = await OwnedCvs(applicant).ToListAsync();
        var cvIds = cvs.Select(c => c.Id).ToList();

        var blockedCvIds = await _applicationStatusRepository.GetAll()
            .Where(s => s.TenantId == applicant.TenantId)
            .Where(s => s.ApplicantCVId.HasValue && cvIds.Contains(s.ApplicantCVId.Value))
            .Where(s => s.AppStatus != AppStatus.Applied)
            .Select(s => s.ApplicantCVId.Value)
            .Distinct()
            .ToListAsync();

        var jobTitles = await GetJobTitleNamesAsync(cvs.Select(c => c.JobTitleId));

        var items = cvs
            .OrderByDescending(c => c.UploadDate)
            .Select(c => ToCvListDto(c, jobTitles, blockedCvIds))
            .ToList();

        return new ListResultDto<CareersCvListDto>(items);
    }

    public async Task<CareersCvDetailDto> GetMyCv(EntityDto input)
    {
        var applicant = await GetCurrentCandidateAsync();
        var cv = await GetOwnedCvAsync(applicant, input.Id);

        return await ToCvDetailDtoAsync(applicant, cv);
    }

    /// <summary>
    /// Ported from legacy <c>UserAccountController.UploadCv</c> POST
    /// (<c>UserAccountController.cs:428-655</c>), which created the CV then bulk-inserted the four child
    /// blocks from parallel comma-separated form fields.
    ///
    /// DEVIATION FROM LEGACY (deliberate): legacy zipped those parallel arrays by INDEX with mismatched
    /// guards -- <c>positionList[i]</c> and <c>startDateList[i]</c> were indexed off the COMPANY list's
    /// length with no bounds check (<c>:576-586</c>, an IndexOutOfRange waiting to happen), and
    /// <c>responsibilitiesList</c> was built from the COMPANY field (<c>:571</c>), so every work-experience
    /// line stored the company name as its responsibilities. Typed child DTOs remove the whole class of bug.
    /// </summary>
    public async Task<CareersCvDetailDto> CreateMyCv(CreateCareersCvInput input)
    {
        var applicant = await GetCurrentCandidateAsync();

        await EnsureCvNameIsFreeAsync(applicant, input.CVName, null);

        var cv = await InsertCvAsync(applicant, input.CVName, input.JobTitleId);
        await ReplaceCvChildrenAsync(applicant, cv, input);

        return await ToCvDetailDtoAsync(applicant, cv);
    }

    public async Task<CareersCvDetailDto> UpdateMyCv(UpdateCareersCvInput input)
    {
        var applicant = await GetCurrentCandidateAsync();

        // Ownership gate. input.Id is untrusted.
        var cv = await GetOwnedCvAsync(applicant, input.Id);

        await EnsureCvNameIsFreeAsync(applicant, input.CVName, cv.Id);

        cv.CVName = input.CVName;
        cv.JobTitleId = input.JobTitleId;
        await _cvRepository.UpdateAsync(cv);

        await ReplaceCvChildrenAsync(applicant, cv, input);

        return await ToCvDetailDtoAsync(applicant, cv);
    }

    /// <summary>
    /// Ported from legacy <c>UserAccountController.DeleteCv</c> (<c>UserAccountController.cs:656-717</c>),
    /// including its rule: a CV may be deleted while it has no application against it or while every
    /// application against it is still <see cref="AppStatus.Applied"/>; once one has progressed the delete
    /// is refused ("CV cannot be deleted as it is already applied!", <c>:678</c>).
    ///
    /// TWO DEVIATIONS FROM LEGACY, both defects there:
    /// * Legacy took the CV oid straight off the request and deleted it WITHOUT ANY OWNERSHIP CHECK
    ///   (<c>:665-673</c>) -- any logged-in candidate could delete any other candidate's CV by posting its
    ///   id. That is precisely the hole this wave exists to close; here <see cref="GetOwnedCvAsync"/> runs
    ///   first.
    /// * Legacy's <c>DeleteCvReferences</c> (<c>:906-935</c>) deleted only the FIRST child row of each type
    ///   (<c>FirstOrDefault(...).Delete()</c>), orphaning the rest. All children are removed here.
    /// </summary>
    public async Task DeleteMyCv(EntityDto input)
    {
        var applicant = await GetCurrentCandidateAsync();
        var cv = await GetOwnedCvAsync(applicant, input.Id);

        var hasProgressedApplication = await _applicationStatusRepository.GetAll()
            .Where(s => s.TenantId == applicant.TenantId)
            .Where(s => s.ApplicantCVId == cv.Id)
            .AnyAsync(s => s.AppStatus != AppStatus.Applied);

        if (hasProgressedApplication)
        {
            throw new UserFriendlyException(L("CareersCvCannotBeDeletedAfterApplying"));
        }

        await DeleteCvChildrenAsync(cv.Id);

        // The applications themselves are recruitment records; they are left alone (ApplicantCVId is
        // nullable and the CV is soft-deleted, so nothing dangles).
        await _cvRepository.DeleteAsync(cv);
    }

    // =============================================================================================
    // Apply / track
    // =============================================================================================

    /// <summary>
    /// Ported from legacy <c>VacancyController.QuickApplyForJob</c> (<c>VacancyController.cs:572-880</c>).
    ///
    /// WHAT LEGACY DID AND WHY THIS DIFFERS:
    /// * Legacy's apply was an ANONYMOUS form post that also REGISTERED the candidate (creating
    ///   <c>Applicant</c> with <c>Password = FirstName[0] + "@1234"</c> and mailing it in cleartext,
    ///   <c>:685</c>/<c>:779</c>). Registration is wave R2 and is not repeated here; this endpoint is for a
    ///   candidate who is already signed in.
    /// * Legacy REFUSED THE APPLY OUTRIGHT if the applicant already existed ("Applicant ... already exist!",
    ///   <c>:868-871</c>). Taken literally that means a returning candidate could never apply to anything --
    ///   the only path through legacy's apply was the first-ever one. That is not portable behaviour, so the
    ///   duplicate rule is reinterpreted at the level it was obviously meant for: ONE APPLICATION PER
    ///   CANDIDATE PER VACANCY. A second apply to the same vacancy is refused; applying to a different
    ///   vacancy is allowed. FLAGGED as this wave's most significant judgement call.
    /// * Legacy did NOT check that the vacancy was still published -- it parsed the posted oid and used it
    ///   (<c>:747-749</c>). Here the vacancy must satisfy the SAME publication predicate the public browse
    ///   surface uses (<see cref="CareersVacancyQueries.Published"/>), so a closed/expired/other-tenant
    ///   vacancy id cannot be applied to. A miss is reported as "no longer available" -- identical to R1's
    ///   detail endpoint, so this is not a probe for unpublished requisitions either.
    /// * Legacy required a .pdf/.doc/.docx upload (<c>:628-646</c>). There is nowhere to put it --
    ///   <c>ApplicantCV</c> has no attachment column in HCMv3 (see the entity remarks). Reported as a gap;
    ///   NOT faked.
    /// * Legacy's two notification emails (candidate confirmation + management alert, <c>:761-845</c>) are
    ///   not ported: they depend on <c>EmailTemplate</c>/<c>NotificationConfig</c>/<c>NotificationGroup</c>/
    ///   <c>EmployeeWorkflowRole</c>, and the candidate one existed largely to deliver the generated
    ///   plaintext password that no longer exists. Reported.
    /// </summary>
    public async Task<CareersApplyOutput> ApplyToVacancy(CareersApplyInput input)
    {
        var applicant = await GetCurrentCandidateAsync();

        var vacancy = await CareersVacancyQueries
            .Published(_vacancyRepository.GetAll(), applicant.TenantId, Clock.Now)
            .FirstOrDefaultAsync(v => v.Id == input.VacancyId);

        if (vacancy == null)
        {
            throw new UserFriendlyException(L("CareersVacancyNotFound"));
        }

        var ownedCvIds = await OwnedCvs(applicant).Select(c => c.Id).ToListAsync();

        var alreadyApplied = await _applicationStatusRepository.GetAll()
            .Where(s => s.TenantId == applicant.TenantId)
            .Where(s => s.VacancyId == vacancy.Id)
            .AnyAsync(s => s.ApplicantCVId.HasValue && ownedCvIds.Contains(s.ApplicantCVId.Value));

        if (alreadyApplied)
        {
            throw new UserFriendlyException(L("CareersAlreadyAppliedToThisVacancy"));
        }

        ApplicantCV cv;
        if (input.ApplicantCvId.HasValue)
        {
            // Ownership gate. Applying with SOMEONE ELSE'S CV would attach their qualifications, history and
            // contact details to this candidate's application.
            cv = await GetOwnedCvAsync(applicant, input.ApplicantCvId.Value);
        }
        else
        {
            // Legacy quick-apply's placeholder CV name (VacancyController.cs:700).
            var generatedName = (applicant.Email ?? applicant.Id.ToString()) + "_" + Clock.Now.Ticks;
            cv = await InsertCvAsync(applicant, generatedName, null);
        }

        var application = new ApplicationStatus
        {
            TenantId = applicant.TenantId,
            VacancyId = vacancy.Id,
            ApplicantCVId = cv.Id,
            AppStatus = AppStatus.Applied,
            CoverLetter = input.CoverLetter ?? string.Empty
        };

        var applicationId = await _applicationStatusRepository.InsertAndGetIdAsync(application);

        return new CareersApplyOutput
        {
            ApplicationId = applicationId,
            ApplicantCvId = cv.Id,
            AppStatus = AppStatus.Applied
        };
    }

    /// <summary>
    /// Ported from legacy <c>UserAccountController.ApplicationStatus</c>
    /// (<c>UserAccountController.cs:828-905</c>). Read-only.
    ///
    /// Legacy matched on <c>a.ApplicantCV.Applicant.UserName == Session["UserName"]</c> -- a string compare
    /// against the legacy credential column. Here the filter is the caller's own CV ids, derived from the
    /// applicant row that was itself resolved from the session user id.
    ///
    /// DEVIATION: legacy walked the org-unit parent chain to display a "Company" alongside "Department"
    /// (<c>:849-886</c>) using a second, non-XAF <c>RecruitingEntities</c> DbContext and an unbounded
    /// <c>while(true)</c> loop with <c>.Single()</c> calls inside it. Only the vacancy's own org unit is
    /// surfaced here (<c>DepartmentName</c>); the company roll-up is not ported.
    /// </summary>
    public async Task<ListResultDto<CareersApplicationDto>> GetMyApplications()
    {
        var applicant = await GetCurrentCandidateAsync();

        var cvs = await OwnedCvs(applicant).ToListAsync();
        var cvNames = cvs.ToDictionary(c => c.Id, c => c.CVName);
        var cvIds = cvs.Select(c => c.Id).ToList();

        if (cvIds.Count == 0)
        {
            return new ListResultDto<CareersApplicationDto>(new List<CareersApplicationDto>());
        }

        var applications = await _applicationStatusRepository.GetAll()
            .Where(s => s.TenantId == applicant.TenantId)
            .Where(s => s.ApplicantCVId.HasValue && cvIds.Contains(s.ApplicantCVId.Value))
            .Include(s => s.Vacancy)
            .ToListAsync();

        var jobTitles = await GetJobTitleNamesAsync(applications.Select(a => a.Vacancy?.JobTitleId));
        var departments = await GetDepartmentNamesAsync(applications.Select(a => a.Vacancy?.OrganizationUnitId));

        var items = applications
            .OrderByDescending(a => a.CreationTime)
            .Select(a => new CareersApplicationDto
            {
                Id = a.Id,
                VacancyId = a.VacancyId,
                VacancyTitle = a.Vacancy?.VacancyTitle,
                JobTitleId = a.Vacancy?.JobTitleId,
                JobTitleName = Lookup(jobTitles, a.Vacancy?.JobTitleId),
                OrganizationUnitId = a.Vacancy?.OrganizationUnitId,
                DepartmentName = a.Vacancy?.OrganizationUnitId != null &&
                                 departments.TryGetValue(a.Vacancy.OrganizationUnitId.Value, out var department)
                    ? department
                    : null,
                ApplicantCvId = a.ApplicantCVId,
                CVName = a.ApplicantCVId.HasValue && cvNames.TryGetValue(a.ApplicantCVId.Value, out var cvName)
                    ? cvName
                    : null,
                AppStatus = a.AppStatus,
                AppliedOn = a.CreationTime,
                ClosingDate = a.Vacancy?.ClosingDate ?? default
            })
            .ToList();

        return new ListResultDto<CareersApplicationDto>(items);
    }

    // =============================================================================================
    // Ownership -- the whole security surface of this service lives in these two methods
    // =============================================================================================

    /// <summary>
    /// Resolves the applicant row for the CALLING user. This is the ONLY way an applicant is identified on
    /// this service.
    ///
    /// Rejects a host session outright (see class remark 4) and cross-checks the careers subdomain against
    /// the token's tenant when one is present (remark 5). The <c>TenantId</c> predicate is stated
    /// explicitly even though the ambient filter would also apply it -- the same belt-and-braces R1 uses,
    /// so a future change to filter handling cannot silently widen this query.
    /// </summary>
    private async Task<Applicant> GetCurrentCandidateAsync()
    {
        var tenantId = AbpSession.TenantId;
        if (tenantId == null)
        {
            // A host user has no careers identity. Falling through would run every query below with the
            // MustHaveTenant filter DISABLED, i.e. across all tenants.
            throw new UserFriendlyException(L("CareersTenantNotResolved"));
        }

        var subdomainTenantId = await _tenantResolver.ResolveTenantIdOrNullAsync();
        if (subdomainTenantId != null && subdomainTenantId.Value != tenantId.Value)
        {
            // A valid token for tenant B replayed against tenant A's careers host.
            throw new UserFriendlyException(L("CareersTenantNotResolved"));
        }

        var userId = AbpSession.GetUserId();

        var applicant = await _applicantRepository.GetAll()
            .Where(a => a.TenantId == tenantId.Value)
            .Where(a => a.UserId == userId)
            .FirstOrDefaultAsync();

        if (applicant == null)
        {
            // Reached by a signed-in NON-candidate (staff) user, or a candidate whose applicant row was
            // removed. Either way there is no profile to serve and no fallback to "the first applicant".
            throw new UserFriendlyException(L("CareersApplicantProfileNotFound"));
        }

        return applicant;
    }

    private IQueryable<ApplicantCV> OwnedCvs(Applicant applicant)
    {
        return _cvRepository.GetAll()
            .Where(c => c.TenantId == applicant.TenantId)
            .Where(c => c.ApplicantId == applicant.Id);
    }

    /// <summary>
    /// Re-reads a client-supplied CV id under the owner predicate. "Not yours" and "does not exist" throw
    /// the SAME message on purpose: a distinct "forbidden" would confirm the row exists and turn id
    /// enumeration into a census of the tenant's CVs.
    /// </summary>
    private async Task<ApplicantCV> GetOwnedCvAsync(Applicant applicant, int cvId)
    {
        var cv = await OwnedCvs(applicant).FirstOrDefaultAsync(c => c.Id == cvId);
        if (cv == null)
        {
            throw new UserFriendlyException(L("CareersCvNotFound"));
        }

        return cv;
    }

    // =============================================================================================
    // Write helpers
    // =============================================================================================

    private async Task EnsureCvNameIsFreeAsync(Applicant applicant, string cvName, int? excludeCvId)
    {
        // Legacy: "CV Title is duplicated" (UserAccountController.cs:650). Scoped to the applicant, as there.
        var query = OwnedCvs(applicant).Where(c => c.CVName == cvName);
        if (excludeCvId.HasValue)
        {
            query = query.Where(c => c.Id != excludeCvId.Value);
        }

        if (await query.AnyAsync())
        {
            throw new UserFriendlyException(L("CareersDuplicateCvName"));
        }
    }

    private async Task<ApplicantCV> InsertCvAsync(Applicant applicant, string cvName, int? jobTitleId)
    {
        var cv = new ApplicantCV
        {
            TenantId = applicant.TenantId,
            ApplicantId = applicant.Id,
            CVName = cvName,
            JobTitleId = jobTitleId,
            UploadDate = Clock.Now
        };

        cv.Id = await _cvRepository.InsertAndGetIdAsync(cv);
        return cv;
    }

    /// <summary>
    /// Replaces the CV's child lines. <paramref name="cv"/> has ALREADY been ownership-checked by the
    /// caller; every child row written here inherits that CV's id and the applicant's tenant, so no child
    /// can be attached to a CV the caller does not own.
    /// </summary>
    private async Task ReplaceCvChildrenAsync(Applicant applicant, ApplicantCV cv, CreateCareersCvInput input)
    {
        await DeleteCvChildrenAsync(cv.Id);

        foreach (var qualification in input.Qualifications ?? new List<CareersCvQualificationDto>())
        {
            await _cvQualificationRepository.InsertAsync(new CVQualification
            {
                TenantId = applicant.TenantId,
                ApplicantCVId = cv.Id,
                QualificationId = qualification.QualificationId,
                University = qualification.University,
                Percentage = qualification.Percentage,
                PassedYear = qualification.PassedYear
            });
        }

        foreach (var experience in input.WorkExperiences ?? new List<CareersCvWorkExperienceDto>())
        {
            await _cvWorkExpRepository.InsertAsync(new CVWorkExp
            {
                TenantId = applicant.TenantId,
                ApplicantCVId = cv.Id,
                Company = experience.Company,
                Position = experience.Position,
                StartDate = experience.StartDate,
                EndDate = experience.EndDate,
                Responsibilities = experience.Responsibilities
            });
        }

        foreach (var skill in input.Skills ?? new List<CareersCvSkillDto>())
        {
            await _cvSkillRepository.InsertAsync(new CVSkill
            {
                TenantId = applicant.TenantId,
                ApplicantCVId = cv.Id,
                Skill = skill.Skill,
                Proficiency = skill.Proficiency
            });
        }

        foreach (var language in input.Languages ?? new List<CareersCvLanguageDto>())
        {
            await _cvLanguageRepository.InsertAsync(new CVLanguage
            {
                TenantId = applicant.TenantId,
                ApplicantCVId = cv.Id,
                LanguageInfoId = language.LanguageInfoId,
                Proficiency = language.Proficiency
            });
        }

        await CurrentUnitOfWork.SaveChangesAsync();
    }

    private async Task DeleteCvChildrenAsync(int cvId)
    {
        await _cvQualificationRepository.DeleteAsync(c => c.ApplicantCVId == cvId);
        await _cvWorkExpRepository.DeleteAsync(c => c.ApplicantCVId == cvId);
        await _cvSkillRepository.DeleteAsync(c => c.ApplicantCVId == cvId);
        await _cvLanguageRepository.DeleteAsync(c => c.ApplicantCVId == cvId);

        await CurrentUnitOfWork.SaveChangesAsync();
    }

    // =============================================================================================
    // Projection helpers
    // =============================================================================================

    private async Task<CareersProfileDto> ToProfileDtoAsync(Applicant applicant)
    {
        var nationalityNames = await GetNationalityNamesAsync(new[] { applicant.NationalityId });
        var jobTitleNames = await GetJobTitleNamesAsync(new[] { applicant.JobTitleId });

        return new CareersProfileDto
        {
            Id = applicant.Id,
            FirstNameEn = applicant.FirstNameEn,
            SecondNameEn = applicant.SecondNameEn,
            ThirdNameEn = applicant.ThirdNameEn,
            LastNameEn = applicant.LastNameEn,
            FirstNameAr = applicant.FirstNameAr,
            SecondNameAr = applicant.SecondNameAr,
            ThirdNameAr = applicant.ThirdNameAr,
            LastNameAr = applicant.LastNameAr,
            FullNameEn = BuildFullName(
                applicant.FirstNameEn, applicant.SecondNameEn, applicant.ThirdNameEn, applicant.LastNameEn),
            Email = applicant.Email,
            NationalityId = applicant.NationalityId,
            NationalityName = Lookup(nationalityNames, applicant.NationalityId),
            JobTitleId = applicant.JobTitleId,
            JobTitleName = Lookup(jobTitleNames, applicant.JobTitleId),
            Gender = applicant.Gender,
            BirthDate = applicant.BirthDate,
            CivilID = applicant.CivilID,
            Mobile = applicant.Mobile,
            Telephone = applicant.Telephone,
            CurrentAddress = applicant.CurrentAddress,
            PermanentAddress = applicant.PermanentAddress,
            HasPhoto = applicant.PhotoPath != null || applicant.LegacyPhoto != null,
            ProfileCompletionPercent = ToPercent(await GetCompletionPointsAsync(applicant))
        };
    }

    private static CareersCvListDto ToCvListDto(
        ApplicantCV cv,
        Dictionary<int, string> jobTitleNames,
        List<int> blockedCvIds)
    {
        return new CareersCvListDto
        {
            Id = cv.Id,
            CVName = cv.CVName,
            JobTitleId = cv.JobTitleId,
            JobTitleName = Lookup(jobTitleNames, cv.JobTitleId),
            UploadDate = cv.UploadDate,
            CanBeDeleted = !blockedCvIds.Contains(cv.Id)
        };
    }

    private async Task<CareersCvDetailDto> ToCvDetailDtoAsync(Applicant applicant, ApplicantCV cv)
    {
        var qualifications = await _cvQualificationRepository.GetAllListAsync(c => c.ApplicantCVId == cv.Id);
        var workExperiences = await _cvWorkExpRepository.GetAllListAsync(c => c.ApplicantCVId == cv.Id);
        var skills = await _cvSkillRepository.GetAllListAsync(c => c.ApplicantCVId == cv.Id);
        var languages = await _cvLanguageRepository.GetAllListAsync(c => c.ApplicantCVId == cv.Id);

        var jobTitleNames = await GetJobTitleNamesAsync(new[] { cv.JobTitleId });
        var qualificationNames = await GetQualificationNamesAsync(qualifications.Select(q => q.QualificationId));
        var languageNames = await GetLanguageNamesAsync(languages.Select(l => l.LanguageInfoId));

        var hasProgressedApplication = await _applicationStatusRepository.GetAll()
            .Where(s => s.TenantId == applicant.TenantId)
            .Where(s => s.ApplicantCVId == cv.Id)
            .AnyAsync(s => s.AppStatus != AppStatus.Applied);

        return new CareersCvDetailDto
        {
            Id = cv.Id,
            CVName = cv.CVName,
            JobTitleId = cv.JobTitleId,
            JobTitleName = Lookup(jobTitleNames, cv.JobTitleId),
            UploadDate = cv.UploadDate,
            CanBeDeleted = !hasProgressedApplication,
            Qualifications = qualifications.Select(q => new CareersCvQualificationDto
            {
                QualificationId = q.QualificationId,
                QualificationName = Lookup(qualificationNames, q.QualificationId),
                University = q.University,
                Percentage = q.Percentage,
                PassedYear = q.PassedYear
            }).ToList(),
            WorkExperiences = workExperiences.Select(w => new CareersCvWorkExperienceDto
            {
                Company = w.Company,
                Position = w.Position,
                StartDate = w.StartDate,
                EndDate = w.EndDate,
                Responsibilities = w.Responsibilities
            }).ToList(),
            Skills = skills.Select(s => new CareersCvSkillDto
            {
                Skill = s.Skill,
                Proficiency = s.Proficiency
            }).ToList(),
            Languages = languages.Select(l => new CareersCvLanguageDto
            {
                LanguageInfoId = l.LanguageInfoId,
                LanguageName = Lookup(languageNames, l.LanguageInfoId),
                Proficiency = l.Proficiency
            }).ToList()
        };
    }

    /// <summary>
    /// Legacy <c>UserAccountController.GetProfileCompletionPoints</c> (<c>:1024-1215</c>): eight checks
    /// worth 60 points in total -- Photo 5, CivilID 5, Nationality 5, PermanentAddress 5, BirthDate 5,
    /// Email 5, Mobile 5 and "has at least one CV" 25.
    ///
    /// DEVIATION: legacy scored BirthDate as complete for anything other than <c>default(DateTime)</c>.
    /// R2's registration stores the legacy "unknown" sentinel 1950-01-01 for every new candidate
    /// (<c>CareersAccountAppService.UnknownBirthDate</c>), which would score every fresh profile as having
    /// a birth date it does not have. The sentinel counts as incomplete here.
    /// </summary>
    private async Task<int> GetCompletionPointsAsync(Applicant applicant)
    {
        var points = 0;

        if (applicant.PhotoPath != null || applicant.LegacyPhoto != null) points += 5;
        if (applicant.CivilID != 0) points += 5;
        if (applicant.NationalityId.HasValue) points += 5;
        if (!string.IsNullOrEmpty(applicant.PermanentAddress)) points += 5;
        if (applicant.BirthDate != default && applicant.BirthDate != CareersAccountAppService.UnknownBirthDate) points += 5;
        if (!string.IsNullOrEmpty(applicant.Email)) points += 5;
        if (applicant.Mobile != 0) points += 5;
        if (await OwnedCvs(applicant).AnyAsync()) points += 25;

        return points;
    }

    private const int TotalCompletionPoints = 60;

    private static int ToPercent(int points)
    {
        return (int)Math.Floor(100m / TotalCompletionPoints * points);
    }

    private static string BuildFullName(params string[] parts)
    {
        return string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    private static string Lookup(Dictionary<int, string> names, int? id)
    {
        return id.HasValue && names.TryGetValue(id.Value, out var name) ? name : null;
    }

    private async Task<Dictionary<int, string>> GetJobTitleNamesAsync(IEnumerable<int?> ids)
    {
        return await GetNamesAsync(_jobTitleRepository, ids, e => e.NameEn);
    }

    private async Task<Dictionary<int, string>> GetNationalityNamesAsync(IEnumerable<int?> ids)
    {
        return await GetNamesAsync(_nationalityRepository, ids, e => e.NameEn);
    }

    private async Task<Dictionary<int, string>> GetQualificationNamesAsync(IEnumerable<int?> ids)
    {
        return await GetNamesAsync(_qualificationRepository, ids, e => e.NameEn);
    }

    private async Task<Dictionary<int, string>> GetLanguageNamesAsync(IEnumerable<int?> ids)
    {
        return await GetNamesAsync(_languageRepository, ids, e => e.NameEn);
    }

    private static async Task<Dictionary<int, string>> GetNamesAsync<TEntity>(
        IRepository<TEntity> repository,
        IEnumerable<int?> ids,
        Func<TEntity, string> nameSelector)
        where TEntity : class, Abp.Domain.Entities.IEntity<int>
    {
        var distinctIds = ids.Where(id => id.HasValue).Select(id => id.Value).Distinct().ToList();
        if (distinctIds.Count == 0)
        {
            return new Dictionary<int, string>();
        }

        var rows = await repository.GetAllListAsync(e => distinctIds.Contains(e.Id));
        return rows.GroupBy(e => e.Id).ToDictionary(g => g.Key, g => nameSelector(g.First()));
    }

    private async Task<Dictionary<long, string>> GetDepartmentNamesAsync(IEnumerable<long?> organizationUnitIds)
    {
        var ids = organizationUnitIds.Where(id => id.HasValue).Select(id => id.Value).Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<long, string>();
        }

        var units = await _organizationUnitRepository.GetAllListAsync(ou => ids.Contains(ou.Id));
        return units.GroupBy(ou => ou.Id).ToDictionary(g => g.Key, g => g.First().DisplayName);
    }
}
