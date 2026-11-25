using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class SalaryRegisterDetailsAppService_Tests : PayrollManagementAppServiceTestBase
{
    private SalaryRegisterDetailsAppService CreateAppService()
    {
        return CreateService(() => new SalaryRegisterDetailsAppService(
            FakeRepository.For<SalaryRegisterDetail>(Context),
            FakeRepository.For<SalaryRegister>(Context)));
    }

    private int CreateEmployee()
    {
        var nationality = new HCMv3.GeneralMasterInfo.Nationality
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = "N",
            NameAr = "N"
        };
        Context.Nationalities.Add(nationality);
        Context.SaveChanges();

        var entity = new Employee
        {
            TenantId = AbpSession.TenantId ?? 1,
            FirstNameEn = "F",
            LastNameEn = "L",
            FirstNameAr = "ف",
            LastNameAr = "ل",
            NationalityId = nationality.Id,
            JoiningDate = new DateTime(2020, 1, 1)
        };
        Context.Employees.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreatePayrollReasonType(string code)
    {
        var entity = new PayrollReasonType { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.PayrollReasonTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateWageType(string code)
    {
        var entity = new WageType { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code, PayrollOrder = 1 };
        Context.WageTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateSalaryRegister(int employeeId, int payrollReasonTypeId, bool locked)
    {
        var entity = new SalaryRegister
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeId = employeeId,
            ForPeriod = new DateTime(2026, 1, 1),
            InPeriod = new DateTime(2026, 1, 1),
            PayrollRunType = PayrollRunType.Regular,
            PayrollResultType = PayrollResultType.Current,
            PayrollReasonTypeId = payrollReasonTypeId,
            Locked = locked
        };
        Context.SalaryRegisters.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_SalaryRegisterDetail_When_Parent_Not_Locked()
    {
        var employeeId = CreateEmployee();
        var reasonTypeId = CreatePayrollReasonType("SRD_RT1");
        var registerId = CreateSalaryRegister(employeeId, reasonTypeId, locked: false);
        var wageTypeId = CreateWageType("SRD_WT1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSalaryRegisterDetailDto
        {
            SalaryRegisterId = registerId,
            WageTypeId = wageTypeId,
            Rate = 10,
            Number = 5,
            Amount = 50
        });

        var result = await service.GetAll(new GetAllSalaryRegisterDetailsInput { SalaryRegisterId = registerId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].Amount.ShouldBe(50);
    }

    [Fact]
    public async Task Should_Throw_When_Creating_Detail_Under_Locked_Parent()
    {
        var employeeId = CreateEmployee();
        var reasonTypeId = CreatePayrollReasonType("SRD_RT2");
        var registerId = CreateSalaryRegister(employeeId, reasonTypeId, locked: true);
        var wageTypeId = CreateWageType("SRD_WT2");
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditSalaryRegisterDetailDto
            {
                SalaryRegisterId = registerId,
                WageTypeId = wageTypeId,
                Rate = 10,
                Number = 5,
                Amount = 50
            }));
    }

    [Fact]
    public async Task Should_Throw_When_Deleting_Detail_Under_Locked_Parent()
    {
        var employeeId = CreateEmployee();
        var reasonTypeId = CreatePayrollReasonType("SRD_RT3");
        var registerId = CreateSalaryRegister(employeeId, reasonTypeId, locked: false);
        var wageTypeId = CreateWageType("SRD_WT3");

        var detail = new SalaryRegisterDetail
        {
            TenantId = AbpSession.TenantId ?? 1,
            SalaryRegisterId = registerId,
            WageTypeId = wageTypeId,
            Rate = 10,
            Number = 5,
            Amount = 50
        };
        Context.SalaryRegisterDetails.Add(detail);
        Context.SaveChanges();

        // Lock the parent register after the detail was created.
        var register = Context.SalaryRegisters.Find(registerId);
        register.Locked = true;
        Context.SaveChanges();

        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.Delete(new Abp.Application.Services.Dto.EntityDto(detail.Id)));
    }

    private int CreateWageType(string code, int payrollOrder)
    {
        var id = CreateWageType(code);
        Context.WageTypes.Find(id).PayrollOrder = payrollOrder;
        Context.SaveChanges();
        return id;
    }

    private void AddDetail(int registerId, int wageTypeId, double amount)
    {
        Context.SalaryRegisterDetails.Add(new SalaryRegisterDetail
        {
            TenantId = AbpSession.TenantId ?? 1,
            SalaryRegisterId = registerId,
            WageTypeId = wageTypeId,
            Rate = amount,
            Number = 1,
            Amount = amount
        });
        Context.SaveChanges();
    }

    /// <summary>
    /// The register page lists only that register's lines, in payslip (wage type payroll order) order, with
    /// the wage type code, whatever order the lines were written in.
    /// </summary>
    [Fact]
    public async Task Should_List_A_Registers_Lines_In_Payroll_Order_With_Wage_Type_Code()
    {
        var employeeId = CreateEmployee();
        var reasonTypeId = CreatePayrollReasonType("SRD_ORD");
        var registerId = CreateSalaryRegister(employeeId, reasonTypeId, locked: false);
        var otherRegisterId = CreateSalaryRegister(employeeId, reasonTypeId, locked: false);
        var late = CreateWageType("SRD_LATE", payrollOrder: 20);
        var early = CreateWageType("SRD_EARLY", payrollOrder: 10);
        AddDetail(registerId, late, 200);
        AddDetail(registerId, early, 100);
        AddDetail(otherRegisterId, early, 999);
        var service = CreateAppService();

        var result = await service.GetAll(new GetAllSalaryRegisterDetailsInput { SalaryRegisterId = registerId });

        result.TotalCount.ShouldBe(2);
        result.Items[0].WageTypeCode.ShouldBe("SRD_EARLY");
        result.Items[0].PayrollOrder.ShouldBe(10);
        result.Items[0].Amount.ShouldBe(100);
        result.Items[1].WageTypeCode.ShouldBe("SRD_LATE");
    }

    /// <summary>
    /// Each line carries its wage type's billing type and "don't impact net pay" flag, which the register
    /// page uses to colour earnings and deductions as legacy did.
    /// </summary>
    [Fact]
    public async Task Should_Return_Billing_Type_And_Net_Pay_Impact_For_Row_Colours()
    {
        var registerId = CreateSalaryRegister(CreateEmployee(), CreatePayrollReasonType("SRD_CLR"), locked: false);
        var deductionId = CreateWageType("SRD_DED", payrollOrder: 1);
        var deduction = Context.WageTypes.Find(deductionId);
        deduction.BillingType = BillingType.Deduction;
        deduction.DontImpactNetPay = true;
        Context.SaveChanges();
        AddDetail(registerId, deductionId, 25);
        var service = CreateAppService();

        var result = await service.GetAll(new GetAllSalaryRegisterDetailsInput { SalaryRegisterId = registerId });

        result.Items.Count.ShouldBe(1);
        result.Items[0].BillingType.ShouldBe(BillingType.Deduction);
        result.Items[0].DontImpactNetPay.ShouldBeTrue();
    }
}
