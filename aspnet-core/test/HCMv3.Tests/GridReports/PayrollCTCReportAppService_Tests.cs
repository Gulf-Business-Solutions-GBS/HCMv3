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
public class PayrollCTCReportAppService_Tests : AppTestBase
{
    private readonly IPayrollCTCReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public PayrollCTCReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IPayrollCTCReportAppService>();
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
                Status = status
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

    private SalaryRegister CreateRegister(Employee employee, DateTime period, PayrollRunType runType = PayrollRunType.Regular)
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
                PayrollRunType = runType,
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
    public async Task Should_Project_Basic_Salary_And_Overtime_From_The_Regular_Register()
    {
        var employee = CreateEmployee("CTC-1");
        var register = CreateRegister(employee, new DateTime(2024, 6, 30));
        CreateDetail(register, CreateWageType("1000"), 500, 30);
        CreateDetail(register, CreateWageType("1007"), 40, 5);

        var result = await _appService.GetPayrollCTCReport(
            new GetPayrollCTCReportInput { Month = 6, Year = 2024, MaxResultCount = 100 });

        var row = result.Items.Single(i => i.EmployeeCode == "CTC-1");
        row.BasicSalary.ShouldBe(500);
        row.WorkingDays.ShouldBe(30);
        row.OvertimeAmount.ShouldBe(40);
        row.OvertimeHours.ShouldBe(5);
    }

    /// <summary>
    /// LEGACY DEFECT REPRODUCED: the base query hard-pins Employee.Status == Active, and the optional
    /// status filter is applied on top -- selecting Inactive returns zero rows even though a Regular
    /// register exists for that (inactive) employee.
    /// </summary>
    [Fact]
    public async Task Should_Return_No_Rows_When_A_Non_Active_Status_Is_Selected_Reproducing_Legacy_Defect()
    {
        var employee = CreateEmployee("CTC-INACTIVE", EmployeeStatus.Inactive);
        CreateDetail(CreateRegister(employee, new DateTime(2024, 6, 30)), CreateWageType("1000"), 500);

        var result = await _appService.GetPayrollCTCReport(new GetPayrollCTCReportInput
        {
            Month = 6,
            Year = 2024,
            EmployeeStatus = EmployeeStatus.Inactive,
            MaxResultCount = 100
        });

        result.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_And_Resolve_Every_Header()
    {
        var a = CreateEmployee("CTC-EXP-A");
        var b = CreateEmployee("CTC-EXP-B");
        CreateDetail(CreateRegister(a, new DateTime(2024, 6, 30)), CreateWageType("1000"), 100);
        CreateDetail(CreateRegister(b, new DateTime(2024, 6, 30)), CreateWageType("1000"), 200);

        var file = await _appService.GetPayrollCTCReportToExcel(
            new GetPayrollCTCReportInput { Month = 6, Year = 2024, MaxResultCount = 1 });

        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<System.Collections.Generic.IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(2);
        exported.First().Keys.Where(k => k.StartsWith("[") && k.EndsWith("]")).ShouldBeEmpty();
    }
}
