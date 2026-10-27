using System;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.PayrollManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GeneralMasterInfo;

/// <summary>
/// The legacy <c>Asset.Earning</c>/<c>Asset.Deduction</c> wage types, restored once Payroll was ported.
/// </summary>
// ReSharper disable once InconsistentNaming
public class AssetsAppService_WageTypes_Tests : AppTestBase
{
    private readonly IAssetsAppService _appService;

    public AssetsAppService_WageTypes_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IAssetsAppService>();
    }

    private int AddWageType(string code, BillingType billingType)
    {
        var id = 0;
        UsingDbContext(context =>
        {
            var entity = context.WageTypes.Add(new WageType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = "WT " + code,
                NameAr = "WT " + code,
                BillingType = billingType,
                Status = true
            }).Entity;
            context.SaveChanges();
            id = entity.Id;
        });
        return id;
    }

    private static CreateOrEditAssetDto NewAsset(string name, int? earningId, int? deductionId)
    {
        return new CreateOrEditAssetDto
        {
            NameEn = name,
            NameAr = name,
            Year = 2026,
            ExpiryDate = new DateTime(2030, 1, 1),
            EarningWageTypeId = earningId,
            DeductionWageTypeId = deductionId
        };
    }

    private async Task<AssetDto> GetByNameAsync(string name)
    {
        var result = await _appService.GetAll(new GetAllAssetsInput { Filter = name, MaxResultCount = 10 });
        return result.Items.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Should_Save_Earning_And_Deduction_Wage_Types_And_List_Their_Names()
    {
        //Arrange
        var earning = AddWageType("1501", BillingType.Earning);
        var deduction = AddWageType("2501", BillingType.Deduction);

        //Act
        await _appService.CreateOrEdit(NewAsset("Car With Wage Types", earning, deduction));

        //Assert
        var item = await GetByNameAsync("Car With Wage Types");
        item.EarningWageTypeId.ShouldBe(earning);
        item.EarningWageTypeName.ShouldBe("WT 1501");
        item.DeductionWageTypeId.ShouldBe(deduction);
        item.DeductionWageTypeName.ShouldBe("WT 2501");
    }

    [Fact]
    public async Task Should_Return_Wage_Types_For_Edit()
    {
        //Arrange
        var earning = AddWageType("1502", BillingType.Earning);
        var deduction = AddWageType("2502", BillingType.Deduction);
        await _appService.CreateOrEdit(NewAsset("Laptop For Edit", earning, deduction));
        var item = await GetByNameAsync("Laptop For Edit");

        //Act
        var forEdit = await _appService.GetAssetForEdit(new EntityDto(item.Id));

        //Assert
        forEdit.Asset.EarningWageTypeId.ShouldBe(earning);
        forEdit.Asset.DeductionWageTypeId.ShouldBe(deduction);
    }

    [Fact]
    public async Task Should_Clear_Wage_Types_On_Update()
    {
        //Arrange
        var earning = AddWageType("1503", BillingType.Earning);
        await _appService.CreateOrEdit(NewAsset("Phone To Clear", earning, null));
        var item = await GetByNameAsync("Phone To Clear");

        //Act
        var update = NewAsset("Phone To Clear", null, null);
        update.Id = item.Id;
        update.Code = item.Code;
        await _appService.CreateOrEdit(update);

        //Assert
        var updated = await GetByNameAsync("Phone To Clear");
        updated.EarningWageTypeId.ShouldBeNull();
        updated.DeductionWageTypeId.ShouldBeNull();
    }
}
