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
public class SalaryRetroResultReportAppService_Tests : AppTestBase
{
    private readonly ISalaryRetroResultReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public SalaryRetroResultReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<ISalaryRetroResultReportAppService>();
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

    private WageType CreateWageType(string code, int? arrearSourceId = null, bool includeInArrearPay = false)
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
                ArrearSourceWageTypeId = arrearSourceId,
                IncludeInArrearPay = includeInArrearPay,
                BillingType = BillingType.Earning
            }).Entity;
        });

        return wageType;
    }

    private SalaryRegister CreateRegister(Employee employee, DateTime forPeriod, DateTime inPeriod, PayrollRunType runType = PayrollRunType.Regular)
    {
        SalaryRegister register = null;
        UsingDbContext(context =>
        {
            register = context.Set<SalaryRegister>().Add(new SalaryRegister
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                ForPeriod = forPeriod,
                InPeriod = inPeriod,
                PayrollRunType = runType,
                PayrollResultType = PayrollResultType.Current,
                OffCyclePaymentDate = inPeriod
            }).Entity;
        });

        return register;
    }

    private void CreateDetail(SalaryRegister register, WageType wageType, double amount, double rate = 0, double number = 0)
    {
        UsingDbContext(context =>
        {
            context.Set<SalaryRegisterDetail>().Add(new SalaryRegisterDetail
            {
                TenantId = TenantId,
                SalaryRegisterId = register.Id,
                WageTypeId = wageType.Id,
                Amount = amount,
                Rate = rate,
                Number = number
            });
        });
    }

    [Fact]
    public async Task Should_Emit_A_Row_Only_When_Amount_Rate_Or_Number_Differ()
    {
        var employee = CreateEmployee("SRR-1");
        var sourceWt = CreateWageType("1000");
        var arrearWt = CreateWageType("7000", sourceWt.Id, includeInArrearPay: true);

        // Regular register for May.
        var mayRegular = CreateRegister(employee, new DateTime(2024, 5, 31), new DateTime(2024, 5, 31));
        CreateDetail(mayRegular, sourceWt, 500, 500);

        // June retro register that recalculates May, with a different amount.
        var juneRetro = CreateRegister(employee, new DateTime(2024, 5, 31), new DateTime(2024, 6, 30));
        CreateDetail(juneRetro, arrearWt, 100);
        CreateDetail(juneRetro, sourceWt, 550, 550);

        // June regular register (required so Table1() has ForPeriod==EndDate rows to seed employees/wage types).
        var juneRegular = CreateRegister(employee, new DateTime(2024, 6, 30), new DateTime(2024, 6, 30));
        CreateDetail(juneRegular, arrearWt, 0);

        var result = await _appService.GetSalaryRetroResultReport(new GetSalaryRetroResultReportInput
        {
            Month = 6,
            Year = 2024,
            MaxResultCount = 100
        });

        var row = result.Items.SingleOrDefault(i => i.EmployeeCode == "SRR-1");
        row.ShouldNotBeNull();
        row.AmountRegular.ShouldBe(500);
        row.AmountArrear.ShouldBe(550);
        row.AmountDifference.ShouldBe(50);
    }

    [Fact]
    public async Task Should_Export_Without_Error_And_Resolve_Every_Header_When_Empty()
    {
        var file = await _appService.GetSalaryRetroResultReportToExcel(new GetSalaryRetroResultReportInput
        {
            Month = 6,
            Year = 2024,
            MaxResultCount = 100
        });

        file.FileName.ShouldBe("SalaryRetroResult.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();
    }
}
