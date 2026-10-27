using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.Organizations;
using HCMv3.GeneralMasterInfo;
using HCMv3.GovtRelationsManagement;
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
public class PayrollNetPayContractDiffReportAppService_Tests : AppTestBase
{
    private readonly IPayrollNetPayContractDiffReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public PayrollNetPayContractDiffReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IPayrollNetPayContractDiffReportAppService>();
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

    private void CreateSponsor(Employee employee, DateTime start, DateTime end, double contractSalary)
    {
        Nationality nationality = null;
        BusinessType businessType = null;
        Governorate governorate = null;
        OrganizationUnit organizationUnit = null;
        UsingDbContext(context =>
        {
            nationality = context.Set<Nationality>().Add(new Nationality
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
            businessType = context.Set<BusinessType>().Add(new BusinessType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "BT",
                NameAr = "BT"
            }).Entity;
            governorate = context.Set<Governorate>().Add(new Governorate
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "GOV",
                NameAr = "GOV"
            }).Entity;
            organizationUnit = context.OrganizationUnits.Add(new OrganizationUnit(TenantId, "Sponsor OU")
            {
                Code = Guid.NewGuid().ToString("N").Substring(0, 8)
            }).Entity;
        });

        UsingDbContext(context =>
        {
            var sponsor = context.Set<Sponsor>().Add(new Sponsor
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "S",
                NameAr = "S",
                NationalityId = nationality.Id,
                BusinessTypeId = businessType.Id,
                GovernorateId = governorate.Id,
                OrganizationUnitId = organizationUnit.Id
            }).Entity;

            var govtJobTitle = context.Set<GovtJobTitle>().Add(new GovtJobTitle
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "G",
                NameAr = "G"
            }).Entity;

            var currency = context.Set<Currency>().Add(new Currency
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 3),
                NameEn = "Cur",
                NameAr = "Cur"
            }).Entity;

            var workVisaType = context.Set<WorkVisaType>().Add(new WorkVisaType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "W",
                NameAr = "W"
            }).Entity;

            context.SaveChanges();

            context.Set<EmployeeSponsor>().Add(new EmployeeSponsor
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                SponsorId = sponsor.Id,
                GovtJobTitleId = govtJobTitle.Id,
                StartDate = start,
                EndDate = end,
                ContractSalary = contractSalary,
                SalaryCurrencyId = currency.Id,
                WorkVisaTypeId = workVisaType.Id
            });
        });
    }

    [Fact]
    public async Task Should_Compute_Difference_Between_Contract_Salary_And_Net_Pay()
    {
        var employee = CreateEmployee("NPCD-1");
        CreateSponsor(employee, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), 1000);
        CreateDetail(CreateRegister(employee, new DateTime(2024, 6, 30)), CreateWageType("9000"), 750);

        var result = await _appService.GetPayrollNetPayContractDiffReport(
            new GetPayrollNetPayContractDiffReportInput { Month = 6, Year = 2024, MaxResultCount = 100 });

        var row = result.Items.Single(i => i.EmployeeCode == "NPCD-1");
        row.ContractPay.ShouldBe(1000);
        row.NetPay.ShouldBe(750);
        row.Difference.ShouldBe(250);
    }

    [Fact]
    public async Task Should_Default_ContractPay_To_Zero_When_No_Current_Sponsor()
    {
        var employee = CreateEmployee("NPCD-NOSPONSOR");
        CreateDetail(CreateRegister(employee, new DateTime(2024, 6, 30)), CreateWageType("9000"), 500);

        var result = await _appService.GetPayrollNetPayContractDiffReport(
            new GetPayrollNetPayContractDiffReportInput { Month = 6, Year = 2024, MaxResultCount = 100 });

        var row = result.Items.Single(i => i.EmployeeCode == "NPCD-NOSPONSOR");
        row.ContractPay.ShouldBe(0);
        row.Difference.ShouldBe(-500);
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_And_Resolve_Every_Header()
    {
        var a = CreateEmployee("NPCD-EXP-A");
        var b = CreateEmployee("NPCD-EXP-B");
        CreateDetail(CreateRegister(a, new DateTime(2024, 6, 30)), CreateWageType("9000"), 100);
        CreateDetail(CreateRegister(b, new DateTime(2024, 6, 30)), CreateWageType("9000"), 200);

        var file = await _appService.GetPayrollNetPayContractDiffReportToExcel(
            new GetPayrollNetPayContractDiffReportInput { Month = 6, Year = 2024, MaxResultCount = 1 });

        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<System.Collections.Generic.IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(2);
        exported.First().Keys.Where(k => k.StartsWith("[") && k.EndsWith("]")).ShouldBeEmpty();
    }
}
