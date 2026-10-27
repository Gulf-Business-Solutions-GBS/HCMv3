using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

/// <summary>
/// The Time Engine screen's employee picker, ported from legacy <c>uscTimeEngine</c> popupTimeEmployees
/// (<c>BuildTimeEmployeePopupList</c>, lines 581-650; filters and counts, lines 652-842; period text, 961-968):
/// <list type="bullet">
/// <item><description>the selected area's employees whose org assignment OVERLAPS the Start-End window, one per code,
/// by code -- no status filter, as legacy;</description></item>
/// <item><description>Processed = a Regular salary register for the area's current period
/// (<c>ForPeriod == InPeriod ==</c> the control record's month end), otherwise Pending;</description></item>
/// <item><description>All / Processed / Pending / Selected with counts over the whole list, a search, select all,
/// and the header's area and "dd/MM/yyyy - dd/MM/yyyy" period.</description></item>
/// </list>
/// </summary>
// ReSharper disable once InconsistentNaming
public class TimeEngineAppService_EmployeePicker_Tests : TimeEngineAppServiceTestBase
{
    private static readonly DateTime PeriodEnd = new DateTime(2026, 9, 30);
    private static readonly DateTime WindowStart = new DateTime(2026, 9, 1);

    private readonly int _areaId;

    public TimeEngineAppService_EmployeePicker_Tests()
    {
        _areaId = CreatePayrollArea("Picker Area", PeriodEnd).Id;
        var otherAreaId = CreatePayrollArea("Other Area", PeriodEnd).Id;

        var paid = CreateEmployee("P002", _areaId);
        CreateEmployee("P001", _areaId);
        CreateEmployee("P003", _areaId, EmployeeStatus.Inactive);

        // Assigned only until mid-September: still overlaps the window.
        CreateEmployee("P004", _areaId, assignedFrom: new DateTime(2019, 1, 1), assignedTo: new DateTime(2026, 9, 15));

        // Left the area before the window: not listed.
        CreateEmployee("P005", _areaId, assignedFrom: new DateTime(2019, 1, 1), assignedTo: new DateTime(2026, 8, 31));

        CreateEmployee("Q001", otherAreaId);

        AddRegularRegister(paid, PeriodEnd);
    }

    [Fact]
    public async Task Should_List_The_Areas_Employees_Overlapping_The_Window_By_Code()
    {
        var output = await GetAsync();

        output.Items.Select(i => i.EmployeeCode).ShouldBe(new[] { "P001", "P002", "P003", "P004" });
        output.AllCount.ShouldBe(4);
    }

    /// <summary>Legacy built the popup without a status filter (unlike the payroll engine's popup).</summary>
    [Fact]
    public async Task Should_List_Inactive_Employees_As_Legacy_Did()
    {
        var output = await GetAsync();

        output.Items.ShouldContain(i => i.EmployeeCode == "P003");
    }

    [Fact]
    public async Task Should_Mark_An_Employee_Already_Paid_This_Period_As_Processed()
    {
        var output = await GetAsync();

        output.Items.Single(i => i.EmployeeCode == "P002").PayrollStatus.ShouldBe(PayrollRunCandidateStatus.Processed);
        output.Items.Single(i => i.EmployeeCode == "P001").PayrollStatus.ShouldBe(PayrollRunCandidateStatus.Pending);
        output.ProcessedCount.ShouldBe(1);
        output.PendingCount.ShouldBe(3);
    }

    /// <summary>Only a REGULAR register of the current period counts.</summary>
    [Fact]
    public async Task Should_Not_Count_An_Off_Cycle_Register_As_Processed()
    {
        var p001 = UsingDbContext(context => context.Employees.Single(e => e.Code == "P001").Id);
        AddRegularRegister(p001, PeriodEnd, PayrollRunType.OffCycle);

        var output = await GetAsync();

        output.Items.Single(i => i.EmployeeCode == "P001").PayrollStatus.ShouldBe(PayrollRunCandidateStatus.Pending);
    }

    [Fact]
    public async Task Should_Filter_By_Processed_Pending_And_Selected_With_Counts_Over_The_Whole_List()
    {
        var processed = await GetAsync(PayrollRunCandidateFilter.Processed);
        processed.Items.Select(i => i.EmployeeCode).ShouldBe(new[] { "P002" });
        processed.AllCount.ShouldBe(4);

        var pending = await GetAsync(PayrollRunCandidateFilter.Pending);
        pending.Items.Select(i => i.EmployeeCode).ShouldBe(new[] { "P001", "P003", "P004" });

        var selected = await GetAsync(PayrollRunCandidateFilter.Selected, selectedCodes: "p004, P001");
        selected.Items.Select(i => i.EmployeeCode).ShouldBe(new[] { "P001", "P004" });
        selected.SelectedCount.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Search_By_Code_Or_Name()
    {
        var output = await SearchAsync("p004");

        output.Items.Select(i => i.EmployeeCode).ShouldBe(new[] { "P004" });
        output.TotalCount.ShouldBe(1);
        output.AllCount.ShouldBe(4, "the filter buttons always count the whole list");
    }

    [Fact]
    public async Task Should_Give_Every_Code_The_Filter_Shows_For_Select_All()
    {
        var codes = await TimeEngineAppService.GetTimeEngineRunCandidateCodes(NewInput(PayrollRunCandidateFilter.Pending));

        codes.ShouldBe(new[] { "P001", "P003", "P004" });
    }

    [Fact]
    public async Task Should_Show_The_Period_As_Legacy_Did()
    {
        var output = await GetAsync();

        output.PeriodText.ShouldBe("01/09/2026 - 30/09/2026");
    }

    /// <summary>Legacy: no end date means the window is the start date alone.</summary>
    [Fact]
    public async Task Should_Use_The_Start_Date_Alone_When_No_End_Date_Is_Given()
    {
        var input = NewInput();
        input.EndDate = null;
        input.StartDate = new DateTime(2026, 9, 20);

        var output = await TimeEngineAppService.GetTimeEngineRunCandidates(input);

        output.PeriodText.ShouldBe("20/09/2026 - 20/09/2026");
        output.Items.ShouldNotContain(i => i.EmployeeCode == "P004", "P004's assignment ended on the 15th");
    }

    /// <summary>Legacy line 590: no payroll area, no list.</summary>
    [Fact]
    public async Task Should_List_Nobody_Without_A_Payroll_Area()
    {
        var input = NewInput();
        input.PayrollAreaName = "Unknown Area";

        var output = await TimeEngineAppService.GetTimeEngineRunCandidates(input);

        output.Items.ShouldBeEmpty();
        output.AllCount.ShouldBe(0);
    }

    /// <summary>Legacy: an area without a control record has no period, so nobody is Processed.</summary>
    [Fact]
    public async Task Should_List_Everyone_As_Pending_When_The_Area_Has_No_Control_Record()
    {
        var bareArea = CreatePayrollArea("Bare Area", calendarMonthEnd: null);
        var employee = CreateEmployee("R001", bareArea.Id);
        AddRegularRegister(employee, PeriodEnd);

        var input = NewInput();
        input.PayrollAreaName = "Bare Area";

        var output = await TimeEngineAppService.GetTimeEngineRunCandidates(input);

        output.Items.ShouldHaveSingleItem().PayrollStatus.ShouldBe(PayrollRunCandidateStatus.Pending);
    }

    private Task<TimeEngineRunCandidatesOutput> GetAsync(
        PayrollRunCandidateFilter filter = PayrollRunCandidateFilter.All,
        string selectedCodes = null,
        string filterText = null)
    {
        var input = NewInput(filter, selectedCodes);
        input.Filter = filterText;
        return TimeEngineAppService.GetTimeEngineRunCandidates(input);
    }

    private Task<TimeEngineRunCandidatesOutput> SearchAsync(string filter)
    {
        return GetAsync(PayrollRunCandidateFilter.All, null, filter);
    }

    private static GetTimeEngineRunCandidatesInput NewInput(
        PayrollRunCandidateFilter filter = PayrollRunCandidateFilter.All,
        string selectedCodes = null)
    {
        return new GetTimeEngineRunCandidatesInput
        {
            PayrollAreaName = "Picker Area",
            StartDate = WindowStart,
            EndDate = PeriodEnd,
            StatusFilter = filter,
            SelectedEmployeeCodes = selectedCodes,
            MaxResultCount = 100
        };
    }
}
