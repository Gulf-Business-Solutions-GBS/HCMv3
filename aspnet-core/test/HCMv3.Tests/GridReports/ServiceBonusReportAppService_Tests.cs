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
public class ServiceBonusReportAppService_Tests : AppTestBase
{
    private readonly IServiceBonusReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public ServiceBonusReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IServiceBonusReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(string code, DateTime joiningDate)
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
                JoiningDate = joiningDate,
                ProbationEndDate = joiningDate.AddMonths(3),
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

    private void CreateDetail(SalaryRegister register, WageType wageType, double amount)
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
                Number = 0
            });
        });
    }

    [Fact]
    public async Task Should_Emit_No_Row_When_Employee_Has_Neither_Bonus_Nor_Arrear()
    {
        CreateEmployee("SB-NONE", new DateTime(2018, 1, 1));

        var result = await _appService.GetServiceBonusReport(new GetServiceBonusReportInput { MaxResultCount = 100 });

        result.Items.Select(i => i.EmployeeCode).ShouldNotContain("SB-NONE");
    }

    [Fact]
    public async Task Should_Compute_Whole_Years_Of_Service_From_Joining_Date_To_Bonus_Period()
    {
        var employee = CreateEmployee("SB-YEARS", new DateTime(2019, 6, 30));
        var register = CreateRegister(employee, new DateTime(2024, 6, 30));
        CreateDetail(register, CreateWageType("35"), 500);

        var result = await _appService.GetServiceBonusReport(new GetServiceBonusReportInput { MaxResultCount = 100 });

        var row = result.Items.Single(i => i.EmployeeCode == "SB-YEARS");
        row.Amount.ShouldBe(500);
        row.ServicePeriod.ShouldBe(5);
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_And_Resolve_Every_Header()
    {
        var a = CreateEmployee("SB-EXP-A", new DateTime(2018, 1, 1));
        var b = CreateEmployee("SB-EXP-B", new DateTime(2018, 1, 1));
        CreateDetail(CreateRegister(a, new DateTime(2024, 6, 30)), CreateWageType("35"), 100);
        CreateDetail(CreateRegister(b, new DateTime(2024, 6, 30)), CreateWageType("36"), 50);

        var file = await _appService.GetServiceBonusReportToExcel(new GetServiceBonusReportInput { MaxResultCount = 1 });

        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<System.Collections.Generic.IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(2);
        exported.First().Keys.Where(k => k.StartsWith("[") && k.EndsWith("]")).ShouldBeEmpty();
    }
}
