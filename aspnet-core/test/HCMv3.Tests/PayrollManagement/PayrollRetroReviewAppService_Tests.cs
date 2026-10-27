using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Runtime.Validation;
using Abp.Timing;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// The Payroll Engine screen's Retro review (legacy <c>uscPayrollEngine.ascx.cs</c>, region "Retro Popup").
/// Resolved from the container, so ABP's unit of work and validation run as on a real call.
/// </summary>
// ReSharper disable once InconsistentNaming
public class PayrollRetroReviewAppService_Tests : AppTestBase
{
    private const int TenantId = 1;
    private const string Area = "Main";
    private static readonly DateTime OpenEnded = new(9999, 12, 31);
    private static readonly DateTime JoiningDate = new(2020, 1, 1);

    // The area's control record: September 2026.
    private static readonly DateTime PeriodStart = new(2026, 9, 1);
    private static readonly DateTime PeriodEnd = new(2026, 9, 30);

    private readonly IPayrollRetroReviewAppService _service;
    private readonly int _areaId;

    public PayrollRetroReviewAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _service = Resolve<IPayrollRetroReviewAppService>();

        _areaId = CreatePayrollArea("PA1", Area);
        CreateControlRecord(_areaId, PeriodStart, PeriodEnd, periodClosed: false);
    }

    // ================================================================== GetRetroCount

    [Fact]
    public async Task Should_Count_Only_Employees_Whose_Retro_Date_Is_Before_The_Period_Start()
    {
        SeedStatus(AddEmployee("E1"), new DateTime(2026, 6, 15)); // retro
        SeedStatus(AddEmployee("E2"), PeriodStart);               // on the start day: not retro
        SeedStatus(AddEmployee("E3"), DateTime.MinValue);         // never set
        AddEmployee("E4");                                        // no payroll status row

        (await _service.GetRetroCount(Area)).ShouldBe(1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("No Such Area")]
    public async Task Should_Count_Zero_For_A_Blank_Or_Unknown_Payroll_Area(string payrollAreaName)
    {
        SeedStatus(AddEmployee("E1"), new DateTime(2026, 6, 15));

        (await _service.GetRetroCount(payrollAreaName)).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Not_Count_Employees_Of_Another_Payroll_Area()
    {
        var otherArea = CreatePayrollArea("PA2", "Other");
        SeedStatus(AddEmployee("E1", areaId: otherArea), new DateTime(2026, 6, 15));

        (await _service.GetRetroCount(Area)).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Not_Count_An_Inactive_Employee_Without_A_Separation()
    {
        SeedStatus(AddEmployee("E1", active: false), new DateTime(2026, 6, 15));

        (await _service.GetRetroCount(Area)).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Count_A_Separated_Employee_With_No_Settlement_Yet()
    {
        var id = AddEmployee("E1", active: false);
        AddSeparation(id, new DateTime(2026, 8, 1));
        SeedStatus(id, new DateTime(2026, 6, 15));

        (await _service.GetRetroCount(Area)).ShouldBe(1);
    }

    [Fact]
    public async Task Should_Not_Count_A_Separated_Employee_Whose_Separation_Was_Revoked()
    {
        var id = AddEmployee("E1", active: false);
        AddSeparation(id, new DateTime(2026, 8, 1), revokeDate: new DateTime(2026, 8, 10));
        SeedStatus(id, new DateTime(2026, 6, 15));

        (await _service.GetRetroCount(Area)).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Not_Count_An_Inactive_Employee_Who_Rejoined_After_The_Separation()
    {
        var id = AddEmployee("E1", active: false);
        AddSeparation(id, new DateTime(2026, 8, 1));
        AddRejoin(id, new DateTime(2026, 8, 20));
        SeedStatus(id, new DateTime(2026, 6, 15));

        (await _service.GetRetroCount(Area)).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Count_A_Settled_Employee_Whose_Settlement_Is_In_The_Current_Period()
    {
        var id = AddEmployee("E1", active: false);
        AddSeparation(id, new DateTime(2026, 8, 1));
        AddSettlement(id, new DateTime(2026, 9, 10));
        SeedStatus(id, new DateTime(2026, 6, 15));

        (await _service.GetRetroCount(Area)).ShouldBe(1);
    }

    [Fact]
    public async Task Should_Not_Count_A_Settled_Employee_Whose_Settlement_Is_Outside_The_Current_Period()
    {
        var id = AddEmployee("E1", active: false);
        AddSeparation(id, new DateTime(2026, 8, 1));
        AddSettlement(id, new DateTime(2026, 8, 15));
        SeedStatus(id, new DateTime(2026, 6, 15));

        (await _service.GetRetroCount(Area)).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Look_At_The_Next_Period_For_A_Settlement_When_The_Period_Is_Closed()
    {
        UsingDbContext(c => c.Set<PayrollControlRecord>().Single(r => r.PayrollAreaId == _areaId).PeriodClosed = true);

        var inNext = AddEmployee("E1", active: false);
        AddSeparation(inNext, new DateTime(2026, 8, 1));
        AddSettlement(inNext, new DateTime(2026, 10, 5));
        SeedStatus(inNext, new DateTime(2026, 6, 15));

        var inCurrent = AddEmployee("E2", active: false);
        AddSeparation(inCurrent, new DateTime(2026, 8, 1));
        AddSettlement(inCurrent, new DateTime(2026, 9, 10));
        SeedStatus(inCurrent, new DateTime(2026, 6, 15));

        var output = await _service.GetRetroReviewEmployees(Input(PayrollRetroReviewFilter.All));

        output.Items.Select(i => i.EmployeeCode).ShouldBe(new[] { "E1" });
    }

    // ================================================================== GetRetroReviewEmployees

    [Fact]
    public async Task Should_Open_On_The_Retro_Tab_With_All_Three_Counts()
    {
        SeedStatus(AddEmployee("E1"), new DateTime(2026, 6, 15));
        SeedStatus(AddEmployee("E2"), new DateTime(2026, 9, 20));
        AddEmployee("E3");

        var output = await _service.GetRetroReviewEmployees(new GetPayrollRetroReviewEmployeesInput
        {
            PayrollAreaName = Area,
        });

        output.Items.Select(i => i.EmployeeCode).ShouldBe(new[] { "E1" });
        output.TotalCount.ShouldBe(1);
        output.AllCount.ShouldBe(3);
        output.RetroCount.ShouldBe(1);
        output.NoRetroCount.ShouldBe(2);
        output.PeriodStartDate.ShouldBe(PeriodStart);
        output.PeriodEndDate.ShouldBe(PeriodEnd);
    }

    [Fact]
    public async Task Should_List_Everyone_On_The_All_Tab_Ordered_By_Code()
    {
        AddEmployee("E3");
        SeedStatus(AddEmployee("E1"), new DateTime(2026, 6, 15));
        AddEmployee("E2");

        var output = await _service.GetRetroReviewEmployees(Input(PayrollRetroReviewFilter.All));

        output.Items.Select(i => i.EmployeeCode).ShouldBe(new[] { "E1", "E2", "E3" });
    }

    [Fact]
    public async Task Should_List_Only_Employees_Without_Retro_On_The_No_Retro_Tab()
    {
        SeedStatus(AddEmployee("E1"), new DateTime(2026, 6, 15));
        SeedStatus(AddEmployee("E2"), new DateTime(2026, 9, 20));
        AddEmployee("E3");

        var output = await _service.GetRetroReviewEmployees(Input(PayrollRetroReviewFilter.NoRetro));

        output.Items.Select(i => i.EmployeeCode).ShouldBe(new[] { "E2", "E3" });
        // A date on or after the period start is shown, but is not a retro.
        output.Items[0].RetroFromDate.ShouldBe(new DateTime(2026, 9, 20));
        output.Items[0].HasRetro.ShouldBeFalse();
        output.Items[0].RetroMonths.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Search_By_Employee_Code_Or_Name()
    {
        AddEmployee("E1", name: "Ahmad Saleh");
        AddEmployee("E2", name: "Fatima Ali");
        AddEmployee("X9", name: "Omar");

        var byName = await _service.GetRetroReviewEmployees(Input(PayrollRetroReviewFilter.All, filter: "fatima"));
        var byCode = await _service.GetRetroReviewEmployees(Input(PayrollRetroReviewFilter.All, filter: "x9"));

        byName.Items.Select(i => i.EmployeeCode).ShouldBe(new[] { "E2" });
        byCode.Items.Select(i => i.EmployeeCode).ShouldBe(new[] { "X9" });
        byName.AllCount.ShouldBe(3); // tab counts ignore the search, as legacy's tabs ignore the grid filter
    }

    [Fact]
    public async Task Should_Page_And_Sort_The_List()
    {
        AddEmployee("E1");
        AddEmployee("E2");
        AddEmployee("E3");

        var page = await _service.GetRetroReviewEmployees(new GetPayrollRetroReviewEmployeesInput
        {
            PayrollAreaName = Area,
            RetroFilter = PayrollRetroReviewFilter.All,
            Sorting = "employeeCode desc",
            SkipCount = 1,
            MaxResultCount = 1,
        });

        page.TotalCount.ShouldBe(3);
        page.Items.Select(i => i.EmployeeCode).ShouldBe(new[] { "E2" });
    }

    [Fact]
    public async Task Should_Count_Retro_Months_From_The_Retro_Month_To_The_Payroll_Month()
    {
        SeedStatus(AddEmployee("E1"), new DateTime(2026, 6, 15));

        var row = (await _service.GetRetroReviewEmployees(Input(PayrollRetroReviewFilter.Retro))).Items.Single();

        row.RetroFromDate.ShouldBe(new DateTime(2026, 6, 15));
        row.RetroMonths.ShouldBe(3); // June -> September
        row.HasRetro.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Show_The_Reason_From_The_Latest_Retro_Reason_Log_Line()
    {
        var id = AddEmployee("E1");
        SeedStatus(id, new DateTime(2026, 6, 15));
        SeedLog(id, new DateTime(2026, 7, 1), "older");
        SeedLog(id, new DateTime(2026, 8, 1), "latest");

        var row = (await _service.GetRetroReviewEmployees(Input(PayrollRetroReviewFilter.Retro))).Items.Single();

        row.Reason.ShouldBe("latest");
    }

    [Fact]
    public async Task Should_Leave_The_Reason_Blank_When_There_Is_No_Retro_Date()
    {
        var id = AddEmployee("E1");
        SeedStatus(id, DateTime.MinValue);
        SeedLog(id, new DateTime(2026, 8, 1), "cleared earlier");

        var row = (await _service.GetRetroReviewEmployees(Input(PayrollRetroReviewFilter.All))).Items.Single();

        row.RetroFromDate.ShouldBeNull();
        row.Reason.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task Should_Measure_Against_This_Month_When_The_Area_Has_No_Control_Record()
    {
        var bare = CreatePayrollArea("PA3", "Bare");
        var twoMonthsAgo = Clock.Now.Date.AddMonths(-2);
        SeedStatus(AddEmployee("E1", areaId: bare, assignmentStart: twoMonthsAgo.AddYears(-1)), twoMonthsAgo);

        var output = await _service.GetRetroReviewEmployees(new GetPayrollRetroReviewEmployeesInput
        {
            PayrollAreaName = "Bare",
            RetroFilter = PayrollRetroReviewFilter.Retro,
        });

        output.PeriodEndDate.ShouldBe(Clock.Now.Date);
        output.PeriodStartDate.ShouldBe(new DateTime(Clock.Now.Year, Clock.Now.Month, 1));
        output.Items.Single().RetroMonths.ShouldBe(2);
    }

    [Theory]
    [InlineData("")]
    [InlineData("No Such Area")]
    public async Task Should_Refuse_The_List_For_A_Blank_Or_Unknown_Payroll_Area(string payrollAreaName)
    {
        await Should.ThrowAsync<Exception>(() => _service.GetRetroReviewEmployees(new GetPayrollRetroReviewEmployeesInput
        {
            PayrollAreaName = payrollAreaName,
        }));
    }

    // ================================================================== UpdateRetroDates

    [Fact]
    public async Task Should_Set_The_Retro_Date_And_Write_A_Retro_Reason_Log_Line()
    {
        var id = AddEmployee("E1");
        SeedStatus(id, DateTime.MinValue);

        var updated = await _service.UpdateRetroDates(Update(Change(id, new DateTime(2026, 5, 10), "  Late salary change  ")));

        updated.ShouldBe(1);
        Status(id).EarliestMasterDataChange.ShouldBe(new DateTime(2026, 5, 10));

        var log = Logs().Single();
        log.EmployeeId.ShouldBe(id);
        log.RetroDate.ShouldBe(new DateTime(2026, 5, 10));
        log.EntityObjectName.ShouldBe("PayrollEngine");
        log.EntityObjectId.ShouldBe(0);
        log.RetroReason.ShouldBe("Manual Retro Update From Payroll Engine. Reason: Late salary change");
        log.TenantId.ShouldBe(TenantId);
    }

    [Fact]
    public async Task Should_Create_The_Payroll_Status_Row_When_The_Employee_Has_None()
    {
        var id = AddEmployee("E1");

        await _service.UpdateRetroDates(Update(Change(id, new DateTime(2026, 5, 10), "r")));

        var status = Status(id);
        status.TenantId.ShouldBe(TenantId);
        status.EarliestMasterDataChange.ShouldBe(new DateTime(2026, 5, 10));
        status.EarliestRetroAccountingDate.ShouldBe(JoiningDate);   // legacy: joining date as the floor
        status.NextTimeEvaluationFrom.ShouldBe(new DateTime(2026, 5, 10));
    }

    [Fact]
    public async Task Should_Remove_The_Retro_When_The_Date_Is_Cleared()
    {
        var id = AddEmployee("E1");
        SeedStatus(id, new DateTime(2026, 6, 15));

        await _service.UpdateRetroDates(Update(Change(id, null, "No longer needed")));

        Status(id).EarliestMasterDataChange.ShouldBe(DateTime.MinValue);
        Logs().Single().RetroDate.ShouldBe(DateTime.MinValue);
        (await _service.GetRetroCount(Area)).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Save_Several_Employees_With_Their_Own_Dates_And_Reasons_Together()
    {
        var first = AddEmployee("E1");
        var second = AddEmployee("E2");

        var updated = await _service.UpdateRetroDates(Update(
            Change(first, new DateTime(2026, 4, 1), "first"),
            Change(second, new DateTime(2026, 7, 1), "second")));

        updated.ShouldBe(2);
        Status(first).EarliestMasterDataChange.ShouldBe(new DateTime(2026, 4, 1));
        Status(second).EarliestMasterDataChange.ShouldBe(new DateTime(2026, 7, 1));
        Logs().Select(l => l.RetroReason).OrderBy(r => r).ShouldBe(new[]
        {
            "Manual Retro Update From Payroll Engine. Reason: first",
            "Manual Retro Update From Payroll Engine. Reason: second",
        });
        (await _service.GetRetroCount(Area)).ShouldBe(2);
    }

    [Fact]
    public async Task Should_Skip_An_Employee_Whose_Date_Did_Not_Change()
    {
        var same = AddEmployee("E1");
        SeedStatus(same, new DateTime(2026, 6, 15));
        var moved = AddEmployee("E2");

        var updated = await _service.UpdateRetroDates(Update(
            Change(same, new DateTime(2026, 6, 15), "unchanged"),
            Change(moved, new DateTime(2026, 7, 1), "moved")));

        updated.ShouldBe(1);
        Logs().Single().EmployeeId.ShouldBe(moved);
    }

    [Fact]
    public async Task Should_Refuse_When_Nothing_Changed()
    {
        var id = AddEmployee("E1");
        SeedStatus(id, new DateTime(2026, 6, 15));

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _service.UpdateRetroDates(Update(Change(id, new DateTime(2026, 6, 15), "same"))));

        Logs().ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Refuse_An_Empty_Save()
    {
        await Should.ThrowAsync<UserFriendlyException>(() => _service.UpdateRetroDates(Update()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_Refuse_A_Change_Without_A_Reason_And_Save_Nothing(string reason)
    {
        var ok = AddEmployee("E1");
        var missing = AddEmployee("E2");

        await Should.ThrowAsync<UserFriendlyException>(() => _service.UpdateRetroDates(Update(
            Change(ok, new DateTime(2026, 4, 1), "fine"),
            Change(missing, new DateTime(2026, 4, 1), reason))));

        Logs().ShouldBeEmpty();
        StatusCount().ShouldBe(0);
    }

    [Fact]
    public async Task Should_Refuse_An_Employee_Who_Is_Not_On_The_Areas_List_And_Save_Nothing()
    {
        var ok = AddEmployee("E1");
        SeedStatus(ok, DateTime.MinValue);
        var outsider = AddEmployee("E2", areaId: CreatePayrollArea("PA2", "Other"));

        await Should.ThrowAsync<UserFriendlyException>(() => _service.UpdateRetroDates(Update(
            Change(ok, new DateTime(2026, 4, 1), "fine"),
            Change(outsider, new DateTime(2026, 4, 1), "not mine"))));

        // All or nothing: the valid employee is not saved either.
        Status(ok).EarliestMasterDataChange.ShouldBe(DateTime.MinValue);
        Logs().ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Refuse_The_Same_Employee_Twice()
    {
        var id = AddEmployee("E1");

        await Should.ThrowAsync<UserFriendlyException>(() => _service.UpdateRetroDates(Update(
            Change(id, new DateTime(2026, 4, 1), "a"),
            Change(id, new DateTime(2026, 5, 1), "b"))));

        Logs().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("No Such Area")]
    public async Task Should_Refuse_A_Save_For_A_Blank_Or_Unknown_Payroll_Area(string payrollAreaName)
    {
        var id = AddEmployee("E1");
        var input = Update(Change(id, new DateTime(2026, 4, 1), "r"));
        input.PayrollAreaName = payrollAreaName;

        await Should.ThrowAsync<Exception>(() => _service.UpdateRetroDates(input));

        Logs().ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Refuse_A_Reason_Longer_Than_500_Characters()
    {
        var id = AddEmployee("E1");

        await Should.ThrowAsync<AbpValidationException>(() =>
            _service.UpdateRetroDates(Update(Change(id, new DateTime(2026, 4, 1), new string('x', 501)))));

        Logs().ShouldBeEmpty();
    }

    // ================================================================== wiring

    [Theory]
    [InlineData(nameof(IPayrollRetroReviewAppService.GetRetroCount), AppPermissions.Pages_PayrollManagement_PayrollEngine)]
    [InlineData(nameof(IPayrollRetroReviewAppService.GetRetroReviewEmployees), AppPermissions.Pages_PayrollManagement_PayrollEngine)]
    [InlineData(nameof(IPayrollRetroReviewAppService.UpdateRetroDates), AppPermissions.Pages_PayrollManagement_PayrollEngine_Run)]
    public void Should_Guard_Each_Method_With_Its_Permission(string methodName, string permission)
    {
        var method = typeof(PayrollRetroReviewAppService).GetMethod(methodName);

        method.GetCustomAttribute<AbpAuthorizeAttribute>().Permissions.ShouldBe(new[] { permission });
    }

    [Fact]
    public void Should_Post_The_List_Input_So_NSwag_Generates_Its_Dto()
    {
        typeof(PayrollRetroReviewAppService)
            .GetMethod(nameof(IPayrollRetroReviewAppService.GetRetroReviewEmployees))
            .GetCustomAttribute<HttpPostAttribute>()
            .ShouldNotBeNull();
    }

    // ================================================================== helpers

    private static GetPayrollRetroReviewEmployeesInput Input(PayrollRetroReviewFilter retroFilter, string filter = null) => new()
    {
        PayrollAreaName = Area,
        RetroFilter = retroFilter,
        Filter = filter,
    };

    private static UpdatePayrollRetroDatesInput Update(params PayrollRetroDateChangeDto[] changes) => new()
    {
        PayrollAreaName = Area,
        Changes = new List<PayrollRetroDateChangeDto>(changes),
    };

    private static PayrollRetroDateChangeDto Change(int employeeId, DateTime? date, string reason) => new()
    {
        EmployeeId = employeeId,
        RetroFromDate = date,
        Reason = reason,
    };

    private EmployeePayrollStatus Status(int employeeId) =>
        UsingDbContext(c => c.Set<EmployeePayrollStatus>().Single(s => s.EmployeeId == employeeId));

    private int StatusCount() => UsingDbContext(c => c.Set<EmployeePayrollStatus>().Count());

    private List<RetroReasonLog> Logs() => UsingDbContext(c => c.Set<RetroReasonLog>().ToList());

    private void SeedStatus(int employeeId, DateTime earliestMasterDataChange) =>
        UsingDbContext(c => c.Set<EmployeePayrollStatus>().Add(new EmployeePayrollStatus
        {
            TenantId = TenantId,
            EmployeeId = employeeId,
            EarliestRetroAccountingDate = JoiningDate,
            EarliestMasterDataChange = earliestMasterDataChange,
            NextTimeEvaluationFrom = DateTime.MinValue,
        }));

    private void SeedLog(int employeeId, DateTime changedOn, string reason) =>
        UsingDbContext(c => c.Set<RetroReasonLog>().Add(new RetroReasonLog
        {
            TenantId = TenantId,
            EmployeeId = employeeId,
            ChangedOn = changedOn,
            RetroDate = new DateTime(2026, 6, 15),
            EntityObjectName = "Test",
            RetroReason = reason,
        }));

    private int CreatePayrollArea(string code, string name)
    {
        PayrollArea area = null;
        UsingDbContext(c =>
        {
            var currency = c.Set<Currency>().Add(new Currency { TenantId = TenantId, Code = "K" + code, NameEn = "KWD", NameAr = "KWD", DecimalCount = 3 }).Entity;
            var period = c.Set<PayrollPeriod>().Add(new PayrollPeriod { TenantId = TenantId, Code = "M" + code, NameEn = "Monthly", NameAr = "Monthly" }).Entity;
            c.SaveChanges();

            area = c.Set<PayrollArea>().Add(new PayrollArea
            {
                TenantId = TenantId, Code = code, Name = name, PayrollPeriodId = period.Id, CurrencyId = currency.Id, CutOffDay = 25
            }).Entity;
        });
        return area.Id;
    }

    private void CreateControlRecord(int areaId, DateTime start, DateTime end, bool periodClosed) =>
        UsingDbContext(c => c.Set<PayrollControlRecord>().Add(new PayrollControlRecord
        {
            TenantId = TenantId,
            PayrollAreaId = areaId,
            PayrollMonth = start.Month,
            PayrollYear = start.Year,
            CalendarMonthStart = start,
            CalendarMonthEnd = end,
            PeriodClosed = periodClosed,
        }));

    private int AddEmployee(string code, bool active = true, int? areaId = null, string name = null, DateTime? assignmentStart = null)
    {
        Employee employee = null;
        UsingDbContext(c =>
        {
            var nationality = c.Set<Nationality>().Add(new Nationality
            {
                TenantId = TenantId, Code = Guid.NewGuid().ToString("N")[..8], NameEn = "N", NameAr = "N"
            }).Entity;
            c.SaveChanges();

            employee = c.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId,
                Code = code,
                FirstNameEn = "F",
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = name ?? code,
                FullNameAr = name ?? code,
                BirthDate = new DateTime(1990, 1, 1),
                Gender = Gender.Male,
                NationalityId = nationality.Id,
                JoiningDate = JoiningDate,
                Status = active ? EmployeeStatus.Active : EmployeeStatus.Inactive,
            }).Entity;
            c.SaveChanges();

            c.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                StartDate = assignmentStart ?? JoiningDate,
                EndDate = OpenEnded,
                PayrollAreaId = areaId ?? _areaId,
            });
        });
        return employee.Id;
    }

    private void AddSeparation(int employeeId, DateTime submissionDate, DateTime? revokeDate = null)
    {
        UsingDbContext(c =>
        {
            var reason = c.Set<SeparationReason>().Add(new SeparationReason
            {
                TenantId = TenantId, Code = Guid.NewGuid().ToString("N")[..8], NameEn = "R", NameAr = "R"
            }).Entity;
            c.SaveChanges();

            c.Set<EmployeeSeparation>().Add(new EmployeeSeparation
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                SubmissionDate = submissionDate,
                LastDayInService = submissionDate,
                PlannedLastDayInService = submissionDate,
                SeparationReasonId = reason.Id,
                RevokeDate = revokeDate,
            });
        });
    }

    private void AddRejoin(int employeeId, DateTime rejoinDate) =>
        UsingDbContext(c => c.Set<EmployeeRejoin>().Add(new EmployeeRejoin
        {
            TenantId = TenantId, EmployeeId = employeeId, RejoinDate = rejoinDate,
        }));

    private void AddSettlement(int employeeId, DateTime compensationDate) =>
        UsingDbContext(c => c.Set<EmployeeFnFPayment>().Add(new EmployeeFnFPayment
        {
            TenantId = TenantId, EmployeeId = employeeId, CompensationDate = compensationDate,
        }));
}
