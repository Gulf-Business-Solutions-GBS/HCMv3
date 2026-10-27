using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.QuotaProvision;
using HCMv3.GridReports.QuotaProvision.Dto;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

// ReSharper disable once InconsistentNaming
public class AirTicketQuotaBalanceReportAppService_Tests : AppTestBase
{
    private readonly IAirTicketQuotaBalanceReportAppService _appService;

    public AirTicketQuotaBalanceReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IAirTicketQuotaBalanceReportAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(string code, DateTime joiningDate)
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
                JoiningDate = joiningDate,
                ProbationEndDate = joiningDate.AddMonths(3),
                NationalityId = nationality.Id,
                Status = EmployeeStatus.Active
            }).Entity;
        });

        return employee;
    }

    private HCMv3.PayrollManagement.TicketType CreateTicketType(string code)
    {
        HCMv3.PayrollManagement.TicketType ticketType = null;
        UsingDbContext(context =>
        {
            ticketType = context.Set<HCMv3.PayrollManagement.TicketType>().Add(new HCMv3.PayrollManagement.TicketType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code + "-En",
                NameAr = code + "-Ar"
            }).Entity;
        });

        return ticketType;
    }

    private void CreateQuota(Employee employee, HCMv3.PayrollManagement.TicketType ticketType, DateTime asOnDate, double accrued, double used, double adjusted)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeTicketQuota>().Add(new EmployeeTicketQuota
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                TicketTypeId = ticketType.Id,
                EntitlementFrom = asOnDate.AddMonths(-6),
                EntitlementTo = asOnDate.AddMonths(6),
                DeductionFrom = asOnDate.AddMonths(-6),
                DeductionTo = asOnDate.AddMonths(6),
                QuotaAccrued = accrued,
                QuotaUsed = used,
                QuotaAdjusted = adjusted
            });
        });
    }

    [Fact]
    public async Task Should_Reproduce_The_Legacy_Balance_Quota_Formula()
    {
        var ticketType = CreateTicketType("TT-BAL");
        var employee = CreateEmployee("ATB-1", new DateTime(2020, 1, 1));
        var asOnDate = new DateTime(2024, 6, 15);

        CreateQuota(employee, ticketType, asOnDate, accrued: 10, used: 3, adjusted: 1);

        var result = await _appService.GetAirTicketQuotaBalanceReport(new GetAirTicketQuotaBalanceReportInput
        {
            FromDate = asOnDate,
            TicketTypeCode = "TT-BAL",
            MaxResultCount = 100
        });

        var row = result.Items.Single(i => i.EmployeeCode == "ATB-1");
        row.AccruedQuota.ShouldBe(10);
        row.UsedQuota.ShouldBe(3);
        row.AdjustedQuota.ShouldBe(1);
        row.BalanceQuota.ShouldBe(6); // 10 - (3 + 1), legacy AccruedQuota - (UsedQuota + AdjustedQuota)
    }

    [Fact]
    public async Task Should_Flag_Comment_When_As_On_Date_Predates_Joining()
    {
        var ticketType = CreateTicketType("TT-JOIN");
        var joiningDate = new DateTime(2024, 1, 1);
        var employee = CreateEmployee("ATB-2", joiningDate);

        var result = await _appService.GetAirTicketQuotaBalanceReport(new GetAirTicketQuotaBalanceReportInput
        {
            FromDate = new DateTime(2023, 6, 1),
            TicketTypeCode = "TT-JOIN",
            EmployeeCode = "ATB-2",
            MaxResultCount = 100
        });

        var row = result.Items.Single();
        row.Comment.ShouldNotBeNullOrEmpty();
        row.BalanceQuota.ShouldBe(0);
    }
}
