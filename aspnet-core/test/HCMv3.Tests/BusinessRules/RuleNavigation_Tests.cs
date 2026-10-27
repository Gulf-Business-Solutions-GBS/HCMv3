using System;
using System.Collections.Generic;
using HCMv3.BusinessRules;
using HCMv3.BusinessRules.Definition;
using HCMv3.BusinessRules.Services;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.BusinessRules;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.BusinessRules;

/// <summary>
/// Covers following declared references -- <c>Leave.Employee.Nationality.NameEn</c> rather than an id a
/// configurer would have to memorise.
///
/// The behaviour worth pinning down is not that a path resolves; it is what happens when it CANNOT. Lazy
/// loading is switched off in this solution, so the naive implementation (read the navigation property)
/// would have read empty almost everywhere and made every such condition quietly false. These tests exist
/// to keep the engine loud instead: an undeclared path fails, an unset reference reads as empty, and a
/// reference is never loaded twice.
/// </summary>
// ReSharper disable once InconsistentNaming
public class RuleNavigation_Tests : AppTestBase
{
    private static readonly DateTime Today = new(2026, 9, 8);

    private readonly IRuleEvaluator _evaluator;
    private readonly IRuleBaseObjectRegistry _registry;
    private readonly IRuleDefinitionValidator _validator;

    public RuleNavigation_Tests()
    {
        LoginAsDefaultTenantAdmin();

        _evaluator = Resolve<IRuleEvaluator>();
        _registry = Resolve<IRuleBaseObjectRegistry>();
        _validator = Resolve<IRuleDefinitionValidator>();
    }

    // -------------------------------------------------------------------------------------------------
    // Path resolution
    // -------------------------------------------------------------------------------------------------

    [Fact]
    public void Should_Resolve_A_Direct_Field_With_No_References()
    {
        var resolved = LeaveParameter().ResolvePath("StartDate");

        resolved.ShouldNotBeNull();
        resolved.IsDirect.ShouldBeTrue();
        resolved.Field.Name.ShouldBe("StartDate");
    }

    [Fact]
    public void Should_Resolve_A_Field_One_Reference_Away()
    {
        var resolved = LeaveParameter().ResolvePath("Employee.Gender");

        resolved.ShouldNotBeNull();
        resolved.IsDirect.ShouldBeFalse();
        resolved.Navigations.Count.ShouldBe(1);
        resolved.Navigations[0].Name.ShouldBe("Employee");
    }

    /// <summary>The path from the user's own example.</summary>
    [Fact]
    public void Should_Resolve_A_Field_Two_References_Away()
    {
        var resolved = LeaveParameter().ResolvePath("Employee.Nationality.NameEn");

        resolved.ShouldNotBeNull();
        resolved.Navigations.Count.ShouldBe(2);
        resolved.Navigations[0].Name.ShouldBe("Employee");
        resolved.Navigations[1].Name.ShouldBe("Nationality");
        resolved.Field.Name.ShouldBe("NameEn");
    }

    [Fact]
    public void Should_Resolve_Case_Insensitively()
    {
        LeaveParameter().ResolvePath("employee.nationality.nameen").ShouldNotBeNull();
    }

    [Fact]
    public void Should_Not_Resolve_A_Reference_That_Was_Never_Declared()
    {
        // Payroll is not declared anywhere on Leave, so no rule may reach it. This is the whitelist doing
        // its job: an undeclared path is unreachable, not merely undocumented.
        LeaveParameter().ResolvePath("Payroll.NetPay").ShouldBeNull();
    }

    [Fact]
    public void Should_Not_Resolve_A_Field_That_Does_Not_Exist_On_The_Related_Record()
    {
        LeaveParameter().ResolvePath("Employee.Nationality.Population").ShouldBeNull();
    }

    // -------------------------------------------------------------------------------------------------
    // Reading through a reference
    // -------------------------------------------------------------------------------------------------

    [Fact]
    public void Should_Read_A_Value_Two_References_Away()
    {
        var loader = new FakeEntityLoader()
            .With(new Employee { Id = 5, NationalityId = 12 })
            .With(new Nationality { Id = 12, NameEn = "Indian" });

        var context = LeaveContext(new EmployeeAbsence { EmployeeId = 5 }, loader);

        var matched = Evaluate(context, Compare("Employee.Nationality.NameEn", "Indian"));

        matched.ShouldBeTrue();
    }

    [Fact]
    public void Should_Not_Match_When_The_Related_Value_Differs()
    {
        var loader = new FakeEntityLoader()
            .With(new Employee { Id = 5, NationalityId = 12 })
            .With(new Nationality { Id = 12, NameEn = "Kuwaiti" });

        var context = LeaveContext(new EmployeeAbsence { EmployeeId = 5 }, loader);

        Evaluate(context, Compare("Employee.Nationality.NameEn", "Indian")).ShouldBeFalse();
    }

    /// <summary>
    /// An employee with no religion recorded is ordinary data. It reads as empty, and the comparison is
    /// false -- the same answer the engine gives for any other unfilled field.
    /// </summary>
    [Fact]
    public void Should_Read_Empty_When_A_Nullable_Reference_Is_Unset()
    {
        var loader = new FakeEntityLoader().With(new Employee { Id = 5, ReligionId = null });

        var context = LeaveContext(new EmployeeAbsence { EmployeeId = 5 }, loader);

        Evaluate(context, Compare("Employee.Religion.NameEn", "Muslim")).ShouldBeFalse();

        // The employee was loaded; the religion never was.
        loader.LoadCount.ShouldBe(1);
    }

    /// <summary>
    /// Several of these foreign keys are non-nullable ints -- <c>Employee.NationalityId</c> is one -- so
    /// "nothing chosen" arrives as 0, not null. Zero must be treated as unset, or every such row issues a
    /// lookup for entity 0 that can only ever come back empty.
    /// </summary>
    [Fact]
    public void Should_Read_Empty_When_A_Non_Nullable_Reference_Is_Left_At_Zero()
    {
        var loader = new FakeEntityLoader().With(new Employee { Id = 5, NationalityId = 0 });

        var context = LeaveContext(new EmployeeAbsence { EmployeeId = 5 }, loader);

        Evaluate(context, Compare("Employee.Nationality.NameEn", "Indian")).ShouldBeFalse();

        // One load for the employee. If this becomes 2, the zero guard has been lost.
        loader.LoadCount.ShouldBe(1);
    }

    [Fact]
    public void Should_Read_Empty_When_The_Related_Record_Is_Missing()
    {
        // The employee id points at nothing -- deleted, or another tenant's row hidden by the filter.
        var context = LeaveContext(new EmployeeAbsence { EmployeeId = 999 }, new FakeEntityLoader());

        Evaluate(context, Compare("Employee.Nationality.NameEn", "Indian")).ShouldBeFalse();
    }

    /// <summary>
    /// The memo is what keeps reference-following affordable. Three reads off the same employee must cost
    /// one load, or a rule with several conditions would quietly multiply the queries on every save.
    /// </summary>
    [Fact]
    public void Should_Load_Each_Related_Record_Only_Once_Per_Execution()
    {
        var loader = new FakeEntityLoader()
            .With(new Employee { Id = 5, NationalityId = 12, Code = "E1", Email = "a@b.c" })
            .With(new Nationality { Id = 12, NameEn = "Indian" });

        var context = LeaveContext(new EmployeeAbsence { EmployeeId = 5 }, loader);

        var definition = new RuleDefinition
        {
            Blocks = new List<RuleBlock>
            {
                new()
                {
                    If = new RuleLogicalExpression
                    {
                        Operator = RuleLogicalOperator.And,
                        Operands = new List<RuleExpression>
                        {
                            Compare("Employee.Code", "E1"),
                            Compare("Employee.Email", "a@b.c"),
                            Compare("Employee.Nationality.NameEn", "Indian")
                        }
                    },
                    Then = new List<RuleAction> { SetAdvancePaid() }
                }
            }
        };

        _evaluator.Evaluate(definition, "MEMO", context).ConditionMatched.ShouldBeTrue();

        // One employee, one nationality -- not one per field read.
        loader.LoadCount.ShouldBe(2);
    }

    // -------------------------------------------------------------------------------------------------
    // Writing through a reference is refused
    // -------------------------------------------------------------------------------------------------

    /// <summary>
    /// Reading a related record is fine; changing one would mean a rule saving a second row as a side
    /// effect of saving the first. Caught at authoring time so nobody ships it.
    /// </summary>
    [Fact]
    public void Should_Reject_An_Assignment_Through_A_Reference()
    {
        var definition = new RuleDefinition
        {
            Blocks = new List<RuleBlock>
            {
                new()
                {
                    Then = new List<RuleAction>
                    {
                        new RuleSetAction
                        {
                            Parameter = EmployeeAbsenceRuleBaseObjectProvider.LeaveParameter,
                            Field = "Employee.Email",
                            Value = new RuleConstantExpression { DataType = RuleDataType.String, Value = "x@y.z" }
                        }
                    }
                }
            }
        };

        var errors = _validator.Validate(
            definition,
            _registry.Get(EmployeeAbsenceRuleBaseObjectProvider.BaseObjectCode),
            RulePurpose.Evaluate);

        errors.ShouldContain(e => e.Contains("related record"));
    }

    [Fact]
    public void Should_Accept_Reading_Through_A_Reference_In_A_Condition()
    {
        var definition = new RuleDefinition
        {
            Blocks = new List<RuleBlock>
            {
                new()
                {
                    If = Compare("Employee.Nationality.NameEn", "Indian"),
                    Then = new List<RuleAction> { SetAdvancePaid() }
                }
            }
        };

        var errors = _validator.Validate(
            definition,
            _registry.Get(EmployeeAbsenceRuleBaseObjectProvider.BaseObjectCode),
            RulePurpose.Evaluate);

        errors.ShouldBeEmpty();
    }

    // -------------------------------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------------------------------

    private RuleParameterDescriptor LeaveParameter()
    {
        return _registry
            .Get(EmployeeAbsenceRuleBaseObjectProvider.BaseObjectCode)
            .FindParameter(EmployeeAbsenceRuleBaseObjectProvider.LeaveParameter);
    }

    private RuleExecutionContext LeaveContext(EmployeeAbsence leave, IRuleEntityLoader loader)
    {
        return new RuleExecutionContext(
                _registry.Get(EmployeeAbsenceRuleBaseObjectProvider.BaseObjectCode),
                RuleEventType.BeforeSave,
                Today,
                AbpSession.TenantId)
            {
                EntityLoader = loader
            }
            .WithParameter(EmployeeAbsenceRuleBaseObjectProvider.LeaveParameter, leave)
            .WithParameter(EmployeeAbsenceRuleBaseObjectProvider.EmployeeParameter, new RuleEmployeeSnapshot())
            .WithParameter(EmployeeAbsenceRuleBaseObjectProvider.LeaveTypeParameter, null);
    }

    private bool Evaluate(RuleExecutionContext context, RuleExpression condition)
    {
        var definition = new RuleDefinition
        {
            Blocks = new List<RuleBlock>
            {
                new() { If = condition, Then = new List<RuleAction> { SetAdvancePaid() } }
            }
        };

        return _evaluator.Evaluate(definition, "NAV", context).ConditionMatched;
    }

    private static RuleExpression Compare(string path, string value)
    {
        return new RuleComparisonExpression
        {
            Operator = RuleComparisonOperator.Equal,
            Left = new RuleFieldExpression
            {
                Parameter = EmployeeAbsenceRuleBaseObjectProvider.LeaveParameter,
                Field = path
            },
            Right = new RuleConstantExpression { DataType = RuleDataType.String, Value = value }
        };
    }

    private static RuleSetAction SetAdvancePaid()
    {
        return new RuleSetAction
        {
            Parameter = EmployeeAbsenceRuleBaseObjectProvider.LeaveParameter,
            Field = "AdvanceLeavePaid",
            Value = new RuleConstantExpression { DataType = RuleDataType.Boolean, Value = "true" }
        };
    }

    /// <summary>
    /// Stands in for the repository lookup, and counts how many times it was asked. The count is the point:
    /// it is what proves the execution memo works rather than merely appearing to.
    /// </summary>
    private sealed class FakeEntityLoader : IRuleEntityLoader
    {
        private readonly Dictionary<string, object> _entities = new();

        public int LoadCount { get; private set; }

        public FakeEntityLoader With<TEntity>(TEntity entity) where TEntity : Abp.Domain.Entities.Entity<int>
        {
            _entities[typeof(TEntity).FullName + "#" + entity.Id] = entity;
            return this;
        }

        public object Load(Type entityType, int id)
        {
            LoadCount++;

            return _entities.TryGetValue(entityType.FullName + "#" + id, out var entity) ? entity : null;
        }
    }
}
