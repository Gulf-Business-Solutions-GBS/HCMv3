using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.PayrollManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GeneralMasterInfo;

/// <summary>
/// Asset Types, now with the legacy <c>IncludeInWorkflow</c> and <c>WageType</c> fields and its own service.
/// </summary>
// ReSharper disable once InconsistentNaming
public class AssetTypesAppService_Tests : AppTestBase
{
    private readonly IAssetTypesAppService _appService;

    public AssetTypesAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IAssetTypesAppService>();
    }

    private int AddWageType(string code)
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
                BillingType = BillingType.Earning,
                Status = true
            }).Entity;
            context.SaveChanges();
            id = entity.Id;
        });
        return id;
    }

    private async Task<AssetTypeDto> GetByNameAsync(string nameEn)
    {
        var result = await _appService.GetAll(new GetAllAssetTypesInput { Filter = nameEn, MaxResultCount = 10 });
        return result.Items.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Should_Create_Asset_Type_With_Workflow_Switch_And_Wage_Type()
    {
        //Arrange
        var wageType = AddWageType("1601");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditAssetTypeDto
        {
            NameEn = "ATT Vehicles",
            NameAr = "ATT مركبات",
            IncludeInWorkflow = true,
            WageTypeId = wageType
        });

        //Assert
        var item = await GetByNameAsync("ATT Vehicles");
        item.IncludeInWorkflow.ShouldBeTrue();
        item.WageTypeId.ShouldBe(wageType);
        item.WageTypeName.ShouldBe("WT 1601");
        item.Code.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_Default_IncludeInWorkflow_To_False()
    {
        //Act
        await _appService.CreateOrEdit(new CreateOrEditAssetTypeDto { NameEn = "ATT Furniture", NameAr = "ATT أثاث" });

        //Assert
        var item = await GetByNameAsync("ATT Furniture");
        item.IncludeInWorkflow.ShouldBeFalse();
        item.WageTypeId.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Get_For_Edit_And_Update()
    {
        //Arrange
        var wageType = AddWageType("1602");
        await _appService.CreateOrEdit(new CreateOrEditAssetTypeDto { NameEn = "ATT Phones", NameAr = "ATT هواتف" });
        var item = await GetByNameAsync("ATT Phones");

        //Act
        var forEdit = await _appService.GetAssetTypeForEdit(new EntityDto(item.Id));
        forEdit.AssetType.IncludeInWorkflow = true;
        forEdit.AssetType.WageTypeId = wageType;
        await _appService.CreateOrEdit(forEdit.AssetType);

        //Assert
        var updated = (await _appService.GetAssetTypeForEdit(new EntityDto(item.Id))).AssetType;
        updated.IncludeInWorkflow.ShouldBeTrue();
        updated.WageTypeId.ShouldBe(wageType);
        updated.Code.ShouldBe(item.Code);
    }

    [Fact]
    public async Task Should_Reject_Duplicate_Name()
    {
        //Arrange
        await _appService.CreateOrEdit(new CreateOrEditAssetTypeDto { NameEn = "ATT Laptops", NameAr = "ATT حواسيب" });

        //Act / Assert
        await Should.ThrowAsync<UserFriendlyException>(() =>
            _appService.CreateOrEdit(new CreateOrEditAssetTypeDto { NameEn = "ATT Laptops", NameAr = "ATT Other" }));
    }

    [Fact]
    public async Task Should_Reject_Duplicate_Code()
    {
        //Arrange
        await _appService.CreateOrEdit(new CreateOrEditAssetTypeDto { Code = "AT-DUP", NameEn = "ATT Tools", NameAr = "ATT أدوات" });

        //Act / Assert
        await Should.ThrowAsync<UserFriendlyException>(() =>
            _appService.CreateOrEdit(new CreateOrEditAssetTypeDto { Code = "AT-DUP", NameEn = "ATT Other Tools", NameAr = "ATT Other" }));
    }

    [Fact]
    public async Task Should_Delete_Asset_Type()
    {
        //Arrange
        await _appService.CreateOrEdit(new CreateOrEditAssetTypeDto { NameEn = "ATT Uniforms", NameAr = "ATT زي" });
        var item = await GetByNameAsync("ATT Uniforms");

        //Act
        await _appService.Delete(new EntityDto(item.Id));

        //Assert
        var result = await _appService.GetAll(new GetAllAssetTypesInput { Filter = "ATT Uniforms", MaxResultCount = 10 });
        result.Items.Any().ShouldBeFalse();
    }
}
