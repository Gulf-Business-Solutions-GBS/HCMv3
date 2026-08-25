using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.PayrollReport;
using HCMv3.GridReports.PayrollReport.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

// ReSharper disable once InconsistentNaming
public class SalaryForecastReportAppService_Tests : AppTestBase
{
    private readonly ISalaryForecastReportAppService _appService;

    public SalaryForecastReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<ISalaryForecastReportAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(string code)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Nationalities.Add(new Nationality
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
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
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
                NameEn = "WT-" + code,
                NameAr = "WT-" + code,
                ShortNameEn = code,
                ShortNameAr = code,
                Status = true
            }).Entity;
        });

        return wageType;
    }

    private void CreateBasicPay(Employee employee, WageType wageType, double amount)
    {
        UsingDbContext(context =>
        {
            var basicPay = context.Set<EmployeeBasicPay>().Add(new EmployeeBasicPay
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                StartDate = DateTime.Today.AddYears(-1),
                EndDate = DateTime.Today.AddYears(1),
                Amount = amount
            }).Entity;

            context.SaveChanges();

            var currency = context.Set<Currency>().Add(new Currency
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 3),
                NameEn = "Cur",
                NameAr = "Cur"
            }).Entity;

            context.SaveChanges();

            context.Set<EmployeeBasicPayDetail>().Add(new EmployeeBasicPayDetail
            {
                TenantId = TenantId,
                EmployeeBasicPayId = basicPay.Id,
                WageTypeId = wageType.Id,
                Amount = amount,
                CurrencyId = currency.Id
            });
        });
    }

    [Fact]
    public async Task Should_Compound_The_Current_Amount_Forward_Five_Years_At_The_Requested_Rate()
    {
        var employee = CreateEmployee("SF-1");
        CreateBasicPay(employee, CreateWageType("1000"), 1000);

        var result = await _appService.GetSalaryForecastReport(new GetSalaryForecastReportInput
        {
            SalaryChangePercent = 10,
            MaxResultCount = 100
        });

        result.ForecastYears.Count.ShouldBe(5);
        var total = result.Items.Sum(i => i.CurrentAmount);
        total.ShouldBe(1000);

        var row = result.Items.First();
        row.ForecastAmounts.Length.ShouldBe(5);
        row.ForecastAmounts[0].ShouldBe(1100, 0.001);
    }

    [Fact]
    public async Task Should_Restrict_To_BasicPay_Wage_Type_When_ForecastType_Is_BasicPay()
    {
        var employee = CreateEmployee("SF-2");
        CreateBasicPay(employee, CreateWageType("1000"), 500);
        CreateBasicPay(employee, CreateWageType("1002"), 200);

        var allResult = await _appService.GetSalaryForecastReport(new GetSalaryForecastReportInput
        {
            ForecastType = SalaryForecastType.All,
            MaxResultCount = 100
        });
        var basicOnlyResult = await _appService.GetSalaryForecastReport(new GetSalaryForecastReportInput
        {
            ForecastType = SalaryForecastType.BasicPay,
            MaxResultCount = 100
        });

        allResult.Items.Sum(i => i.CurrentAmount).ShouldBe(700);
        basicOnlyResult.Items.Sum(i => i.CurrentAmount).ShouldBe(500);
    }
}
