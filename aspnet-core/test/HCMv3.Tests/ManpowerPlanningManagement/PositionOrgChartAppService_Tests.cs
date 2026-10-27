using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.ManpowerPlanningManagement;
using HCMv3.ManpowerPlanningManagement.Dto;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ManpowerPlanningManagement;

/// <summary>
/// Covers the Position Org Chart feed. The behaviour worth pinning down is the traversal over
/// <c>Position.ParentPositionId</c> -- the column added for this screen -- and how a card resolves the
/// person currently holding the post, including when nobody does.
/// </summary>
// ReSharper disable once InconsistentNaming
public class PositionOrgChartAppService_Tests : ManpowerPlanningManagementAppServiceTestBase
{
    private PositionOrgChartAppService CreateAppService()
    {
        return CreateService(() => new PositionOrgChartAppService(
            FakeRepository.For<Position>(Context),
            FakeRepository.For<PositionRelationship>(Context),
            FakeRepository.For<Employee>(Context)));
    }

    [Fact]
    public async Task Should_Focus_The_Root_Of_The_Largest_Tree_When_Nothing_Requested()
    {
        var tree = CreatePositionTree();
        var service = CreateAppService();

        var result = await service.GetNode(new GetPositionOrgChartNodeInput());

        result.Focus.ShouldNotBeNull();
        result.Focus.Id.ShouldBe(tree.GeneralManagerId);
        result.Parent.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Return_Only_The_Immediate_Parent()
    {
        var tree = CreatePositionTree();
        var service = CreateAppService();

        var result = await service.GetNode(new GetPositionOrgChartNodeInput { PositionId = tree.StorekeeperId });

        // Two positions sit above this one, but the chart draws one parent row and steps up by re-centring.
        result.Focus.Id.ShouldBe(tree.StorekeeperId);
        result.Parent.ShouldNotBeNull();
        result.Parent.Id.ShouldBe(tree.WarehouseManagerId);
    }

    [Fact]
    public async Task Should_Return_Children_Ordered_By_Name()
    {
        var tree = CreatePositionTree();
        var service = CreateAppService();

        var result = await service.GetNode(new GetPositionOrgChartNodeInput { PositionId = tree.SupplyManagerId });

        result.Children.Select(x => x.NameEn).ShouldBe(new[] { "Logistics Officer", "Warehouse Manager" });
    }

    [Fact]
    public async Task Should_Count_Direct_Children_Separately_From_Everything_Below()
    {
        var tree = CreatePositionTree();
        var service = CreateAppService();

        var result = await service.GetNode(new GetPositionOrgChartNodeInput { PositionId = tree.GeneralManagerId });

        // Supply manager directly; supply manager, its two children and the storekeeper in total.
        result.Focus.DirectChildCount.ShouldBe(1);
        result.Focus.TotalChildCount.ShouldBe(4);

        result.Children.Single().DirectChildCount.ShouldBe(2);
        result.Children.Single().TotalChildCount.ShouldBe(3);
    }

    [Fact]
    public async Task Should_Show_The_Current_Holder_On_The_Card()
    {
        var tree = CreatePositionTree();
        var employeeId = CreateEmployee("POC-1");
        AddHolder(tree.SupplyManagerId, employeeId, new DateTime(2024, 1, 1));

        var service = CreateAppService();

        var result = await service.GetNode(new GetPositionOrgChartNodeInput { PositionId = tree.SupplyManagerId });

        result.Focus.IsVacant.ShouldBeFalse();
        result.Focus.HolderEmployeeId.ShouldBe(employeeId);
        result.Focus.HolderFullNameEn.ShouldBe("F L POC-1");
    }

    [Fact]
    public async Task Should_Take_The_Latest_Holder_When_A_Position_Has_Had_Several()
    {
        var tree = CreatePositionTree();
        var previous = CreateEmployee("POC-OLD");
        var current = CreateEmployee("POC-NEW");

        AddHolder(tree.SupplyManagerId, previous, new DateTime(2020, 1, 1));
        AddHolder(tree.SupplyManagerId, current, new DateTime(2025, 1, 1));

        var service = CreateAppService();

        var result = await service.GetNode(new GetPositionOrgChartNodeInput { PositionId = tree.SupplyManagerId });

        result.Focus.HolderEmployeeId.ShouldBe(current);
    }

    [Fact]
    public async Task Should_Mark_A_Position_With_No_Holder_As_Vacant()
    {
        var tree = CreatePositionTree();
        var service = CreateAppService();

        var result = await service.GetNode(new GetPositionOrgChartNodeInput { PositionId = tree.SupplyManagerId });

        result.Focus.IsVacant.ShouldBeTrue();
        result.Focus.HolderEmployeeId.ShouldBeNull();
        result.Focus.HolderFullNameEn.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Survive_A_Cycle_In_The_Position_Hierarchy()
    {
        // Written directly, bypassing the app service's own guard, to stand in for a row that predates the
        // check or arrived through an import. An unguarded traversal would never return.
        var first = CreatePosition("Cycle One", null);
        var second = CreatePosition("Cycle Two", first);
        SetParent(first, second);

        var service = CreateAppService();

        var result = await service.GetNode(new GetPositionOrgChartNodeInput { PositionId = first });

        result.Focus.Id.ShouldBe(first);
        result.Focus.TotalChildCount.ShouldBe(1);
        result.Parent.Id.ShouldBe(second);
    }

    [Fact]
    public async Task Should_Ignore_A_Position_Parented_To_Itself()
    {
        var loner = CreatePosition("Self Parent", null);
        SetParent(loner, loner);

        var service = CreateAppService();

        var result = await service.GetNode(new GetPositionOrgChartNodeInput { PositionId = loner });

        result.Focus.Id.ShouldBe(loner);
        result.Parent.ShouldBeNull();
        result.Children.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Return_Empty_Result_When_No_Position_Has_A_Parent()
    {
        CreatePosition("Standalone", null);
        var service = CreateAppService();

        var result = await service.GetNode(new GetPositionOrgChartNodeInput());

        result.Focus.ShouldBeNull();
        result.Parent.ShouldBeNull();
        result.Children.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Search_By_Name_And_Code()
    {
        var tree = CreatePositionTree();
        var service = CreateAppService();

        var byName = await service.SearchNodes(new SearchPositionOrgChartNodesInput { Filter = "Warehouse" });
        byName.Select(x => x.Id).ShouldBe(new[] { tree.WarehouseManagerId });

        var byCode = await service.SearchNodes(new SearchPositionOrgChartNodesInput { Filter = "PC-GM" });
        byCode.Select(x => x.Id).ShouldBe(new[] { tree.GeneralManagerId });
    }

    [Fact]
    public async Task Should_Clamp_Search_Result_Count_To_The_Allowed_Maximum()
    {
        CreatePositionTree();
        var service = CreateAppService();

        var results = await service.SearchNodes(new SearchPositionOrgChartNodesInput
        {
            MaxResultCount = int.MaxValue
        });

        results.Count.ShouldBeLessThanOrEqualTo(SearchPositionOrgChartNodesInput.MaxAllowedResultCount);
    }

    /// <summary>
    /// General Manager
    ///   Supply Manager
    ///     Logistics Officer
    ///     Warehouse Manager
    ///       Storekeeper
    /// </summary>
    private PositionTree CreatePositionTree()
    {
        var generalManager = CreatePosition("General Manager", null, "PC-GM");
        var supplyManager = CreatePosition("Supply Manager", generalManager, "PC-SM");
        var logisticsOfficer = CreatePosition("Logistics Officer", supplyManager, "PC-LO");
        var warehouseManager = CreatePosition("Warehouse Manager", supplyManager, "PC-WM");
        var storekeeper = CreatePosition("Storekeeper", warehouseManager, "PC-SK");

        return new PositionTree
        {
            GeneralManagerId = generalManager,
            SupplyManagerId = supplyManager,
            LogisticsOfficerId = logisticsOfficer,
            WarehouseManagerId = warehouseManager,
            StorekeeperId = storekeeper
        };
    }

    private int CreatePosition(string nameEn, int? parentPositionId, string code = null)
    {
        var entity = new Position
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code ?? Guid.NewGuid().ToString("N").Substring(0, 8),
            NameEn = nameEn,
            NameAr = nameEn,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(9999, 12, 31),
            Active = true,
            FTELimit = 1,
            ParentPositionId = parentPositionId
        };

        Context.Positions.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private void SetParent(int positionId, int parentPositionId)
    {
        var entity = Context.Positions.Single(x => x.Id == positionId);
        entity.ParentPositionId = parentPositionId;
        Context.SaveChanges();
    }

    private void AddHolder(int positionId, int employeeId, DateTime startDate)
    {
        Context.PositionRelationships.Add(new PositionRelationship
        {
            TenantId = AbpSession.TenantId ?? 1,
            PositionId = positionId,
            EmployeeId = employeeId,
            RelationType = PositionRelationType.Holder,
            StartDate = startDate,
            EndDate = new DateTime(9999, 12, 31)
        });
        Context.SaveChanges();
    }

    private class PositionTree
    {
        public int GeneralManagerId { get; set; }

        public int SupplyManagerId { get; set; }

        public int LogisticsOfficerId { get; set; }

        public int WarehouseManagerId { get; set; }

        public int StorekeeperId { get; set; }
    }
}
