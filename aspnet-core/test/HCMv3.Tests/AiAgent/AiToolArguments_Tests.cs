using System;
using HCMv3.AiAgent.Tools;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.AiAgent;

/// <summary>Tolerant, but never guessing: what small models actually send.</summary>
// ReSharper disable once InconsistentNaming
public class AiToolArguments_Tests
{
    [Theory]
    [InlineData(@"{""n"":3}")]
    [InlineData(@"{""n"":""3""}")]
    public void Should_Accept_A_Number_Sent_As_Either_Type(string json)
    {
        AiToolArguments.Parse(json).GetInt("n").ShouldBe(3);
    }

    [Theory]
    [InlineData(@"{""d"":""2026-10-01""}")]
    [InlineData(@"{""d"":""1/10/2026""}")]
    [InlineData(@"{""d"":""01-10-2026""}")]
    [InlineData(@"{""d"":""1 October 2026""}")]
    [InlineData(@"{""d"":""2026-10-01T00:00:00""}")]
    public void Should_Accept_Common_Unambiguous_Date_Forms(string json)
    {
        AiToolArguments.Parse(json).GetDate("d").ShouldBe(new DateTime(2026, 10, 1));
    }

    [Fact]
    public void Should_Read_Day_First_Because_That_Is_How_Kuwait_Writes_Dates()
    {
        // 02/03 is 2 March here, not 3 February.
        AiToolArguments.Parse(@"{""d"":""02/03/2026""}").GetDate("d").ShouldBe(new DateTime(2026, 3, 2));
    }

    [Theory]
    [InlineData(@"{""b"":true}", true)]
    [InlineData(@"{""b"":""false""}", false)]
    public void Should_Accept_A_Boolean_Sent_As_Either_Type(string json, bool expected)
    {
        AiToolArguments.Parse(json).GetBool("b").ShouldBe(expected);
    }

    [Fact]
    public void Should_Treat_Missing_And_Null_As_Not_Given()
    {
        var arguments = AiToolArguments.Parse(@"{""a"":null,""s"":""  ""}");

        arguments.GetInt("a").ShouldBeNull();
        arguments.GetDate("missing").ShouldBeNull();
        arguments.GetString("s").ShouldBeNull();
    }

    [Theory]
    [InlineData(@"{""n"":""three""}")]
    [InlineData(@"{""n"":2.5}")]
    public void Should_Refuse_A_Number_It_Would_Have_To_Guess(string json)
    {
        Should.Throw<AiToolArgumentException>(() => AiToolArguments.Parse(json).GetInt("n"));
    }

    [Fact]
    public void Should_Refuse_A_Date_It_Would_Have_To_Guess()
    {
        Should.Throw<AiToolArgumentException>(() => AiToolArguments.Parse(@"{""d"":""next Tuesday""}").GetDate("d"))
            .Message.ShouldContain("yyyy-MM-dd");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Should_Treat_No_Arguments_As_An_Empty_Object(string json)
    {
        AiToolArguments.Parse(json).GetString("anything").ShouldBeNull();
    }

    [Theory]
    [InlineData("{not json")]
    [InlineData("[1,2]")]
    public void Should_Refuse_Arguments_That_Are_Not_A_Json_Object(string json)
    {
        Should.Throw<AiToolArgumentException>(() => AiToolArguments.Parse(json));
    }
}
