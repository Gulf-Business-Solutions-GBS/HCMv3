using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.Authorization.Roles;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// The wage types a non-payroll transaction may use: legacy <c>EmployeeNonPayrollTrans.NonPayrollWageType</c>
/// (HCM10.Module, EmployeeNonPayrollTrans.cs 39-75), offered by <c>GetNonPayrollWageTypes</c> and enforced on save.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeNonPayrollTrans_WageTypeFilter_Tests : AppTestBase
{
    private readonly IEmployeeNonPayrollTransAppService _appService;

    public EmployeeNonPayrollTrans_WageTypeFilter_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeNonPayrollTransAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private int CreateWageType(
        string code,
        bool status = true,
        BillingType billingType = BillingType.Earning,
        bool calculatedPay = true,
        bool basicPay = false,
        bool arrearPay = false,
        bool secured = false)
    {
        var id = 0;
        UsingDbContext(context =>
        {
            id = context.Set<WageType>().Add(new WageType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code,
                NameAr = code,
                PayrollOrder = 1,
                Status = status,
                BillingType = billingType,
                IncludeInCalculatedPay = calculatedPay,
                IncludeInBasicPay = basicPay,
                IncludeInArrearPay = arrearPay,
                IsSecured = secured
            }).Entity.Id;
            context.SaveChanges();
        });
        return id;
    }

    private async Task<int[]> OfferedIds()
    {
        return (await _appService.GetNonPayrollWageTypes()).Items.Select(w => w.Id).ToArray();
    }

    [Fact]
    public async Task Should_Offer_An_Active_Calculated_Earning()
    {
        var id = CreateWageType("NP-OK");

        (await OfferedIds()).ShouldContain(id);
    }

    [Fact]
    public async Task Should_Offer_A_Calculated_Deduction()
    {
        var id = CreateWageType("NP-DED", billingType: BillingType.Deduction);

        (await OfferedIds()).ShouldContain(id);
    }

    [Theory]
    [InlineData("inactive")]
    [InlineData("time type")]
    [InlineData("provision")]
    [InlineData("not calculated pay")]
    [InlineData("basic pay")]
    [InlineData("arrear pay")]
    public async Task Should_Not_Offer_A_Wage_Type_Legacy_Left_Out(string reason)
    {
        var id = reason switch
        {
            "inactive" => CreateWageType("NP-X1", status: false),
            "time type" => CreateWageType("NP-X2", billingType: BillingType.TimeType),
            "provision" => CreateWageType("NP-X3", billingType: BillingType.Provision),
            "not calculated pay" => CreateWageType("NP-X4", calculatedPay: false),
            "basic pay" => CreateWageType("NP-X5", basicPay: true),
            _ => CreateWageType("NP-X6", arrearPay: true)
        };

        (await OfferedIds()).ShouldNotContain(id);
    }

    [Fact]
    public async Task Should_Not_Offer_Net_Pay_9000()
    {
        var id = CreateWageType("9000");

        (await OfferedIds()).ShouldNotContain(id);
    }

    [Fact]
    public async Task Should_Not_Offer_A_Wage_Type_Owned_By_A_Loan_Type()
    {
        var id = CreateWageType("NP-LOAN");
        UsingDbContext(context =>
        {
            context.Set<LoanType>().Add(new LoanType
            {
                TenantId = TenantId,
                Code = "NPL" + Guid.NewGuid().ToString("N").Substring(0, 4),
                NameEn = "Loan",
                NameAr = "Loan",
                WageTypeId = id
            });
        });

        (await OfferedIds()).ShouldNotContain(id);
    }

    [Fact]
    public async Task Should_Offer_A_Secured_Wage_Type_Only_To_A_Role_It_Names()
    {
        var hidden = CreateWageType("NP-SEC1", secured: true);
        var shown = CreateWageType("NP-SEC2", secured: true);

        UsingDbContext(context =>
        {
            var adminRoleId = context.Roles.Single(r => r.TenantId == TenantId && r.Name == StaticRoleNames.Tenants.Admin).Id;
            context.Set<WageTypeSecurityRole>().Add(new WageTypeSecurityRole
            {
                TenantId = TenantId,
                WageTypeId = shown,
                RoleId = adminRoleId
            });
        });

        var offered = await OfferedIds();
        offered.ShouldContain(shown);
        offered.ShouldNotContain(hidden);
    }

    [Fact]
    public async Task Should_Refuse_To_Save_A_Wage_Type_That_Is_Not_Offered()
    {
        var employeeId = CreateEmployee("NP-EMP1");
        var notAllowed = CreateWageType("NP-REF", calculatedPay: false);

        await Should.ThrowAsync<UserFriendlyException>(() => _appService.CreateOrEdit(new CreateOrEditEmployeeNonPayrollTransDto
        {
            EmployeeId = employeeId,
            WageTypeId = notAllowed,
            Amount = 10,
            PaymentDate = new DateTime(2026, 1, 15)
        }));
    }

    [Fact]
    public async Task Should_Keep_An_Old_Record_Whose_Wage_Type_No_Longer_Qualifies_When_It_Is_Not_Changed()
    {
        // Legacy only filtered the dropdown; it never refused a save. A record created before the wage type stopped
        // qualifying must stay editable as long as its wage type is left alone.
        var employeeId = CreateEmployee("NP-EMP2");
        var wageTypeId = CreateWageType("NP-OLD");
        var id = 0;
        UsingDbContext(context =>
        {
            id = context.Set<EmployeeNonPayrollTrans>().Add(new EmployeeNonPayrollTrans
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                WageTypeId = wageTypeId,
                Amount = 10,
                PaymentDate = new DateTime(2026, 1, 15)
            }).Entity.Id;
            context.SaveChanges();
            context.Set<WageType>().Single(w => w.Id == wageTypeId).Status = false;
        });

        await _appService.CreateOrEdit(new CreateOrEditEmployeeNonPayrollTransDto
        {
            Id = id,
            EmployeeId = employeeId,
            WageTypeId = wageTypeId,
            Amount = 20,
            PaymentDate = new DateTime(2026, 1, 15)
        });

        UsingDbContext(context => context.Set<EmployeeNonPayrollTrans>().Single(t => t.Id == id).Amount.ShouldBe(20));
    }

    private int CreateEmployee(string code)
    {
        var id = 0;
        UsingDbContext(context =>
        {
            var nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
            context.SaveChanges();

            id = context.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId,
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
            }).Entity.Id;
            context.SaveChanges();
        });
        return id;
    }
}
