using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Organizations;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;
using HCMv3.OrganizationManagement.Dto;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.OrganizationManagement;

/// <summary>
/// Covers the Company Structure Overview chart feed. The interesting behaviour is all in the headcount
/// roll-up: the direct figure counts only assignments naming a unit exactly, while the total figure rolls
/// up every descendant -- both resolved as of a caller-supplied date rather than "now".
/// </summary>
public class CompanyStructureChartAppService_Tests : AppTestBase
{
    private readonly ICompanyStructureChartAppService _companyStructureChartAppService;

    public CompanyStructureChartAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _companyStructureChartAppService = Resolve<ICompanyStructureChartAppService>();
    }

    [Fact]
    public async Task Should_Focus_First_Root_When_No_Node_Requested()
    {
        CreateTwoLevelTree();

        // Resolved from the database rather than hard-coded: the test base already seeds its own OU tree,
        // so "the first root in tree order" is not necessarily this fixture's root.
        long expectedRootId = 0;
        string expectedRootName = null;
        UsingDbContext(context =>
        {
            var firstRoot = context.Set<OrganizationUnit>()
                .Where(x => x.ParentId == null)
                .OrderBy(x => x.Code)
                .First();
            expectedRootId = firstRoot.Id;
            expectedRootName = firstRoot.DisplayName;
        });

        var result = await _companyStructureChartAppService.GetNode(new GetCompanyStructureNodeInput
        {
            AsOfDate = new DateTime(2026, 6, 1)
        });

        result.Focus.ShouldNotBeNull();
        result.Focus.Id.ShouldBe(expectedRootId);
        result.Focus.NameEn.ShouldBe(expectedRootName);
        result.Focus.ParentId.ShouldBeNull();
        result.Ancestors.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Return_Children_In_Tree_Order_With_Profile_And_Type_Details()
    {
        var tree = CreateTwoLevelTree();

        var result = await _companyStructureChartAppService.GetNode(new GetCompanyStructureNodeInput
        {
            OrganizationUnitId = tree.LegalEntityId,
            AsOfDate = new DateTime(2026, 6, 1)
        });

        result.Children.Select(x => x.NameEn).ShouldBe(new[] { "Consumer Goods", "Human Resources" });

        var consumerGoods = result.Children.Single(x => x.NameEn == "Consumer Goods");
        consumerGoods.Code.ShouldBe("2001");
        consumerGoods.NameAr.ShouldBe("السلع الاستهلاكية");
        consumerGoods.OrganizationUnitTypeCode.ShouldBe(OrganizationUnitType.Level2Code);
        consumerGoods.OrganizationUnitTypeNameEn.ShouldBe("Department");
        consumerGoods.ParentId.ShouldBe(tree.LegalEntityId);
    }

    [Fact]
    public async Task Should_Count_Direct_Employees_Against_The_Exact_Unit_Only()
    {
        var tree = CreateTwoLevelTree();
        AssignEmployeeToUnit("EMP-A", tree.ConsumerGoodsId, new DateTime(2020, 1, 1), new DateTime(2099, 12, 31));
        AssignEmployeeToUnit("EMP-B", tree.LegalEntityId, new DateTime(2020, 1, 1), new DateTime(2099, 12, 31));

        var result = await _companyStructureChartAppService.GetNode(new GetCompanyStructureNodeInput
        {
            OrganizationUnitId = tree.LegalEntityId,
            AsOfDate = new DateTime(2026, 6, 1)
        });

        // Only the employee assigned to the legal entity itself -- the one in Consumer Goods is a
        // descendant, so it must not inflate the parent's direct figure.
        result.Focus.DirectEmployeeCount.ShouldBe(1);
        result.Children.Single(x => x.NameEn == "Consumer Goods").DirectEmployeeCount.ShouldBe(1);
        result.Children.Single(x => x.NameEn == "Human Resources").DirectEmployeeCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Roll_Descendant_Employees_Into_The_Total_Count()
    {
        var tree = CreateTwoLevelTree();
        AssignEmployeeToUnit("EMP-A", tree.ConsumerGoodsId, new DateTime(2020, 1, 1), new DateTime(2099, 12, 31));
        AssignEmployeeToUnit("EMP-B", tree.LegalEntityId, new DateTime(2020, 1, 1), new DateTime(2099, 12, 31));
        AssignEmployeeToUnit("EMP-C", tree.ECommerceId, new DateTime(2020, 1, 1), new DateTime(2099, 12, 31));

        var result = await _companyStructureChartAppService.GetNode(new GetCompanyStructureNodeInput
        {
            OrganizationUnitId = tree.LegalEntityId,
            AsOfDate = new DateTime(2026, 6, 1)
        });

        // Legal entity (1) + Consumer Goods (1) + E-Commerce, which is a grandchild under Consumer Goods (1).
        result.Focus.TotalEmployeeCount.ShouldBe(3);
        result.Focus.DirectEmployeeCount.ShouldBe(1);

        var consumerGoods = result.Children.Single(x => x.NameEn == "Consumer Goods");
        consumerGoods.DirectEmployeeCount.ShouldBe(1);
        consumerGoods.TotalEmployeeCount.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Exclude_Assignments_That_Do_Not_Span_The_As_Of_Date()
    {
        var tree = CreateTwoLevelTree();
        AssignEmployeeToUnit("EMP-LEAVER", tree.ConsumerGoodsId, new DateTime(2020, 1, 1), new DateTime(2024, 12, 31));
        AssignEmployeeToUnit("EMP-JOINER", tree.ConsumerGoodsId, new DateTime(2027, 1, 1), new DateTime(2099, 12, 31));
        AssignEmployeeToUnit("EMP-CURRENT", tree.ConsumerGoodsId, new DateTime(2020, 1, 1), new DateTime(2099, 12, 31));

        var result = await _companyStructureChartAppService.GetNode(new GetCompanyStructureNodeInput
        {
            OrganizationUnitId = tree.LegalEntityId,
            AsOfDate = new DateTime(2026, 6, 1)
        });

        result.Children.Single(x => x.NameEn == "Consumer Goods").DirectEmployeeCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Count_An_Employee_Once_Per_Unit_Despite_Overlapping_Assignments()
    {
        var tree = CreateTwoLevelTree();
        var employee = CreateEmployee("EMP-DUP");

        // Two rows naming the same unit and both spanning the as-of date -- a data condition the
        // EndDate-chaining is meant to prevent, but the count must not double up if it slips through.
        AddOrgAssignment(employee.Id, tree.ConsumerGoodsId, new DateTime(2020, 1, 1), new DateTime(2099, 12, 31));
        AddOrgAssignment(employee.Id, tree.ConsumerGoodsId, new DateTime(2021, 1, 1), new DateTime(2099, 12, 31));

        var result = await _companyStructureChartAppService.GetNode(new GetCompanyStructureNodeInput
        {
            OrganizationUnitId = tree.LegalEntityId,
            AsOfDate = new DateTime(2026, 6, 1)
        });

        result.Children.Single(x => x.NameEn == "Consumer Goods").DirectEmployeeCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Report_Immediate_Child_Unit_Count_On_Each_Card()
    {
        var tree = CreateTwoLevelTree();

        var result = await _companyStructureChartAppService.GetNode(new GetCompanyStructureNodeInput
        {
            OrganizationUnitId = tree.LegalEntityId,
            AsOfDate = new DateTime(2026, 6, 1)
        });

        result.Focus.ChildUnitCount.ShouldBe(2);
        result.Children.Single(x => x.NameEn == "Consumer Goods").ChildUnitCount.ShouldBe(1);
        result.Children.Single(x => x.NameEn == "Human Resources").ChildUnitCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Return_Root_First_Breadcrumb_For_A_Nested_Focus_Node()
    {
        var tree = CreateTwoLevelTree();

        var result = await _companyStructureChartAppService.GetNode(new GetCompanyStructureNodeInput
        {
            OrganizationUnitId = tree.ECommerceId,
            AsOfDate = new DateTime(2026, 6, 1)
        });

        result.Focus.Id.ShouldBe(tree.ECommerceId);
        result.Ancestors.Select(x => x.NameEn).ShouldBe(new[] { "Legal Entity", "Consumer Goods" });
        result.Children.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Default_As_Of_Date_To_Today_When_Not_Supplied()
    {
        CreateTwoLevelTree();

        var result = await _companyStructureChartAppService.GetNode(new GetCompanyStructureNodeInput());

        result.AsOfDate.ShouldBe(Abp.Timing.Clock.Now.Date);
    }

    [Fact]
    public async Task Should_Return_Empty_Result_When_Tenant_Has_No_Organization_Units()
    {
        // Clear the tree the test base seeds, deepest-first so no row is removed before its children.
        UsingDbContext(context =>
        {
            var organizationUnits = context.Set<OrganizationUnit>()
                .OrderByDescending(x => x.Code.Length)
                .ToList();

            foreach (var organizationUnit in organizationUnits)
            {
                context.Set<OrganizationUnit>().Remove(organizationUnit);
                context.SaveChanges();
            }
        });

        var result = await _companyStructureChartAppService.GetNode(new GetCompanyStructureNodeInput
        {
            AsOfDate = new DateTime(2026, 6, 1)
        });

        result.Focus.ShouldBeNull();
        result.Children.ShouldBeEmpty();
        result.Ancestors.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Return_Roots_And_Types_For_The_Toolbar()
    {
        var tree = CreateTwoLevelTree();

        var result = await _companyStructureChartAppService.GetFilterOptions();

        result.Roots.Select(x => x.Id).ShouldContain(tree.LegalEntityId);
        result.Roots.ShouldAllBe(x => x.ParentId == null);
        result.OrganizationUnitTypes.Select(x => x.Code)
            .ShouldBe(new[] { OrganizationUnitType.Level1Code, OrganizationUnitType.Level2Code });
    }

    [Fact]
    public async Task Should_Search_Nodes_By_English_Name_Arabic_Name_And_Code()
    {
        var tree = CreateTwoLevelTree();

        var byEnglishName = await _companyStructureChartAppService.SearchNodes(
            new SearchCompanyStructureNodesInput { Filter = "Consumer" });
        byEnglishName.Select(x => x.Id).ShouldBe(new[] { tree.ConsumerGoodsId });

        var byArabicName = await _companyStructureChartAppService.SearchNodes(
            new SearchCompanyStructureNodesInput { Filter = "الاستهلاكية" });
        byArabicName.Select(x => x.Id).ShouldBe(new[] { tree.ConsumerGoodsId });

        var byCode = await _companyStructureChartAppService.SearchNodes(
            new SearchCompanyStructureNodesInput { Filter = "2008" });
        byCode.Select(x => x.Id).ShouldBe(new[] { tree.HumanResourcesId });
    }

    [Fact]
    public async Task Should_Narrow_Search_By_Organization_Unit_Type()
    {
        var tree = CreateTwoLevelTree();

        var result = await _companyStructureChartAppService.SearchNodes(new SearchCompanyStructureNodesInput
        {
            OrganizationUnitTypeId = tree.LegalEntityTypeId
        });

        result.Select(x => x.Id).ShouldBe(new[] { tree.LegalEntityId });
    }

    [Fact]
    public async Task Should_Confine_Search_To_The_Selected_Root_Subtree()
    {
        var tree = CreateTwoLevelTree();
        var otherRootId = AddOrganizationUnit("00008", "Other Legal Entity", null);

        var withinTree = await _companyStructureChartAppService.SearchNodes(new SearchCompanyStructureNodesInput
        {
            RootOrganizationUnitId = tree.LegalEntityId
        });

        withinTree.Select(x => x.Id).ShouldNotContain(otherRootId);
        withinTree.Select(x => x.Id).ShouldContain(tree.ConsumerGoodsId);
    }

    [Fact]
    public async Task Should_Clamp_Search_Result_Count_To_The_Allowed_Maximum()
    {
        CreateTwoLevelTree();

        var result = await _companyStructureChartAppService.SearchNodes(new SearchCompanyStructureNodesInput
        {
            MaxResultCount = int.MaxValue
        });

        result.Count.ShouldBeLessThanOrEqualTo(SearchCompanyStructureNodesInput.MaxAllowedResultCount);
    }

    /// <summary>
    /// Builds the shape the assertions above rely on:
    /// <code>
    /// Legal Entity (00009, Level1)
    ///   Consumer Goods    (00009.00001, Level2, code 2001)
    ///     E-Commerce      (00009.00001.00001, Level2, code 2002)
    ///   Human Resources   (00009.00002, Level2, code 2008)
    /// </code>
    ///
    /// The "00009" branch is deliberate: <c>TestOrganizationUnitsBuilder</c> already seeds every default
    /// tenant with an OU1/OU2 tree occupying codes 00001 and 00002. Since ancestor and descendant
    /// resolution both work by tree-code prefix, reusing those codes would splice this fixture into the
    /// seeded tree and make the breadcrumb assertions meaningless.
    /// </summary>
    private SeededTree CreateTwoLevelTree()
    {
        var legalEntityTypeId = AddOrganizationUnitType(OrganizationUnitType.Level1Code, "Legal Entity");
        var departmentTypeId = AddOrganizationUnitType(OrganizationUnitType.Level2Code, "Department");

        var legalEntityId = AddOrganizationUnit("00009", "Legal Entity", null);
        var consumerGoodsId = AddOrganizationUnit("00009.00001", "Consumer Goods", legalEntityId);
        var eCommerceId = AddOrganizationUnit("00009.00001.00001", "E-Commerce", consumerGoodsId);
        var humanResourcesId = AddOrganizationUnit("00009.00002", "Human Resources", legalEntityId);

        AddOrgUnitProfile(legalEntityId, "1001", "الكيان القانوني", legalEntityTypeId);
        AddOrgUnitProfile(consumerGoodsId, "2001", "السلع الاستهلاكية", departmentTypeId);
        AddOrgUnitProfile(eCommerceId, "2002", "التجارة الإلكترونية", departmentTypeId);
        AddOrgUnitProfile(humanResourcesId, "2008", "الموارد البشرية", departmentTypeId);

        return new SeededTree
        {
            LegalEntityTypeId = legalEntityTypeId,
            LegalEntityId = legalEntityId,
            ConsumerGoodsId = consumerGoodsId,
            ECommerceId = eCommerceId,
            HumanResourcesId = humanResourcesId
        };
    }

    private long AddOrganizationUnit(string treeCode, string displayName, long? parentId)
    {
        long organizationUnitId = 0;
        UsingDbContext(context =>
        {
            var organizationUnit = context.Set<OrganizationUnit>().Add(new OrganizationUnit
            {
                TenantId = AbpSession.TenantId,
                Code = treeCode,
                DisplayName = displayName,
                ParentId = parentId
            }).Entity;
            context.SaveChanges();
            organizationUnitId = organizationUnit.Id;
        });
        return organizationUnitId;
    }

    private int AddOrganizationUnitType(string code, string nameEn)
    {
        var organizationUnitTypeId = 0;
        UsingDbContext(context =>
        {
            var organizationUnitType = context.Set<OrganizationUnitType>().Add(new OrganizationUnitType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = nameEn,
                NameAr = nameEn
            }).Entity;
            context.SaveChanges();
            organizationUnitTypeId = organizationUnitType.Id;
        });
        return organizationUnitTypeId;
    }

    private void AddOrgUnitProfile(long organizationUnitId, string code, string nameAr, int organizationUnitTypeId)
    {
        UsingDbContext(context =>
        {
            context.Set<OrgUnitProfile>().Add(new OrgUnitProfile
            {
                TenantId = AbpSession.TenantId ?? 1,
                OrganizationUnitId = organizationUnitId,
                Code = code,
                NameAr = nameAr,
                OrganizationUnitTypeId = organizationUnitTypeId
            });
        });
    }

    private void AssignEmployeeToUnit(string employeeCode, long organizationUnitId, DateTime startDate, DateTime endDate)
    {
        var employee = CreateEmployee(employeeCode);
        AddOrgAssignment(employee.Id, organizationUnitId, startDate, endDate);
    }

    private void AddOrgAssignment(int employeeId, long organizationUnitId, DateTime startDate, DateTime endDate)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeId = employeeId,
                StartDate = startDate,
                EndDate = endDate,
                OrganizationUnitId = organizationUnitId
            });
        });
    }

    private Employee CreateEmployee(string code)
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
                FullNameEn = "F L",
                FullNameAr = "ف ل",
                BirthDate = new DateTime(1990, 1, 1),
                NationalityId = nationality.Id
            }).Entity;
        });

        return employee;
    }

    private class SeededTree
    {
        public int LegalEntityTypeId { get; set; }

        public long LegalEntityId { get; set; }

        public long ConsumerGoodsId { get; set; }

        public long ECommerceId { get; set; }

        public long HumanResourcesId { get; set; }
    }
}
