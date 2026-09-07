using System;
using HCMv3.BusinessRules;
using HCMv3.BusinessRules.Seed;
using HCMv3.BusinessRules.Services;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.BusinessRules;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.BusinessRules;

/// <summary>
/// Covers the leave validation worked example: a male employee may not request maternity leave.
///
/// This is the engine's third shape -- after a derivation and a routing decision, a rule that REFUSES a
/// save. The cases below are as much about what it does not do: it must not fire on the right leave type
/// for the right gender, nor on the wrong type for the wrong gender, because a validation that is even
/// slightly over-eager blocks legitimate requests and gets switched off entirely.
/// </summary>
// ReSharper disable once InconsistentNaming
public class MaternityLeaveRule_Tests : AppTestBase
{
    private const int MaternityAbsenceTypeId = 67;
    private const int AnnualLeaveAbsenceTypeId = 61;

    private static readonly DateTime Today = new(2026, 9, 7);

    private readonly IRuleEvaluator _evaluator;
    private readonly IRuleBaseObjectRegistry _registry;
    private readonly IRuleDefinitionValidator _validator;

    public MaternityLeaveRule_Tests()
    {
        LoginAsDefaultTenantAdmin();

        _evaluator = Resolve<IRuleEvaluator>();
        _registry = Resolve<IRuleBaseObjectRegistry>();
        _validator = Resolve<IRuleDefinitionValidator>();
    }

    [Fact]
    public void Should_Refuse_Maternity_Leave_For_A_Male_Employee()
    {
        var result = Run(MaternityAbsenceTypeId, Gender.Male);

        result.ConditionMatched.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].MessageKey.ShouldBe(SampleBusinessRuleDefinitions.MaternityLeaveGenderErrorKey);
    }

    [Fact]
    public void Should_Allow_Maternity_Leave_For_A_Female_Employee()
    {
        var result = Run(MaternityAbsenceTypeId, Gender.Female);

        result.ConditionMatched.ShouldBeFalse();
        result.Errors.ShouldBeEmpty();
    }

    /// <summary>The gender check must not leak onto every other leave type a man requests.</summary>
    [Fact]
    public void Should_Allow_A_Male_Employee_To_Request_Annual_Leave()
    {
        var result = Run(AnnualLeaveAbsenceTypeId, Gender.Male);

        result.ConditionMatched.ShouldBeFalse();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Allow_A_Female_Employee_To_Request_Annual_Leave()
    {
        Run(AnnualLeaveAbsenceTypeId, Gender.Female).Errors.ShouldBeEmpty();
    }

    /// <summary>
    /// A validation rule may raise errors but never write. Enforced by the validator at authoring time and
    /// by the runner at execution time; this asserts the rule as shipped is genuinely error-only.
    /// </summary>
    [Fact]
    public void Should_Be_A_Valid_Validation_Rule()
    {
        var definition = SampleBusinessRuleDefinitions.MaternityLeaveGenderCheck(MaternityAbsenceTypeId);
        var baseObject = _registry.Get(EmployeeAbsenceRuleBaseObjectProvider.BaseObjectCode);

        _validator.Validate(definition, baseObject, RulePurpose.Validate).ShouldBeEmpty();
    }

    /// <summary>
    /// The leave base object must be armable on the events a validation needs, and NOT on routing -- it has
    /// no Routing parameter, so an OnRoute assignment could only ever fail.
    /// </summary>
    [Fact]
    public void Should_Support_Save_And_Validate_Events_But_Not_Routing()
    {
        var baseObject = _registry.Get(EmployeeAbsenceRuleBaseObjectProvider.BaseObjectCode);

        baseObject.Supports(RuleEventType.BeforeSave).ShouldBeTrue();
        baseObject.Supports(RuleEventType.OnValidate).ShouldBeTrue();
        baseObject.Supports(RuleEventType.OnRoute).ShouldBeFalse();
    }

    /// <summary>
    /// The employee parameter is read-only, so no leave rule can rewrite the person's gender to get around
    /// the check it is subject to.
    /// </summary>
    [Fact]
    public void Should_Expose_The_Employee_As_Read_Only()
    {
        var baseObject = _registry.Get(EmployeeAbsenceRuleBaseObjectProvider.BaseObjectCode);

        var employee = baseObject.FindParameter(EmployeeAbsenceRuleBaseObjectProvider.EmployeeParameter);

        employee.ShouldNotBeNull();
        employee.IsWritable.ShouldBeFalse();
        employee.FindField("Gender").ShouldNotBeNull();
    }

    private RuleEvaluationResult Run(int absenceTypeId, Gender gender)
    {
        var baseObject = _registry.Get(EmployeeAbsenceRuleBaseObjectProvider.BaseObjectCode);

        var context = new RuleExecutionContext(baseObject, RuleEventType.BeforeSave, Today, AbpSession.TenantId)
            .WithParameter(
                EmployeeAbsenceRuleBaseObjectProvider.LeaveParameter,
                new EmployeeAbsence
                {
                    AbsenceTypeId = absenceTypeId,
                    StartDate = Today,
                    EndDate = Today.AddDays(60)
                })
            .WithParameter(
                EmployeeAbsenceRuleBaseObjectProvider.EmployeeParameter,
                new RuleEmployeeSnapshot { EmployeeId = 1, Gender = gender })
            .WithParameter(
                EmployeeAbsenceRuleBaseObjectProvider.LeaveTypeParameter,
                new AbsenceType { Id = absenceTypeId, NameEn = "Maternity - KW", IsActive = true });

        return _evaluator.Evaluate(
            SampleBusinessRuleDefinitions.MaternityLeaveGenderCheck(MaternityAbsenceTypeId),
            SampleBusinessRuleDefinitions.MaternityLeaveGenderRuleCode,
            context);
    }
}
