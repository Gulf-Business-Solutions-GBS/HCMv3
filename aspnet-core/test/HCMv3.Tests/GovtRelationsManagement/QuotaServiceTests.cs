using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GovtRelationsManagement;
using HCMv3.GovtRelationsManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GovtRelationsManagement;

// ReSharper disable once InconsistentNaming
public class QuotaServiceTests : AppTestBase
{
    private readonly IQuotaService _quotaService;

    public QuotaServiceTests()
    {
        LoginAsDefaultTenantAdmin();
        _quotaService = Resolve<IQuotaService>();
    }

    private void EnableSponsorGovtJobTitleQuota(bool enabled)
    {
        UsingDbContext(context =>
        {
            context.Set<ManagePersonnelSettings>().Add(new ManagePersonnelSettings
            {
                TenantId = AbpSession.TenantId ?? 1,
                EnableSponsorGovtJobTitleQuota = enabled
            });
            context.SaveChanges();
        });
    }

    private int CreateNationality(string code, string nationalityGroupNameEn)
    {
        return UsingDbContext(context =>
        {
            int? groupId = null;
            if (nationalityGroupNameEn != null)
            {
                var group = new NationalityGroup { TenantId = AbpSession.TenantId ?? 1, Code = nationalityGroupNameEn, NameEn = nationalityGroupNameEn, NameAr = nationalityGroupNameEn };
                context.Set<NationalityGroup>().Add(group);
                context.SaveChanges();
                groupId = group.Id;
            }

            var nationality = new Nationality { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code, NationalityGroupId = groupId };
            context.Set<Nationality>().Add(nationality);
            context.SaveChanges();
            return nationality.Id;
        });
    }

    private int CreateBusinessType(string code)
    {
        return UsingDbContext(context =>
        {
            var entity = new BusinessType { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
            context.Set<BusinessType>().Add(entity);
            context.SaveChanges();
            return entity.Id;
        });
    }

    private int CreateGovernorate(string code)
    {
        return UsingDbContext(context =>
        {
            var entity = new Governorate { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
            context.Set<Governorate>().Add(entity);
            context.SaveChanges();
            return entity.Id;
        });
    }

    private long CreateOrganizationUnit(string displayName)
    {
        return UsingDbContext(context =>
        {
            var entity = new Abp.Organizations.OrganizationUnit(AbpSession.TenantId, displayName, null) { Code = displayName };
            context.Set<Abp.Organizations.OrganizationUnit>().Add(entity);
            context.SaveChanges();
            return entity.Id;
        });
    }

    private int CreateSponsor(string code, int nationalityId, int businessTypeId, int governorateId, long organizationUnitId)
    {
        return UsingDbContext(context =>
        {
            var entity = new Sponsor
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code,
                NationalityId = nationalityId,
                BusinessTypeId = businessTypeId,
                GovernorateId = governorateId,
                OrganizationUnitId = organizationUnitId
            };
            context.Set<Sponsor>().Add(entity);
            context.SaveChanges();
            return entity.Id;
        });
    }

    private int CreateGovtJobCategory(string code)
    {
        return UsingDbContext(context =>
        {
            var entity = new GovtJobCategory { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
            context.Set<GovtJobCategory>().Add(entity);
            context.SaveChanges();
            return entity.Id;
        });
    }

    private int CreateGovtJobTitle(string code, int govtJobCategoryId)
    {
        return UsingDbContext(context =>
        {
            var entity = new GovtJobTitle { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code, GovtJobCategoryId = govtJobCategoryId };
            context.Set<GovtJobTitle>().Add(entity);
            context.SaveChanges();
            return entity.Id;
        });
    }

    private int CreateEmployee(string firstName, int nationalityId)
    {
        return UsingDbContext(context =>
        {
            var entity = new Employee
            {
                TenantId = AbpSession.TenantId ?? 1,
                FirstNameEn = firstName,
                LastNameEn = "Doe",
                FirstNameAr = firstName,
                LastNameAr = "Doe",
                FullNameEn = firstName,
                FullNameAr = firstName,
                NationalityId = nationalityId
            };
            context.Set<Employee>().Add(entity);
            context.SaveChanges();
            return entity.Id;
        });
    }

    private int CreateCurrency(string code)
    {
        return UsingDbContext(context =>
        {
            var entity = new Currency { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
            context.Set<Currency>().Add(entity);
            context.SaveChanges();
            return entity.Id;
        });
    }

    private int CreateWorkVisaType(string code)
    {
        return UsingDbContext(context =>
        {
            var entity = new WorkVisaType { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
            context.Set<WorkVisaType>().Add(entity);
            context.SaveChanges();
            return entity.Id;
        });
    }

    private void CreateEmployeeSponsor(int employeeId, int sponsorId, int govtJobTitleId, DateTime startDate, DateTime endDate)
    {
        var currencyId = CreateCurrency("EMP_SP_CUR_" + employeeId);
        var workVisaTypeId = CreateWorkVisaType("EMP_SP_WVT_" + employeeId);

        UsingDbContext(context =>
        {
            context.Set<EmployeeSponsor>().Add(new EmployeeSponsor
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeId = employeeId,
                SponsorId = sponsorId,
                GovtJobTitleId = govtJobTitleId,
                StartDate = startDate,
                EndDate = endDate,
                SalaryCurrencyId = currencyId,
                WorkVisaTypeId = workVisaTypeId
            });
            context.SaveChanges();
        });
    }

    [Fact]
    public async Task CalculateSponsorJobTitleQuotaAsync_Should_NoOp_When_Feature_Disabled()
    {
        EnableSponsorGovtJobTitleQuota(false);

        var nationalityId = CreateNationality("N1", "Citizen");
        var businessTypeId = CreateBusinessType("BT1");
        var governorateId = CreateGovernorate("GOV1");
        var organizationUnitId = CreateOrganizationUnit("Org1");
        var sponsorId = CreateSponsor("SP1", nationalityId, businessTypeId, governorateId, organizationUnitId);
        var jobCategoryId = CreateGovtJobCategory("JC1");
        CreateGovtJobTitle("JT1", jobCategoryId);

        await _quotaService.CalculateSponsorJobTitleQuotaAsync();

        var count = UsingDbContext(context => context.Set<SponsorJobTitle>().Count(r => r.SponsorId == sponsorId));
        count.ShouldBe(0);
    }

    [Fact]
    public async Task CalculateSponsorJobTitleQuotaAsync_Should_Create_Row_With_Default_Values_And_Compute_Used_Counts()
    {
        EnableSponsorGovtJobTitleQuota(true);

        var citizenNationalityId = CreateNationality("CIT", "Citizen");
        var nonCitizenNationalityId = CreateNationality("FOR", "Foreigner");

        var businessTypeId = CreateBusinessType("BT2");
        var governorateId = CreateGovernorate("GOV2");
        var organizationUnitId = CreateOrganizationUnit("Org2");
        var sponsorId = CreateSponsor("SP2", citizenNationalityId, businessTypeId, governorateId, organizationUnitId);

        var jobCategoryId = CreateGovtJobCategory("JC2");
        var govtJobTitleId = CreateGovtJobTitle("JT2", jobCategoryId);

        var citizenEmployeeId = CreateEmployee("CitizenEmp", citizenNationalityId);
        var nonCitizenEmployeeId = CreateEmployee("NonCitizenEmp", nonCitizenNationalityId);

        var today = DateTime.Now.Date;
        CreateEmployeeSponsor(citizenEmployeeId, sponsorId, govtJobTitleId, today.AddYears(-1), today.AddYears(1));
        CreateEmployeeSponsor(nonCitizenEmployeeId, sponsorId, govtJobTitleId, today.AddYears(-1), today.AddYears(1));

        await _quotaService.CalculateSponsorJobTitleQuotaAsync();

        var sponsorJobTitle = UsingDbContext(context => context.Set<SponsorJobTitle>()
            .First(r => r.SponsorId == sponsorId && r.GovtJobTitleId == govtJobTitleId));

        sponsorJobTitle.NoofPosition.ShouldBe(999);
        sponsorJobTitle.CitizenPercent.ShouldBe(10);
        sponsorJobTitle.CitizenQuotaAccrued.ShouldBe(100);
        sponsorJobTitle.NonCitizenPercent.ShouldBe(90);
        sponsorJobTitle.NonCitizenQuotaAccrued.ShouldBe(899);
        sponsorJobTitle.StartDate.ShouldBe(new DateTime(2023, 1, 1));
        sponsorJobTitle.EndDate.ShouldBe(new DateTime(9999, 12, 31));
        sponsorJobTitle.CitizenQuotaUsed.ShouldBe(1);
        sponsorJobTitle.NonCitizenQuotaUsed.ShouldBe(1);
    }

    [Fact]
    public async Task CalculateSponsorJobTitleQuotaAsync_Should_Update_Existing_Row_Used_Counts_Without_Resetting_Accrued()
    {
        EnableSponsorGovtJobTitleQuota(true);

        var citizenNationalityId = CreateNationality("CIT2", "Citizen");
        var businessTypeId = CreateBusinessType("BT3");
        var governorateId = CreateGovernorate("GOV3");
        var organizationUnitId = CreateOrganizationUnit("Org3");
        var sponsorId = CreateSponsor("SP3", citizenNationalityId, businessTypeId, governorateId, organizationUnitId);

        var jobCategoryId = CreateGovtJobCategory("JC3");
        var govtJobTitleId = CreateGovtJobTitle("JT3", jobCategoryId);

        UsingDbContext(context =>
        {
            context.Set<SponsorJobTitle>().Add(new SponsorJobTitle
            {
                TenantId = AbpSession.TenantId ?? 1,
                SponsorId = sponsorId,
                GovtJobTitleId = govtJobTitleId,
                StartDate = new DateTime(2023, 1, 1),
                EndDate = new DateTime(9999, 12, 31),
                NoofPosition = 500,
                CitizenPercent = 50,
                CitizenQuotaAccrued = 250,
                NonCitizenPercent = 50,
                NonCitizenQuotaAccrued = 250,
                CitizenQuotaUsed = 0,
                NonCitizenQuotaUsed = 0
            });
            context.SaveChanges();
        });

        var citizenEmployeeId = CreateEmployee("CitizenEmp2", citizenNationalityId);
        var today = DateTime.Now.Date;
        CreateEmployeeSponsor(citizenEmployeeId, sponsorId, govtJobTitleId, today.AddYears(-1), today.AddYears(1));

        await _quotaService.CalculateSponsorJobTitleQuotaAsync();

        var sponsorJobTitle = UsingDbContext(context => context.Set<SponsorJobTitle>()
            .First(r => r.SponsorId == sponsorId && r.GovtJobTitleId == govtJobTitleId));

        // Pre-existing accrual configuration must be left untouched (only Used counters are recomputed).
        sponsorJobTitle.NoofPosition.ShouldBe(500);
        sponsorJobTitle.CitizenQuotaAccrued.ShouldBe(250);
        sponsorJobTitle.CitizenQuotaUsed.ShouldBe(1);
        sponsorJobTitle.NonCitizenQuotaUsed.ShouldBe(0);
    }
}
