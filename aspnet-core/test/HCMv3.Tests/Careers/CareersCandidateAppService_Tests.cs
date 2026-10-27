using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Authorization.Roles;
using Abp.Timing;
using Abp.UI;
using Castle.MicroKernel.Registration;
using HCMv3.Authorization.Roles;
using HCMv3.Careers;
using HCMv3.Careers.Dto;
using HCMv3.MultiTenancy;
using HCMv3.Recruitment;
using HCMv3.Recruitment.Dto;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Careers;

/// <summary>
/// Tests for the AUTHENTICATED candidate surface (wave R3a).
///
/// HARNESS: R2's, not R1's -- the real ABP container and the real <c>HCMv3DbContext</c>. These endpoints
/// WRITE (applications, CVs, profile edits) and the claim under test is "row X ended up owned by candidate A
/// and candidate B cannot touch it". Fakes would prove only that the fakes behave. Every assertion reads the
/// actual table back with <c>IgnoreQueryFilters()</c> so a row written to the wrong applicant or the wrong
/// tenant cannot hide behind a filter.
///
/// THE CENTRAL RISK THIS FILE EXISTS TO COVER: R1's risk was cross-TENANT. R3a's is cross-CANDIDATE INSIDE
/// ONE TENANT, where the <c>MustHaveTenant</c> filter is no protection at all -- candidates A and B are both
/// legitimately in tenant A and the filter admits both to every row. So the id-enumeration tests below
/// (candidate A posting candidate B's real, existing CV id) are the load-bearing ones.
///
/// Candidates:
///   A, B -- tenant A (the seeded "Default" tenant). Same tenant on purpose.
///   C    -- tenant B. Covers the cross-tenant case still holding.
/// </summary>
// ReSharper disable once InconsistentNaming
public class CareersCandidateAppService_Tests : AppTestBase
{
    private const string TenantBTenancyName = "initech";

    private readonly ICareersTenantResolver _tenantResolver = Substitute.For<ICareersTenantResolver>();

    private readonly ICareersCandidateAppService _candidateAppService;

    private readonly int _tenantA;
    private readonly int _tenantB;

    private readonly Candidate _candidateA;
    private readonly Candidate _candidateB;
    private readonly Candidate _candidateC;

    private sealed class Candidate
    {
        public int TenantId { get; set; }
        public long UserId { get; set; }
        public int ApplicantId { get; set; }
    }

    public CareersCandidateAppService_Tests()
    {
        _candidateAppService = Resolve<ICareersCandidateAppService>();

        _tenantA = GetTenantIdByTenancyName("Default");
        _tenantB = CreateSecondTenant();

        _candidateA = CreateCandidate(_tenantA, "a@example.com");
        _candidateB = CreateCandidate(_tenantA, "b@example.com");
        _candidateC = CreateCandidate(_tenantB, "c@example.com");

        // Default: no careers subdomain on the request (the API may legitimately be called on a non-careers
        // host). The service must then trust the token's tenant. Individual tests override this.
        _tenantResolver.ResolveTenantIdOrNullAsync().Returns(Task.FromResult((int?)null));
    }

    protected override void PreInitialize()
    {
        base.PreInitialize();

        LocalIocManager.IocContainer.Register(
            Component.For<ICareersTenantResolver>().Instance(_tenantResolver).IsDefault()
        );
    }

    // ---------------------------------------------------------------------------------------------
    // Harness helpers
    // ---------------------------------------------------------------------------------------------

    private int GetTenantIdByTenancyName(string tenancyName)
    {
        return UsingDbContext(null, ctx => ctx.Tenants.IgnoreQueryFilters()
            .Single(t => t.TenancyName == tenancyName).Id);
    }

    private int CreateSecondTenant()
    {
        return UsingDbContext(null, ctx =>
        {
            var existing = ctx.Tenants.IgnoreQueryFilters()
                .FirstOrDefault(t => t.TenancyName == TenantBTenancyName);
            if (existing != null)
            {
                return existing.Id;
            }

            var tenant = ctx.Tenants.Add(new Tenant(TenantBTenancyName, "Initech") { IsActive = true }).Entity;
            ctx.SaveChanges();
            return tenant.Id;
        });
    }

    /// <summary>
    /// Creates an ABP user in the <c>Candidate</c> role plus the linked <see cref="Applicant"/> row, exactly
    /// as wave R2's registration would leave things.
    /// </summary>
    private Candidate CreateCandidate(int tenantId, string email)
    {
        return UsingDbContext(null, ctx =>
        {
            var user = ctx.Users.Add(new HCMv3.Authorization.Users.User
            {
                TenantId = tenantId,
                UserName = email,
                EmailAddress = email,
                Name = "Cand",
                Surname = "Idate",
                IsActive = true,
                IsEmailConfirmed = true,
                Password = "hashed"
            }).Entity;
            user.SetNormalizedNames();
            ctx.SaveChanges();

            var applicant = ctx.Applicants.Add(new Applicant
            {
                TenantId = tenantId,
                UserId = user.Id,
                Email = email,
                FirstNameEn = "Cand",
                LastNameEn = "Idate",
                FirstNameAr = "مرشح",
                LastNameAr = "مرشح",
                BirthDate = CareersAccountAppService.UnknownBirthDate
            }).Entity;
            ctx.SaveChanges();

            return new Candidate
            {
                TenantId = tenantId,
                UserId = user.Id,
                ApplicantId = applicant.Id
            };
        });
    }

    private void LoginAs(Candidate candidate)
    {
        AbpSession.TenantId = candidate.TenantId;
        AbpSession.UserId = candidate.UserId;
    }

    /// <summary>A vacancy that satisfies R1's publication predicate (Open + not yet closed).</summary>
    private int CreateOpenVacancy(int tenantId, string title = "Backend Engineer")
    {
        return UsingDbContext(null, ctx =>
        {
            var vacancy = ctx.Vacancies.Add(new Vacancy
            {
                TenantId = tenantId,
                VacancyTitle = title,
                VacancyStatus = VacancyStatus.Open,
                PublishOn = Clock.Now.AddDays(-1),
                ClosingDate = Clock.Now.AddDays(30)
            }).Entity;
            ctx.SaveChanges();
            return vacancy.Id;
        });
    }

    private int CreateClosedVacancy(int tenantId)
    {
        return UsingDbContext(null, ctx =>
        {
            var vacancy = ctx.Vacancies.Add(new Vacancy
            {
                TenantId = tenantId,
                VacancyTitle = "Filled Already",
                // Any status other than Open fails R1's publication predicate.
                VacancyStatus = VacancyStatus.Dropped,
                PublishOn = Clock.Now.AddDays(-30),
                ClosingDate = Clock.Now.AddDays(30)
            }).Entity;
            ctx.SaveChanges();
            return vacancy.Id;
        });
    }

    private int CreateExpiredVacancy(int tenantId)
    {
        return UsingDbContext(null, ctx =>
        {
            var vacancy = ctx.Vacancies.Add(new Vacancy
            {
                TenantId = tenantId,
                VacancyTitle = "Expired",
                VacancyStatus = VacancyStatus.Open,
                PublishOn = Clock.Now.AddDays(-90),
                ClosingDate = Clock.Now.AddDays(-1)
            }).Entity;
            ctx.SaveChanges();
            return vacancy.Id;
        });
    }

    private async Task<int> CreateCvForAsync(Candidate candidate, string name)
    {
        LoginAs(candidate);
        var cv = await _candidateAppService.CreateMyCv(new CreateCareersCvInput { CVName = name });
        return cv.Id;
    }

    private static UpdateCareersProfileInput NewProfileInput(string firstNameEn = "Updated")
    {
        return new UpdateCareersProfileInput
        {
            FirstNameEn = firstNameEn,
            LastNameEn = "Person",
            FirstNameAr = "شخص",
            LastNameAr = "شخص",
            Gender = HCMv3.GeneralMasterInfo.Gender.Female,
            BirthDate = new DateTime(1990, 5, 4),
            CivilID = 123456789,
            Mobile = 55512345,
            PermanentAddress = "1 Test Street"
        };
    }

    // =============================================================================================
    // OWNERSHIP -- cross-candidate, same tenant. The sharp edge of this wave.
    // =============================================================================================

    [Fact]
    public async Task Should_Resolve_The_Profile_From_The_Callers_Own_UserId()
    {
        LoginAs(_candidateA);
        var profileA = await _candidateAppService.GetMyProfile();

        LoginAs(_candidateB);
        var profileB = await _candidateAppService.GetMyProfile();

        profileA.Id.ShouldBe(_candidateA.ApplicantId);
        profileB.Id.ShouldBe(_candidateB.ApplicantId);
        profileA.Id.ShouldNotBe(profileB.Id);

        // Same tenant -- so nothing about the tenant filter separated these two. Only the UserId predicate
        // did.
        _candidateA.TenantId.ShouldBe(_candidateB.TenantId);
    }

    [Fact]
    public async Task Should_Not_Let_A_Candidate_Read_Another_Candidates_Cv_By_Id()
    {
        var cvOfB = await CreateCvForAsync(_candidateB, "B private CV");

        LoginAs(_candidateA);

        // Direct id enumeration: a real, existing CV id belonging to another candidate of the SAME tenant.
        await Should.ThrowAsync<UserFriendlyException>(
            () => _candidateAppService.GetMyCv(new EntityDto(cvOfB)));
    }

    [Fact]
    public async Task Should_Not_Let_A_Candidate_Update_Another_Candidates_Cv_By_Id()
    {
        var cvOfB = await CreateCvForAsync(_candidateB, "B original title");

        LoginAs(_candidateA);

        await Should.ThrowAsync<UserFriendlyException>(
            () => _candidateAppService.UpdateMyCv(new UpdateCareersCvInput
            {
                Id = cvOfB,
                CVName = "Hijacked",
                Skills = { new CareersCvSkillDto { Skill = "injected" } }
            }));

        // Nothing was written -- not the name, and not a child row smuggled in under someone else's CV.
        var stored = UsingDbContext(null, ctx => ctx.ApplicantCVs.IgnoreQueryFilters().Single(c => c.Id == cvOfB));
        stored.CVName.ShouldBe("B original title");
        stored.ApplicantId.ShouldBe(_candidateB.ApplicantId);

        var injectedSkills = UsingDbContext(null, ctx => ctx.CVSkills.IgnoreQueryFilters()
            .Count(s => s.ApplicantCVId == cvOfB));
        injectedSkills.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Not_Let_A_Candidate_Delete_Another_Candidates_Cv_By_Id()
    {
        var cvOfB = await CreateCvForAsync(_candidateB, "B CV");

        LoginAs(_candidateA);

        // Legacy DeleteCv (UserAccountController.cs:656-673) took the oid straight off the request with NO
        // ownership check at all -- this test is the regression guard for that exact hole.
        await Should.ThrowAsync<UserFriendlyException>(
            () => _candidateAppService.DeleteMyCv(new EntityDto(cvOfB)));

        var stored = UsingDbContext(null, ctx => ctx.ApplicantCVs.IgnoreQueryFilters()
            .Single(c => c.Id == cvOfB));
        stored.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Not_Let_A_Candidate_Apply_Using_Another_Candidates_Cv()
    {
        var cvOfB = await CreateCvForAsync(_candidateB, "B CV");
        var vacancyId = CreateOpenVacancy(_tenantA);

        LoginAs(_candidateA);

        await Should.ThrowAsync<UserFriendlyException>(
            () => _candidateAppService.ApplyToVacancy(new CareersApplyInput
            {
                VacancyId = vacancyId,
                ApplicantCvId = cvOfB
            }));

        var applications = UsingDbContext(null, ctx => ctx.ApplicationStatuses.IgnoreQueryFilters()
            .Count(s => s.VacancyId == vacancyId));
        applications.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Report_Someone_Elses_Cv_Identically_To_A_Nonexistent_One()
    {
        var cvOfB = await CreateCvForAsync(_candidateB, "B CV");

        LoginAs(_candidateA);

        var forSomeoneElses = await Should.ThrowAsync<UserFriendlyException>(
            () => _candidateAppService.GetMyCv(new EntityDto(cvOfB)));

        var forNonexistent = await Should.ThrowAsync<UserFriendlyException>(
            () => _candidateAppService.GetMyCv(new EntityDto(999999)));

        // Identical message => the endpoint is not an oracle for "which CV ids exist in this tenant".
        forSomeoneElses.Message.ShouldBe(forNonexistent.Message);
    }

    [Fact]
    public async Task Should_Only_List_The_Callers_Own_Cvs()
    {
        await CreateCvForAsync(_candidateA, "A CV 1");
        await CreateCvForAsync(_candidateA, "A CV 2");
        await CreateCvForAsync(_candidateB, "B CV");

        LoginAs(_candidateA);
        var cvs = await _candidateAppService.GetMyCvs();

        cvs.Items.Count.ShouldBe(2);
        cvs.Items.Select(c => c.CVName).ShouldNotContain("B CV");
    }

    [Fact]
    public async Task Should_Only_List_The_Callers_Own_Applications()
    {
        var vacancyId = CreateOpenVacancy(_tenantA);

        LoginAs(_candidateB);
        await _candidateAppService.ApplyToVacancy(new CareersApplyInput { VacancyId = vacancyId });

        LoginAs(_candidateA);
        await _candidateAppService.ApplyToVacancy(new CareersApplyInput { VacancyId = vacancyId });

        var mine = await _candidateAppService.GetMyApplications();

        // Both candidates applied to the SAME vacancy in the SAME tenant. A query that filtered by vacancy
        // or by tenant instead of by owned-CV would return two rows here.
        mine.Items.Count.ShouldBe(1);

        var myCvIds = UsingDbContext(null, ctx => ctx.ApplicantCVs.IgnoreQueryFilters()
            .Where(c => c.ApplicantId == _candidateA.ApplicantId).Select(c => c.Id).ToList());

        mine.Items.Single().ApplicantCvId.ShouldNotBeNull();
        myCvIds.ShouldContain(mine.Items.Single().ApplicantCvId.Value);
    }

    [Fact]
    public async Task Should_Not_Let_A_Profile_Update_Touch_Another_Candidates_Row()
    {
        LoginAs(_candidateA);
        await _candidateAppService.UpdateMyProfile(NewProfileInput("OnlyA"));

        var rows = UsingDbContext(null, ctx => ctx.Applicants.IgnoreQueryFilters()
            .Where(a => a.Id == _candidateA.ApplicantId || a.Id == _candidateB.ApplicantId)
            .ToList());

        rows.Single(a => a.Id == _candidateA.ApplicantId).FirstNameEn.ShouldBe("OnlyA");
        rows.Single(a => a.Id == _candidateB.ApplicantId).FirstNameEn.ShouldBe("Cand");

        // And the link columns are not writable from input.
        rows.Single(a => a.Id == _candidateA.ApplicantId).UserId.ShouldBe(_candidateA.UserId);
        rows.Single(a => a.Id == _candidateA.ApplicantId).TenantId.ShouldBe(_tenantA);
    }

    // =============================================================================================
    // Cross-tenant -- R1/R2's guarantees must still hold on the authenticated surface
    // =============================================================================================

    [Fact]
    public async Task Should_Not_Let_A_Candidate_Reach_Another_Tenants_Cv()
    {
        var cvOfC = await CreateCvForAsync(_candidateC, "C CV");

        LoginAs(_candidateA);

        await Should.ThrowAsync<UserFriendlyException>(
            () => _candidateAppService.GetMyCv(new EntityDto(cvOfC)));
    }

    [Fact]
    public async Task Should_Not_Let_A_Candidate_Apply_To_Another_Tenants_Vacancy()
    {
        var vacancyOnTenantB = CreateOpenVacancy(_tenantB, "Tenant B role");

        LoginAs(_candidateA);

        await Should.ThrowAsync<UserFriendlyException>(
            () => _candidateAppService.ApplyToVacancy(new CareersApplyInput { VacancyId = vacancyOnTenantB }));

        var applications = UsingDbContext(null, ctx => ctx.ApplicationStatuses.IgnoreQueryFilters()
            .Count(s => s.VacancyId == vacancyOnTenantB));
        applications.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Refuse_A_Host_Session()
    {
        // A host user has no candidate identity, and a null tenant in ABP switches the MustHaveTenant filter
        // OFF -- i.e. it would widen every query in the service to the whole installation.
        AbpSession.TenantId = null;
        AbpSession.UserId = _candidateA.UserId;

        await Should.ThrowAsync<UserFriendlyException>(() => _candidateAppService.GetMyProfile());
    }

    [Fact]
    public async Task Should_Refuse_When_The_Careers_Subdomain_Disagrees_With_The_Token()
    {
        // A valid tenant A token replayed against tenant B's careers host.
        _tenantResolver.ResolveTenantIdOrNullAsync().Returns(Task.FromResult((int?)_tenantB));

        LoginAs(_candidateA);

        await Should.ThrowAsync<UserFriendlyException>(() => _candidateAppService.GetMyProfile());

        // ...and it works from its own host.
        _tenantResolver.ResolveTenantIdOrNullAsync().Returns(Task.FromResult((int?)_tenantA));
        (await _candidateAppService.GetMyProfile()).Id.ShouldBe(_candidateA.ApplicantId);
    }

    [Fact]
    public async Task Should_Refuse_A_Signed_In_User_With_No_Applicant_Row()
    {
        // The seeded tenant admin: authenticated, in the right tenant, but has no candidate profile. Must
        // not fall back to "the first applicant of this tenant".
        LoginAsDefaultTenantAdmin();

        await Should.ThrowAsync<UserFriendlyException>(() => _candidateAppService.GetMyProfile());
    }

    /// <summary>
    /// Confirms rather than assumes the claim that a candidate cannot reach the recruiter-facing surface.
    /// <c>ApplicantsAppService</c> is class-level <c>[AbpAuthorize(Pages_Recruitment_Applicants)]</c> and the
    /// <c>Candidate</c> role is permissionless, so the call must be refused with an authorization failure --
    /// which is a DIFFERENT and stronger failure than the ownership checks above.
    /// </summary>
    [Fact]
    public async Task Should_Not_Let_A_Candidate_Reach_The_Recruiter_Applicants_Service()
    {
        var applicantsAppService = Resolve<IApplicantsAppService>();

        LoginAs(_candidateA);

        await Should.ThrowAsync<AbpAuthorizationException>(
            () => applicantsAppService.GetAll(new GetAllApplicantsInput()));

        await Should.ThrowAsync<AbpAuthorizationException>(
            () => applicantsAppService.GetApplicantForEdit(new EntityDto(_candidateB.ApplicantId)));
    }

    [Fact]
    public void Should_Not_Grant_The_Candidate_Role_Any_Permission()
    {
        var candidateRoleGrants = UsingDbContext(null, ctx =>
            (from role in ctx.Roles.IgnoreQueryFilters()
             join permission in ctx.Permissions.IgnoreQueryFilters().OfType<RolePermissionSetting>()
                 on role.Id equals permission.RoleId
             where role.Name == StaticRoleNames.Tenants.Candidate
             select permission.Name).ToList());

        candidateRoleGrants.ShouldBeEmpty();
    }

    // =============================================================================================
    // Apply -- legacy rules
    // =============================================================================================

    [Fact]
    public async Task Should_Apply_To_A_Published_Vacancy()
    {
        var vacancyId = CreateOpenVacancy(_tenantA);

        LoginAs(_candidateA);
        var result = await _candidateAppService.ApplyToVacancy(new CareersApplyInput
        {
            VacancyId = vacancyId,
            CoverLetter = "Please consider me."
        });

        result.AppStatus.ShouldBe(AppStatus.Applied);

        var application = UsingDbContext(null, ctx => ctx.ApplicationStatuses.IgnoreQueryFilters()
            .Single(s => s.Id == result.ApplicationId));

        application.TenantId.ShouldBe(_tenantA);
        application.VacancyId.ShouldBe(vacancyId);
        application.ApplicantCVId.ShouldBe(result.ApplicantCvId);
        application.AppStatus.ShouldBe(AppStatus.Applied);
        application.CoverLetter.ShouldBe("Please consider me.");

        // The auto-created placeholder CV (legacy quick-apply behaviour) belongs to the caller.
        var cv = UsingDbContext(null, ctx => ctx.ApplicantCVs.IgnoreQueryFilters()
            .Single(c => c.Id == result.ApplicantCvId));
        cv.ApplicantId.ShouldBe(_candidateA.ApplicantId);
        cv.TenantId.ShouldBe(_tenantA);
    }

    [Fact]
    public async Task Should_Apply_With_An_Existing_Own_Cv()
    {
        var cvId = await CreateCvForAsync(_candidateA, "Senior CV");
        var vacancyId = CreateOpenVacancy(_tenantA);

        LoginAs(_candidateA);
        var result = await _candidateAppService.ApplyToVacancy(new CareersApplyInput
        {
            VacancyId = vacancyId,
            ApplicantCvId = cvId
        });

        result.ApplicantCvId.ShouldBe(cvId);
    }

    [Fact]
    public async Task Should_Refuse_To_Apply_To_A_Closed_Vacancy()
    {
        var vacancyId = CreateClosedVacancy(_tenantA);

        LoginAs(_candidateA);

        await Should.ThrowAsync<UserFriendlyException>(
            () => _candidateAppService.ApplyToVacancy(new CareersApplyInput { VacancyId = vacancyId }));
    }

    [Fact]
    public async Task Should_Refuse_To_Apply_To_An_Expired_Vacancy()
    {
        // Same publication predicate as R1's browse surface: Open AND ClosingDate >= now. If apply and
        // browse ever disagree, an id posted to apply reaches a vacancy the portal no longer shows.
        var vacancyId = CreateExpiredVacancy(_tenantA);

        LoginAs(_candidateA);

        await Should.ThrowAsync<UserFriendlyException>(
            () => _candidateAppService.ApplyToVacancy(new CareersApplyInput { VacancyId = vacancyId }));
    }

    [Fact]
    public async Task Should_Refuse_A_Duplicate_Application_To_The_Same_Vacancy()
    {
        var vacancyId = CreateOpenVacancy(_tenantA);

        LoginAs(_candidateA);
        await _candidateAppService.ApplyToVacancy(new CareersApplyInput { VacancyId = vacancyId });

        await Should.ThrowAsync<UserFriendlyException>(
            () => _candidateAppService.ApplyToVacancy(new CareersApplyInput { VacancyId = vacancyId }));

        var count = UsingDbContext(null, ctx => ctx.ApplicationStatuses.IgnoreQueryFilters()
            .Count(s => s.VacancyId == vacancyId));
        count.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Refuse_A_Duplicate_Even_When_A_Different_Own_Cv_Is_Used()
    {
        var vacancyId = CreateOpenVacancy(_tenantA);
        var firstCv = await CreateCvForAsync(_candidateA, "CV one");
        var secondCv = await CreateCvForAsync(_candidateA, "CV two");

        LoginAs(_candidateA);
        await _candidateAppService.ApplyToVacancy(new CareersApplyInput
        {
            VacancyId = vacancyId,
            ApplicantCvId = firstCv
        });

        await Should.ThrowAsync<UserFriendlyException>(
            () => _candidateAppService.ApplyToVacancy(new CareersApplyInput
            {
                VacancyId = vacancyId,
                ApplicantCvId = secondCv
            }));
    }

    [Fact]
    public async Task Should_Allow_Applying_To_A_Second_Vacancy()
    {
        var first = CreateOpenVacancy(_tenantA, "Role one");
        var second = CreateOpenVacancy(_tenantA, "Role two");

        LoginAs(_candidateA);
        await _candidateAppService.ApplyToVacancy(new CareersApplyInput { VacancyId = first });
        await _candidateAppService.ApplyToVacancy(new CareersApplyInput { VacancyId = second });

        var applications = await _candidateAppService.GetMyApplications();
        applications.Items.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Not_Let_A_Duplicate_Check_Leak_Across_Candidates()
    {
        // B applying must not block A from applying to the same vacancy.
        var vacancyId = CreateOpenVacancy(_tenantA);

        LoginAs(_candidateB);
        await _candidateAppService.ApplyToVacancy(new CareersApplyInput { VacancyId = vacancyId });

        LoginAs(_candidateA);
        var result = await _candidateAppService.ApplyToVacancy(new CareersApplyInput { VacancyId = vacancyId });

        result.AppStatus.ShouldBe(AppStatus.Applied);
    }

    // =============================================================================================
    // CV maintenance
    // =============================================================================================

    [Fact]
    public async Task Should_Create_A_Cv_With_Its_Children_Owned_By_The_Caller()
    {
        LoginAs(_candidateA);

        var cv = await _candidateAppService.CreateMyCv(new CreateCareersCvInput
        {
            CVName = "Full CV",
            Qualifications = { new CareersCvQualificationDto { University = "State U", Percentage = 88, PassedYear = 2015 } },
            WorkExperiences =
            {
                new CareersCvWorkExperienceDto
                {
                    Company = "Acme",
                    Position = "Dev",
                    StartDate = new DateTime(2016, 1, 1),
                    EndDate = new DateTime(2020, 1, 1),
                    Responsibilities = "Built things"
                }
            },
            Skills = { new CareersCvSkillDto { Skill = "C#" } },
            Languages = { new CareersCvLanguageDto() }
        });

        cv.Qualifications.Count.ShouldBe(1);
        cv.WorkExperiences.Count.ShouldBe(1);
        cv.Skills.Count.ShouldBe(1);
        cv.Languages.Count.ShouldBe(1);

        // Legacy stored the COMPANY field as Responsibilities (UserAccountController.cs:571) -- typed DTOs
        // make that impossible.
        cv.WorkExperiences.Single().Responsibilities.ShouldBe("Built things");

        var storedTenantIds = UsingDbContext(null, ctx => new[]
        {
            ctx.CVQualifications.IgnoreQueryFilters().Single(c => c.ApplicantCVId == cv.Id).TenantId,
            ctx.CVWorkExps.IgnoreQueryFilters().Single(c => c.ApplicantCVId == cv.Id).TenantId,
            ctx.CVSkills.IgnoreQueryFilters().Single(c => c.ApplicantCVId == cv.Id).TenantId,
            ctx.CVLanguages.IgnoreQueryFilters().Single(c => c.ApplicantCVId == cv.Id).TenantId
        });

        storedTenantIds.ShouldAllBe(t => t == _tenantA);
    }

    [Fact]
    public async Task Should_Refuse_A_Duplicate_Cv_Name_For_The_Same_Candidate()
    {
        LoginAs(_candidateA);
        await _candidateAppService.CreateMyCv(new CreateCareersCvInput { CVName = "My CV" });

        await Should.ThrowAsync<UserFriendlyException>(
            () => _candidateAppService.CreateMyCv(new CreateCareersCvInput { CVName = "My CV" }));
    }

    [Fact]
    public async Task Should_Allow_Two_Candidates_To_Use_The_Same_Cv_Name()
    {
        // Legacy scoped its duplicate check to the applicant; the rule must not leak between candidates.
        await CreateCvForAsync(_candidateA, "My CV");
        var cvOfB = await CreateCvForAsync(_candidateB, "My CV");

        cvOfB.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Should_Replace_Cv_Children_On_Update()
    {
        LoginAs(_candidateA);

        var cv = await _candidateAppService.CreateMyCv(new CreateCareersCvInput
        {
            CVName = "Draft",
            Skills = { new CareersCvSkillDto { Skill = "old" }, new CareersCvSkillDto { Skill = "older" } }
        });

        var updated = await _candidateAppService.UpdateMyCv(new UpdateCareersCvInput
        {
            Id = cv.Id,
            CVName = "Final",
            Skills = { new CareersCvSkillDto { Skill = "new" } }
        });

        updated.CVName.ShouldBe("Final");
        updated.Skills.Count.ShouldBe(1);
        updated.Skills.Single().Skill.ShouldBe("new");

        var liveSkills = UsingDbContext(null, ctx => ctx.CVSkills.IgnoreQueryFilters()
            .Count(s => s.ApplicantCVId == cv.Id && !s.IsDeleted));
        liveSkills.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Delete_An_Own_Cv_That_Has_Not_Been_Applied_With()
    {
        var cvId = await CreateCvForAsync(_candidateA, "Unused CV");

        LoginAs(_candidateA);
        await _candidateAppService.DeleteMyCv(new EntityDto(cvId));

        var stored = UsingDbContext(null, ctx => ctx.ApplicantCVs.IgnoreQueryFilters().Single(c => c.Id == cvId));
        stored.IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Delete_An_Own_Cv_While_Its_Application_Is_Still_Applied()
    {
        // Legacy's rule (UserAccountController.cs:665-667): AppStatus.Applied does not block deletion.
        var vacancyId = CreateOpenVacancy(_tenantA);

        LoginAs(_candidateA);
        var result = await _candidateAppService.ApplyToVacancy(new CareersApplyInput { VacancyId = vacancyId });

        await _candidateAppService.DeleteMyCv(new EntityDto(result.ApplicantCvId));

        var stored = UsingDbContext(null, ctx => ctx.ApplicantCVs.IgnoreQueryFilters()
            .Single(c => c.Id == result.ApplicantCvId));
        stored.IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Refuse_To_Delete_A_Cv_Whose_Application_Has_Progressed()
    {
        var vacancyId = CreateOpenVacancy(_tenantA);

        LoginAs(_candidateA);
        var result = await _candidateAppService.ApplyToVacancy(new CareersApplyInput { VacancyId = vacancyId });

        // The recruiter shortlists them.
        UsingDbContext(null, ctx =>
        {
            var application = ctx.ApplicationStatuses.IgnoreQueryFilters().Single(s => s.Id == result.ApplicationId);
            application.AppStatus = AppStatus.Shortlisted;
        });

        await Should.ThrowAsync<UserFriendlyException>(
            () => _candidateAppService.DeleteMyCv(new EntityDto(result.ApplicantCvId)));

        var stored = UsingDbContext(null, ctx => ctx.ApplicantCVs.IgnoreQueryFilters()
            .Single(c => c.Id == result.ApplicantCvId));
        stored.IsDeleted.ShouldBeFalse();

        // And the flag the client uses to hide the button agrees with the server's refusal.
        var listed = await _candidateAppService.GetMyCvs();
        listed.Items.Single(c => c.Id == result.ApplicantCvId).CanBeDeleted.ShouldBeFalse();
    }

    // =============================================================================================
    // Profile
    // =============================================================================================

    [Fact]
    public async Task Should_Update_The_Callers_Own_Profile()
    {
        LoginAs(_candidateA);

        var result = await _candidateAppService.UpdateMyProfile(NewProfileInput());

        result.FirstNameEn.ShouldBe("Updated");
        result.FullNameEn.ShouldBe("Updated Person");

        var stored = UsingDbContext(null, ctx => ctx.Applicants.IgnoreQueryFilters()
            .Single(a => a.Id == _candidateA.ApplicantId));

        stored.LastNameAr.ShouldBe("شخص");
        stored.CivilID.ShouldBe(123456789);
        stored.BirthDate.ShouldBe(new DateTime(1990, 5, 4));
        stored.PermanentAddress.ShouldBe("1 Test Street");
    }

    [Fact]
    public async Task Should_Not_Change_The_Email_From_The_Profile_Form()
    {
        // Legacy rewrote Applicant.Email AND Applicant.UserName -- the login credential -- from this form
        // (UserAccountController.cs:157-158). The input DTO has no email field; assert the stored value is
        // untouched so re-adding one fails here.
        LoginAs(_candidateA);
        await _candidateAppService.UpdateMyProfile(NewProfileInput());

        var stored = UsingDbContext(null, ctx => ctx.Applicants.IgnoreQueryFilters()
            .Single(a => a.Id == _candidateA.ApplicantId));

        stored.Email.ShouldBe("a@example.com");

        typeof(UpdateCareersProfileInput).GetProperties().Select(p => p.Name).ToList()
            .ShouldNotContain("Email");
    }

    [Fact]
    public async Task Should_Score_A_Fresh_Profile_As_Incomplete_And_Improve_As_It_Is_Filled_In()
    {
        LoginAs(_candidateA);

        var before = await _candidateAppService.GetMyProfile();

        // Fresh R2 registration: email only (5/60). The 1950-01-01 "unknown birth date" sentinel must NOT
        // score as a completed birth date.
        before.ProfileCompletionPercent.ShouldBe(8);

        var after = await _candidateAppService.UpdateMyProfile(NewProfileInput());

        // + CivilID, PermanentAddress, real BirthDate, Mobile.
        after.ProfileCompletionPercent.ShouldBeGreaterThan(before.ProfileCompletionPercent);

        await _candidateAppService.CreateMyCv(new CreateCareersCvInput { CVName = "CV" });
        var withCv = await _candidateAppService.GetMyProfile();

        // The CV block is worth 25 of the 60 points.
        withCv.ProfileCompletionPercent.ShouldBeGreaterThan(after.ProfileCompletionPercent);
    }
}
