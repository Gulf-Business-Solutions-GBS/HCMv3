using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.PayrollManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GeneralMasterInfo;

/// <summary>
/// The legacy <c>ViolationType.WageType</c> link and its <c>ViolationWageType</c> picker filter, restored once
/// Payroll was ported.
/// </summary>
// ReSharper disable once InconsistentNaming
public class DisciplinaryTypesAppService_WageType_Tests : AppTestBase
{
    private readonly IDisciplinaryTypesAppService _appService;

    public DisciplinaryTypesAppService_WageType_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IDisciplinaryTypesAppService>();
    }

    private int AddWageType(
        string code,
        BillingType billingType,
        bool status = true,
        bool inBasicPay = false,
        bool inCalculatedPay = false,
        bool inArrearPay = false)
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
                Status = status,
                IncludeInBasicPay = inBasicPay,
                IncludeInCalculatedPay = inCalculatedPay,
                IncludeInArrearPay = inArrearPay
            }).Entity;
            context.SaveChanges();
            id = entity.Id;
        });
        return id;
    }

    private async Task<DisciplinaryTypeDto> GetByNameAsync(string nameEn)
    {
        var result = await _appService.GetAll(new GetAllDisciplinaryTypesInput { Filter = nameEn, MaxResultCount = 10 });
        return result.Items.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Should_Save_Wage_Type_And_List_Its_Name()
    {
        //Arrange
        var wageType = AddWageType("2701", BillingType.Deduction);

        //Act
        await _appService.CreateOrEdit(new CreateOrEditDisciplinaryTypeDto
        {
            NameEn = "DTW Late Arrival",
            NameAr = "DTW Late Arrival",
            RepeatDurationInDays = 30,
            WageTypeId = wageType
        });

        //Assert
        var item = await GetByNameAsync("DTW Late Arrival");
        item.WageTypeId.ShouldBe(wageType);
        item.WageTypeName.ShouldBe("WT 2701");
    }

    [Fact]
    public async Task Should_Return_Wage_Type_For_Edit_And_Clear_It_On_Update()
    {
        //Arrange
        var wageType = AddWageType("2702", BillingType.Deduction);
        await _appService.CreateOrEdit(new CreateOrEditDisciplinaryTypeDto
        {
            NameEn = "DTW Absence",
            NameAr = "DTW Absence",
            RepeatDurationInDays = 30,
            WageTypeId = wageType
        });
        var item = await GetByNameAsync("DTW Absence");
        var forEdit = (await _appService.GetDisciplinaryTypeForEdit(new EntityDto(item.Id))).DisciplinaryType;
        forEdit.WageTypeId.ShouldBe(wageType);

        //Act
        forEdit.WageTypeId = null;
        await _appService.CreateOrEdit(forEdit);

        //Assert
        (await GetByNameAsync("DTW Absence")).WageTypeId.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Offer_Only_Legacy_Violation_Wage_Types()
    {
        //Arrange
        var allowed = AddWageType("2703", BillingType.Deduction);
        var earning = AddWageType("1703", BillingType.Earning);
        var inactive = AddWageType("2704", BillingType.Deduction, status: false);
        var basicPay = AddWageType("2705", BillingType.Deduction, inBasicPay: true);
        var calculated = AddWageType("2706", BillingType.Deduction, inCalculatedPay: true);
        var arrear = AddWageType("2707", BillingType.Deduction, inArrearPay: true);
        var netPay = AddWageType("9000", BillingType.Deduction);

        //Act
        var ids = (await _appService.GetViolationWageTypes(new NullableIdDto())).Items.Select(w => w.Id).ToList();

        //Assert
        ids.ShouldContain(allowed);
        ids.ShouldNotContain(earning);
        ids.ShouldNotContain(inactive);
        ids.ShouldNotContain(basicPay);
        ids.ShouldNotContain(calculated);
        ids.ShouldNotContain(arrear);
        ids.ShouldNotContain(netPay);
    }

    [Fact]
    public async Task Should_Keep_The_Current_Wage_Type_In_The_Picker()
    {
        //Arrange
        var earning = AddWageType("1704", BillingType.Earning);

        //Act
        var ids = (await _appService.GetViolationWageTypes(new NullableIdDto(earning))).Items.Select(w => w.Id).ToList();

        //Assert
        ids.ShouldContain(earning);
    }
}
