using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.QuotaProvision;
using HCMv3.GridReports.QuotaProvision.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

// ReSharper disable once InconsistentNaming
public class EndOfServiceReportAppService_Tests : AppTestBase
{
    private readonly IEndOfServiceReportAppService _appService;

    public EndOfServiceReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEndOfServiceReportAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(string code)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Set<Nationality>().Add(new Nationality
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
        });

        Employee employee = null;
        UsingDbContext(context =>
        {
            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId,
                Code = code,
                FirstNameEn = "F" + code,
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "Full " + code,
                FullNameAr = "عربي " + code,
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2018, 1, 1),
                ProbationEndDate = new DateTime(2018, 4, 1),
                NationalityId = nationality.Id,
                Status = EmployeeStatus.Active
            }).Entity;
        });

        return employee;
    }

    private WageType CreateWageType(string code)
    {
        WageType wageType = null;
        UsingDbContext(context =>
        {
            wageType = context.Set<WageType>().Add(new WageType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code + "-En",
                NameAr = code + "-Ar",
                PayrollOrder = 1,
                Status = true
            }).Entity;
        });

        return wageType;
    }

    private void CreateSalaryRegisterLine(Employee employee, DateTime period, string wageTypeCode, double amount)
    {
        var wageType = CreateWageType(wageTypeCode + "-" + Guid.NewGuid().ToString("N").Substring(0, 4));
        UsingDbContext(context =>
        {
            var register = context.Set<SalaryRegister>().Add(new SalaryRegister
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                ForPeriod = period,
                InPeriod = period,
                PayrollRunType = PayrollRunType.Regular
            }).Entity;
            context.SaveChanges();

            context.Set<SalaryRegisterDetail>().Add(new SalaryRegisterDetail
            {
                TenantId = TenantId,
                SalaryRegisterId = register.Id,
                WageTypeId = wageType.Id,
                Amount = amount,
                Rate = amount,
                Number = 1
            });
        });
    }

    [Fact]
    public async Task Should_Throw_When_Payroll_Calendar_Is_Missing()
    {
        CreateEmployee("EOS-NOCAL");

        await Should.ThrowAsync<Exception>(async () =>
            await _appService.GetEndOfServiceReport(new GetEndOfServiceReportInput
            {
                Month = 3,
                Year = 2099,
                MaxResultCount = 100
            }));
    }

    [Fact]
    public async Task Should_Compute_Net_Available_Balance_As_Gl_Provision_Minus_Loan()
    {
        var employee = CreateEmployee("EOS-CALC");
        var period = new DateTime(2024, 5, 31);

        UsingDbContext(context =>
        {
            var currency = context.Set<Currency>().Add(new Currency
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 3),
                NameEn = "Cur",
                NameAr = "Cur"
            }).Entity;

            var payrollPeriod = context.Set<PayrollPeriod>().Add(new PayrollPeriod
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "P",
                NameAr = "P"
            }).Entity;
            context.SaveChanges();

            var payrollArea = context.Set<PayrollArea>().Add(new PayrollArea
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                Name = "PA",
                CurrencyId = currency.Id,
                PayrollPeriodId = payrollPeriod.Id
            }).Entity;
            context.SaveChanges();

            context.Set<PayrollCalendar>().Add(new PayrollCalendar
            {
                TenantId = TenantId,
                PayrollAreaId = payrollArea.Id,
                PayrollMonth = 5,
                PayrollYear = 2024,
                StartDate = new DateTime(2024, 5, 1),
                EndDate = period
            });
        });

        CreateSalaryRegisterLine(employee, period, "3003", 1500);

        var loanType = default(LoanType);
        UsingDbContext(context =>
        {
            loanType = context.Set<LoanType>().Add(new LoanType
            {
                TenantId = TenantId,
                Code = "12",
                NameEn = "EOS Gratuity Loan",
                NameAr = "EOS Gratuity Loan"
            }).Entity;
            context.SaveChanges();

            context.Set<EmployeeLoan>().Add(new EmployeeLoan
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                LoanTypeId = loanType.Id,
                DueAmount = 400
            });
        });

        var result = await _appService.GetEndOfServiceReport(new GetEndOfServiceReportInput
        {
            Month = 5,
            Year = 2024,
            EmployeeCode = "EOS-CALC",
            MaxResultCount = 100
        });

        var row = result.Items.Single();
        row.GLProvisionAmount.ShouldBe(1500);
        row.LoanAmount.ShouldBe(400);
        row.NetAvailableBalance.ShouldBe(1100);
    }
}
