using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Uow;
using HCMv3.Authorization.Roles;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// The wage type clone behind "Copy ... and create a new wage type" (legacy <c>ObjectCopyController.GetWageType</c>).
/// </summary>
// ReSharper disable once InconsistentNaming
public class WageTypeCopyService_Tests : AppTestBase
{
    private readonly IWageTypeCopyService _service;

    public WageTypeCopyService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _service = Resolve<IWageTypeCopyService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private int AddWageType(
        string code,
        BillingType billingType = BillingType.Deduction,
        int payrollOrder = 0,
        bool inArrearPay = false,
        int? arrearSourceId = null,
        Category category = Category.Custom)
    {
        var id = 0;
        UsingDbContext(context =>
        {
            var entity = context.WageTypes.Add(new WageType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = "WT " + code,
                NameAr = "WT " + code,
                BillingType = billingType,
                BillingMethod = BillingMethod.FixedDaysPerMonth,
                Rate = 2.5,
                FixedDays = 26,
                Status = false,
                Category = category,
                PayrollOrder = payrollOrder,
                IncludeInArrearPay = inArrearPay,
                IncludeInIndemnity = true,
                ArrearSourceWageTypeId = arrearSourceId
            }).Entity;
            context.SaveChanges();
            id = entity.Id;
        });
        return id;
    }

    private async Task<int> CopyAsync(int sourceId, string name)
    {
        using var uow = Resolve<IUnitOfWorkManager>().Begin();
        var result = await _service.CopyAsync(sourceId, name);
        await uow.CompleteAsync();
        return result;
    }

    private WageType Get(int id)
    {
        return UsingDbContext(context => context.WageTypes.Single(w => w.Id == id));
    }

    [Fact]
    public async Task Should_Copy_Settings_Under_The_New_Name_As_Active()
    {
        var source = AddWageType("2001");

        var copy = Get(await CopyAsync(source, "Car Loan"));

        copy.Id.ShouldNotBe(source);
        copy.NameEn.ShouldBe("Car Loan");
        copy.NameAr.ShouldBe("Car Loan");
        copy.Status.ShouldBeTrue();
        copy.BillingType.ShouldBe(BillingType.Deduction);
        copy.BillingMethod.ShouldBe(BillingMethod.FixedDaysPerMonth);
        copy.Rate.ShouldBe(2.5);
        copy.FixedDays.ShouldBe(26);
        copy.IncludeInIndemnity.ShouldBeTrue();
        copy.ArrearSourceWageTypeId.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Take_The_Next_Free_Custom_Code_Of_The_Same_Billing_Type()
    {
        var source = AddWageType("2001");
        AddWageType("2005");
        AddWageType("2006", category: Category.Technical);
        AddWageType("2010", billingType: BillingType.Earning);
        AddWageType("9000");

        var copy = Get(await CopyAsync(source, "Copy"));

        // Highest custom deduction is 2005 (the technical, earning and Net Pay codes don't count); 2006 is taken.
        copy.Code.ShouldBe("2007");
    }

    [Fact]
    public async Task Should_Order_A_Loan_Deduction_After_The_Other_Loan_Deductions()
    {
        var source = AddWageType("2001", payrollOrder: 50100);
        var otherLoan = AddWageType("2002", payrollOrder: 50200);
        AddWageType("2003", payrollOrder: 40000);
        UsingDbContext(context =>
        {
            context.LoanTypes.Add(new LoanType { TenantId = TenantId, Code = "1", NameEn = "A", NameAr = "A", WageTypeId = source });
            context.LoanTypes.Add(new LoanType { TenantId = TenantId, Code = "2", NameEn = "B", NameAr = "B", WageTypeId = otherLoan });
        });

        var copy = Get(await CopyAsync(source, "Copy"));

        copy.PayrollOrder.ShouldBe(50210);
    }

    [Fact]
    public async Task Should_Order_Any_Other_Wage_Type_Below_The_Reserved_Range()
    {
        var source = AddWageType("2001", payrollOrder: 300);
        AddWageType("2002", payrollOrder: 49000);
        AddWageType("2003", payrollOrder: 50500);

        var copy = Get(await CopyAsync(source, "Copy"));

        copy.PayrollOrder.ShouldBe(49010);
    }

    [Fact]
    public async Task Should_Copy_Leave_Exception_And_Role_Rows()
    {
        var source = AddWageType("2001");
        var except = AddWageType("2002");
        UsingDbContext(context =>
        {
            var absence = context.AbsenceTypes.Add(new AbsenceType { TenantId = TenantId, Code = "AL", NameEn = "Annual", NameAr = "سنوية" }).Entity;
            var role = context.Roles.Add(new Role(TenantId, "WtCopyRole", "WtCopyRole")).Entity;
            context.SaveChanges();
            context.WageTypeLeaves.Add(new WageTypeLeave { TenantId = TenantId, WageTypeId = source, AbsenceTypeId = absence.Id });
            context.WageTypeExceptions.Add(new WageTypeException { TenantId = TenantId, WageTypeId = source, ExceptWageTypeId = except });
            context.WageTypeSecurityRoles.Add(new WageTypeSecurityRole { TenantId = TenantId, WageTypeId = source, RoleId = role.Id });
        });

        var copyId = await CopyAsync(source, "Copy");

        UsingDbContext(context =>
        {
            context.WageTypeLeaves.Count(r => r.WageTypeId == copyId).ShouldBe(1);
            context.WageTypeExceptions.Single(r => r.WageTypeId == copyId).ExceptWageTypeId.ShouldBe(except);
            context.WageTypeSecurityRoles.Count(r => r.WageTypeId == copyId).ShouldBe(1);
        });
    }

    [Fact]
    public async Task Should_Copy_The_Arrear_Wage_Type_And_Point_It_At_The_Copy()
    {
        var source = AddWageType("2001", payrollOrder: 100);
        AddWageType("3001", inArrearPay: true, arrearSourceId: source);

        var copyId = await CopyAsync(source, "Car Loan");

        var arrear = UsingDbContext(context => context.WageTypes.Single(w => w.ArrearSourceWageTypeId == copyId));
        arrear.NameEn.ShouldBe("Car Loan Arrear");
        arrear.Code.ShouldBe("3002");
        arrear.IncludeInArrearPay.ShouldBeTrue();
        arrear.PayrollOrder.ShouldBe(Get(copyId).PayrollOrder + 10);
    }

    [Fact]
    public async Task Should_Not_Create_An_Arrear_Copy_When_The_Source_Has_None()
    {
        var source = AddWageType("2001");

        var copyId = await CopyAsync(source, "Copy");

        UsingDbContext(context => context.WageTypes.Any(w => w.ArrearSourceWageTypeId == copyId).ShouldBeFalse());
    }
}
