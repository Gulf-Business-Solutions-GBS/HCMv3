using HCMv3.PayrollManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

public class CostElementNamesTests
{
    [Fact]
    public void Should_Expose_Seven_Fixed_Cost_Element_Slot_Names()
    {
        CostElementNames.CostElement1.ShouldBe("CostElement1");
        CostElementNames.CostElement2.ShouldBe("CostElement2");
        CostElementNames.CostElement3.ShouldBe("CostElement3");
        CostElementNames.CostElement4.ShouldBe("CostElement4");
        CostElementNames.CostElement5.ShouldBe("CostElement5");
        CostElementNames.CostElement6.ShouldBe("CostElement6");
        CostElementNames.CostElement7.ShouldBe("CostElement7");
    }
}
