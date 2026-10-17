using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.PayrollManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GeneralMasterInfo;

/// <summary>
/// Loan Type Detail now carries the wage type a Variable loan's percentage is taken of, with legacy
/// <c>LoanTypeDetail.PerformValidation</c>: wage type required, listed once per loan type, percent above 0 and
/// at most 100.
/// </summary>
// ReSharper disable once InconsistentNaming
public class LoanTypeDetailsAppService_Tests : AppTestBase
{
    private readonly ILoanTypeDetailsAppService _appService;

    public LoanTypeDetailsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<ILoanTypeDetailsAppService>();
    }

    private (int LoanTypeId, int BasicId) Arrange()
    {
        var ids = (0, 0);
        UsingDbContext(context =>
        {
            var tenantId = AbpSession.TenantId ?? 1;
            var loanType = context.LoanTypes.Add(new LoanType
            {
                TenantId = tenantId,
                Code = "LT1",
                NameEn = "Salary Advance",
                NameAr = "سلفة",
                LoanDeductionType = LoanDeductionType.Variable
            }).Entity;
            var basic = context.WageTypes.Add(new WageType
            {
                TenantId = tenantId,
                Code = "1000",
                NameEn = "Basic",
                NameAr = "أساسي",
                BillingType = BillingType.Earning
            }).Entity;
            context.SaveChanges();
            ids = (loanType.Id, basic.Id);
        });
        return ids;
    }

    [Fact]
    public async Task Should_Save_The_WageType_And_List_It_By_Code_And_Name()
    {
        var (loanTypeId, basicId) = Arrange();

        await _appService.CreateOrEdit(new CreateOrEditLoanTypeDetailDto
        {
            LoanTypeId = loanTypeId,
            WageTypeId = basicId,
            Percent = 10
        });

        var row = (await _appService.GetAll(new GetAllLoanTypeDetailsInput { MaxResultCount = 10 })).Items.Single();
        row.WageTypeId.ShouldBe(basicId);
        row.WageTypeName.ShouldBe("1000 - Basic");
        row.Percent.ShouldBe(10);
    }

    [Fact]
    public async Task Should_Reject_A_Row_Without_A_WageType()
    {
        var (loanTypeId, _) = Arrange();

        await Should.ThrowAsync<UserFriendlyException>(() => _appService.CreateOrEdit(new CreateOrEditLoanTypeDetailDto
        {
            LoanTypeId = loanTypeId,
            Percent = 10
        }));
    }

    [Fact]
    public async Task Should_Reject_The_Same_WageType_Twice_On_One_LoanType()
    {
        var (loanTypeId, basicId) = Arrange();
        await _appService.CreateOrEdit(new CreateOrEditLoanTypeDetailDto
        {
            LoanTypeId = loanTypeId,
            WageTypeId = basicId,
            Percent = 10
        });

        await Should.ThrowAsync<UserFriendlyException>(() => _appService.CreateOrEdit(new CreateOrEditLoanTypeDetailDto
        {
            LoanTypeId = loanTypeId,
            WageTypeId = basicId,
            Percent = 5
        }));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100.5)]
    public async Task Should_Reject_A_Percent_Outside_0_To_100(double percent)
    {
        var (loanTypeId, basicId) = Arrange();

        await Should.ThrowAsync<UserFriendlyException>(() => _appService.CreateOrEdit(new CreateOrEditLoanTypeDetailDto
        {
            LoanTypeId = loanTypeId,
            WageTypeId = basicId,
            Percent = percent
        }));
    }

    [Fact]
    public async Task Editing_A_Row_Should_Not_Trip_Its_Own_Duplicate_Check()
    {
        var (loanTypeId, basicId) = Arrange();
        await _appService.CreateOrEdit(new CreateOrEditLoanTypeDetailDto
        {
            LoanTypeId = loanTypeId,
            WageTypeId = basicId,
            Percent = 10
        });
        var id = (await _appService.GetAll(new GetAllLoanTypeDetailsInput { MaxResultCount = 10 })).Items.Single().Id;

        await _appService.CreateOrEdit(new CreateOrEditLoanTypeDetailDto
        {
            Id = id,
            LoanTypeId = loanTypeId,
            WageTypeId = basicId,
            Percent = 100
        });

        UsingDbContext(context => context.LoanTypeDetails.Single(d => d.Id == id).Percent.ShouldBe(100));
    }
}
