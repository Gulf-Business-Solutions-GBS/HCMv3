using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GeneralMasterInfo;

/// <summary>
/// The Loan Types wage type dropdown offers what legacy <c>LoanType.LoanDedWageType</c> offered: active
/// deduction wage types outside basic/calculated/arrear pay, not Net Pay, not used by a leave, overtime or
/// violation type.
/// </summary>
// ReSharper disable once InconsistentNaming
public class LoanTypesAppService_GetDeductionWageTypes_Tests : AppTestBase
{
    private readonly ILoanTypesAppService _appService;

    public LoanTypesAppService_GetDeductionWageTypes_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<ILoanTypesAppService>();
    }

    private int AddWageType(string code, BillingType billingType, bool status = true, bool inBasicPay = false)
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
                IncludeInBasicPay = inBasicPay
            }).Entity;
            context.SaveChanges();
            id = entity.Id;
        });
        return id;
    }

    [Fact]
    public async Task Should_Offer_Only_Free_Active_Deductions()
    {
        var loanDeduction = AddWageType("2001", BillingType.Deduction);
        var earning = AddWageType("1000", BillingType.Earning);
        var inactive = AddWageType("2002", BillingType.Deduction, status: false);
        var basicPayDeduction = AddWageType("2003", BillingType.Deduction, inBasicPay: true);
        var netPay = AddWageType("9000", BillingType.Deduction);
        var leaveDeduction = AddWageType("2004", BillingType.Deduction);
        var overtimeDeduction = AddWageType("2005", BillingType.Deduction);
        var violationDeduction = AddWageType("2006", BillingType.Deduction);
        UsingDbContext(context =>
        {
            var tenantId = AbpSession.TenantId ?? 1;
            context.AbsenceTypes.Add(new AbsenceType
            {
                TenantId = tenantId,
                Code = "UL",
                NameEn = "Unpaid",
                NameAr = "بدون أجر",
                WageTypeId = leaveDeduction
            });
            context.OvertimeTypes.Add(new OvertimeType
            {
                TenantId = tenantId,
                Code = "OT",
                NameEn = "Overtime",
                NameAr = "إضافي",
                WageTypeId = overtimeDeduction
            });
            context.DisciplinaryTypes.Add(new DisciplinaryType
            {
                TenantId = tenantId,
                Code = "LATE",
                NameEn = "Late arrival",
                NameAr = "تأخير",
                WageTypeId = violationDeduction
            });
        });

        var ids = (await _appService.GetDeductionWageTypes(new NullableIdDto())).Items.Select(i => i.Id).ToList();

        ids.ShouldContain(loanDeduction);
        ids.ShouldNotContain(earning);
        ids.ShouldNotContain(inactive);
        ids.ShouldNotContain(basicPayDeduction);
        ids.ShouldNotContain(netPay);
        ids.ShouldNotContain(leaveDeduction);
        ids.ShouldNotContain(overtimeDeduction);
        ids.ShouldNotContain(violationDeduction);
    }

    [Fact]
    public async Task Should_Still_List_The_LoanTypes_Current_WageType()
    {
        var inactive = AddWageType("2002", BillingType.Deduction, status: false);

        var ids = (await _appService.GetDeductionWageTypes(new NullableIdDto(inactive))).Items.Select(i => i.Id);

        ids.ShouldContain(inactive);
    }
}
