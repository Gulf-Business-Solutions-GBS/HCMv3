using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.Transaction;
using HCMv3.GridReports.Transaction.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

// ReSharper disable once InconsistentNaming
public class EmployeeNonPayrollReportAppService_Tests : AppTestBase
{
    private readonly IEmployeeNonPayrollReportAppService _appService;

    public EmployeeNonPayrollReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeNonPayrollReportAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private static string NewCode() => Guid.NewGuid().ToString("N").Substring(0, 8);

    private Employee CreateEmployee(string code)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Nationalities.Add(new Nationality { TenantId = TenantId, Code = NewCode(), NameEn = "N", NameAr = "N" }).Entity;
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
                JoiningDate = new DateTime(2015, 1, 1),
                ProbationEndDate = new DateTime(2015, 4, 1),
                NationalityId = nationality.Id
            }).Entity;
        });

        return employee;
    }

    private WageType CreateWageType(string code)
    {
        WageType wageType = null;
        UsingDbContext(context =>
        {
            wageType = context.Set<WageType>().Add(new WageType { TenantId = TenantId, Code = code, NameEn = code + "-En", NameAr = code + "-Ar" }).Entity;
        });
        return wageType;
    }

    private void CreateTrans(Employee employee, WageType wageType, DateTime paymentDate, double amount = 100, bool isLocked = false)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeNonPayrollTrans>().Add(new EmployeeNonPayrollTrans
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                WageTypeId = wageType.Id,
                Amount = amount,
                PaymentDate = paymentDate,
                IsLocked = isLocked
            });
        });
    }

    private static GetEmployeeNonPayrollReportInput Input(DateTime from, DateTime to, string employeeCode = null, string wageTypeCode = null)
    {
        return new GetEmployeeNonPayrollReportInput { FromDate = from, ToDate = to, EmployeeCode = employeeCode, WageTypeCode = wageTypeCode, MaxResultCount = 100, SkipCount = 0 };
    }

    [Fact]
    public async Task Should_Filter_By_Payment_Date_Window_And_WageType()
    {
        var wt = CreateWageType(NewCode());
        var otherWt = CreateWageType(NewCode());
        var employee = CreateEmployee("ENP-1");

        CreateTrans(employee, wt, new DateTime(2024, 6, 1));
        CreateTrans(employee, otherWt, new DateTime(2024, 6, 1));

        var result = await _appService.GetEmployeeNonPayrollReport(Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), "ENP-1", wt.Code));

        result.Items.Count.ShouldBe(1);
        result.Items.Single().WageTypeName.ShouldBe(wt.NameEn);
    }

    [Fact]
    public async Task Should_Resolve_OrgLevel_Columns_Without_Crashing_When_No_Org_Assignment()
    {
        var wt = CreateWageType(NewCode());
        var employee = CreateEmployee("ENP-ORG");
        CreateTrans(employee, wt, new DateTime(2024, 6, 1));

        var result = await _appService.GetEmployeeNonPayrollReport(Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), "ENP-ORG"));

        result.Items.Single().OrgLevel3.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task Should_Export_Every_Filtered_Row()
    {
        var wt = CreateWageType(NewCode());
        var a = CreateEmployee("ENPEXP-A");
        var b = CreateEmployee("ENPEXP-B");
        CreateTrans(a, wt, new DateTime(2024, 6, 1));
        CreateTrans(b, wt, new DateTime(2024, 6, 1));

        var file = await _appService.GetEmployeeNonPayrollReportToExcel(Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), "ENPEXP-A,ENPEXP-B"));

        file.FileName.ShouldBe("EmployeeNonPayroll.xlsx");
    }
}
