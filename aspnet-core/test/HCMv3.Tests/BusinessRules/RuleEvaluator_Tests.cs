using System;
using System.Collections.Generic;
using HCMv3.BusinessRules;
using HCMv3.BusinessRules.Definition;
using HCMv3.BusinessRules.Seed;
using HCMv3.BusinessRules.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.BusinessRules;
using HCMv3.Workflow.BusinessRules;
using HCMv3.Workflow.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.BusinessRules;

/// <summary>
/// Covers the interpreter: conditions, arithmetic, functions, assignment, and the boundaries a rule author
/// is not allowed to cross.
///
/// The two worked examples are asserted here rather than only end to end, because this is the layer where
/// their behaviour is actually decided -- an end-to-end test that passes tells you the wiring works, but a
/// test at this level tells you the RULE is right, which is the thing a configurer will be editing.
/// </summary>
// ReSharper disable once InconsistentNaming
public class RuleEvaluator_Tests : AppTestBase
{
    private static readonly DateTime Today = new(2026, 9, 7);

    private readonly IRuleEvaluator _evaluator;
    private readonly IRuleBaseObjectRegistry _registry;
    private readonly IRuleDefinitionValidator _validator;

    public RuleEvaluator_Tests()
    {
        LoginAsDefaultTenantAdmin();

        _evaluator = Resolve<IRuleEvaluator>();
        _registry = Resolve<IRuleBaseObjectRegistry>();
        _validator = Resolve<IRuleDefinitionValidator>();
    }

    // -------------------------------------------------------------------------------------------------
    // Worked example 1: probation end date
    // -------------------------------------------------------------------------------------------------

    [Fact]
    public void Should_Derive_Probation_End_Date_As_Joining_Date_Plus_100_Days_When_No_Length_Is_Stated()
    {
        var employee = new Employee { JoiningDate = new DateTime(2026, 1, 1), ProbationDays = 0 };

        var result = RunEmployeeRule(SampleBusinessRuleDefinitions.ProbationEndDate(), employee);

        result.ConditionMatched.ShouldBeTrue();
        employee.ProbationEndDate.ShouldBe(new DateTime(2026, 4, 11));
    }

    /// <summary>
    /// The else branch reproduces what <c>EmployeesAppService.ApplyComputedFields</c> does today. This test
    /// is what makes the rule safe to arm on an existing tenant: everyone with a stated probation length
    /// keeps exactly the date they already had.
    /// </summary>
    [Fact]
    public void Should_Use_The_Stated_Probation_Length_When_There_Is_One()
    {
        var employee = new Employee { JoiningDate = new DateTime(2026, 1, 1), ProbationDays = 90 };

        RunEmployeeRule(SampleBusinessRuleDefinitions.ProbationEndDate(), employee);

        employee.ProbationEndDate.ShouldBe(new DateTime(2026, 1, 1).AddDays(90));
    }

    [Fact]
    public void Should_Record_The_Field_It_Assigned()
    {
        var employee = new Employee { JoiningDate = new DateTime(2026, 1, 1), ProbationDays = 0 };

        var result = RunEmployeeRule(SampleBusinessRuleDefinitions.ProbationEndDate(), employee);

        result.Writes.Count.ShouldBe(1);
        result.Writes[0].Field.ShouldBe("ProbationEndDate");
        result.Writes[0].Parameter.ShouldBe("Employee");
    }

    // -------------------------------------------------------------------------------------------------
    // Worked example 2: leave approval routing
    // -------------------------------------------------------------------------------------------------

    [Fact]
    public void Should_Route_The_Chro_Leave_Request_To_The_Board_Chain()
    {
        var decision = RunRoutingRule(
            SampleBusinessRuleDefinitions.LeaveRequestType,
            new RuleEmployeeSnapshot { JobTitleCode = "CHRO", IsManager = true, DirectReportCount = 6 });

        decision.RequestMasterCode.ShouldBe(SampleBusinessRuleDefinitions.BoardLeaveChainCode);
    }

    [Fact]
    public void Should_Route_A_Manager_Leave_Request_To_The_Manager_Chain()
    {
        var decision = RunRoutingRule(
            SampleBusinessRuleDefinitions.LeaveRequestType,
            new RuleEmployeeSnapshot { JobTitleCode = "DEPT_HEAD", IsManager = true, DirectReportCount = 3 });

        decision.RequestMasterCode.ShouldBe(SampleBusinessRuleDefinitions.ManagerLeaveChainCode);
    }

    /// <summary>
    /// An ordinary employee changes nothing, so the resolver falls through to the workflow-group lookup it
    /// always used. This is the property that makes arming a routing rule a safe act.
    /// </summary>
    [Fact]
    public void Should_Leave_Routing_Untouched_For_An_Ordinary_Employee()
    {
        var decision = RunRoutingRule(
            SampleBusinessRuleDefinitions.LeaveRequestType,
            new RuleEmployeeSnapshot { JobTitleCode = "CLERK", IsManager = false });

        decision.IsResolved.ShouldBeFalse();
    }

    [Fact]
    public void Should_Not_Route_A_Request_Of_Another_Type()
    {
        var decision = RunRoutingRule(
            "EmployeeLoan",
            new RuleEmployeeSnapshot { JobTitleCode = "CHRO", IsManager = true });

        decision.IsResolved.ShouldBeFalse();
    }

    /// <summary>
    /// Every block is evaluated, so the CHRO also satisfies "is a manager". The NOT in the second block is
    /// what stops the later block from overwriting the board chain -- if this ever regresses, the CHRO
    /// silently routes to the manager chain and nobody notices until an audit.
    /// </summary>
    [Fact]
    public void Should_Not_Let_The_Manager_Block_Overwrite_The_Chro_Decision()
    {
        var decision = RunRoutingRule(
            SampleBusinessRuleDefinitions.LeaveRequestType,
            new RuleEmployeeSnapshot { JobTitleCode = "CHRO", IsManager = true, DirectReportCount = 9 });

        decision.RequestMasterCode.ShouldNotBe(SampleBusinessRuleDefinitions.ManagerLeaveChainCode);
    }

    // -------------------------------------------------------------------------------------------------
    // Null semantics -- the part rule authors most often get wrong
    // -------------------------------------------------------------------------------------------------

    /// <summary>
    /// Both a greater-than and its apparent opposite are false against an empty field. That is SQL's answer
    /// and the only defensible one: treating empty as zero would fire rules on exactly the records the
    /// system knows least about.
    /// </summary>
    [Theory]
    [InlineData(RuleComparisonOperator.GreaterThan)]
    [InlineData(RuleComparisonOperator.LessThanOrEqual)]
    [InlineData(RuleComparisonOperator.LessThan)]
    [InlineData(RuleComparisonOperator.GreaterThanOrEqual)]
    public void Should_Answer_False_For_Any_Ordering_Comparison_Against_An_Empty_Field(
        RuleComparisonOperator comparisonOperator)
    {
        var employee = new Employee { Email = null };

        var definition = SingleBlock(
            new RuleComparisonExpression
            {
                Operator = comparisonOperator,
                Left = Field("Employee", "Email"),
                Right = Constant("a", RuleDataType.String)
            },
            SetProbationDays(1));

        RunEmployeeRule(definition, employee).ConditionMatched.ShouldBeFalse();
    }

    [Fact]
    public void Should_Detect_An_Empty_Field_With_IsEmpty()
    {
        var employee = new Employee { Email = "   " };

        var definition = SingleBlock(
            new RuleComparisonExpression
            {
                Operator = RuleComparisonOperator.IsEmpty,
                Left = Field("Employee", "Email")
            },
            SetProbationDays(7));

        RunEmployeeRule(definition, employee).ConditionMatched.ShouldBeTrue();
        employee.ProbationDays.ShouldBe(7);
    }

    // -------------------------------------------------------------------------------------------------
    // Boundaries a rule may not cross
    // -------------------------------------------------------------------------------------------------

    /// <summary>
    /// The whitelist is the security boundary. A rule referencing a field no descriptor declares must fail,
    /// not silently evaluate to null -- otherwise a typo becomes a condition that is quietly always false.
    /// </summary>
    [Fact]
    public void Should_Refuse_To_Read_A_Field_That_Is_Not_Exposed()
    {
        var definition = SingleBlock(
            new RuleComparisonExpression
            {
                Operator = RuleComparisonOperator.Equal,
                Left = Field("Employee", "CivilID"),
                Right = Constant("1", RuleDataType.Integer)
            },
            SetProbationDays(1));

        var exception = Should.Throw<RuleExecutionException>(
            () => RunEmployeeRule(definition, new Employee()));

        exception.Message.ShouldContain("CivilID");
    }

    [Fact]
    public void Should_Refuse_To_Assign_A_Read_Only_Field()
    {
        var definition = UnconditionalBlock(new RuleSetAction
        {
            Parameter = "Employee",
            Field = "Code",
            Value = Constant("HACKED", RuleDataType.String)
        });

        var employee = new Employee { Code = "E001" };

        Should.Throw<RuleExecutionException>(() => RunEmployeeRule(definition, employee));

        employee.Code.ShouldBe("E001");
    }

    /// <summary>Routing rules see the applier, but must not be able to edit them.</summary>
    [Fact]
    public void Should_Refuse_To_Assign_Through_A_Read_Only_Parameter()
    {
        var definition = UnconditionalBlock(new RuleSetAction
        {
            Parameter = WorkflowRoutingRuleBaseObjectProvider.ApplierParameter,
            Field = "JobTitleCode",
            Value = Constant("CHRO", RuleDataType.String)
        });

        var baseObject = _registry.Get(WorkflowRoutingRuleBaseObjectProvider.BaseObjectCode);

        var errors = _validator.Validate(definition, baseObject, RulePurpose.Route);

        errors.ShouldContain(e => e.Contains("read-only"));
    }

    [Fact]
    public void Should_Refuse_An_Unregistered_Function()
    {
        var definition = UnconditionalBlock(new RuleSetAction
        {
            Parameter = "Employee",
            Field = "ProbationDays",
            Value = new RuleFunctionExpression { Name = "DropAllTables", Args = new List<RuleExpression>() }
        });

        Should.Throw<RuleExecutionException>(() => RunEmployeeRule(definition, new Employee()))
            .Message.ShouldContain("DropAllTables");
    }

    [Fact]
    public void Should_Refuse_A_Function_Called_With_The_Wrong_Number_Of_Arguments()
    {
        var definition = UnconditionalBlock(new RuleSetAction
        {
            Parameter = "Employee",
            Field = "ProbationEndDate",
            Value = new RuleFunctionExpression
            {
                Name = "AddCalendarDays",
                Args = new List<RuleExpression> { Field("Employee", "JoiningDate") }
            }
        });

        Should.Throw<RuleExecutionException>(() => RunEmployeeRule(definition, new Employee()));
    }

    /// <summary>Named, so the author can find the rule. A bare DivideByZeroException names nothing.</summary>
    [Fact]
    public void Should_Name_The_Rule_When_It_Divides_By_Zero()
    {
        var definition = UnconditionalBlock(new RuleSetAction
        {
            Parameter = "Employee",
            Field = "ProbationDays",
            Value = new RuleArithmeticExpression
            {
                Operator = RuleArithmeticOperator.Divide,
                Left = Constant("10", RuleDataType.Integer),
                Right = Constant("0", RuleDataType.Integer)
            }
        });

        var exception = Should.Throw<RuleExecutionException>(
            () => RunEmployeeRule(definition, new Employee(), "MY_RULE"));

        exception.RuleCode.ShouldBe("MY_RULE");
        exception.Message.ShouldContain("divides by zero");
    }

    /// <summary>
    /// The budget is what stops configuration alone from holding a save open. It is shared across the whole
    /// event, so splitting a pathological rule in two does not evade it.
    /// </summary>
    [Fact]
    public void Should_Stop_A_Rule_That_Exhausts_The_Step_Budget()
    {
        var definition = SingleBlock(
            new RuleComparisonExpression
            {
                Operator = RuleComparisonOperator.Equal,
                Left = Constant("1", RuleDataType.Integer),
                Right = Constant("1", RuleDataType.Integer)
            },
            SetProbationDays(1));

        var context = EmployeeContext(new Employee());
        context.RemainingSteps = 1;

        Should.Throw<RuleExecutionException>(() => _evaluator.Evaluate(definition, "BUDGET", context))
            .Message.ShouldContain("steps");
    }

    // -------------------------------------------------------------------------------------------------
    // Variables and functions
    // -------------------------------------------------------------------------------------------------

    [Fact]
    public void Should_Evaluate_A_Variable_Once_And_Reuse_It()
    {
        var definition = new RuleDefinition
        {
            Variables = new List<RuleVariable>
            {
                new()
                {
                    Name = "Tenure",
                    DataType = RuleDataType.Integer,
                    Value = new RuleFunctionExpression
                    {
                        Name = "GetAgeInYears",
                        Args = new List<RuleExpression> { Field("Employee", "JoiningDate") }
                    }
                }
            },
            Blocks = new List<RuleBlock>
            {
                new()
                {
                    If = new RuleComparisonExpression
                    {
                        Operator = RuleComparisonOperator.GreaterThanOrEqual,
                        Left = new RuleVariableExpression { Name = "Tenure" },
                        Right = Constant("5", RuleDataType.Integer)
                    },
                    Then = new List<RuleAction> { SetProbationDays(0) }
                }
            }
        };

        var employee = new Employee { JoiningDate = Today.AddYears(-6), ProbationDays = 30 };

        RunEmployeeRule(definition, employee).ConditionMatched.ShouldBeTrue();
        employee.ProbationDays.ShouldBe(0);
    }

    [Fact]
    public void Should_Refuse_A_Variable_Used_Before_It_Is_Defined()
    {
        var definition = SingleBlock(
            new RuleComparisonExpression
            {
                Operator = RuleComparisonOperator.Equal,
                Left = new RuleVariableExpression { Name = "Missing" },
                Right = Constant("1", RuleDataType.Integer)
            },
            SetProbationDays(1));

        Should.Throw<RuleExecutionException>(() => RunEmployeeRule(definition, new Employee()));
    }

    /// <summary>
    /// Counts both ends, matching the SuccessFactors function of the same name and every leave calculation
    /// in this system. A single-day absence is one day.
    /// </summary>
    [Fact]
    public void Should_Count_Calendar_Days_Inclusively()
    {
        var definition = UnconditionalBlock(new RuleSetAction
        {
            Parameter = "Employee",
            Field = "ProbationDays",
            Value = new RuleFunctionExpression
            {
                Name = "GetNumberOfCalendarDays",
                Args = new List<RuleExpression>
                {
                    Constant("2026-01-01", RuleDataType.Date),
                    Constant("2026-01-01", RuleDataType.Date)
                }
            }
        });

        var employee = new Employee();

        RunEmployeeRule(definition, employee);

        employee.ProbationDays.ShouldBe(1);
    }

    // -------------------------------------------------------------------------------------------------
    // Purpose enforcement
    // -------------------------------------------------------------------------------------------------

    /// <summary>
    /// A validation-purpose rule containing an assignment is caught when it is saved, not silently ignored
    /// at run time -- otherwise the author ships a rule whose Then branch does nothing.
    /// </summary>
    [Fact]
    public void Should_Reject_An_Assignment_In_A_Validation_Rule()
    {
        var definition = UnconditionalBlock(SetProbationDays(1));

        var errors = _validator.Validate(
            definition,
            _registry.Get(EmployeeRuleBaseObjectProvider.BaseObjectCode),
            RulePurpose.Validate);

        errors.ShouldContain(e => e.Contains("Validate"));
    }

    [Fact]
    public void Should_Evaluate_But_Not_Apply_Writes_When_Writes_Are_Disallowed()
    {
        var employee = new Employee { JoiningDate = new DateTime(2026, 1, 1), ProbationDays = 0 };

        var context = EmployeeContext(employee);
        context.AllowWrites = false;

        var result = _evaluator.Evaluate(SampleBusinessRuleDefinitions.ProbationEndDate(), "DRY_RUN", context);

        result.ConditionMatched.ShouldBeTrue();
        result.Writes.ShouldBeEmpty();
        employee.ProbationEndDate.ShouldBe(default);
        context.GetTrace().ShouldContain("would set");
    }

    [Fact]
    public void Should_Collect_Errors_Raised_By_A_Rule_Rather_Than_Throwing()
    {
        var definition = UnconditionalBlock(new RuleErrorAction
        {
            MessageKey = "SomeValidationMessage",
            Args = new List<RuleExpression>()
        });

        var result = RunEmployeeRule(definition, new Employee());

        result.Errors.Count.ShouldBe(1);
        result.Errors[0].MessageKey.ShouldBe("SomeValidationMessage");
    }

    // -------------------------------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------------------------------

    private RuleEvaluationResult RunEmployeeRule(RuleDefinition definition, Employee employee, string code = "TEST")
    {
        return _evaluator.Evaluate(definition, code, EmployeeContext(employee));
    }

    private RuleExecutionContext EmployeeContext(Employee employee)
    {
        return new RuleExecutionContext(
                _registry.Get(EmployeeRuleBaseObjectProvider.BaseObjectCode),
                RuleEventType.BeforeSave,
                Today,
                AbpSession.TenantId)
            .WithParameter(EmployeeRuleBaseObjectProvider.EmployeeParameter, employee);
    }

    private WorkflowRoutingDecision RunRoutingRule(string requestType, RuleEmployeeSnapshot applier)
    {
        var decision = new WorkflowRoutingDecision();

        var context = new RuleExecutionContext(
                _registry.Get(WorkflowRoutingRuleBaseObjectProvider.BaseObjectCode),
                RuleEventType.OnRoute,
                Today,
                AbpSession.TenantId)
            .WithParameter(
                WorkflowRoutingRuleBaseObjectProvider.RequestParameter,
                new RuleWorkflowRequestSnapshot { RequestType = requestType })
            .WithParameter(WorkflowRoutingRuleBaseObjectProvider.ApplierParameter, applier)
            .WithParameter(WorkflowRoutingRuleBaseObjectProvider.RoutingParameter, decision);

        _evaluator.Evaluate(SampleBusinessRuleDefinitions.LeaveApprovalRouting(), "ROUTING", context);

        return decision;
    }

    private static RuleDefinition SingleBlock(RuleExpression condition, RuleAction action)
    {
        return new RuleDefinition
        {
            Blocks = new List<RuleBlock>
            {
                new() { If = condition, Then = new List<RuleAction> { action } }
            }
        };
    }

    private static RuleDefinition UnconditionalBlock(RuleAction action)
    {
        return new RuleDefinition
        {
            Blocks = new List<RuleBlock>
            {
                new() { Then = new List<RuleAction> { action } }
            }
        };
    }

    private static RuleSetAction SetProbationDays(int days)
    {
        return new RuleSetAction
        {
            Parameter = "Employee",
            Field = "ProbationDays",
            Value = Constant(days.ToString(), RuleDataType.Integer)
        };
    }

    private static RuleFieldExpression Field(string parameter, string field)
    {
        return new RuleFieldExpression { Parameter = parameter, Field = field };
    }

    private static RuleConstantExpression Constant(string value, RuleDataType dataType)
    {
        return new RuleConstantExpression { Value = value, DataType = dataType };
    }
}
