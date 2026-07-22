using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Runtime.Validation;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.GeneralMasterInfo.Dto;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GeneralMasterInfo;

// ReSharper disable once InconsistentNaming
public class AccessUnitsAppService_Tests : AppTestBase
{
    private readonly IAccessUnitsAppService _accessUnitsAppService;

    public AccessUnitsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _accessUnitsAppService = Resolve<IAccessUnitsAppService>();
    }

    private AccessUnit CreateTestAccessUnit(string code, string nameEn, string nameAr, string ip = "127.0.0.1")
    {
        AccessUnit entity = null;
        UsingDbContext(context =>
        {
            entity = context.AccessUnits.Add(new AccessUnit
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = nameEn,
                NameAr = nameAr,
                IPAddress = ip
            }).Entity;
        });
        return entity;
    }

    [Fact]
    public async Task Should_Create_AccessUnit()
    {
        //Act
        await _accessUnitsAppService.CreateOrEdit(new CreateOrEditAccessUnitDto
        {
            Code = "100",
            NameEn = "Head Office",
            NameAr = "المكتب الرئيسي",
            IPAddress = "10.0.0.1"
        });

        //Assert
        UsingDbContext(context =>
        {
            var unit = context.AccessUnits.FirstOrDefault(a => a.Code == "100");
            unit.ShouldNotBeNull();
            unit.NameEn.ShouldBe("Head Office");
            unit.NameAr.ShouldBe("المكتب الرئيسي");
        });
    }

    [Fact]
    public async Task Should_Auto_Generate_Code_When_Blank()
    {
        //Arrange
        CreateTestAccessUnit("5", "Existing", "موجود");

        //Act - create without a code
        await _accessUnitsAppService.CreateOrEdit(new CreateOrEditAccessUnitDto
        {
            NameEn = "Auto Coded",
            NameAr = "مشفر تلقائيا"
        });

        //Assert - next numeric code = 6
        UsingDbContext(context =>
        {
            var unit = context.AccessUnits.FirstOrDefault(a => a.NameEn == "Auto Coded");
            unit.ShouldNotBeNull();
            unit.Code.ShouldBe("6");
        });
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Code()
    {
        //Arrange
        CreateTestAccessUnit("200", "First", "الأول");

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _accessUnitsAppService.CreateOrEdit(new CreateOrEditAccessUnitDto
            {
                Code = "200",
                NameEn = "Second",
                NameAr = "الثاني"
            }));
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Name()
    {
        //Arrange
        CreateTestAccessUnit("300", "Warehouse", "مستودع");

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _accessUnitsAppService.CreateOrEdit(new CreateOrEditAccessUnitDto
            {
                Code = "301",
                NameEn = "Warehouse",
                NameAr = "مستودع 2"
            }));
    }

    [Fact]
    public async Task Should_Get_All_With_Filter()
    {
        //Arrange
        CreateTestAccessUnit("400", "Alpha Site", "ألفا");
        CreateTestAccessUnit("401", "Beta Site", "بيتا");

        //Act
        var result = await _accessUnitsAppService.GetAll(new GetAllAccessUnitsInput { Filter = "Alpha" });

        //Assert
        result.TotalCount.ShouldBe(1);
        result.Items.Single().NameEn.ShouldBe("Alpha Site");
    }

    [Fact]
    public async Task Should_Get_AccessUnit_For_Edit()
    {
        //Arrange
        var entity = CreateTestAccessUnit("500", "Editable", "قابل للتعديل");

        //Act
        var output = await _accessUnitsAppService.GetAccessUnitForEdit(new EntityDto(entity.Id));

        //Assert
        output.AccessUnit.ShouldNotBeNull();
        output.AccessUnit.Id.ShouldBe(entity.Id);
        output.AccessUnit.NameEn.ShouldBe("Editable");
    }

    [Fact]
    public async Task Should_Update_AccessUnit()
    {
        //Arrange
        var entity = CreateTestAccessUnit("600", "Old Name", "اسم قديم");

        //Act
        await _accessUnitsAppService.CreateOrEdit(new CreateOrEditAccessUnitDto
        {
            Id = entity.Id,
            Code = "600",
            NameEn = "New Name",
            NameAr = "اسم جديد",
            IPAddress = "192.168.1.1"
        });

        //Assert
        UsingDbContext(context =>
        {
            var unit = context.AccessUnits.Single(a => a.Id == entity.Id);
            unit.NameEn.ShouldBe("New Name");
            unit.IPAddress.ShouldBe("192.168.1.1");
        });
    }

    [Fact]
    public async Task Should_Delete_AccessUnit()
    {
        //Arrange
        var entity = CreateTestAccessUnit("700", "To Delete", "للحذف");

        //Act
        await _accessUnitsAppService.Delete(new EntityDto(entity.Id));

        //Assert - FullAuditedEntity performs a soft delete (row remains with IsDeleted = true)
        UsingDbContext(context =>
        {
            var unit = context.AccessUnits.IgnoreQueryFilters().Single(a => a.Id == entity.Id);
            unit.IsDeleted.ShouldBeTrue();
        });
    }
}
