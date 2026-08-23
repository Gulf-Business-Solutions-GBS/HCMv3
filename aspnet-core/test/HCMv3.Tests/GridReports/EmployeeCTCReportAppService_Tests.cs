using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.Employees;
using HCMv3.GridReports.Employees.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Employee CTC grid-report port (legacy <c>uscRptEmployeeCTC.ascx</c>, <c>Table1()</c>).
/// The distinctive semantics pinned here are the dynamic basic-pay wage-type pivot, the payroll-area
/// filter, the salary-register provision columns and the "export ignores page size" rule.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeCTCReportAppService_Tests : AppTestBase
{
    private const string BasicPayWageTypeCode = "1000";
    private const string AllowanceWageTypeCode = "1001";
    private const string IndemnityProvisionWageTypeCode = "3003";
    private const string LeaveProvisionWageTypeCode = "3004";

    private readonly IEmployeeCTCReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeCTCReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeCTCReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(string code, EmployeeStatus status = EmployeeStatus.Active)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "NAT-" + code,
                NameAr = "NAT-" + code + "-Ar"
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
                Status = status
            }).Entity;
        });

        return employee;
    }

    private WageType GetOrCreateWageType(string code, int payrollOrder = 0)
    {
        WageType wageType = null;
        UsingDbContext(context =>
        {
            wageType = context.Set<WageType>().FirstOrDefault(w => w.Code == code);
            if (wageType == null)
            {
                wageType = context.Set<WageType>().Add(new WageType
                {
                    TenantId = TenantId,
                    Code = code,
                    NameEn = "WT-" + code,
                    NameAr = "WT-" + code + "-Ar",
                    PayrollOrder = payrollOrder
                }).Entity;
            }
        });

        return wageType;
    }

    private PayrollArea CreatePayrollArea(string code)
    {
        PayrollArea payrollArea = null;

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

            payrollArea = context.Set<PayrollArea>().Add(new PayrollArea
            {
                TenantId = TenantId,
                Code = code,
                Name = "PA-" + code,
                CurrencyId = currency.Id,
                PayrollPeriodId = payrollPeriod.Id
            }).Entity;

            context.SaveChanges();
        });

        return payrollArea;
    }

    /// <summary>Creates an org assignment current as of today -- the only one legacy's CTC columns read.</summary>
    private void CreateCurrentOrgAssignment(Employee employee, PayrollArea payrollArea)
    {
        var today = DateTime.Now.Date;

        UsingDbContext(context =>
        {
            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                StartDate = today.AddYears(-1),
                EndDate = today.AddYears(1),
                PayrollAreaId = payrollArea?.Id
            });
        });
    }

    private void CreateCurrentBasicPay(Employee employee, params (string wageTypeCode, double amount)[] details)
    {
        var today = DateTime.Now.Date;

        UsingDbContext(context =>
        {
            var currency = context.Set<Currency>().Add(new Currency
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 3),
                NameEn = "Cur",
                NameAr = "Cur"
            }).Entity;
            context.SaveChanges();

            var basicPay = context.Set<EmployeeBasicPay>().Add(new EmployeeBasicPay
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                StartDate = today.AddYears(-1),
                EndDate = today.AddYears(1),
                Amount = details.Sum(d => d.amount)
            }).Entity;
            context.SaveChanges();

            foreach (var detail in details)
            {
                var wageType = context.Set<WageType>().First(w => w.Code == detail.wageTypeCode);

                context.Set<EmployeeBasicPayDetail>().Add(new EmployeeBasicPayDetail
                {
                    TenantId = TenantId,
                    EmployeeBasicPayId = basicPay.Id,
                    WageTypeId = wageType.Id,
                    CurrencyId = currency.Id,
                    Amount = detail.amount
                });
            }
        });
    }

    private void CreateRegularRegisterWithProvisions(Employee employee, DateTime period)
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
                PayrollRunType = PayrollRunType.Regular
            }).Entity;
        });

        var leaveWageType = GetOrCreateWageType(LeaveProvisionWageTypeCode);
        var indemnityWageType = GetOrCreateWageType(IndemnityProvisionWageTypeCode);

        UsingDbContext(context =>
        {
            context.Set<SalaryRegisterDetail>().Add(new SalaryRegisterDetail
            {
                TenantId = TenantId,
                SalaryRegisterId = register.Id,
                WageTypeId = leaveWageType.Id,
                Number = 12,
                Amount = 1200
            });

            context.Set<SalaryRegisterDetail>().Add(new SalaryRegisterDetail
            {
                TenantId = TenantId,
                SalaryRegisterId = register.Id,
                WageTypeId = indemnityWageType.Id,
                Number = 30,
                Amount = 3000
            });
        });
    }

    private static GetEmployeeCTCReportInput Input(
        string employeeCode = null,
        EmployeeStatus? status = null,
        int? payrollAreaId = null,
        int maxResultCount = 100)
    {
        return new GetEmployeeCTCReportInput
        {
            EmployeeCode = employeeCode,
            EmployeeStatus = status,
            PayrollAreaId = payrollAreaId,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Pivot_Basic_Pay_Wage_Types_And_Report_The_Effective_Date_And_Total()
    {
        GetOrCreateWageType(BasicPayWageTypeCode, 1);
        GetOrCreateWageType(AllowanceWageTypeCode, 2);

        var employee = CreateEmployee("CTC-WAGE");
        CreateCurrentBasicPay(employee, (BasicPayWageTypeCode, 400), (AllowanceWageTypeCode, 100));

        var result = await _appService.GetEmployeeCTCReport(Input(employeeCode: "CTC-WAGE"));

        result.WageTypeNames.ShouldContain("WT-" + BasicPayWageTypeCode);
        result.WageTypeNames.ShouldContain("WT-" + AllowanceWageTypeCode);

        var row = result.Items.Single();
        row.WageAmounts.Single(w => w.WageTypeCode == BasicPayWageTypeCode).Amount.ShouldBe(400);
        row.WageAmounts.Single(w => w.WageTypeCode == AllowanceWageTypeCode).Amount.ShouldBe(100);

        // Legacy TotalSalary is EmployeeBasicPay.Amount, not the sum of the details.
        row.TotalSalary.ShouldBe(500);
        row.EffectiveDate.ShouldNotBeNull();
    }

    [Fact]
    public async Task Should_Read_Provisions_From_The_Latest_Regular_In_Period_Register()
    {
        var employee = CreateEmployee("CTC-PROV");

        CreateRegularRegisterWithProvisions(employee, new DateTime(2024, 6, 30));

        var result = await _appService.GetEmployeeCTCReport(Input(employeeCode: "CTC-PROV"));

        var row = result.Items.Single();
        row.LeaveBalance.ShouldBe(12);
        row.LeaveProvisionAmount.ShouldBe(1200);
        row.IndemnityBalance.ShouldBe(30);
        row.IndemnityProvisionAmount.ShouldBe(3000);
    }

    [Fact]
    public async Task Should_Filter_By_Payroll_Area_Of_The_Current_Org_Assignment()
    {
        var included = CreateEmployee("CTC-PA-IN");
        var excluded = CreateEmployee("CTC-PA-OUT");

        var wanted = CreatePayrollArea("CTC-PA-1");
        var other = CreatePayrollArea("CTC-PA-2");

        CreateCurrentOrgAssignment(included, wanted);
        CreateCurrentOrgAssignment(excluded, other);

        var result = await _appService.GetEmployeeCTCReport(
            Input(employeeCode: "CTC-PA-IN,CTC-PA-OUT", payrollAreaId: wanted.Id));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("CTC-PA-IN");
        codes.ShouldNotContain("CTC-PA-OUT");

        result.Items.Single().PayrollAreaName.ShouldBe("PA-CTC-PA-1");
    }

    [Fact]
    public async Task Should_Filter_By_Employee_Status()
    {
        CreateEmployee("CTC-ST-ACTIVE");
        CreateEmployee("CTC-ST-INACTIVE", EmployeeStatus.Inactive);

        var result = await _appService.GetEmployeeCTCReport(
            Input(employeeCode: "CTC-ST-ACTIVE,CTC-ST-INACTIVE", status: EmployeeStatus.Inactive));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("CTC-ST-INACTIVE");
        codes.ShouldNotContain("CTC-ST-ACTIVE");
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        CreateEmployee("CTC-EXP-A");
        CreateEmployee("CTC-EXP-B");
        CreateEmployee("CTC-EXP-C");
        CreateEmployee("CTC-EXP-OUT");

        // Page size of 1 -- the export must ignore it and still return every filtered row.
        var file = await _appService.GetEmployeeCTCReportToExcel(
            Input(employeeCode: "CTC-EXP-A,CTC-EXP-B,CTC-EXP-C", maxResultCount: 1));

        file.FileName.ShouldBe("EmployeeCTC.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(3);
        var exportedCodes = exported.Select(r => r["Code"]?.ToString()).OrderBy(c => c).ToList();
        exportedCodes.ShouldBe(new List<string> { "CTC-EXP-A", "CTC-EXP-B", "CTC-EXP-C" });
    }
}
