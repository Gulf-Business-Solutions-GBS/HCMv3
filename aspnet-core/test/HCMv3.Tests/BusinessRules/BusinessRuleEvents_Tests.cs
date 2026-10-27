using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.BusinessRules;
using HCMv3.BusinessRules.Dto;
using HCMv3.BusinessRules.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.BusinessRules;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.BusinessRules;

/// <summary>
/// The OnValidate and BeforeDelete events, end to end: a rule armed through the real app service, fired by a
/// real save or delete inside a unit of work.
///
/// <para>
/// End to end on purpose. Both events existed in the enum and were offered on the arming screen long before
/// anything fired them, and no test noticed, because every test built a context by hand and evaluated it.
/// The only test that proves an event works is one where the event itself is the thing that runs the rule.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class BusinessRuleEvents_Tests : BusinessRuleIntegrationTestBase
{
    // ---------------------------------------------------------------- BeforeDelete

    [Fact]
    public async Task Should_Refuse_Deleting_A_Leave_When_A_BeforeDelete_Rule_Matches()
    {
        var absenceId = CreateAbsence(totalLeaveDays: 3);
        await ArmAsync("NO_DELETE", RulePurpose.Validate, AlwaysRaise(), RuleEventType.BeforeDelete);

        await Should.ThrowAsync<UserFriendlyException>(() => DeleteAbsenceAsync(absenceId));

        IsDeleted(absenceId).ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Delete_A_Leave_When_The_BeforeDelete_Rule_Does_Not_Match()
    {
        var absenceId = CreateAbsence(totalLeaveDays: 3);
        await ArmAsync("NO_DELETE_LONG", RulePurpose.Validate, RaiseWhenLeaveDaysOver(50), RuleEventType.BeforeDelete);

        await DeleteAbsenceAsync(absenceId);

        IsDeleted(absenceId).ShouldBeTrue();
    }

    // ---------------------------------------------------------------- OnValidate

    [Fact]
    public async Task Should_Refuse_Saving_A_Leave_When_An_OnValidate_Rule_Matches()
    {
        await ArmAsync("MAX_50", RulePurpose.Validate, RaiseWhenLeaveDaysOver(50), RuleEventType.OnValidate);

        await Should.ThrowAsync<UserFriendlyException>(() => InsertAbsenceAsync(totalLeaveDays: 60));
    }

    [Fact]
    public async Task Should_Save_A_Leave_When_The_OnValidate_Rule_Does_Not_Match()
    {
        await ArmAsync("MAX_50_OK", RulePurpose.Validate, RaiseWhenLeaveDaysOver(50), RuleEventType.OnValidate);

        await Should.NotThrowAsync(() => InsertAbsenceAsync(totalLeaveDays: 10));
    }

    [Fact]
    public async Task Should_Validate_The_Values_BeforeSave_Derived_Not_The_Values_Typed()
    {
        // The user types 10 days, which passes. BeforeSave then derives 99. OnValidate must judge the 99 that
        // is about to be written -- validating first would pass a record saved with a value it never checked.
        await ArmAsync("DERIVE_99", RulePurpose.Evaluate, SetLeaveDays(99), RuleEventType.BeforeSave);
        await ArmAsync("MAX_50_DERIVED", RulePurpose.Validate, RaiseWhenLeaveDaysOver(50), RuleEventType.OnValidate);

        await Should.ThrowAsync<UserFriendlyException>(() => InsertAbsenceAsync(totalLeaveDays: 10));
    }

    [Fact]
    public async Task Should_Write_A_BeforeSave_Derived_Value_To_The_Database()
    {
        // Reads the value back from the DATABASE, not the entity in memory. ABP raises the -ing events after
        // that SaveChanges has executed its SQL, so a derivation could look applied in memory and never be
        // stored; only a fresh read proves it reached the row.
        await ArmAsync("DERIVE_42", RulePurpose.Evaluate, SetLeaveDays(42), RuleEventType.BeforeSave);

        var absenceId = await InsertAbsenceAsync(totalLeaveDays: 10);

        StoredLeaveDays(absenceId).ShouldBe(42);
    }

    [Fact]
    public async Task Should_Not_Store_A_Leave_An_OnValidate_Rule_Refused()
    {
        await ArmAsync("MAX_50_NOT_STORED", RulePurpose.Validate, RaiseWhenLeaveDaysOver(50), RuleEventType.OnValidate);

        await Should.ThrowAsync<UserFriendlyException>(() => InsertAbsenceAsync(totalLeaveDays: 60));

        StoredAbsenceCount().ShouldBe(0);
    }

    // ---------------------------------------------------------------- arming

    [Theory]
    [InlineData(RuleEventType.OnValidate)]
    [InlineData(RuleEventType.BeforeDelete)]
    public async Task Should_Refuse_Arming_A_Rule_That_Sets_A_Field_On_A_Check_Event(RuleEventType eventType)
    {
        var ruleId = await CreateRuleAsync("SETS_" + eventType, RulePurpose.Evaluate, SetLeaveDays(5));

        await Should.ThrowAsync<UserFriendlyException>(() => RulesAppService.CreateOrEditAssignment(
            new CreateOrEditBusinessRuleAssignmentDto { BusinessRuleId = ruleId, EventType = eventType, IsActive = true }));
    }

    [Fact]
    public async Task Should_Refuse_Editing_An_Armed_Rule_Into_One_Its_Event_Cannot_Run()
    {
        // Arm-time checks alone would miss this: the rule is valid when armed and only becomes invalid later.
        var ruleId = await ArmAsync("EDITED_LATER", RulePurpose.Evaluate, AlwaysRaise(), RuleEventType.OnValidate);

        await Should.ThrowAsync<UserFriendlyException>(() => RulesAppService.CreateOrEditBusinessRule(
            RuleInput("EDITED_LATER", RulePurpose.Evaluate, SetLeaveDays(5), ruleId)));
    }

    [Fact]
    public async Task Should_Offer_BeforeDelete_On_Leave_And_Employee()
    {
        var metadata = await RulesAppService.GetRuleMetadata();

        foreach (var code in new[] { EmployeeAbsenceRuleBaseObjectProvider.BaseObjectCode, EmployeeRuleBaseObjectProvider.BaseObjectCode })
        {
            metadata.BaseObjects.Single(b => b.Code == code).SupportedEvents.ShouldContain(RuleEventType.BeforeDelete);
        }
    }

    // ---------------------------------------------------------------- runner

    [Fact]
    public async Task Should_Report_Armed_Rules_Only_For_The_Event_They_Are_Armed_On()
    {
        await ArmAsync("ONLY_ON_VALIDATE", RulePurpose.Validate, RaiseWhenLeaveDaysOver(50), RuleEventType.OnValidate);

        var runner = Resolve<IBusinessRuleRunner>();
        var code = EmployeeAbsenceRuleBaseObjectProvider.BaseObjectCode;

        (await runner.HasArmedRulesAsync(AbpSession.TenantId, code, RuleEventType.OnValidate, DateTime.Today)).ShouldBeTrue();
        (await runner.HasArmedRulesAsync(AbpSession.TenantId, code, RuleEventType.BeforeSave, DateTime.Today)).ShouldBeFalse();
    }

    [Fact]
    public void Should_Refuse_At_Run_Time_An_Action_The_Event_Does_Not_Allow()
    {
        // Arming refuses this; the run-time check covers a definition edited after arming by any other route.
        var baseObject = Resolve<IRuleBaseObjectRegistry>().Find(EmployeeAbsenceRuleBaseObjectProvider.BaseObjectCode);
        var context = new RuleExecutionContext(baseObject, RuleEventType.OnValidate, DateTime.Today, AbpSession.TenantId)
        {
            AllowedActionKinds = RuleEventActionPolicy.AllowedKindsFor(RuleEventType.OnValidate)
        };
        context.WithParameter(EmployeeAbsenceRuleBaseObjectProvider.LeaveParameter, new EmployeeAbsence { TotalLeaveDays = 1 });

        Should.Throw<RuleExecutionException>(() =>
            Resolve<IRuleEvaluator>().Evaluate(SetLeaveDays(5), "RUNTIME_SET", context));
    }
}
