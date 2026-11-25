using System.Linq;
using System.Threading.Tasks;
using Abp.Runtime.Validation;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.PayrollManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GeneralMasterInfo;

/// <summary>
/// Copy Loan Type: legacy <c>ObjectCopyController</c> "Copy LoanType" (<c>LoanTypeCopy</c>).
/// </summary>
// ReSharper disable once InconsistentNaming
public class LoanTypesAppService_Copy_Tests : AppTestBase
{
    private readonly ILoanTypesAppService _appService;

    public LoanTypesAppService_Copy_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<ILoanTypesAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private (int LoanTypeId, int WageTypeId, int DetailWageTypeId) AddSourceLoanType(bool withWageType = true)
    {
        var result = (0, 0, 0);
        UsingDbContext(context =>
        {
            var wageType = context.WageTypes.Add(new WageType
            {
                TenantId = TenantId, Code = "2001", NameEn = "Loan", NameAr = "Loan", BillingType = BillingType.Deduction
            }).Entity;
            var detailWageType = context.WageTypes.Add(new WageType
            {
                TenantId = TenantId, Code = "1000", NameEn = "Basic", NameAr = "Basic", BillingType = BillingType.Earning
            }).Entity;
            context.SaveChanges();

            var loanType = context.LoanTypes.Add(new LoanType
            {
                TenantId = TenantId,
                Code = "7",
                NameEn = "Personal Loan",
                NameAr = "قرض شخصي",
                LoanDeductionType = LoanDeductionType.Variable,
                RecoveryOption = RecoveryOption.ThroughPayroll,
                EligiblePeriod = 6,
                EligibleCountPerYear = 2,
                MaxInstallments = 12,
                EligibleAmount = EligibleAmount.BasicSalary,
                EligibleAmountNumber = 1500,
                WageTypeId = withWageType ? wageType.Id : null
            }).Entity;
            context.SaveChanges();

            context.LoanTypeDetails.Add(new LoanTypeDetail
            {
                TenantId = TenantId, LoanTypeId = loanType.Id, WageTypeId = detailWageType.Id, Percent = 40
            });
            context.SaveChanges();

            result = (loanType.Id, wageType.Id, detailWageType.Id);
        });
        return result;
    }

    private LoanType GetLoanType(int id) => UsingDbContext(context => context.LoanTypes.Single(t => t.Id == id));

    [Fact]
    public async Task Should_Copy_Settings_With_A_New_Code()
    {
        var source = AddSourceLoanType();

        var copy = GetLoanType(await _appService.Copy(new CopyLoanTypeInput
        {
            LoanTypeId = source.LoanTypeId, NameEn = " Car Loan ", NameAr = "قرض سيارة"
        }));

        copy.NameEn.ShouldBe("Car Loan");
        copy.NameAr.ShouldBe("قرض سيارة");
        copy.Code.ShouldBe("8");
        copy.LoanDeductionType.ShouldBe(LoanDeductionType.Variable);
        copy.RecoveryOption.ShouldBe(RecoveryOption.ThroughPayroll);
        copy.EligiblePeriod.ShouldBe(6);
        copy.EligibleCountPerYear.ShouldBe(2);
        copy.MaxInstallments.ShouldBe(12);
        copy.EligibleAmount.ShouldBe(EligibleAmount.BasicSalary);
        copy.EligibleAmountNumber.ShouldBe(1500);
    }

    [Fact]
    public async Task Should_Copy_The_Wage_Type_Percentages()
    {
        var source = AddSourceLoanType();

        var copyId = await _appService.Copy(new CopyLoanTypeInput { LoanTypeId = source.LoanTypeId, NameEn = "Car Loan" });

        UsingDbContext(context =>
        {
            var detail = context.LoanTypeDetails.Single(d => d.LoanTypeId == copyId);
            detail.WageTypeId.ShouldBe(source.DetailWageTypeId);
            detail.Percent.ShouldBe(40);
        });
    }

    [Fact]
    public async Task Should_Default_The_Arabic_Name_To_The_English_Name()
    {
        var source = AddSourceLoanType();

        var copy = GetLoanType(await _appService.Copy(new CopyLoanTypeInput { LoanTypeId = source.LoanTypeId, NameEn = "Car Loan" }));

        copy.NameAr.ShouldBe("Car Loan");
    }

    [Fact]
    public async Task Should_Link_The_Same_Wage_Type_By_Default()
    {
        var source = AddSourceLoanType();

        var copy = GetLoanType(await _appService.Copy(new CopyLoanTypeInput { LoanTypeId = source.LoanTypeId, NameEn = "Car Loan" }));

        copy.WageTypeId.ShouldBe(source.WageTypeId);
    }

    [Fact]
    public async Task Should_Use_A_New_Wage_Type_When_Asked_To_Create_One()
    {
        var source = AddSourceLoanType();

        var copy = GetLoanType(await _appService.Copy(new CopyLoanTypeInput
        {
            LoanTypeId = source.LoanTypeId,
            NameEn = "Car Loan",
            WageTypeOption = WageTypeCopyOption.CopyAndCreate,
            WageTypeName = "Car Loan Deduction"
        }));

        copy.WageTypeId.ShouldNotBeNull();
        copy.WageTypeId.ShouldNotBe(source.WageTypeId);
        UsingDbContext(context =>
            context.WageTypes.Single(w => w.Id == copy.WageTypeId).NameEn.ShouldBe("Car Loan Deduction"));
    }

    [Fact]
    public async Task Should_Leave_The_Wage_Type_Empty_When_The_Source_Has_None()
    {
        var source = AddSourceLoanType(withWageType: false);

        var copy = GetLoanType(await _appService.Copy(new CopyLoanTypeInput
        {
            LoanTypeId = source.LoanTypeId,
            NameEn = "Car Loan",
            WageTypeOption = WageTypeCopyOption.CopyAndCreate,
            WageTypeName = "Car Loan Deduction"
        }));

        copy.WageTypeId.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Require_A_Wage_Type_Name_When_Creating_One()
    {
        var source = AddSourceLoanType();

        await Should.ThrowAsync<UserFriendlyException>(() => _appService.Copy(new CopyLoanTypeInput
        {
            LoanTypeId = source.LoanTypeId,
            NameEn = "Car Loan",
            WageTypeOption = WageTypeCopyOption.CopyAndCreate,
            WageTypeName = "  "
        }));
    }

    [Fact]
    public async Task Should_Refuse_A_Name_Already_In_Use()
    {
        var source = AddSourceLoanType();

        await Should.ThrowAsync<UserFriendlyException>(() => _appService.Copy(new CopyLoanTypeInput
        {
            LoanTypeId = source.LoanTypeId, NameEn = "Personal Loan"
        }));
    }

    [Fact]
    public async Task Should_Require_An_English_Name()
    {
        var source = AddSourceLoanType();

        await Should.ThrowAsync<AbpValidationException>(() => _appService.Copy(new CopyLoanTypeInput
        {
            LoanTypeId = source.LoanTypeId, NameEn = ""
        }));
    }

    [Fact]
    public async Task Should_Not_Create_A_Wage_Type_When_The_Copy_Is_Refused()
    {
        var source = AddSourceLoanType();
        var before = UsingDbContext(context => context.WageTypes.Count());

        await Should.ThrowAsync<UserFriendlyException>(() => _appService.Copy(new CopyLoanTypeInput
        {
            LoanTypeId = source.LoanTypeId,
            NameEn = "Personal Loan",
            WageTypeOption = WageTypeCopyOption.CopyAndCreate,
            WageTypeName = "Car Loan Deduction"
        }));

        UsingDbContext(context => context.WageTypes.Count()).ShouldBe(before);
    }
}
