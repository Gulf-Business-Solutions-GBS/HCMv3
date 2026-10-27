using System.Collections.Generic;
using HCMv3.BusinessRules;
using HCMv3.BusinessRules.Definition;
using HCMv3.BusinessRules.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.BusinessRules;

/// <summary>
/// Which actions each event may carry out. A plain matrix, but the one place every event's meaning is
/// pinned down: a change here changes what an administrator is allowed to arm.
/// </summary>
// ReSharper disable once InconsistentNaming
public class RuleEventActionPolicy_Tests
{
    [Theory]
    [InlineData(RuleEventType.OnValidate)]
    [InlineData(RuleEventType.BeforeDelete)]
    public void Should_Allow_Only_Errors_On_Check_Events(RuleEventType eventType)
    {
        RuleEventActionPolicy.Allows(eventType, RuleErrorAction.KindName).ShouldBeTrue();
        RuleEventActionPolicy.Allows(eventType, RuleSetAction.KindName).ShouldBeFalse();
    }

    [Theory]
    [InlineData(RuleEventType.OnInit)]
    [InlineData(RuleEventType.BeforeSave)]
    [InlineData(RuleEventType.OnRoute)]
    public void Should_Allow_Setting_Fields_Where_The_Record_Can_Still_Change(RuleEventType eventType)
    {
        RuleEventActionPolicy.Allows(eventType, RuleSetAction.KindName).ShouldBeTrue();
        RuleEventActionPolicy.Allows(eventType, RuleErrorAction.KindName).ShouldBeTrue();
    }

    [Fact]
    public void Should_Not_Allow_Setting_The_Saved_Record_After_It_Is_Saved()
    {
        RuleEventActionPolicy.Allows(RuleEventType.AfterSave, RuleSetAction.KindName).ShouldBeFalse();
    }

    [Fact]
    public void Should_Allow_Nothing_On_None()
    {
        RuleEventActionPolicy.AllowedKindsFor(RuleEventType.None).ShouldBeEmpty();
    }

    [Fact]
    public void Should_Find_Nothing_Disallowed_When_Every_Action_Fits()
    {
        var definition = Definition(Then(Error()));

        RuleEventActionPolicy.FindDisallowedKinds(definition, RuleEventType.OnValidate).ShouldBeEmpty();
    }

    [Fact]
    public void Should_Report_A_Disallowed_Kind_Once_However_Often_It_Appears()
    {
        var definition = Definition(Then(Set(), Set()), Then(Set()));

        RuleEventActionPolicy.FindDisallowedKinds(definition, RuleEventType.OnValidate)
            .ShouldBe(new[] { RuleSetAction.KindName });
    }

    [Fact]
    public void Should_Check_Else_Branches_Too()
    {
        // An Else that assigns is exactly as unable to run on OnValidate as a Then that does.
        var definition = Definition(new RuleBlock
        {
            If = new RuleConstantExpression { DataType = RuleDataType.Boolean, Value = "true" },
            Then = new List<RuleAction> { Error() },
            Else = new List<RuleAction> { Set() }
        });

        RuleEventActionPolicy.FindDisallowedKinds(definition, RuleEventType.BeforeDelete)
            .ShouldBe(new[] { RuleSetAction.KindName });
    }

    private static RuleDefinition Definition(params RuleBlock[] blocks)
    {
        return new RuleDefinition { Blocks = new List<RuleBlock>(blocks) };
    }

    private static RuleBlock Then(params RuleAction[] actions)
    {
        return new RuleBlock { Then = new List<RuleAction>(actions) };
    }

    private static RuleSetAction Set()
    {
        return new RuleSetAction
        {
            Parameter = "Leave",
            Field = "TotalLeaveDays",
            Value = new RuleConstantExpression { DataType = RuleDataType.Decimal, Value = "1" }
        };
    }

    private static RuleErrorAction Error()
    {
        return new RuleErrorAction { MessageKey = "SomeKey" };
    }
}
