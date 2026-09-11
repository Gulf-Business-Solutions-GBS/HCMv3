using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Threading;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.GoLiveSetup.ReferenceData;
using HCMv3.OrganizationManagement;
using HCMv3.GoLiveSetupV2;
using HCMv3.GoLiveSetupV2.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;
using HCMv3.TimeManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GoLiveSetupV2;

/// <summary>
/// "Load Employee Data", the step before Run payroll. The customer gives code, full name, joining date, civil ID
/// and monthly salary per employee, and one organisation unit name; everything else a payroll needs around an
/// employee is created: the org unit, the payroll area, an employee group and sub-group set up as Kuwait
/// labour law says, an open-ended contract, org assignment and salary -- and for what is not asked, a NO_
/// record rather than a guess.
/// </summary>
// ReSharper disable once InconsistentNaming
public class GoLiveV2LoadEmployees_Tests : AppTestBase
{
    private const string Company = "ABC Holding";
    private const string DefaultArea = "Default Payroll Area";
    private static readonly DateTime PayDate = new(2026, 9, 30);
    private static readonly DateTime Joined = new(2020, 1, 15);
    private static readonly DateTime OpenEnded = new(9999, 12, 31);

    private readonly IGoLiveV2AppService _goLive;

    public GoLiveV2LoadEmployees_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _goLive = Resolve<IGoLiveV2AppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    // ---------------------------------------------------------------- the employee

    [Fact]
    public async Task Should_Load_An_Employee_From_Code_Name_Joining_Date_And_Civil_Id()
    {
        GivenTheReferenceData();

        var output = await _goLive.LoadEmployees(Input(Row("E001", "Ahmed Ali Hassan", Joined, "290011501234")));

        output.EmployeesCreated.ShouldBe(1);
        output.Rows.Single().Succeeded.ShouldBeTrue();
        UsingDbContext(context =>
        {
            var employee = context.Employees.Single(e => e.TenantId == TenantId && e.Code == "E001");
            employee.FullNameEn.ShouldBe("Ahmed Ali Hassan");
            employee.JoiningDate.ShouldBe(Joined);
            employee.CivilID.ShouldBe(290011501234);
            employee.Status.ShouldBe(EmployeeStatus.Active);
        });
    }

    /// <summary>
    /// Nothing is guessed. What the grid does not ask points at a NO_ record in its lookup, so it reads as
    /// missing -- and gender, a fixed list rather than a lookup, is "not specified".
    /// </summary>
    [Fact]
    public async Task Should_Mark_What_Was_Not_Asked_As_Not_Given()
    {
        GivenTheReferenceData();

        var output = await _goLive.LoadEmployees(Input(Row("E001", "Ahmed Ali", Joined, "290011501234")));

        output.PlaceholdersCreated.ShouldBe(9);
        UsingDbContext(context =>
        {
            var employee = context.Employees.Single(e => e.TenantId == TenantId && e.Code == "E001");
            employee.Gender.ShouldBe(Gender.NotSpecified);
            context.Nationalities.Single(n => n.Id == employee.NationalityId).Code.ShouldBe("NO_NATIONALITY");
            context.Titles.Single(n => n.Id == employee.TitleId).Code.ShouldBe("NO_TITLE");
            context.MaritalStatuses.Single(n => n.Id == employee.MaritalStatusId).Code.ShouldBe("NO_MARITAL_STATUS");
            context.Religions.Single(n => n.Id == employee.ReligionId).Code.ShouldBe("NO_RELIGION");
            context.Governorates.Single(n => n.Id == employee.GovernorateId).Code.ShouldBe("NO_GOVERNORATE");
            context.Countries.Single(n => n.Id == employee.CountryId).Code.ShouldBe("NO_COUNTRY");
            context.Qualifications.Single(n => n.Id == employee.QualificationId).Code.ShouldBe("NO_QUALIFICATION");
            context.Cities.Single(n => n.Id == employee.BirthPlaceId).Code.ShouldBe("NO_BIRTH_PLACE");
            context.RecruitmentTypes.Single(n => n.Id == employee.RecruitmentTypeId).Code.ShouldBe("NO_RECRUITMENT_TYPE");
        });
    }

    [Fact]
    public async Task Should_Create_Each_No_Record_Once()
    {
        GivenTheReferenceData();
        await _goLive.LoadEmployees(Input(Row("E001", "Ahmed Ali", Joined, "290011501234")));

        var second = await _goLive.LoadEmployees(Input(Row("E002", "Sara Khalid", Joined, "290011501235")));

        second.PlaceholdersCreated.ShouldBe(0);
        UsingDbContext(context => context.Nationalities.Count(n => n.TenantId == TenantId && n.Code == "NO_NATIONALITY").ShouldBe(1));
    }

    /// <summary>
    /// The payroll engine refuses an employee without a payroll status. It gets one when loaded, with the values
    /// the time engine gives an employee it meets for the first time: evaluate time from the joining date, no
    /// master-data change pending, nothing accounted yet.
    /// </summary>
    [Fact]
    public async Task Should_Give_Each_Employee_A_Payroll_Status_From_The_Joining_Date()
    {
        GivenTheReferenceData();

        await _goLive.LoadEmployees(Input(Row("E001", "Ahmed Ali", Joined, "290011501234")));

        UsingDbContext(context =>
        {
            var employeeId = context.Employees.Single(e => e.TenantId == TenantId && e.Code == "E001").Id;
            var status = context.EmployeePayrollStatuses.Single(s => s.EmployeeId == employeeId);
            status.NextTimeEvaluationFrom.ShouldBe(Joined);
            status.EarliestRetroAccountingDate.ShouldBe(Joined);
            status.EarliestMasterDataChange.ShouldBe(default);
            status.AccountedTo.ShouldBe(default);
            status.DoNoAccountAfter.ShouldBe(default);
        });
    }

    /// <summary>
    /// Basic salary and four allowances, as one open-ended monthly pay record from the joining date with a line
    /// per amount given. The allowances are optional; a blank one is not paid, not paid as zero.
    /// </summary>
    [Fact]
    public async Task Should_Give_Each_Employee_A_Monthly_Salary_From_The_Grid()
    {
        GivenTheReferenceData();
        var row = Row("E001", "Ahmed Ali", Joined, "290011501234", basic: 600);
        row.AccommodationAllowance = 150;
        row.MobileAllowance = 10;
        row.TransportAllowance = 50;
        row.FoodAllowance = 25;
        row.OtherAllowance = 5;

        await _goLive.LoadEmployees(Input(row));

        UsingDbContext(context =>
        {
            var employeeId = context.Employees.Single(e => e.TenantId == TenantId && e.Code == "E001").Id;
            var pay = context.EmployeeBasicPays.Single(p => p.EmployeeId == employeeId);
            pay.StartDate.ShouldBe(Joined);
            pay.EndDate.ShouldBe(OpenEnded);
            pay.PayInterval.ShouldBe(PayInterval.Monthly);
            pay.Amount.ShouldBe(840);

            var lines = context.EmployeeBasicPayDetails
                .Where(d => d.EmployeeBasicPayId == pay.Id)
                .Select(d => new { d.WageType.Code, d.Amount })
                .OrderBy(d => d.Code)
                .ToList();
            lines.Select(l => (l.Code, l.Amount)).ShouldBe(new[] { ("1000", 600d), ("1002", 50d), ("1011", 10d), ("1018", 5d), ("1027", 150d), ("1038", 25d) });
        });
    }

    [Fact]
    public async Task Should_Give_Each_Employee_An_Open_Ended_Contract_From_The_Joining_Date()
    {
        GivenTheReferenceData();

        await _goLive.LoadEmployees(Input(Row("E001", "Ahmed Ali", Joined, "290011501234")));

        UsingDbContext(context =>
        {
            var employeeId = context.Employees.Single(e => e.TenantId == TenantId && e.Code == "E001").Id;
            var contract = context.EmployeeContracts.Single(c => c.EmployeeId == employeeId);
            contract.StartDate.ShouldBe(Joined);
            contract.EndDate.ShouldBe(OpenEnded);
            context.ContractTypes.Single(t => t.Id == contract.ContractTypeId).NameEn.ShouldBe("Unlimited");
        });
    }

    /// <summary>
    /// The org assignment is what puts somebody into a payroll: the engine finds employees through it, and
    /// reads the payroll area and the sub-group from it. All five links are checked here.
    /// </summary>
    [Fact]
    public async Task Should_Assign_Each_Employee_To_The_Org_Unit_Payroll_Area_Group_And_Sub_Group()
    {
        GivenTheReferenceData();

        await _goLive.LoadEmployees(Input(Row("E001", "Ahmed Ali", Joined, "290011501234")));

        UsingDbContext(context =>
        {
            var employeeId = context.Employees.Single(e => e.TenantId == TenantId && e.Code == "E001").Id;
            var assignment = context.EmployeeOrgAssignments.Single(a => a.EmployeeId == employeeId);

            assignment.StartDate.ShouldBe(Joined);
            assignment.EndDate.ShouldBe(OpenEnded);
            context.PayrollAreas.Single(a => a.Id == assignment.PayrollAreaId).Name.ShouldBe(DefaultArea);
            context.OrganizationUnits.Single(o => o.Id == assignment.OrganizationUnitId).DisplayName.ShouldBe(Company);
            context.EmployeeGroups.Single(g => g.Id == assignment.EmployeeGroupId).NameEn.ShouldBe("Default Normal Staff");
            context.EmployeeSubGroups.Single(s => s.Id == assignment.EmployeeSubGroupId).NameEn.ShouldBe("Default Grade");
        });
    }

    // ---------------------------------------------------------------- what is created around the employees

    [Fact]
    public async Task Should_Create_The_Org_Unit_Once_And_Reuse_It()
    {
        GivenTheReferenceData();

        var first = await _goLive.LoadEmployees(Input(Row("E001", "Ahmed Ali", Joined, "290011501234")));
        var second = await _goLive.LoadEmployees(Input(Row("E002", "Sara Khalid", Joined, "290011501235")));

        first.OrganizationUnitCreated.ShouldBeTrue();
        second.OrganizationUnitCreated.ShouldBeFalse();
        UsingDbContext(context =>
        {
            var unit = context.OrganizationUnits.Single(o => o.TenantId == TenantId && o.DisplayName == Company);
            context.OrgUnitProfiles.Count(p => p.OrganizationUnitId == unit.Id).ShouldBe(1);
        });
    }

    [Fact]
    public async Task Should_Create_The_Default_Payroll_Area_When_There_Is_None()
    {
        GivenTheReferenceData();

        var output = await _goLive.LoadEmployees(Input(Row("E001", "Ahmed Ali", Joined, "290011501234")));

        output.PayrollAreaCreated.ShouldBeTrue();
        UsingDbContext(context => context.PayrollAreas.Count(a => a.TenantId == TenantId && a.Name == DefaultArea).ShouldBe(1));
    }

    /// <summary>
    /// Kuwait Labour Law (Law 6/2010), as the Default tenant's own "Benefits_Normal" grade is set up: indemnity
    /// of 15 days' pay a year for the first five years and 26 days' pay a year after that, on a 26-day month,
    /// capped at 18 months' pay; 30 days' annual leave and 15 days' paid sick leave a year.
    /// </summary>
    [Fact]
    public async Task Should_Set_Up_Default_Normal_Staff_As_Kuwait_Labour_Law_Says()
    {
        GivenTheReferenceData();

        var output = await _goLive.LoadEmployees(Input(Row("E001", "Ahmed Ali", Joined, "290011501234")));

        output.EmployeeGroupCreated.ShouldBeTrue();
        output.EmployeeSubGroupCreated.ShouldBeTrue();
        output.GratuitySlabsCreated.ShouldBe(2);
        output.LeaveEntitlementsCreated.ShouldBe(2);
        output.SickLeaveRuleCreated.ShouldBeTrue();

        UsingDbContext(context =>
        {
            var group = context.EmployeeGroups.Single(g => g.TenantId == TenantId && g.NameEn == "Default Normal Staff");
            group.IndemnityMaxNumber.ShouldBe(18);
            group.IndemnityMaxUnit.ShouldBe(Unit.Month);
            group.IndemnityMaxAccruePercentage.ShouldBe(100);

            var subGroup = context.EmployeeSubGroups.Single(s => s.TenantId == TenantId && s.NameEn == "Default Grade");
            subGroup.EmployeeGroupId.ShouldBe(group.Id);

            var slabs = context.EmployeeSubGroupGratuities
                .Where(g => g.EmployeeSubGroupId == subGroup.Id)
                .OrderBy(g => g.ChangeYear)
                .ToList();
            slabs.Select(s => (s.ChangeYear, s.GratuityDays, s.BasisDays)).ShouldBe(new[] { (5d, 15d, 26d), (99d, 26d, 26d) });
            slabs.ShouldAllBe(s => s.EndDate == OpenEnded && !s.ImpactFromDateOfJoining);

            var leave = context.EmployeeSubGroupAbsences
                .Where(a => a.EmployeeSubGroupId == subGroup.Id)
                .Select(a => new { a.AbsenceType.Code, a.TotalDays })
                .OrderBy(a => a.Code)
                .ToList();
            leave.Select(l => (l.Code, l.TotalDays)).ShouldBe(new[] { ("1", 30), ("3", 15) });
        });
    }

    /// <summary>
    /// Art. 69 after the 15 full-pay days: 10 days at three quarters, 10 at half, 10 at a quarter, 30 unpaid --
    /// as slabs of (last unpaid-sick day, % deducted), all in force together, on the sick-leave entitlement.
    /// </summary>
    [Fact]
    public async Task Should_Attach_The_Kuwait_Sick_Leave_Deduction_Rule()
    {
        GivenTheReferenceData();

        await _goLive.LoadEmployees(Input(Row("E001", "Ahmed Ali", Joined, "290011501234")));

        UsingDbContext(context =>
        {
            var sickLeave = context.EmployeeSubGroupAbsences.Single(a => a.TenantId == TenantId && a.AbsenceType.Code == "3");
            sickLeave.AbsenceDeductionRuleId.ShouldNotBeNull();

            var slabs = context.AbsenceDedRuleDetails
                .Where(d => d.AbsenceDeductionRuleId == sickLeave.AbsenceDeductionRuleId)
                .OrderBy(d => d.DaysCount)
                .ToList();
            slabs.Select(s => (s.DaysCount, s.DeductionNumber)).ShouldBe(new[] { (10d, 25d), (20d, 50d), (30d, 75d), (60d, 100d) });
            slabs.ShouldAllBe(s => s.DeductionUnit == DeductionUnit.Percentage && s.EndDate == OpenEnded);

            // Annual leave carries no deduction rule.
            context.EmployeeSubGroupAbsences.Single(a => a.TenantId == TenantId && a.AbsenceType.Code == "1")
                .AbsenceDeductionRuleId.ShouldBeNull();
        });
    }

    /// <summary>The company is a "Company" organisation unit, which carries the payroll area as well.</summary>
    [Fact]
    public async Task Should_Make_The_Company_A_Company_Type_Unit_Carrying_The_Payroll_Area()
    {
        GivenTheReferenceData();

        await _goLive.LoadEmployees(Input(Row("E001", "Ahmed Ali", Joined, "290011501234")));

        UsingDbContext(context =>
        {
            var unit = context.OrganizationUnits.Single(o => o.TenantId == TenantId && o.DisplayName == Company);
            var profile = context.OrgUnitProfiles.Single(p => p.OrganizationUnitId == unit.Id);
            var type = context.OrganizationUnitTypes.Single(t => t.Id == profile.OrganizationUnitTypeId);

            type.NameEn.ShouldBe("Company");
            type.Code.ShouldBe(OrganizationUnitType.Level1Code);
            type.AssignPayrollArea.ShouldBeTrue();
            context.PayrollAreas.Single(a => a.Id == profile.PayrollAreaId).Name.ShouldBe(DefaultArea);
        });
    }

    [Fact]
    public async Task Should_Not_Duplicate_The_Kuwait_Defaults_On_A_Second_Load()
    {
        GivenTheReferenceData();
        await _goLive.LoadEmployees(Input(Row("E001", "Ahmed Ali", Joined, "290011501234")));

        var second = await _goLive.LoadEmployees(Input(Row("E002", "Sara Khalid", Joined, "290011501235")));

        second.EmployeeGroupCreated.ShouldBeFalse();
        second.EmployeeSubGroupCreated.ShouldBeFalse();
        second.GratuitySlabsCreated.ShouldBe(0);
        second.LeaveEntitlementsCreated.ShouldBe(0);
        UsingDbContext(context =>
        {
            context.EmployeeGroups.Count(g => g.TenantId == TenantId && g.NameEn == "Default Normal Staff").ShouldBe(1);
            context.EmployeeSubGroupGratuities.Count(g => g.TenantId == TenantId).ShouldBe(2);
            context.EmployeeSubGroupAbsences.Count(a => a.TenantId == TenantId).ShouldBe(2);
        });
    }

    // ---------------------------------------------------------------- the grid's rows

    [Fact]
    public async Task Should_Report_Bad_Rows_And_Still_Load_The_Good_Ones()
    {
        GivenTheReferenceData();

        var output = await _goLive.LoadEmployees(Input(
            Row("E001", "Ahmed Ali", Joined, "290011501234"),
            Row("E002", "No Civil Id", Joined, null),
            Row("E001", "Same Code Again", Joined, "290011509999"),
            Row("E003", "Same Civil Id", Joined, "290011501234"),
            Row("E004", "No Joining Date", null, "290011508888"),
            Row("E005", "Letters In Civil Id", Joined, "29001ABC"),
            Row("E006", "No Basic Salary", Joined, "290011507777", basic: null)));

        output.EmployeesCreated.ShouldBe(1);
        output.Rows.Count.ShouldBe(7);
        output.Rows.Where(r => r.Succeeded).Select(r => r.RowNumber).ShouldBe(new[] { 1 });
        output.Rows.Where(r => !r.Succeeded).ShouldAllBe(r => !string.IsNullOrWhiteSpace(r.Message));
        UsingDbContext(context => context.Employees.Count(e => e.TenantId == TenantId).ShouldBe(1));
    }

    [Fact]
    public async Task Should_Leave_An_Employee_Already_Loaded_Alone()
    {
        // Loading the same sheet twice is the normal way to fix a mistake and try again.
        GivenTheReferenceData();
        await _goLive.LoadEmployees(Input(Row("E001", "Ahmed Ali", Joined, "290011501234")));

        var again = await _goLive.LoadEmployees(Input(Row("E001", "Ahmed Ali", Joined, "290011501234")));

        again.EmployeesCreated.ShouldBe(0);
        again.EmployeesAlreadyLoaded.ShouldBe(1);
        again.Rows.Single().Succeeded.ShouldBeTrue();
        UsingDbContext(context =>
        {
            var employeeId = context.Employees.Single(e => e.TenantId == TenantId && e.Code == "E001").Id;
            context.EmployeeContracts.Count(c => c.EmployeeId == employeeId).ShouldBe(1);
            context.EmployeeOrgAssignments.Count(a => a.EmployeeId == employeeId).ShouldBe(1);
            context.EmployeeBasicPays.Count(p => p.EmployeeId == employeeId).ShouldBe(1);
            context.EmployeePayrollStatuses.Count(s => s.EmployeeId == employeeId).ShouldBe(1);
        });
    }

    [Fact]
    public async Task Should_Refuse_Without_An_Unlimited_Contract_Type()
    {
        // Only the Kuwait pack seeds contract types. Guessing another type would misdescribe every contract.
        AsyncHelper.RunSync(() => Resolve<IReferenceDataSeeder>().SeedPackAsync("universal", TenantId));

        var ex = await Should.ThrowAsync<UserFriendlyException>(() =>
            _goLive.LoadEmployees(Input(Row("E001", "Ahmed Ali", Joined, "290011501234"))));

        ex.Message.ShouldContain("Unlimited");
    }

    // ---------------------------------------------------------------- the payroll area dropdown

    [Fact]
    public async Task Should_List_The_Payroll_Areas_For_The_Dropdown()
    {
        GivenTheReferenceData();
        await _goLive.LoadEmployees(Input(Row("E001", "Ahmed Ali", Joined, "290011501234")));

        var names = await _goLive.GetPayrollAreaNames();

        names.Items.ShouldContain(DefaultArea);
    }

    // ---------------------------------------------------------------- where Run payroll goes next

    /// <summary>An empty payroll area is where this step comes in: Run payroll now points back to it.</summary>
    [Fact]
    public async Task Should_Point_To_Loading_Employees_When_The_Payroll_Area_Has_Nobody()
    {
        GivenTheReferenceData();
        await _goLive.SetUpPayrollMonth(new GoLiveV2SetUpPayrollMonthInput { PayDate = PayDate, PayrollAreaName = DefaultArea });

        var result = await _goLive.RunPayroll(new GoLiveV2RunPayrollInput { PayDate = PayDate, PayrollAreaName = DefaultArea });

        result.StoppedAt.ShouldBe(GoLiveV2StoppedAt.NoEmployees);
        result.NextStep.ShouldBe(GoLiveV2NextStep.LoadEmployees);
    }

    /// <summary>
    /// Working time, as the customer set it: planned time with no punches, 08:00 to 17:00, Friday the weekly
    /// off. One duty for working days, one weekly-off duty, a duty period laying them over the week with an
    /// (empty) holiday calendar, and a planned-time policy -- plus the four settings records the time engine
    /// reads -- all created once for the company.
    /// </summary>
    [Fact]
    public async Task Should_Set_Up_Working_Time_Eight_To_Five_With_Friday_Off()
    {
        GivenTheReferenceData();

        var output = await _goLive.LoadEmployees(Input(Row("E001", "Ahmed Ali", Joined, "290011501234")));

        output.WorkingTimeCreated.ShouldBeTrue();
        UsingDbContext(context =>
        {
            context.ManageTimeSettingses.Count(s => s.TenantId == TenantId).ShouldBe(1);
            context.ManagePayrollSettingses.Count(s => s.TenantId == TenantId).ShouldBe(1);
            context.ManagePersonnelSettingses.Count(s => s.TenantId == TenantId).ShouldBe(1);
            context.ManageSecuritySettingses.Count(s => s.TenantId == TenantId).ShouldBe(1);

            var period = context.DutyPeriods.Single(p => p.TenantId == TenantId);
            period.HolidayCalendarId.ShouldNotBeNull();

            var working = context.Duties.Single(d => d.Id == period.SundayDutyId);
            working.StartTime.ShouldBe(new TimeSpan(8, 0, 0));
            working.EndTime.ShouldBe(new TimeSpan(17, 0, 0));
            working.IsWeeklyOff.ShouldBeFalse();
            new[] { period.MondayDutyId, period.TuesdayDutyId, period.WednesdayDutyId, period.ThursdayDutyId, period.SaturdayDutyId }
                .ShouldAllBe(id => id == working.Id);

            context.Duties.Single(d => d.Id == period.FridayDutyId).IsWeeklyOff.ShouldBeTrue();

            var policy = context.TimePolicies.Single(p => p.TenantId == TenantId);
            policy.TimeMgmtStatus.ShouldBe(TimeMgmtStatus.PlannedTime);
            policy.ShouldSignIn.ShouldBeFalse();
            policy.ShouldSignOut.ShouldBeFalse();
            policy.ShouldBreakSignIn.ShouldBeFalse();
            policy.ShouldBreakSignOut.ShouldBeFalse();
        });
    }

    [Fact]
    public async Task Should_Give_Each_Employee_The_Working_Time_From_The_Joining_Date()
    {
        GivenTheReferenceData();

        await _goLive.LoadEmployees(Input(Row("E001", "Ahmed Ali", Joined, "290011501234")));
        var second = await _goLive.LoadEmployees(Input(Row("E002", "Sara Khalid", Joined, "290011501235")));

        second.WorkingTimeCreated.ShouldBeFalse();
        UsingDbContext(context =>
        {
            var employeeId = context.Employees.Single(e => e.TenantId == TenantId && e.Code == "E001").Id;
            context.EmployeeDutyPeriods.Single(p => p.EmployeeId == employeeId).StartDate.ShouldBe(Joined);
            context.EmployeeTimePolicies.Single(p => p.EmployeeId == employeeId).StartDate.ShouldBe(Joined);
            context.DutyPeriods.Count(p => p.TenantId == TenantId).ShouldBe(1);
            context.Duties.Count(d => d.TenantId == TenantId).ShouldBe(2);
        });
    }

    /// <summary>
    /// The end of the chain: with working time in place the time engine gets through the month, and the payroll
    /// engine pays the employee -- the first payroll on a company that started with nothing.
    /// </summary>
    [Fact]
    public async Task Should_Run_The_First_Payroll_Once_Working_Time_Is_Set_Up()
    {
        GivenTheReferenceData();
        await _goLive.LoadEmployees(Input(Row("E001", "Ahmed Ali", Joined, "290011501234")));
        await _goLive.SetUpPayrollMonth(new GoLiveV2SetUpPayrollMonthInput { PayDate = PayDate, PayrollAreaName = DefaultArea });

        var result = await _goLive.RunPayroll(new GoLiveV2RunPayrollInput { PayDate = PayDate, PayrollAreaName = DefaultArea });

        result.StoppedAt.ShouldNotBe(GoLiveV2StoppedAt.TimeEngine, result.Message);
        result.Succeeded.ShouldBeTrue(result.Message);
        result.StoppedAt.ShouldBe(GoLiveV2StoppedAt.None);
        result.Employees.Single().Succeeded.ShouldBeTrue();
        UsingDbContext(context =>
        {
            var employeeId = context.Employees.Single(e => e.TenantId == TenantId && e.Code == "E001").Id;
            context.TimeRegisters.Count(r => r.EmployeeId == employeeId && r.DayDate.Month == 9 && r.DayDate.Year == 2026).ShouldBe(30);

            // The first payslip: a full month's basic salary of 500, and nothing taken off it.
            var register = context.SalaryRegisters.Single(r => r.EmployeeId == employeeId && r.ForPeriod == PayDate && r.InPeriod == PayDate);
            var lines = context.SalaryRegisterDetails
                .Where(d => d.SalaryRegisterId == register.Id)
                .Select(d => new { d.WageType.Code, d.Amount })
                .ToList();
            lines.Single(l => l.Code == "1000").Amount.ShouldBe(500, 0.001);
            lines.Single(l => l.Code == "9000").Amount.ShouldBe(500, 0.001);
        });
    }

    // ---------------------------------------------------------------- arrange

    /// <summary>Currency (universal), contract types (Kuwait), leave and wage types (payroll) -- all are read.</summary>
    private void GivenTheReferenceData()
    {
        var seeder = Resolve<IReferenceDataSeeder>();
        foreach (var pack in new[] { "universal", "kuwait", "payroll" })
        {
            AsyncHelper.RunSync(() => seeder.SeedPackAsync(pack, TenantId));
        }
    }

    private static GoLiveV2LoadEmployeesInput Input(params GoLiveV2EmployeeRowDto[] rows) =>
        new()
        {
            OrganizationUnitName = Company,
            PayrollAreaName = DefaultArea,
            Employees = new List<GoLiveV2EmployeeRowDto>(rows),
        };

    private static GoLiveV2EmployeeRowDto Row(string code, string fullName, DateTime? joined, string civilId, double? basic = 500) =>
        new() { Code = code, FullName = fullName, JoiningDate = joined, CivilId = civilId, BasicSalary = basic };
}
