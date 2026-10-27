using System.Linq;
using System.Threading.Tasks;
using HCMv3.OrganizationManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.OrganizationManagement;

/// <summary>
/// Legacy <c>CostController</c>: cost element slot N is named after the cost element type whose Code is
/// "CostElementN", slots with no such type are hidden, and each slot offers only cost elements of its type.
/// </summary>
// ReSharper disable once InconsistentNaming
public class CostElementSlotsAppService_Tests : AppTestBase
{
    private readonly ICostElementSlotsAppService _appService;

    public CostElementSlotsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<ICostElementSlotsAppService>();
    }

    private int AddType(string code, string nameEn, string nameAr)
    {
        var id = 0;
        UsingDbContext(context =>
        {
            var type = context.CostElementTypes.Add(new CostElementType
            {
                TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = nameEn, NameAr = nameAr
            }).Entity;
            context.SaveChanges();
            id = type.Id;
        });
        return id;
    }

    private int AddElement(int? typeId, string code, string nameEn)
    {
        var id = 0;
        UsingDbContext(context =>
        {
            var element = context.CostElements.Add(new CostElement
            {
                TenantId = AbpSession.TenantId ?? 1, CostElementTypeId = typeId, Code = code, NameEn = nameEn, NameAr = nameEn
            }).Entity;
            context.SaveChanges();
            id = element.Id;
        });
        return id;
    }

    [Fact]
    public async Task Should_Return_No_Slots_When_No_Slot_Type_Is_Configured()
    {
        AddType("PROJ", "Project", "مشروع");

        var result = await _appService.GetSlots();

        result.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Return_Only_Configured_Slots_With_The_Type_Names()
    {
        var departmentTypeId = AddType("CostElement1", "Cost Center", "مركز التكلفة");
        AddType("CostElement3", "Project", "مشروع");

        var result = await _appService.GetSlots();

        result.Items.Select(s => s.Slot).ShouldBe(new[] { 1, 3 });
        var first = result.Items.First();
        first.CostElementTypeId.ShouldBe(departmentTypeId);
        first.NameEn.ShouldBe("Cost Center");
        first.NameAr.ShouldBe("مركز التكلفة");
    }

    [Fact]
    public async Task Should_Offer_Only_Cost_Elements_Of_The_Slot_Type()
    {
        var centerTypeId = AddType("CostElement1", "Cost Center", "مركز التكلفة");
        var projectTypeId = AddType("CostElement2", "Project", "مشروع");
        var otherTypeId = AddType("REGION", "Region", "منطقة");
        var centerB = AddElement(centerTypeId, "CC-B", "Center B");
        var centerA = AddElement(centerTypeId, "CC-A", "Center A");
        var project = AddElement(projectTypeId, "PR-1", "Project 1");
        AddElement(otherTypeId, "RG-1", "Region 1");
        AddElement(null, "NT-1", "No type");

        var result = await _appService.GetSlots();

        result.Items.Single(s => s.Slot == 1).Options.Select(o => o.Id).ShouldBe(new[] { centerA, centerB });
        result.Items.Single(s => s.Slot == 2).Options.Select(o => o.Id).ShouldBe(new[] { project });
    }

    [Fact]
    public async Task Should_Ignore_Codes_Beyond_The_Seventh_Slot()
    {
        AddType("CostElement8", "Eighth", "الثامن");
        AddType("CostElement7", "Seventh", "السابع");

        var result = await _appService.GetSlots();

        result.Items.Select(s => s.Slot).ShouldBe(new[] { 7 });
    }
}
