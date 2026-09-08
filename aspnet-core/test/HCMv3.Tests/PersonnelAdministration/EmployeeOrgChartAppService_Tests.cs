using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// Covers the Employee Org Chart feed. The interesting behaviour is all in the reporting-graph traversal:
/// EmployeeManager has no materialised path, so the manager chain and the descendant counts are computed
/// by walking edges in memory -- including the awkward cases the data permits but the traversal must
/// survive, namely cycles and self-management.
/// </summary>
public class EmployeeOrgChartAppService_Tests : AppTestBase
{
    private static readonly DateTime Always = new DateTime(2099, 12, 31);
    private static readonly DateTime LongAgo = new DateTime(2020, 1, 1);

    private readonly IEmployeeOrgChartAppService _employeeOrgChartAppService;

    public EmployeeOrgChartAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _employeeOrgChartAppService = Resolve<IEmployeeOrgChartAppService>();
    }

    [Fact]
    public async Task Should_Focus_The_Root_Of_The_Largest_Tree_When_Nobody_Requested()
    {
        var tree = CreateReportingTree();

        var result = await _employeeOrgChartAppService.GetNode(new GetEmployeeOrgChartNodeInput());

        result.Focus.ShouldNotBeNull();
        result.Focus.Id.ShouldBe(tree.GeneralManagerId);
        result.Manager.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Return_Only_The_Immediate_Manager_Not_The_Whole_Chain()
    {
        var tree = CreateReportingTree();

        var result = await _employeeOrgChartAppService.GetNode(new GetEmployeeOrgChartNodeInput
        {
            EmployeeId = tree.WarehouseOfficerId
        });

        result.Focus.Id.ShouldBe(tree.WarehouseOfficerId);

        // Three levels sit above this person, but the chart draws one row of manager and steps up by
        // re-centring, so only the nearest one comes back.
        result.Manager.ShouldNotBeNull();
        result.Manager.Id.ShouldBe(tree.WarehouseManagerId);
    }

    [Fact]
    public async Task Should_Step_Up_One_Level_At_A_Time()
    {
        var tree = CreateReportingTree();

        var atOfficer = await _employeeOrgChartAppService.GetNode(new GetEmployeeOrgChartNodeInput
        {
            EmployeeId = tree.WarehouseOfficerId
        });

        var atWarehouseManager = await _employeeOrgChartAppService.GetNode(new GetEmployeeOrgChartNodeInput
        {
            EmployeeId = atOfficer.Manager.Id
        });

        // Walking up by following Manager each time reaches the top, which is what the up arrow does.
        atWarehouseManager.Manager.Id.ShouldBe(tree.SupplyManagerId);

        var atSupplyManager = await _employeeOrgChartAppService.GetNode(new GetEmployeeOrgChartNodeInput
        {
            EmployeeId = atWarehouseManager.Manager.Id
        });

        atSupplyManager.Manager.Id.ShouldBe(tree.GeneralManagerId);
    }

    [Fact]
    public async Task Should_Return_Direct_Reports_Ordered_By_Name()
    {
        var tree = CreateReportingTree();

        var result = await _employeeOrgChartAppService.GetNode(new GetEmployeeOrgChartNodeInput
        {
            EmployeeId = tree.SupplyManagerId
        });

        result.DirectReports.Select(x => x.FullNameEn)
            .ShouldBe(new[] { "Asad Zafar Butt", "Bassel Tharwat", "Huma Faisal" });
    }

    [Fact]
    public async Task Should_Count_Direct_Reports_Against_The_Person_Only()
    {
        var tree = CreateReportingTree();

        var result = await _employeeOrgChartAppService.GetNode(new GetEmployeeOrgChartNodeInput
        {
            EmployeeId = tree.SupplyManagerId
        });

        // Three report to the supply manager directly; the warehouse officer is one level further down and
        // must not inflate the direct figure.
        result.Focus.DirectReportCount.ShouldBe(3);
        result.DirectReports.Single(x => x.Id == tree.WarehouseManagerId).DirectReportCount.ShouldBe(1);
        result.DirectReports.Single(x => x.Id == tree.LogisticsOfficerId).DirectReportCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Roll_Every_Level_Into_The_Total_Report_Count()
    {
        var tree = CreateReportingTree();

        var result = await _employeeOrgChartAppService.GetNode(new GetEmployeeOrgChartNodeInput
        {
            EmployeeId = tree.GeneralManagerId
        });

        // Supply manager, their three reports, and the warehouse officer below one of those.
        result.Focus.DirectReportCount.ShouldBe(1);
        result.Focus.TotalReportCount.ShouldBe(5);

        result.DirectReports.Single(x => x.Id == tree.SupplyManagerId).TotalReportCount.ShouldBe(4);
    }

    [Fact]
    public async Task Should_Break_A_Tie_On_Start_Date_Using_The_Later_End_Date()
    {
        var tree = CreateReportingTree();
        var corrected = CreateEmployee("EOC-FIX", "Corrected Person", Gender.Female);

        // Two rows sharing a start date -- a correction entered against the original. The one running
        // further into the future is the surviving record.
        AddReportingLine(corrected.Id, tree.WarehouseManagerId, LongAgo, new DateTime(2024, 12, 31));
        AddReportingLine(corrected.Id, tree.SupplyManagerId, LongAgo, Always);

        var result = await _employeeOrgChartAppService.GetNode(new GetEmployeeOrgChartNodeInput
        {
            EmployeeId = tree.SupplyManagerId
        });

        result.DirectReports.Select(x => x.Id).ShouldContain(corrected.Id);
    }

    [Fact]
    public async Task Should_Survive_A_Cycle_In_The_Reporting_Lines()
    {
        var first = CreateEmployee("EOC-CYC1", "Cycle One", Gender.Male);
        var second = CreateEmployee("EOC-CYC2", "Cycle Two", Gender.Female);

        // A manages B and B manages A. Nonsense, but the data permits it, and an unguarded walk would
        // never terminate -- this test exists to prove the guards hold.
        AddReportingLine(second.Id, first.Id, LongAgo, Always);
        AddReportingLine(first.Id, second.Id, LongAgo, Always);

        var result = await _employeeOrgChartAppService.GetNode(new GetEmployeeOrgChartNodeInput
        {
            EmployeeId = first.Id
        });

        result.Focus.Id.ShouldBe(first.Id);
        result.Focus.TotalReportCount.ShouldBe(1);
        result.Manager.Id.ShouldBe(second.Id);
    }

    [Fact]
    public async Task Should_Ignore_An_Employee_Listed_As_Their_Own_Manager()
    {
        var loner = CreateEmployee("EOC-SELF", "Self Manager", Gender.Male);
        AddReportingLine(loner.Id, loner.Id, LongAgo, Always);

        var result = await _employeeOrgChartAppService.GetNode(new GetEmployeeOrgChartNodeInput
        {
            EmployeeId = loner.Id
        });

        result.Focus.Id.ShouldBe(loner.Id);
        result.Manager.ShouldBeNull();
        result.DirectReports.ShouldBeEmpty();
        result.Focus.TotalReportCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Resolve_Job_Title_From_The_Latest_Assignment()
    {
        var tree = CreateReportingTree();

        var result = await _employeeOrgChartAppService.GetNode(new GetEmployeeOrgChartNodeInput
        {
            EmployeeId = tree.SupplyManagerId
        });

        result.Focus.JobTitleEn.ShouldBe("Senior Manager");
        result.Focus.JobTitleAr.ShouldBe("مدير أول");
    }

    [Fact]
    public async Task Should_Report_Whether_A_Photo_Exists_Without_Returning_It()
    {
        var withPhoto = CreateEmployee("EOC-PIC", "Has Photo", Gender.Male, photo: new byte[] { 1, 2, 3 });
        var withoutPhoto = CreateEmployee("EOC-NOPIC", "No Photo", Gender.Female);
        AddReportingLine(withoutPhoto.Id, withPhoto.Id, LongAgo, Always);

        var result = await _employeeOrgChartAppService.GetNode(new GetEmployeeOrgChartNodeInput
        {
            EmployeeId = withPhoto.Id
        });

        result.Focus.HasPhoto.ShouldBeTrue();
        result.DirectReports.Single().HasPhoto.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Carry_Gender_So_The_Card_Can_Pick_A_Blank_Avatar()
    {
        var manager = CreateEmployee("EOC-G1", "Male Manager", Gender.Male);
        var report = CreateEmployee("EOC-G2", "Female Report", Gender.Female);
        AddReportingLine(report.Id, manager.Id, LongAgo, Always);

        var result = await _employeeOrgChartAppService.GetNode(new GetEmployeeOrgChartNodeInput
        {
            EmployeeId = manager.Id
        });

        result.Focus.Gender.ShouldBe(Gender.Male);
        result.DirectReports.Single().Gender.ShouldBe(Gender.Female);
    }

    [Fact]
    public async Task Should_Return_Empty_Result_When_No_Reporting_Lines_Exist()
    {
        var result = await _employeeOrgChartAppService.GetNode(new GetEmployeeOrgChartNodeInput());

        result.Focus.ShouldBeNull();
        result.Manager.ShouldBeNull();
        result.DirectReports.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Use_The_Latest_Reporting_Line_When_An_Employee_Has_Several()
    {
        var tree = CreateReportingTree();
        var mover = CreateEmployee("EOC-MOVE", "Moved Person", Gender.Male);

        // Started under the warehouse manager, moved to the supply manager last year. The chart shows the
        // present picture, so the newer row wins -- and the older one must not leave them on both teams.
        AddReportingLine(mover.Id, tree.WarehouseManagerId, LongAgo, new DateTime(2024, 12, 31));
        AddReportingLine(mover.Id, tree.SupplyManagerId, new DateTime(2025, 1, 1), Always);

        var supplyManagerView = await _employeeOrgChartAppService.GetNode(new GetEmployeeOrgChartNodeInput
        {
            EmployeeId = tree.SupplyManagerId
        });

        supplyManagerView.DirectReports.Select(x => x.Id).ShouldContain(mover.Id);

        var warehouseManagerView = await _employeeOrgChartAppService.GetNode(new GetEmployeeOrgChartNodeInput
        {
            EmployeeId = tree.WarehouseManagerId
        });

        warehouseManagerView.DirectReports.Select(x => x.Id).ShouldNotContain(mover.Id);
    }

    [Fact]
    public async Task Should_Count_A_Lapsed_Reporting_Line_That_Was_Never_Superseded()
    {
        var tree = CreateReportingTree();
        var lapsed = CreateEmployee("EOC-LAPSED", "Lapsed Line", Gender.Female);

        // The only row on record for this person, and its range has ended. Dropping it would empty the
        // chart for any tenant whose rows were loaded once and never re-dated -- which is exactly why the
        // as-of filter was removed.
        AddReportingLine(lapsed.Id, tree.SupplyManagerId, LongAgo, new DateTime(2024, 12, 31));

        var result = await _employeeOrgChartAppService.GetNode(new GetEmployeeOrgChartNodeInput
        {
            EmployeeId = tree.SupplyManagerId
        });

        result.DirectReports.Select(x => x.Id).ShouldContain(lapsed.Id);
        result.Focus.DirectReportCount.ShouldBe(4);
    }

    [Fact]
    public async Task Should_Search_By_English_Name_Arabic_Name_And_Code()
    {
        var tree = CreateReportingTree();

        var byEnglishName = await _employeeOrgChartAppService.SearchNodes(
            new SearchEmployeeOrgChartNodesInput { Filter = "Bassel" });
        byEnglishName.Select(x => x.Id).ShouldBe(new[] { tree.WarehouseManagerId });

        var byArabicName = await _employeeOrgChartAppService.SearchNodes(
            new SearchEmployeeOrgChartNodesInput { Filter = "باسل" });
        byArabicName.Select(x => x.Id).ShouldBe(new[] { tree.WarehouseManagerId });

        var byCode = await _employeeOrgChartAppService.SearchNodes(
            new SearchEmployeeOrgChartNodesInput { Filter = "EOC-GM" });
        byCode.Select(x => x.Id).ShouldBe(new[] { tree.GeneralManagerId });
    }

    [Fact]
    public async Task Should_Carry_Report_Counts_On_Search_Results()
    {
        var tree = CreateReportingTree();

        var results = await _employeeOrgChartAppService.SearchNodes(
            new SearchEmployeeOrgChartNodesInput { Filter = "Mian" });

        results.Single(x => x.Id == tree.SupplyManagerId).TotalReportCount.ShouldBe(4);
    }

    [Fact]
    public async Task Should_Clamp_Search_Result_Count_To_The_Allowed_Maximum()
    {
        CreateReportingTree();

        var results = await _employeeOrgChartAppService.SearchNodes(new SearchEmployeeOrgChartNodesInput
        {
            MaxResultCount = int.MaxValue,
        });

        results.Count.ShouldBeLessThanOrEqualTo(SearchEmployeeOrgChartNodesInput.MaxAllowedResultCount);
    }

    /// <summary>
    /// Builds the shape the assertions rely on, modelled on the reference design:
    /// <code>
    /// Ahmed Eldeeb (General Manager)
    ///   Mian Zeeshan (Senior Manager)
    ///     Asad Zafar Butt
    ///     Bassel Tharwat (Manager - Warehouse)
    ///       Ofelia Alvarez
    ///     Huma Faisal
    /// </code>
    /// </summary>
    private ReportingTree CreateReportingTree()
    {
        var seniorManagerTitleId = AddJobTitle("JT-SM", "Senior Manager", "مدير أول");

        var generalManager = CreateEmployee("EOC-GM", "Ahmed Eldeeb", Gender.Male);
        var supplyManager = CreateEmployee("EOC-SM", "Mian Zeeshan", Gender.Male, nameAr: "ميان زيشان");
        var logisticsOfficer = CreateEmployee("EOC-LO", "Asad Zafar Butt", Gender.Male);
        var warehouseManager = CreateEmployee("EOC-WM", "Bassel Tharwat", Gender.Male, nameAr: "باسل ثروت");
        var logisticsOfficer2 = CreateEmployee("EOC-LO2", "Huma Faisal", Gender.Female);
        var warehouseOfficer = CreateEmployee("EOC-WO", "Ofelia Alvarez", Gender.Female);

        AddReportingLine(supplyManager.Id, generalManager.Id, LongAgo, Always);
        AddReportingLine(logisticsOfficer.Id, supplyManager.Id, LongAgo, Always);
        AddReportingLine(warehouseManager.Id, supplyManager.Id, LongAgo, Always);
        AddReportingLine(logisticsOfficer2.Id, supplyManager.Id, LongAgo, Always);
        AddReportingLine(warehouseOfficer.Id, warehouseManager.Id, LongAgo, Always);

        AddOrgAssignment(supplyManager.Id, seniorManagerTitleId, LongAgo, Always);

        return new ReportingTree
        {
            GeneralManagerId = generalManager.Id,
            SupplyManagerId = supplyManager.Id,
            LogisticsOfficerId = logisticsOfficer.Id,
            WarehouseManagerId = warehouseManager.Id,
            WarehouseOfficerId = warehouseOfficer.Id
        };
    }

    private int AddJobTitle(string code, string nameEn, string nameAr)
    {
        var jobTitleId = 0;
        UsingDbContext(context =>
        {
            var jobTitle = context.Set<JobTitle>().Add(new JobTitle
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = nameEn,
                NameAr = nameAr
            }).Entity;
            context.SaveChanges();
            jobTitleId = jobTitle.Id;
        });
        return jobTitleId;
    }

    private void AddReportingLine(int employeeId, int managerId, DateTime startDate, DateTime endDate)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeManager>().Add(new EmployeeManager
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeId = employeeId
                ManagerId = managerId,
                StartDate = startDate,
                EndDate = endDate
            });
        });
    }

    private void AddOrgAssignment(int employeeId, int jobTitleId, DateTime startDate, DateTime endDate)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeId = employeeId
                JobTitleId = jobTitleId,
                StartDate = startDate,
                EndDate = endDate
            });
        });
    }

    private Employee CreateEmployee(string code, string fullNameEn, Gender gender, string nameAr = null, byte[] photo = null)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Set<Nationality>().Add(new Nationality
            {
                TenantId = AbpSession.TenantId ?? 1,
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
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                FirstNameEn = "F",
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = fullNameEn,
                FullNameAr = nameAr ?? fullNameEn,
                BirthDate = new DateTime(1990, 1, 1),
                Gender = gender,
                NationalityId = nationality.Id,
                Photo = photo
            }).Entity;
        });

        return employee;
    }

    private class ReportingTree
    {
        public int GeneralManagerId { get; set; }

        public int SupplyManagerId { get; set; }

        public int LogisticsOfficerId { get; set; }

        public int WarehouseManagerId { get; set; }

        public int WarehouseOfficerId { get; set; }
    }
}
