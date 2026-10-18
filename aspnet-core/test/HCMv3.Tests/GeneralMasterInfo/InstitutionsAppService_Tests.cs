using System;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.GeneralMasterInfo.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GeneralMasterInfo;

// ReSharper disable once InconsistentNaming
public class InstitutionsAppService_Tests : AppTestBase
{
    private readonly IInstitutionsAppService _appService;

    public InstitutionsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IInstitutionsAppService>();
    }

    private Country CreateTestCountry(string nameEn)
    {
        Country country = null;
        UsingDbContext(context =>
        {
            country = context.Countries.Add(new Country
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = nameEn,
                NameAr = nameEn
            }).Entity;
        });
        return country;
    }

    private async Task<InstitutionDto> GetByNameAsync(string nameEn)
    {
        var result = await _appService.GetAll(new GetAllInstitutionsInput { Filter = nameEn, MaxResultCount = 10 });
        return result.Items.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Should_Save_Country_And_List_Its_Name()
    {
        //Arrange
        var country = CreateTestCountry("Kuwait");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditInstitutionDto
        {
            NameEn = "Institution With Country",
            NameAr = "Institution With Country",
            CountryId = country.Id
        });

        //Assert
        var item = await GetByNameAsync("Institution With Country");
        item.CountryId.ShouldBe(country.Id);
        item.CountryName.ShouldBe("Kuwait");
    }

    [Fact]
    public async Task Should_Return_Country_For_Edit()
    {
        //Arrange
        var country = CreateTestCountry("Oman");
        await _appService.CreateOrEdit(new CreateOrEditInstitutionDto
        {
            NameEn = "Institution For Edit",
            NameAr = "Institution For Edit",
            CountryId = country.Id
        });
        var item = await GetByNameAsync("Institution For Edit");

        //Act
        var forEdit = await _appService.GetInstitutionForEdit(new EntityDto(item.Id));

        //Assert
        forEdit.Institution.CountryId.ShouldBe(country.Id);
    }

    [Fact]
    public async Task Should_Change_And_Clear_Country_On_Update()
    {
        //Arrange
        var first = CreateTestCountry("Qatar");
        var second = CreateTestCountry("Bahrain");
        await _appService.CreateOrEdit(new CreateOrEditInstitutionDto
        {
            NameEn = "Institution Update",
            NameAr = "Institution Update",
            CountryId = first.Id
        });
        var item = await GetByNameAsync("Institution Update");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditInstitutionDto
        {
            Id = item.Id,
            Code = item.Code,
            NameEn = "Institution Update",
            NameAr = "Institution Update",
            CountryId = second.Id
        });

        //Assert
        (await GetByNameAsync("Institution Update")).CountryId.ShouldBe(second.Id);

        //Act - clear
        await _appService.CreateOrEdit(new CreateOrEditInstitutionDto
        {
            Id = item.Id,
            Code = item.Code,
            NameEn = "Institution Update",
            NameAr = "Institution Update",
            CountryId = null
        });

        //Assert
        (await GetByNameAsync("Institution Update")).CountryId.ShouldBeNull();
    }
}
