using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.QuotaProvision;
using HCMv3.GridReports.QuotaProvision.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

// ReSharper disable once InconsistentNaming
public class AnnualLeaveSheetReportAppService_Tests : AppTestBase
{
    private readonly IAnnualLeaveSheetReportAppService _appService;

    public AnnualLeaveSheetReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IAnnualLeaveSheetReportAppService>();
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

    private TimeTransferType CreateTransferType(string code)
    {
        TimeTransferType type = null;
        UsingDbContext(context =>
        {
            type = context.Set<TimeTransferType>().Add(new TimeTransferType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });
        return type;
    }

    [Fact]
    public async Task Should_Compute_Quota_Accrued_As_Total_Accrual_Minus_Opening_Balance()
    {
        var employee = CreateEmployee("ALS-1");
        var transferType = CreateTransferType("1");

        UsingDbContext(context =>
        {
            context.Set<TimeTransfer>().Add(new TimeTransfer
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                TransferTypeId = transferType.Id,
                TransferDate = new DateTime(2024, 1, 1),
                TransferCount = 5
            });

            context.Set<TimeRegister>().Add(new TimeRegister
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                DayDate = new DateTime(2024, 6, 1),
                AnnualLeaveAccrualPerDay = 12
            });
        });

        var result = await _appService.GetAnnualLeaveSheetReport(new GetAnnualLeaveSheetReportInput
        {
            FromDate = new DateTime(2024, 6, 15),
            EmployeeCode = "ALS-1",
            MaxResultCount = 100
        });

        var row = result.Items.Single();
        row.OpeningBalance.ShouldBe(5);
        row.QuotaAccrued.ShouldBe(7); // legacy line 701: QuotaAccrued (total 12) - OpeningBalance (5)
    }
}
