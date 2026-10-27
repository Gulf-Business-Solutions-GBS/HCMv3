using System.Linq;
using HCMv3.ReportManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ReportManagement;

/// <summary>
/// Tests for report DISCOVERY.
///
/// The point of discovering rather than registering is that a report written next year is schedulable
/// the day it exists, with no entry added anywhere. These tests protect that property: they assert
/// against the REAL application assembly, so if the convention ever drifts -- a base class renamed, an
/// export method signature changed -- the count collapses and this fails, instead of the wizard
/// quietly offering fewer reports than the system has.
/// </summary>
// ReSharper disable once InconsistentNaming
public class SchedulableReportCatalog_Tests : AppTestBase
{
    private readonly ISchedulableReportCatalog _catalog;

    public SchedulableReportCatalog_Tests()
    {
        _catalog = Resolve<ISchedulableReportCatalog>();
    }

    [Fact]
    public void Should_Discover_The_Grid_Reports_Without_Any_Registration()
    {
        var all = _catalog.GetAll();

        // 138 grid report services existed when discovery was written. The floor is deliberately well
        // below that: this asserts "discovery is working", not "nobody may delete a report".
        all.Count.ShouldBeGreaterThan(100);
    }

    [Fact]
    public void Every_Discovered_Report_Should_Be_Executable()
    {
        // A descriptor missing any of these would fail at run time, mid-run, after the schedule had
        // already been saved and had already started mailing people.
        foreach (var descriptor in _catalog.GetAll())
        {
            descriptor.ReportKey.ShouldNotBeNullOrWhiteSpace();
            descriptor.DisplayName.ShouldNotBeNullOrWhiteSpace();
            descriptor.ServiceInterface.ShouldNotBeNull();
            descriptor.ExportMethod.ShouldNotBeNull();
            descriptor.InputType.ShouldNotBeNull();
            descriptor.ExportMethod.GetParameters().Length.ShouldBe(1);
        }
    }

    [Fact]
    public void Should_Find_A_Known_Report_By_Key()
    {
        var descriptor = _catalog.Find("EmployeeAttendanceReportAppService");

        descriptor.ShouldNotBeNull();
        descriptor.InputType.Name.ShouldBe("GetEmployeeAttendanceReportInput");
    }

    [Fact]
    public void Should_Match_The_Key_Case_Insensitively()
    {
        // Keys are stored on a schedule as free text and round-trip through JSON; a casing difference
        // must not turn into "report not found" months after the schedule was created.
        _catalog.Find("employeeattendancereportappservice").ShouldNotBeNull();
    }

    [Fact]
    public void Should_Return_Null_For_An_Unknown_Key()
    {
        _catalog.Find("NoSuchReportAppService").ShouldBeNull();
    }

    [Fact]
    public void Should_Describe_A_Report_Parameters_For_The_Wizard()
    {
        var report = _catalog.GetForClient()
            .First(r => r.ReportKey == "EmployeeAttendanceReportAppService");

        report.Parameters.ShouldNotBeEmpty();

        // Paging and sorting belong to the grid, not to a schedule: a scheduled export always covers
        // every matching row, so offering these in the wizard would be meaningless at best.
        report.Parameters.ShouldNotContain(p => p.Name == "SkipCount");
        report.Parameters.ShouldNotContain(p => p.Name == "MaxResultCount");
        report.Parameters.ShouldNotContain(p => p.Name == "Sorting");
    }

    [Fact]
    public void Should_Offer_Allowed_Values_For_Enum_Parameters()
    {
        // So the wizard can render a dropdown rather than asking somebody to type an enum name.
        var enumParameter = _catalog.GetForClient()
            .SelectMany(r => r.Parameters)
            .FirstOrDefault(p => p.AllowedValues != null && p.AllowedValues.Count > 0);

        enumParameter.ShouldNotBeNull();
    }
}
