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
public class EmployeePaymentReportAppService_Tests : AppTestBase
{
    private readonly IEmployeePaymentReportAppService _appService;

    public EmployeePaymentReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeePaymentReportAppService>();
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

    private void CreatePayment(Employee employee, WageType wageType, DateTime paymentDate, double amount = 250)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeePayment>().Add(new EmployeePayment
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                WageTypeId = wageType.Id,
                Amount = amount,
                PaymentDate = paymentDate
            });
        });
    }

    private static GetEmployeePaymentReportInput Input(DateTime from, DateTime to, string employeeCode = null, string wageTypeCode = null)
    {
        return new GetEmployeePaymentReportInput { FromDate = from, ToDate = to, EmployeeCode = employeeCode, WageTypeCode = wageTypeCode, MaxResultCount = 100, SkipCount = 0 };
    }

    [Fact]
    public async Task Should_Project_Amount_As_Number_Column()
    {
        var wt = CreateWageType(NewCode());
        var employee = CreateEmployee("EPAY-1");
        CreatePayment(employee, wt, new DateTime(2024, 6, 1), amount: 325);

        var result = await _appService.GetEmployeePaymentReport(Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), "EPAY-1"));

        result.Items.Single().Number.ShouldBe(325);
    }

    [Fact]
    public async Task Should_Filter_By_WageType_Code()
    {
        var wt = CreateWageType(NewCode());
        var otherWt = CreateWageType(NewCode());
        var employee = CreateEmployee("EPAY-WT");
        CreatePayment(employee, wt, new DateTime(2024, 6, 1));
        CreatePayment(employee, otherWt, new DateTime(2024, 6, 1));

        var result = await _appService.GetEmployeePaymentReport(Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), "EPAY-WT", wt.Code));

        result.Items.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Resolve_OrgLevel_Columns_Without_Crashing()
    {
        var wt = CreateWageType(NewCode());
        var employee = CreateEmployee("EPAY-ORG");
        CreatePayment(employee, wt, new DateTime(2024, 6, 1));

        var result = await _appService.GetEmployeePaymentReport(Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), "EPAY-ORG"));

        result.Items.Single().OrgLevel1.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task Should_Export_Every_Filtered_Row()
    {
        var wt = CreateWageType(NewCode());
        var a = CreateEmployee("EPAYEXP-A");
        var b = CreateEmployee("EPAYEXP-B");
        CreatePayment(a, wt, new DateTime(2024, 6, 1));
        CreatePayment(b, wt, new DateTime(2024, 6, 1));

        var file = await _appService.GetEmployeePaymentReportToExcel(Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), "EPAYEXP-A,EPAYEXP-B"));

        file.FileName.ShouldBe("EmployeePayment.xlsx");
    }
}
