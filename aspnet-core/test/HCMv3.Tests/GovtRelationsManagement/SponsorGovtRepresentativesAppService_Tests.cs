using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.GovtRelationsManagement;
using HCMv3.GovtRelationsManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GovtRelationsManagement;

// ReSharper disable once InconsistentNaming
public class SponsorGovtRepresentativesAppService_Tests : GovtRelationsManagementAppServiceTestBase
{
    private SponsorGovtRepresentativesAppService CreateAppService()
    {
        return CreateService(() => new SponsorGovtRepresentativesAppService(FakeRepository.For<SponsorGovtRepresentative>(Context)));
    }

    private int CreateSponsor(string code)
    {
        var nationality = new Nationality { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.Nationalities.Add(nationality);
        var businessType = new BusinessType { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.BusinessTypes.Add(businessType);
        var governorate = new Governorate { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.Governorates.Add(governorate);
        var orgUnit = new Abp.Organizations.OrganizationUnit(AbpSession.TenantId, code, null) { Code = code };
        Context.OrganizationUnits.Add(orgUnit);
        Context.SaveChanges();

        var sponsor = new Sponsor
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code,
            NationalityId = nationality.Id,
            BusinessTypeId = businessType.Id,
            GovernorateId = governorate.Id,
            OrganizationUnitId = orgUnit.Id
        };
        Context.Sponsors.Add(sponsor);
        Context.SaveChanges();
        return sponsor.Id;
    }

    private int CreateEmployee(string code)
    {
        var nationality = new Nationality { TenantId = AbpSession.TenantId ?? 1, Code = "N" + code, NameEn = "N" + code, NameAr = "N" + code };
        Context.Nationalities.Add(nationality);
        Context.SaveChanges();

        var entity = new HCMv3.PersonnelAdministration.Employee
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            FirstNameEn = code,
            LastNameEn = code,
            FirstNameAr = code,
            LastNameAr = code,
            NationalityId = nationality.Id
        };
        Context.Employees.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_SponsorGovtRepresentative()
    {
        var sponsorId = CreateSponsor("SP1");
        var employeeId = CreateEmployee("EMP1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSponsorGovtRepresentativeDto
        {
            SponsorId = sponsorId,
            EmployeeId = employeeId,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 6, 30)
        });

        var result = await service.GetAll(new GetAllSponsorGovtRepresentativesInput { SponsorId = sponsorId });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_On_Overlapping_Date_Range()
    {
        var sponsorId = CreateSponsor("SP2");
        var employeeId1 = CreateEmployee("EMP2");
        var employeeId2 = CreateEmployee("EMP3");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSponsorGovtRepresentativeDto
        {
            SponsorId = sponsorId,
            EmployeeId = employeeId1,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 6, 30)
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditSponsorGovtRepresentativeDto
            {
                SponsorId = sponsorId,
                EmployeeId = employeeId2,
                StartDate = new DateTime(2025, 3, 1),
                EndDate = new DateTime(2025, 9, 30)
            }));
    }
}
