using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.Workflow;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// Covers the distinction the whole leave design turns on: quota is consumed only by APPROVED leave, while
/// leave awaiting approval consumes BOOKABLE.
///
/// <para>
/// Getting this wrong in either direction is costly. Count pending against quota and payroll pays for leave
/// that may still be rejected. Ignore pending when deciding what can be booked and an employee submits the
/// same fortnight three times, each application passing its own check because the others are invisible.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class LeaveBalanceService_Tests : AppTestBase
{
    private readonly ILeaveBalanceService _balanceService;

    public LeaveBalanceService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _balanceService = Resolve<ILeaveBalanceService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private int CreateEmployee(string code)
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
                FirstNameEn = "F",
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "F L",
                FullNameAr = "ف ل",
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                NationalityId = nationality.Id
            }).Entity;
        });

        return employee.Id;
    }

    private int CreateAbsenceType(string nameEn = "Annual")
    {
        AbsenceType absenceType = null;
        UsingDbContext(context =>
        {
            absenceType = context.Set<AbsenceType>().Add(new AbsenceType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = nameEn,
                NameAr = nameEn
            }).Entity;
        });

        return absenceType.Id;
    }

    private void CreateQuota(
        int employeeId,
        int absenceTypeId,
        double accrued,
        double used = 0,
        double adjusted = 0,
        double forfeited = 0)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeAbsenceQuota>().Add(new EmployeeAbsenceQuota
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                AbsenceTypeId = absenceTypeId,
                EntitlementFrom = new DateTime(2026, 1, 1),
                EntitlementTo = new DateTime(2026, 12, 31),
                DeductionFrom = new DateTime(2026, 1, 1),
                DeductionTo = new DateTime(2026, 12, 31),
                QuotaAccrued = accrued,
                QuotaUsed = used,
                QuotaAdjusted = adjusted,
                QuotaForfeited = forfeited
            });
        });
    }

    private void CreateLeave(int employeeId, int absenceTypeId, double days, RequestStatus? status)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeAbsence>().Add(new EmployeeAbsence
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                AbsenceTypeId = absenceTypeId,
                StartDate = new DateTime(2026, 6, 1),
                EndDate = new DateTime(2026, 6, 1).AddDays(days - 1),
                TotalLeaveDays = days,
                WorkflowStatus = status
            });
        });
    }

    private static readonly DateTime AsOf = new DateTime(2026, 6, 1);

    [Fact]
    public async Task Should_Compute_Remaining_From_The_Quota_Row()
    {
        var employeeId = CreateEmployee("BAL100");
        var typeId = CreateAbsenceType();
        CreateQuota(employeeId, typeId, accrued: 30, used: 5, adjusted: 2, forfeited: 1);

        var balance = await _balanceService.GetBalanceAsync(employeeId, typeId, AsOf);

        balance.Accrued.ShouldBe(30);
        balance.Used.ShouldBe(5);
        balance.Remaining.ShouldBe(26); // 30 + 2 - 1 - 5
    }

    [Fact]
    public async Task Bookable_Should_Equal_Remaining_When_Nothing_Is_Pending()
    {
        var employeeId = CreateEmployee("BAL200");
        var typeId = CreateAbsenceType();
        CreateQuota(employeeId, typeId, accrued: 20, used: 4);

        var balance = await _balanceService.GetBalanceAsync(employeeId, typeId, AsOf);

        balance.Pending.ShouldBe(0);
        balance.Bookable.ShouldBe(balance.Remaining);
        balance.Bookable.ShouldBe(16);
    }

    [Fact]
    public async Task Pending_Leave_Should_Reduce_Bookable_But_Not_Remaining()
    {
        // The rule stated plainly. Quota is untouched by a request that may still be rejected; what the
        // employee can still book is not.
        var employeeId = CreateEmployee("BAL300");
        var typeId = CreateAbsenceType();
        CreateQuota(employeeId, typeId, accrued: 20, used: 4);
        CreateLeave(employeeId, typeId, days: 3, status: RequestStatus.Open);

        var balance = await _balanceService.GetBalanceAsync(employeeId, typeId, AsOf);

        balance.Used.ShouldBe(4);
        balance.Remaining.ShouldBe(16);
        balance.Pending.ShouldBe(3);
        balance.Bookable.ShouldBe(13);
    }

    [Theory]
    [InlineData(RequestStatus.Draft)]     // not submitted -- may never be
    [InlineData(RequestStatus.Rejected)]  // released
    [InlineData(RequestStatus.Approved)]  // already counted in QuotaUsed; counting it again would double it
    [InlineData(RequestStatus.Closed)]
    [InlineData(null)]                    // direct HR entry, already in QuotaUsed
    public async Task Only_Open_Leave_Should_Count_As_Pending(RequestStatus? status)
    {
        var employeeId = CreateEmployee("BAL400" + status);
        var typeId = CreateAbsenceType();
        CreateQuota(employeeId, typeId, accrued: 20, used: 4);
        CreateLeave(employeeId, typeId, days: 3, status: status);

        var balance = await _balanceService.GetBalanceAsync(employeeId, typeId, AsOf);

        balance.Pending.ShouldBe(0);
        balance.Bookable.ShouldBe(16);
    }

    [Fact]
    public async Task Should_Sum_Several_Pending_Requests()
    {
        // The case that motivates the whole rule: without it each of these would pass its own check.
        var employeeId = CreateEmployee("BAL500");
        var typeId = CreateAbsenceType();
        CreateQuota(employeeId, typeId, accrued: 20);
        CreateLeave(employeeId, typeId, days: 5, status: RequestStatus.Open);
        CreateLeave(employeeId, typeId, days: 4, status: RequestStatus.Open);

        var balance = await _balanceService.GetBalanceAsync(employeeId, typeId, AsOf);

        balance.Pending.ShouldBe(9);
        balance.Bookable.ShouldBe(11);
    }

    [Fact]
    public async Task Pending_Of_One_Type_Should_Not_Affect_Another()
    {
        var employeeId = CreateEmployee("BAL600");
        var annualId = CreateAbsenceType("Annual");
        var sickId = CreateAbsenceType("Sick");
        CreateQuota(employeeId, annualId, accrued: 20);
        CreateQuota(employeeId, sickId, accrued: 10);
        CreateLeave(employeeId, annualId, days: 5, status: RequestStatus.Open);

        var annual = await _balanceService.GetBalanceAsync(employeeId, annualId, AsOf);
        var sick = await _balanceService.GetBalanceAsync(employeeId, sickId, AsOf);

        annual.Bookable.ShouldBe(15);
        sick.Bookable.ShouldBe(10);
    }

    [Fact]
    public async Task Another_Employees_Pending_Leave_Should_Not_Affect_This_One()
    {
        var meId = CreateEmployee("BAL700ME");
        var otherId = CreateEmployee("BAL700OTHER");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 20);
        CreateQuota(otherId, typeId, accrued: 20);
        CreateLeave(otherId, typeId, days: 7, status: RequestStatus.Open);

        var mine = await _balanceService.GetBalanceAsync(meId, typeId, AsOf);

        mine.Pending.ShouldBe(0);
        mine.Bookable.ShouldBe(20);
    }

    [Fact]
    public async Task Should_Select_The_Quota_Period_Covering_The_Date()
    {
        // An employee holds one quota row per type per year; the periods must be selected, not summed.
        var employeeId = CreateEmployee("BAL800");
        var typeId = CreateAbsenceType();

        CreateQuota(employeeId, typeId, accrued: 30);
        UsingDbContext(context =>
        {
            context.Set<EmployeeAbsenceQuota>().Add(new EmployeeAbsenceQuota
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                AbsenceTypeId = typeId,
                EntitlementFrom = new DateTime(2025, 1, 1),
                EntitlementTo = new DateTime(2025, 12, 31),
                DeductionFrom = new DateTime(2025, 1, 1),
                DeductionTo = new DateTime(2025, 12, 31),
                QuotaAccrued = 99
            });
        });

        var balance = await _balanceService.GetBalanceAsync(employeeId, typeId, AsOf);

        balance.Accrued.ShouldBe(30);
    }

    [Fact]
    public async Task Should_Return_Nothing_For_An_Employee_With_No_Quota()
    {
        // Some absence types are deliberately not quota-controlled; an empty result means "no limit to
        // enforce", not "zero days available".
        var employeeId = CreateEmployee("BAL900");

        var balances = await _balanceService.GetBalancesAsync(employeeId, AsOf);

        balances.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_List_Every_Type_The_Employee_Holds_Quota_For()
    {
        var employeeId = CreateEmployee("BAL1000");
        CreateQuota(employeeId, CreateAbsenceType("Annual"), accrued: 20);
        CreateQuota(employeeId, CreateAbsenceType("Sick"), accrued: 10);

        var balances = await _balanceService.GetBalancesAsync(employeeId, AsOf);

        balances.Count.ShouldBe(2);
        balances.Select(b => b.AbsenceTypeNameEn).ShouldContain("Annual");
        balances.Select(b => b.AbsenceTypeNameEn).ShouldContain("Sick");
    }

    [Fact]
    public async Task Bookable_Should_Be_Able_To_Go_Negative()
    {
        // Reported rather than clamped to zero: an employee who has been granted more than their quota is a
        // real situation an HR user needs to see, and hiding it behind a floor of zero would make the number
        // look fine.
        var employeeId = CreateEmployee("BAL1100");
        var typeId = CreateAbsenceType();
        CreateQuota(employeeId, typeId, accrued: 5, used: 8);

        var balance = await _balanceService.GetBalanceAsync(employeeId, typeId, AsOf);

        balance.Remaining.ShouldBe(-3);
        balance.Bookable.ShouldBe(-3);
    }
}
