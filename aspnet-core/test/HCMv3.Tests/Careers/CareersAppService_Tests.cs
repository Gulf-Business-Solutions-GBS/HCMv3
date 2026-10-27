using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Domain.Repositories;
using Abp.Organizations;
using Abp.UI;
using HCMv3.Careers;
using HCMv3.Careers.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.MultiTenancy;
using HCMv3.OrganizationManagement;
using HCMv3.Recruitment;
using HCMv3.Tests.Recruitment;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Careers;

/// <summary>
/// Tests for the PUBLIC careers read surface. Reuses the Recruitment bespoke-test harness (private in-memory
/// Sqlite <see cref="RecruitmentTestDbContext"/> + <see cref="FakeRepository"/>), which is exactly the right
/// shape here: that harness has NO ABP data filters at all, so it reproduces the worst case an anonymous
/// request presents in production (session TenantId null -> MustHaveTenant filter disabled). Anything these
/// tests see is what <see cref="CareersAppService"/>'s own explicit <c>TenantId</c> predicate lets through.
///
/// Tenant A = 1, Tenant B = 2 throughout.
/// </summary>
// ReSharper disable once InconsistentNaming
public class CareersAppService_Tests : RecruitmentAppServiceTestBase
{
    private const int TenantA = 1;
    private const int TenantB = 2;

    private readonly List<Tenant> _tenants = new List<Tenant>
    {
        new Tenant("acme", "Acme Corp") { Id = TenantA, IsActive = true },
        new Tenant("globex", "Globex") { Id = TenantB, IsActive = true }
    };

    private CareersAppService CreateAppService(int? resolvedTenantId)
    {
        var resolver = Substitute.For<ICareersTenantResolver>();
        resolver.ResolveTenantIdOrNullAsync().Returns(Task.FromResult(resolvedTenantId));

        var organizationUnitRepository = Substitute.For<IRepository<OrganizationUnit, long>>();
        organizationUnitRepository
            .GetAllListAsync(Arg.Any<Expression<Func<OrganizationUnit, bool>>>())
            .Returns(Task.FromResult(new List<OrganizationUnit>()));

        var tenantRepository = Substitute.For<IRepository<Tenant>>();
        tenantRepository
            .FirstOrDefaultAsync(Arg.Any<Expression<Func<Tenant, bool>>>())
            .Returns(call => Task.FromResult(
                _tenants.FirstOrDefault(call.Arg<Expression<Func<Tenant, bool>>>().Compile())));

        return CreateService(() => new CareersAppService(
            FakeRepository.For<Vacancy>(Context),
            organizationUnitRepository,
            tenantRepository,
            resolver));
    }

    private int CreateJobTitle(int tenantId, string nameEn = "Engineer")
    {
        var entity = new JobTitle { TenantId = tenantId, NameEn = nameEn, NameAr = "مهندس" };
        Context.JobTitles.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateWorkLocation(int tenantId, string nameEn = "Kuwait City")
    {
        var entity = new WorkLocation { TenantId = tenantId, NameEn = nameEn, NameAr = "الكويت" };
        Context.WorkLocations.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateQualification(int tenantId, string nameEn = "BSc")
    {
        var entity = new Qualification { TenantId = tenantId, NameEn = nameEn, NameAr = "بكالوريوس" };
        Context.Qualifications.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    /// <summary>
    /// Inserts a vacancy straight into the context. Defaults are the legacy "publicly visible" shape:
    /// <see cref="VacancyStatus.Open"/> and a future closing date.
    /// </summary>
    private Vacancy CreateVacancy(
        int tenantId,
        string title = "Senior Engineer",
        VacancyStatus status = VacancyStatus.Open,
        DateTime? closingDate = null,
        DateTime? publishOn = null,
        int? jobTitleId = null,
        int? workLocationId = null,
        int? qualificationId = null)
    {
        var entity = new Vacancy
        {
            TenantId = tenantId,
            Code = Guid.NewGuid().ToString("N").Substring(0, 8),
            VacancyTitle = title,
            VacancyStatus = status,
            PublishOn = publishOn ?? DateTime.Now.AddDays(-5),
            ClosingDate = closingDate ?? DateTime.Now.AddDays(30),
            RequestionDate = DateTime.Now.AddDays(-10),
            JobTitleId = jobTitleId,
            WorkLocationId = workLocationId,
            QualificationId = qualificationId,
            OrganizationUnitId = 1,
            JobDescription = "Build things.",
            YearsOfExperience = 5,
            TotalVacancy = 3,
            MonthlySalaryRange = "1000-2000",
            IsBudgeted = true,
            SLADaysToFill = 30,
            RecruiterId = 99
        };

        Context.Vacancies.Add(entity);
        Context.SaveChanges();
        return entity;
    }

    // ---------------------------------------------------------------------------------------------
    // Tenant isolation
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task Should_Return_Only_The_Resolved_Tenants_Vacancies()
    {
        CreateVacancy(TenantA, "Tenant A Role");
        CreateVacancy(TenantA, "Tenant A Second Role");
        CreateVacancy(TenantB, "Tenant B Role");

        var result = await CreateAppService(TenantA).GetPublishedVacancies(new GetPublishedVacanciesInput());

        result.TotalCount.ShouldBe(2);
        result.Items.Select(i => i.VacancyTitle).ShouldBe(
            new[] { "Tenant A Role", "Tenant A Second Role" }, ignoreOrder: true);
        result.Items.ShouldNotContain(i => i.VacancyTitle == "Tenant B Role");
    }

    [Fact]
    public async Task Should_Return_Only_The_Other_Tenants_Vacancies_When_That_Subdomain_Is_Resolved()
    {
        CreateVacancy(TenantA, "Tenant A Role");
        CreateVacancy(TenantB, "Tenant B Role");

        var result = await CreateAppService(TenantB).GetPublishedVacancies(new GetPublishedVacanciesInput());

        result.TotalCount.ShouldBe(1);
        result.Items[0].VacancyTitle.ShouldBe("Tenant B Role");
    }

    [Fact]
    public async Task Should_Reject_The_List_When_No_Tenant_Can_Be_Resolved()
    {
        CreateVacancy(TenantA, "Tenant A Role");
        CreateVacancy(TenantB, "Tenant B Role");

        // The whole point: an unresolved subdomain must NOT fall through to host/null-tenant and return
        // every tenant's vacancies.
        await Should.ThrowAsync<UserFriendlyException>(
            () => CreateAppService(null).GetPublishedVacancies(new GetPublishedVacanciesInput()));
    }

    [Fact]
    public async Task Should_Reject_The_Detail_When_No_Tenant_Can_Be_Resolved()
    {
        var vacancy = CreateVacancy(TenantA);

        await Should.ThrowAsync<UserFriendlyException>(
            () => CreateAppService(null).GetPublishedVacancy(new EntityDto(vacancy.Id)));
    }

    [Fact]
    public async Task Should_Reject_The_Filters_When_No_Tenant_Can_Be_Resolved()
    {
        CreateVacancy(TenantA, jobTitleId: CreateJobTitle(TenantA));

        await Should.ThrowAsync<UserFriendlyException>(
            () => CreateAppService(null).GetVacancyFilters());
    }

    [Fact]
    public async Task Should_Reject_Branding_When_No_Tenant_Can_Be_Resolved()
    {
        await Should.ThrowAsync<UserFriendlyException>(
            () => CreateAppService(null).GetTenantBranding());
    }

    [Fact]
    public async Task Should_Not_Return_Another_Tenants_Vacancy_By_Id()
    {
        var tenantBVacancy = CreateVacancy(TenantB, "Tenant B Role");

        // Enumerating ids from tenant A's subdomain must not reach tenant B's row.
        await Should.ThrowAsync<UserFriendlyException>(
            () => CreateAppService(TenantA).GetPublishedVacancy(new EntityDto(tenantBVacancy.Id)));
    }

    [Fact]
    public async Task Should_Scope_Filter_Facets_To_The_Resolved_Tenant()
    {
        var tenantAJobTitle = CreateJobTitle(TenantA, "Tenant A Engineer");
        var tenantBJobTitle = CreateJobTitle(TenantB, "Tenant B Engineer");
        CreateVacancy(TenantA, jobTitleId: tenantAJobTitle, workLocationId: CreateWorkLocation(TenantA, "A City"));
        CreateVacancy(TenantB, jobTitleId: tenantBJobTitle, workLocationId: CreateWorkLocation(TenantB, "B City"));

        var filters = await CreateAppService(TenantA).GetVacancyFilters();

        filters.JobTitles.Count.ShouldBe(1);
        filters.JobTitles[0].Id.ShouldBe(tenantAJobTitle);
        filters.JobTitles[0].Name.ShouldBe("Tenant A Engineer");
        filters.JobTitles[0].Count.ShouldBe(1);

        filters.WorkLocations.Count.ShouldBe(1);
        filters.WorkLocations[0].Name.ShouldBe("A City");
    }

    // ---------------------------------------------------------------------------------------------
    // Publication state (legacy VacancyController.cs:148-150)
    // ---------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(VacancyStatus.Filled)]
    [InlineData(VacancyStatus.OnHold)]
    [InlineData(VacancyStatus.Dropped)]
    public async Task Should_Not_Return_Vacancies_Whose_Status_Is_Not_Open(VacancyStatus status)
    {
        CreateVacancy(TenantA, "Hidden", status: status);

        var result = await CreateAppService(TenantA).GetPublishedVacancies(new GetPublishedVacanciesInput());

        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Not_Return_Vacancies_Whose_Closing_Date_Has_Passed()
    {
        CreateVacancy(TenantA, "Expired", closingDate: DateTime.Now.AddDays(-1));
        CreateVacancy(TenantA, "Live", closingDate: DateTime.Now.AddDays(1));

        var result = await CreateAppService(TenantA).GetPublishedVacancies(new GetPublishedVacanciesInput());

        result.TotalCount.ShouldBe(1);
        result.Items[0].VacancyTitle.ShouldBe("Live");
    }

    [Fact]
    public async Task Should_Not_Return_An_Unpublished_Vacancy_By_Id()
    {
        var onHold = CreateVacancy(TenantA, "On Hold", status: VacancyStatus.OnHold);
        var expired = CreateVacancy(TenantA, "Expired", closingDate: DateTime.Now.AddDays(-1));

        var service = CreateAppService(TenantA);

        await Should.ThrowAsync<UserFriendlyException>(
            () => service.GetPublishedVacancy(new EntityDto(onHold.Id)));
        await Should.ThrowAsync<UserFriendlyException>(
            () => service.GetPublishedVacancy(new EntityDto(expired.Id)));
    }

    [Fact]
    public async Task Should_Not_Surface_Unpublished_Vacancies_In_Filter_Facets()
    {
        var jobTitleId = CreateJobTitle(TenantA, "Ghost Role");
        CreateVacancy(TenantA, status: VacancyStatus.Dropped, jobTitleId: jobTitleId);

        var filters = await CreateAppService(TenantA).GetVacancyFilters();

        filters.JobTitles.ShouldBeEmpty();
    }

    // ---------------------------------------------------------------------------------------------
    // Detail / list projection
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task Should_Return_Vacancy_Detail_With_Public_Fields_Only()
    {
        var vacancy = CreateVacancy(
            TenantA,
            "Senior Engineer",
            jobTitleId: CreateJobTitle(TenantA),
            workLocationId: CreateWorkLocation(TenantA),
            qualificationId: CreateQualification(TenantA));

        var dto = await CreateAppService(TenantA).GetPublishedVacancy(new EntityDto(vacancy.Id));

        dto.Id.ShouldBe(vacancy.Id);
        dto.VacancyTitle.ShouldBe("Senior Engineer");
        dto.JobTitleName.ShouldBe("Engineer");
        dto.WorkLocationName.ShouldBe("Kuwait City");
        dto.QualificationName.ShouldBe("BSc");
        dto.JobDescription.ShouldBe("Build things.");
        dto.YearsOfExperience.ShouldBe(5);

        // Guard against someone later "helpfully" widening the public DTO: none of these internal fields
        // may ever appear on it. Asserted structurally so the test fails at compile-free runtime if added.
        var properties = typeof(PublicVacancyDetailDto).GetProperties().Select(p => p.Name).ToList();
        properties.ShouldNotContain("TenantId");
        properties.ShouldNotContain("MonthlySalaryRange");
        properties.ShouldNotContain("TotalVacancy");
        properties.ShouldNotContain("RecruiterId");
        properties.ShouldNotContain("IsBudgeted");
        properties.ShouldNotContain("VacancyStatus");
        properties.ShouldNotContain("SLADaysToFill");
        properties.ShouldNotContain("RequestionDate");
        properties.ShouldNotContain("OnbGroupId");
        properties.ShouldNotContain("TemplateOnbGroupId");
        properties.ShouldNotContain("CreatorUserId");
    }

    [Fact]
    public async Task Should_Fall_Back_To_Unspecified_For_A_Vacancy_With_No_Location()
    {
        var vacancy = CreateVacancy(TenantA);

        var dto = await CreateAppService(TenantA).GetPublishedVacancy(new EntityDto(vacancy.Id));

        // Legacy VacancyController.cs:209 / 333.
        dto.WorkLocationName.ShouldBe("Unspecified");
        dto.QualificationName.ShouldBe("Unspecified");
    }

    // ---------------------------------------------------------------------------------------------
    // Filtering / paging / sorting
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task Should_Search_By_Vacancy_Title()
    {
        CreateVacancy(TenantA, "Software Engineer");
        CreateVacancy(TenantA, "Accountant");

        var result = await CreateAppService(TenantA)
            .GetPublishedVacancies(new GetPublishedVacanciesInput { Filter = "Engineer" });

        result.TotalCount.ShouldBe(1);
        result.Items[0].VacancyTitle.ShouldBe("Software Engineer");
    }

    [Fact]
    public async Task Should_Filter_By_Job_Title_Ids()
    {
        var wanted = CreateJobTitle(TenantA, "Wanted");
        var other = CreateJobTitle(TenantA, "Other");
        CreateVacancy(TenantA, "Wanted Role", jobTitleId: wanted);
        CreateVacancy(TenantA, "Other Role", jobTitleId: other);

        var result = await CreateAppService(TenantA).GetPublishedVacancies(
            new GetPublishedVacanciesInput { JobTitleIds = new List<int> { wanted } });

        result.TotalCount.ShouldBe(1);
        result.Items[0].VacancyTitle.ShouldBe("Wanted Role");
    }

    [Fact]
    public async Task Should_Filter_By_Work_Location_Ids()
    {
        var wanted = CreateWorkLocation(TenantA, "Wanted City");
        var other = CreateWorkLocation(TenantA, "Other City");
        CreateVacancy(TenantA, "Wanted Role", workLocationId: wanted);
        CreateVacancy(TenantA, "Other Role", workLocationId: other);

        var result = await CreateAppService(TenantA).GetPublishedVacancies(
            new GetPublishedVacanciesInput { WorkLocationIds = new List<int> { wanted } });

        result.TotalCount.ShouldBe(1);
        result.Items[0].VacancyTitle.ShouldBe("Wanted Role");
    }

    [Fact]
    public async Task Should_Default_To_Newest_First()
    {
        CreateVacancy(TenantA, "Older", publishOn: DateTime.Now.AddDays(-20));
        CreateVacancy(TenantA, "Newer", publishOn: DateTime.Now.AddDays(-1));

        var result = await CreateAppService(TenantA).GetPublishedVacancies(new GetPublishedVacanciesInput());

        result.Items[0].VacancyTitle.ShouldBe("Newer");
    }

    [Fact]
    public async Task Should_Reject_Sorting_Expressions_That_Are_Not_Whitelisted()
    {
        // Anonymous callers must not be able to feed arbitrary expressions to System.Linq.Dynamic.Core.
        var input = new GetPublishedVacanciesInput { Sorting = "MonthlySalaryRange.ToString()" };
        input.Normalize();
        input.Sorting.ShouldBe(GetPublishedVacanciesInput.DefaultSorting);

        var allowed = new GetPublishedVacanciesInput { Sorting = "vacancytitle asc" };
        allowed.Normalize();
        allowed.Sorting.ShouldBe("VacancyTitle ASC");
    }

    [Fact]
    public async Task Should_Cap_Page_Size_For_Anonymous_Callers()
    {
        var input = new GetPublishedVacanciesInput { MaxResultCount = 100000 };
        input.Normalize();
        input.MaxResultCount.ShouldBe(10);
    }

    // ---------------------------------------------------------------------------------------------
    // Branding
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task Should_Return_Branding_For_The_Resolved_Tenant_Only()
    {
        var branding = await CreateAppService(TenantB).GetTenantBranding();

        branding.TenantId.ShouldBe(TenantB);
        branding.TenancyName.ShouldBe("globex");
        branding.Name.ShouldBe("Globex");
        branding.HasLightLogo.ShouldBeFalse();
        branding.HasDarkLogo.ShouldBeFalse();

        var properties = typeof(PublicTenantBrandingDto).GetProperties().Select(p => p.Name).ToList();
        properties.ShouldNotContain("ConnectionString");
        properties.ShouldNotContain("EditionId");
        properties.ShouldNotContain("SubscriptionEndDateUtc");
    }
}
