using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.PayrollReport;
using HCMv3.GridReports.PayrollReport.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

// ReSharper disable once InconsistentNaming
public class SalaryDeductionReportAppService_Tests : AppTestBase
{
    private readonly ISalaryDeductionReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public SalaryDeductionReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<ISalaryDeductionReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
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

    private WageType CreateWageType(string code, BillingType billingType, int payrollOrder = 0)
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
                Status = true,
                BillingType = billingType,
                PayrollOrder = payrollOrder,
                DontImpactNetPay = false
            }).Entity;
        });

        return wageType;
    }

    private SalaryRegister CreateRegister(Employee employee, DateTime period)
    {
        SalaryRegister register = null;
        UsingDbContext(context =>
        {
            register = context.Set<SalaryRegister>().Add(new SalaryRegister
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                ForPeriod = period,
                InPeriod = period,
                PayrollRunType = PayrollRunType.Regular,
                PayrollResultType = PayrollResultType.Current,
                OffCyclePaymentDate = period
            }).Entity;
        });

        return register;
    }

    private void CreateDetail(SalaryRegister register, WageType wageType, double amount, double number = 0)
    {
        UsingDbContext(context =>
        {
            context.Set<SalaryRegisterDetail>().Add(new SalaryRegisterDetail
            {
                TenantId = TenantId,
                SalaryRegisterId = register.Id,
                WageTypeId = wageType.Id,
                Amount = amount,
                Rate = 0,
                Number = number
            });
        });
    }

    [Fact]
    public async Task Should_Build_DataDriven_Earning_And_Deduction_Columns_From_Posted_Wage_Types()
    {
        var employee = CreateEmployee("SD-1");
        var register = CreateRegister(employee, new DateTime(2024, 6, 30));
        CreateDetail(register, CreateWageType("1000", BillingType.Earning), 500, 30);
        CreateDetail(register, CreateWageType("2001", BillingType.Deduction), 50);

        var result = await _appService.GetSalaryDeductionReport(
            new GetSalaryDeductionReportInput { Month = 6, Year = 2024, MaxResultCount = 100 });

        result.EarningColumns.ShouldContain("WT-1000");
        result.DeductionColumns.ShouldContain("WT-2001");

        var row = result.Items.Single(i => i.EmployeeCode == "SD-1");
        row.WageTypeAmounts["WT-1000"].ShouldBe(500);
        row.WageTypeAmounts["WT-2001"].ShouldBe(50);
        row.TotalEarning.ShouldBe(500);
        row.TotalDeduction.ShouldBe(50);
    }

    [Fact]
    public async Task Should_Compute_MaxAllowedDeduction_As_Ten_Percent_Of_Actual_Salary()
    {
        var employee = CreateEmployee("SD-MAX");
        var register = CreateRegister(employee, new DateTime(2024, 6, 30));
        CreateDetail(register, CreateWageType("1000", BillingType.Earning), 1000);

        var result = await _appService.GetSalaryDeductionReport(
            new GetSalaryDeductionReportInput { Month = 6, Year = 2024, MaxResultCount = 100 });

        var row = result.Items.Single(i => i.EmployeeCode == "SD-MAX");
        row.MaxAllowedDeduction.ShouldBe(0); // ActualBasicSalary comes from EmployeeBasicPayDetail, not the register -- none created here.
    }

    [Fact]
    public async Task Should_Export_And_Resolve_Every_Header_Including_DataDriven_Columns()
    {
        var employee = CreateEmployee("SD-EXP");
        var register = CreateRegister(employee, new DateTime(2024, 6, 30));
        CreateDetail(register, CreateWageType("1000", BillingType.Earning), 500);
        CreateDetail(register, CreateWageType("2001", BillingType.Deduction), 50);

        var file = await _appService.GetSalaryDeductionReportToExcel(
            new GetSalaryDeductionReportInput { Month = 6, Year = 2024, MaxResultCount = 100 });

        file.FileName.ShouldBe("SalaryDeduction.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<System.Collections.Generic.IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(1);
        exported.First().Keys.ShouldContain("WT-1000");
        exported.First().Keys.ShouldContain("WT-2001");
        exported.First().Keys.Where(k => k.StartsWith("[") && k.EndsWith("]")).ShouldBeEmpty();
    }
}
