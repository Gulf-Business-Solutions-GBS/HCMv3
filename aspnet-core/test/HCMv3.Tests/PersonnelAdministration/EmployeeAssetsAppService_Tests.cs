using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeAssetsAppService_Tests : AppTestBase
{
    private readonly IEmployeeAssetsAppService _appService;

    public EmployeeAssetsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeAssetsAppService>();
    }

    private Employee CreateTestEmployee(string code)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
        });

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
                NationalityId = nationality.Id
            }).Entity;
        });
        return employee;
    }

    private Asset CreateTestAsset(string code)
    {
        Asset asset = null;
        UsingDbContext(context =>
        {
            asset = context.Set<Asset>().Add(new Asset
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });
        return asset;
    }

    [Fact]
    public async Task Should_Create_EmployeeAsset()
    {
        //Arrange
        var employee = CreateTestEmployee("EA100");
        var asset = CreateTestAsset("AS100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeAssetDto
        {
            EmployeeId = employee.Id,
            AssetId = asset.Id,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 12, 31),
            AssetDeductionMode = AssetDeductionMode.Full
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeAssetsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_When_EndDate_Earlier_Than_StartDate()
    {
        //Arrange
        var employee = CreateTestEmployee("EA200");
        var asset = CreateTestAsset("AS200");

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeAssetDto
            {
                EmployeeId = employee.Id,
                AssetId = asset.Id,
                StartDate = new DateTime(2024, 12, 31),
                EndDate = new DateTime(2024, 1, 1)
            }));
    }
}
