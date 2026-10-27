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
public class EmployeeAssetInvoicesAppService_Tests : AppTestBase
{
    private readonly IEmployeeAssetInvoicesAppService _appService;

    public EmployeeAssetInvoicesAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeAssetInvoicesAppService>();
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
    public async Task Should_Create_EmployeeAssetInvoice()
    {
        //Arrange
        var employee = CreateTestEmployee("EAI100");
        var asset = CreateTestAsset("ASI100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeAssetInvoiceDto
        {
            EmployeeId = employee.Id,
            AssetId = asset.Id,
            InvoiceDate = new DateTime(2024, 3, 15),
            InvoiceAmount = 50
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeAssetInvoicesInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_When_Duplicate_Invoice_For_Same_Month()
    {
        //Arrange
        var employee = CreateTestEmployee("EAI200");
        var asset = CreateTestAsset("ASI200");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeAssetInvoiceDto
        {
            EmployeeId = employee.Id,
            AssetId = asset.Id,
            InvoiceDate = new DateTime(2024, 3, 15),
            InvoiceAmount = 50
        });

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeAssetInvoiceDto
            {
                EmployeeId = employee.Id,
                AssetId = asset.Id,
                InvoiceDate = new DateTime(2024, 3, 20),
                InvoiceAmount = 60
            }));
    }
}
