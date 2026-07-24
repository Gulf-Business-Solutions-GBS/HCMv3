using System;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GovtRelationsManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeSponsorsAppService_Tests : AppTestBase
{
    private readonly IEmployeeSponsorsAppService _appService;

    public EmployeeSponsorsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeSponsorsAppService>();
    }

    private NationalityGroup CreateNationalityGroup(string code, string nameEn)
    {
        NationalityGroup group = null;
        UsingDbContext(context =>
        {
            group = context.Set<NationalityGroup>().Add(new NationalityGroup
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = nameEn,
                NameAr = nameEn
            }).Entity;
        });
        return group;
    }

    private Nationality CreateNationality(string code, int? nationalityGroupId)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Set<Nationality>().Add(new Nationality
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code,
                NationalityGroupId = nationalityGroupId
            }).Entity;
        });
        return nationality;
    }

    private Employee CreateTestEmployee(string code, int nationalityId)
    {
        Employee employee = null;
        UsingDbContext(context =>
        {
            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                FirstNameEn = "F",
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "F L",
                FullNameAr = "ف ل",
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                NationalityId = nationalityId
            }).Entity;
        });
        return employee;
    }

    private Sponsor CreateTestSponsor(string code)
    {
        var nationality = CreateNationality(code + "-N", null);

        BusinessType businessType = null;
        Governorate governorate = null;
        Abp.Organizations.OrganizationUnit orgUnit = null;
        UsingDbContext(context =>
        {
            businessType = context.Set<BusinessType>().Add(new BusinessType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;

            governorate = context.Set<Governorate>().Add(new Governorate
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;

            orgUnit = context.Set<Abp.Organizations.OrganizationUnit>().Add(
                new Abp.Organizations.OrganizationUnit(AbpSession.TenantId, code, null) { Code = code }).Entity;
        });

        Sponsor sponsor = null;
        UsingDbContext(context =>
        {
            sponsor = context.Set<Sponsor>().Add(new Sponsor
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code,
                NationalityId = nationality.Id,
                BusinessTypeId = businessType.Id,
                GovernorateId = governorate.Id,
                OrganizationUnitId = orgUnit.Id
            }).Entity;
        });
        return sponsor;
    }

    private GovtJobTitle CreateTestGovtJobTitle(string code)
    {
        GovtJobCategory category = null;
        UsingDbContext(context =>
        {
            category = context.Set<GovtJobCategory>().Add(new GovtJobCategory
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });

        GovtJobTitle jobTitle = null;
        UsingDbContext(context =>
        {
            jobTitle = context.Set<GovtJobTitle>().Add(new GovtJobTitle
            {
                TenantId = AbpSession.TenantId ?? 1,
                GovtJobCategoryId = category.Id,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });
        return jobTitle;
    }

    private SponsorJobTitle CreateTestSponsorJobTitle(int sponsorId, int govtJobTitleId, int noOfPosition, int citizenPercent, int nonCitizenPercent)
    {
        SponsorJobTitle sponsorJobTitle = null;
        UsingDbContext(context =>
        {
            sponsorJobTitle = context.Set<SponsorJobTitle>().Add(new SponsorJobTitle
            {
                TenantId = AbpSession.TenantId ?? 1,
                SponsorId = sponsorId,
                GovtJobTitleId = govtJobTitleId,
                StartDate = new DateTime(2024, 1, 1),
                EndDate = new DateTime(2024, 12, 31),
                NoofPosition = noOfPosition,
                CitizenPercent = citizenPercent,
                CitizenQuotaAccrued = (int)Math.Floor(noOfPosition * citizenPercent / 100.0),
                NonCitizenPercent = nonCitizenPercent,
                NonCitizenQuotaAccrued = (int)Math.Ceiling(noOfPosition * nonCitizenPercent / 100.0)
            }).Entity;
        });
        return sponsorJobTitle;
    }

    private WorkVisaType CreateTestWorkVisaType(string code)
    {
        WorkVisaType workVisaType = null;
        UsingDbContext(context =>
        {
            workVisaType = context.Set<WorkVisaType>().Add(new WorkVisaType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });
        return workVisaType;
    }

    private Currency CreateTestCurrency(string code)
    {
        Currency currency = null;
        UsingDbContext(context =>
        {
            currency = context.Set<Currency>().Add(new Currency
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });
        return currency;
    }

    [Fact]
    public async Task Should_Increment_CitizenQuotaUsed_On_Create_And_Decrement_On_Delete()
    {
        //Arrange
        var citizenGroup = CreateNationalityGroup("CGZ100", "Citizen");
        var nationality = CreateNationality("NZ100", citizenGroup.Id);
        var employee = CreateTestEmployee("ESP100", nationality.Id);
        var sponsor = CreateTestSponsor("SPZ100");
        var jobTitle = CreateTestGovtJobTitle("JTZ100");
        var sponsorJobTitle = CreateTestSponsorJobTitle(sponsor.Id, jobTitle.Id, 10, 50, 50);
        var workVisaType = CreateTestWorkVisaType("WVZ100");
        var currency = CreateTestCurrency("CURZ100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeSponsorDto
        {
            EmployeeId = employee.Id,
            SponsorId = sponsor.Id,
            GovtJobTitleId = jobTitle.Id,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 12, 31),
            SalaryCurrencyId = currency.Id,
            WorkVisaTypeId = workVisaType.Id
        });

        //Assert: CitizenQuotaUsed incremented
        UsingDbContext(context =>
        {
            var updated = context.Set<SponsorJobTitle>().Find(sponsorJobTitle.Id);
            updated.CitizenQuotaUsed.ShouldBe(1);
            updated.NonCitizenQuotaUsed.ShouldBe(0);
        });

        var created = await _appService.GetAll(new GetAllEmployeeSponsorsInput { EmployeeId = employee.Id });
        var id = created.Items[0].Id;

        //Act: delete
        await _appService.Delete(new Abp.Application.Services.Dto.EntityDto(id));

        //Assert: CitizenQuotaUsed decremented back to 0
        UsingDbContext(context =>
        {
            var updated = context.Set<SponsorJobTitle>().Find(sponsorJobTitle.Id);
            updated.CitizenQuotaUsed.ShouldBe(0);
        });
    }

    [Fact]
    public async Task Should_Not_Decrement_CitizenQuotaUsed_Below_Zero()
    {
        //Arrange: SponsorJobTitle starts at CitizenQuotaUsed == 0, and an EmployeeSponsor row is inserted
        //directly (bypassing CreateOrEdit, so the Used counter was never incremented for it) -- this
        //simulates an out-of-order/inconsistent state where Delete must not push the counter negative.
        var citizenGroup = CreateNationalityGroup("CGZ200", "Citizen");
        var nationality = CreateNationality("NZ200", citizenGroup.Id);
        var employee = CreateTestEmployee("ESP200", nationality.Id);
        var sponsor = CreateTestSponsor("SPZ200");
        var jobTitle = CreateTestGovtJobTitle("JTZ200");
        var sponsorJobTitle = CreateTestSponsorJobTitle(sponsor.Id, jobTitle.Id, 10, 50, 50);
        var workVisaType = CreateTestWorkVisaType("WVZ200");
        var currency = CreateTestCurrency("CURZ200");

        EmployeeSponsor employeeSponsor = null;
        UsingDbContext(context =>
        {
            employeeSponsor = context.Set<EmployeeSponsor>().Add(new EmployeeSponsor
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeId = employee.Id,
                SponsorId = sponsor.Id,
                GovtJobTitleId = jobTitle.Id,
                StartDate = new DateTime(2024, 1, 1),
                EndDate = new DateTime(2024, 12, 31),
                SalaryCurrencyId = currency.Id,
                WorkVisaTypeId = workVisaType.Id
            }).Entity;
        });

        //Act
        await _appService.Delete(new Abp.Application.Services.Dto.EntityDto(employeeSponsor.Id));

        //Assert: floored at 0, not -1
        UsingDbContext(context =>
        {
            var updated = context.Set<SponsorJobTitle>().Find(sponsorJobTitle.Id);
            updated.CitizenQuotaUsed.ShouldBe(0);
        });
    }
}
