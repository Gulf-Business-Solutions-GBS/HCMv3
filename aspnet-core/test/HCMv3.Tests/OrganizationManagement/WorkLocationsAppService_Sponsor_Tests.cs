using System;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.GovtRelationsManagement;
using HCMv3.OrganizationManagement;
using HCMv3.OrganizationManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.OrganizationManagement;

/// <summary>
/// The legacy <c>WorkLocation.Sponsor</c> link, restored once GovtRelationsManagement was ported.
/// </summary>
// ReSharper disable once InconsistentNaming
public class WorkLocationsAppService_Sponsor_Tests : AppTestBase
{
    private readonly IWorkLocationsAppService _appService;

    public WorkLocationsAppService_Sponsor_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IWorkLocationsAppService>();
    }

    private Sponsor CreateTestSponsor(string code)
    {
        Nationality nationality = null;
        BusinessType businessType = null;
        Governorate governorate = null;
        Abp.Organizations.OrganizationUnit orgUnit = null;
        UsingDbContext(context =>
        {
            nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;

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
                NameEn = code + " Sponsor",
                NameAr = code,
                NationalityId = nationality.Id,
                BusinessTypeId = businessType.Id,
                GovernorateId = governorate.Id,
                OrganizationUnitId = orgUnit.Id
            }).Entity;
        });
        return sponsor;
    }

    private async Task<WorkLocationDto> GetByNameAsync(string nameEn)
    {
        var result = await _appService.GetAll(new GetAllWorkLocationsInput { Filter = nameEn, MaxResultCount = 10 });
        return result.Items.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Should_Save_Sponsor_And_List_Its_Name()
    {
        //Arrange
        var sponsor = CreateTestSponsor("WLS1");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditWorkLocationDto
        {
            NameEn = "Sponsored Location",
            NameAr = "Sponsored Location",
            IsActive = true,
            SponsorId = sponsor.Id
        });

        //Assert
        var item = await GetByNameAsync("Sponsored Location");
        item.SponsorId.ShouldBe(sponsor.Id);
        item.SponsorName.ShouldBe("WLS1 Sponsor");
    }

    [Fact]
    public async Task Should_Return_Sponsor_For_Edit_And_Clear_It_On_Update()
    {
        //Arrange
        var sponsor = CreateTestSponsor("WLS2");
        await _appService.CreateOrEdit(new CreateOrEditWorkLocationDto
        {
            NameEn = "Location To Clear",
            NameAr = "Location To Clear",
            IsActive = true,
            SponsorId = sponsor.Id
        });
        var item = await GetByNameAsync("Location To Clear");
        (await _appService.GetWorkLocationForEdit(new EntityDto(item.Id))).WorkLocation.SponsorId.ShouldBe(sponsor.Id);

        //Act
        await _appService.CreateOrEdit(new CreateOrEditWorkLocationDto
        {
            Id = item.Id,
            Code = item.Code,
            NameEn = "Location To Clear",
            NameAr = "Location To Clear",
            IsActive = true,
            SponsorId = null
        });

        //Assert
        (await GetByNameAsync("Location To Clear")).SponsorId.ShouldBeNull();
    }
}
