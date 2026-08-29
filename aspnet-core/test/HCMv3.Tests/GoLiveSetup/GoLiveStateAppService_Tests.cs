using System.Linq;
using System.Threading.Tasks;
using HCMv3.GoLiveSetup;
using HCMv3.GoLiveSetup.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GoLiveSetup;

// ReSharper disable once InconsistentNaming
public class GoLiveStateAppService_Tests : AppTestBase
{
    private readonly IGoLiveStateAppService _service;

    public GoLiveStateAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _service = Resolve<IGoLiveStateAppService>();
    }

    private Task Save(string stepId, string payload) =>
        _service.SaveStep(new SaveGoLiveStepInput { StepId = stepId, Payload = payload });

    [Fact]
    public async Task Should_Report_Nothing_Saved_For_A_Fresh_Tenant()
    {
        var state = await _service.GetState();

        state.Steps.ShouldBeEmpty();
        state.LastSavedUtc.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Save_And_Read_Back_A_Step()
    {
        await Save("S1.2", "{\"answers\":{\"Q2.04\":\"Fixed30\"}}");

        var state = await _service.GetState();

        state.Steps.Count.ShouldBe(1);
        state.Steps[0].StepId.ShouldBe("S1.2");
        state.Steps[0].Payload.ShouldContain("Fixed30");
        state.LastSavedUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task Should_Update_A_Step_Rather_Than_Adding_A_Second_Row()
    {
        await Save("S1.2", "{\"v\":1}");
        await Save("S1.2", "{\"v\":2}");

        var state = await _service.GetState();

        state.Steps.Count.ShouldBe(1);
        state.Steps[0].Payload.ShouldContain("\"v\":2");
    }

    [Fact]
    public async Task Should_Keep_Steps_Independent()
    {
        await Save("S1.2", "{\"a\":1}");
        await Save("S3.1", "{\"rows\":[]}");

        var state = await _service.GetState();

        state.Steps.Count.ShouldBe(2);
        state.Steps.Select(s => s.StepId).OrderBy(s => s).ShouldBe(new[] { "S1.2", "S3.1" });
    }

    [Fact]
    public async Task Should_Delete_The_Row_When_A_Step_Is_Cleared()
    {
        await Save("S1.2", "{\"a\":1}");
        await Save("S1.2", null);

        var state = await _service.GetState();

        state.Steps.ShouldBeEmpty();
    }

    /// <summary>
    /// The entity is soft-deleted, so the cleared row is still physically present. Without the filtered
    /// unique index this second save would hit a duplicate-key violation.
    /// </summary>
    [Fact]
    public async Task Should_Allow_Saving_A_Step_Again_After_It_Was_Cleared()
    {
        await Save("S1.2", "{\"a\":1}");
        await Save("S1.2", null);
        await Save("S1.2", "{\"a\":2}");

        var state = await _service.GetState();

        state.Steps.Count.ShouldBe(1);
        state.Steps[0].Payload.ShouldContain("\"a\":2");
    }

    [Fact]
    public async Task Should_Treat_Whitespace_As_Cleared()
    {
        await Save("S1.2", "{\"a\":1}");
        await Save("S1.2", "   ");

        (await _service.GetState()).Steps.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Ignore_Clearing_A_Step_That_Was_Never_Saved()
    {
        await Save("S9.9", null);

        (await _service.GetState()).Steps.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Store_A_Payload_Far_Larger_Than_A_Normal_Column()
    {
        // A 2,000-row employee sheet is legitimately large; the column is nvarchar(max).
        var big = "{\"rows\":[" + string.Join(",", Enumerable.Range(0, 2000).Select(i => $"{{\"code\":\"{i}\"}}")) + "]}";

        await Save("S3.1", big);

        var state = await _service.GetState();
        state.Steps[0].Payload.Length.ShouldBe(big.Length);
    }

    [Fact]
    public async Task Should_Discard_The_Whole_Draft_On_Clear()
    {
        await Save("S1.2", "{\"a\":1}");
        await Save("S3.1", "{\"b\":2}");

        await _service.ClearState();

        (await _service.GetState()).Steps.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Report_The_Most_Recent_Save_Time()
    {
        await Save("S1.2", "{\"a\":1}");
        var first = (await _service.GetState()).LastSavedUtc;

        await Save("S3.1", "{\"b\":2}");
        var second = (await _service.GetState()).LastSavedUtc;

        first.ShouldNotBeNull();
        second.ShouldNotBeNull();
        second.Value.ShouldBeGreaterThanOrEqualTo(first.Value);
    }
}
