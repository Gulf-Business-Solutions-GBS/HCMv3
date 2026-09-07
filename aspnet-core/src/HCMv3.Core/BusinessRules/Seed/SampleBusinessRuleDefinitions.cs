using System.Collections.Generic;
using HCMv3.BusinessRules.Definition;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration.BusinessRules;
using HCMv3.Workflow.BusinessRules;

namespace HCMv3.BusinessRules.Seed;

/// <summary>
/// The three worked examples the engine was built to prove -- a derivation, a routing decision and a
/// validation -- expressed in the same object model the authoring UI produces.
///
/// <para>
/// They exist for three jobs at once: they are what the "install sample rules" action creates, they are
/// what the engine's tests assert against, and they are the reference an implementer copies when writing
/// the third rule. Building them through the object model rather than as JSON string literals is
/// deliberate -- a literal would drift from the serializer the moment either changed, and these are meant
/// to stay correct.
/// </para>
/// </summary>
public static class SampleBusinessRuleDefinitions
{
    public const string ProbationEndDateRuleCode = "PROBATION_END_DATE";
    public const string LeaveApprovalRoutingRuleCode = "LEAVE_APPROVAL_ROUTING";
    public const string MaternityLeaveGenderRuleCode = "MATERNITY_LEAVE_GENDER_CHECK";

    /// <summary>
    /// Localization key the maternity rule raises. Lives here rather than in the rule JSON as free text so
    /// the entry in HCMv3.xml and the rule cannot drift apart.
    /// </summary>
    public const string MaternityLeaveGenderErrorKey = "MaternityLeaveIsNotAvailableForMaleEmployees";

    /// <summary>
    /// How the installer recognises the maternity absence type when no id is supplied. A substring match on
    /// the English name, because absence type CODES in this system are bare numbers ("7") that carry no
    /// meaning across tenants.
    /// </summary>
    public const string MaternityAbsenceTypeNameFragment = "Maternity";

    /// <summary>Default probation length when the employee record does not state one.</summary>
    public const int DefaultProbationDays = 100;

    /// <summary>The request type the routing sample applies to. Leave is <c>EmployeeAbsence</c> in HCMv3.</summary>
    public const string LeaveRequestType = "EmployeeAbsence";

    /// <summary>Job title code that routes to the board chain.</summary>
    public const string ChroJobTitleCode = "CHRO";

    /// <summary>Approval chain codes the routing sample selects. These must exist as <c>RequestMaster.Code</c>.</summary>
    public const string BoardLeaveChainCode = "BOARD_LEAVE";

    public const string ManagerLeaveChainCode = "MGR_LEAVE";

    /// <summary>
    /// Derives the probation end date on every employee save.
    ///
    /// <para><b>If</b> the record states no probation length, use joining date plus
    /// <see cref="DefaultProbationDays"/> calendar days. <b>Otherwise</b> use the length the record states.
    /// </para>
    ///
    /// <para>
    /// This replaces the hardcoded line in <c>EmployeesAppService.ApplyComputedFields</c>. The else branch
    /// reproduces that line exactly, which is what makes the rule safe to arm on an existing tenant: every
    /// employee who already has a probation length keeps the date they had, and only the ones with a blank
    /// length -- who previously got their joining date back as their probation end date, which was never
    /// intended -- start getting something sensible.
    /// </para>
    /// </summary>
    public static RuleDefinition ProbationEndDate()
    {
        var joiningDate = Field(EmployeeRuleBaseObjectProvider.EmployeeParameter, "JoiningDate");
        var probationDays = Field(EmployeeRuleBaseObjectProvider.EmployeeParameter, "ProbationDays");

        return new RuleDefinition
        {
            Version = 1,
            Blocks = new List<RuleBlock>
            {
                new()
                {
                    If = new RuleComparisonExpression
                    {
                        Operator = RuleComparisonOperator.LessThanOrEqual,
                        Left = probationDays,
                        Right = Constant("0", RuleDataType.Integer)
                    },
                    Then = new List<RuleAction>
                    {
                        SetProbationEndDate(joiningDate, Constant(DefaultProbationDays.ToString(), RuleDataType.Integer))
                    },
                    Else = new List<RuleAction>
                    {
                        SetProbationEndDate(joiningDate, probationDays)
                    }
                }
            }
        };
    }

    /// <summary>
    /// Chooses the approval chain for a leave request based on who is applying.
    ///
    /// <para>
    /// Two independent blocks, not an if/else-if, because the engine evaluates every block -- so the second
    /// block has to exclude the CHRO explicitly. That is not a wart to work around; it is what makes rules
    /// composable, since blocks added later cannot silently be shadowed by an earlier branch. It does mean
    /// an author writing overlapping conditions must think about precedence, which is why the last matching
    /// assignment wins and the ordering is visible in the builder.
    /// </para>
    /// </summary>
    public static RuleDefinition LeaveApprovalRouting()
    {
        var isLeaveRequest = new RuleComparisonExpression
        {
            Operator = RuleComparisonOperator.Equal,
            Left = Field(WorkflowRoutingRuleBaseObjectProvider.RequestParameter, "RequestType"),
            Right = Constant(LeaveRequestType, RuleDataType.String)
        };

        var isChro = new RuleComparisonExpression
        {
            Operator = RuleComparisonOperator.Equal,
            Left = Field(WorkflowRoutingRuleBaseObjectProvider.ApplierParameter, "JobTitleCode"),
            Right = Constant(ChroJobTitleCode, RuleDataType.String)
        };

        var isManager = new RuleComparisonExpression
        {
            Operator = RuleComparisonOperator.Equal,
            Left = Field(WorkflowRoutingRuleBaseObjectProvider.ApplierParameter, "IsManager"),
            Right = Constant("true", RuleDataType.Boolean)
        };

        return new RuleDefinition
        {
            Version = 1,
            Blocks = new List<RuleBlock>
            {
                // The CHRO's own leave goes to the board chain.
                new()
                {
                    If = And(isLeaveRequest, isChro),
                    Then = new List<RuleAction> { SetChain(BoardLeaveChainCode) }
                },

                // Any other manager goes to the senior chain. The NOT is load-bearing: without it the CHRO
                // would match this block too, and being evaluated second it would overwrite the board chain.
                new()
                {
                    If = And(isLeaveRequest, new RuleNotExpression { Operand = isChro }, isManager),
                    Then = new List<RuleAction> { SetChain(ManagerLeaveChainCode) }
                }
            }
        };
    }

    /// <summary>
    /// Refuses maternity leave for a male employee.
    ///
    /// <para><b>If</b> the leave type is the maternity type <b>and</b> the applying employee's gender is
    /// Male, raise <see cref="MaternityLeaveGenderErrorKey"/>. The engine turns that into a
    /// <c>UserFriendlyException</c> in the user's own language, and the save is aborted.</para>
    ///
    /// <para>
    /// The maternity type is passed IN rather than hardcoded, because absence type ids differ per tenant
    /// and per environment -- a rule written against a literal id is a rule that silently matches nothing
    /// after a data refresh. The installer resolves the id once and bakes it in, which is exactly what the
    /// authoring UI's lookup picker does.
    /// </para>
    ///
    /// <para>
    /// Purpose is <see cref="RulePurpose.Validate"/>, so the engine discards any assignment this rule might
    /// contain even if somebody later adds one. A validation may refuse a save; it may not quietly change
    /// the record it is judging.
    /// </para>
    /// </summary>
    public static RuleDefinition MaternityLeaveGenderCheck(int maternityAbsenceTypeId)
    {
        var isMaternityLeave = new RuleComparisonExpression
        {
            Operator = RuleComparisonOperator.Equal,
            Left = Field(EmployeeAbsenceRuleBaseObjectProvider.LeaveParameter, "AbsenceTypeId"),
            Right = Constant(maternityAbsenceTypeId.ToString(), RuleDataType.Lookup)
        };

        var isMale = new RuleComparisonExpression
        {
            Operator = RuleComparisonOperator.Equal,
            Left = Field(EmployeeAbsenceRuleBaseObjectProvider.EmployeeParameter, "Gender"),
            Right = Constant(((int)Gender.Male).ToString(), RuleDataType.Enum)
        };

        return new RuleDefinition
        {
            Version = 1,
            Blocks = new List<RuleBlock>
            {
                new()
                {
                    If = And(isMaternityLeave, isMale),
                    Then = new List<RuleAction>
                    {
                        new RuleErrorAction
                        {
                            MessageKey = MaternityLeaveGenderErrorKey,
                            Args = new List<RuleExpression>()
                        }
                    }
                }
            }
        };
    }

    private static RuleSetAction SetProbationEndDate(RuleExpression joiningDate, RuleExpression days)
    {
        return new RuleSetAction
        {
            Parameter = EmployeeRuleBaseObjectProvider.EmployeeParameter,
            Field = "ProbationEndDate",
            Value = new RuleFunctionExpression
            {
                Name = "AddCalendarDays",
                Args = new List<RuleExpression> { joiningDate, days }
            }
        };
    }

    private static RuleSetAction SetChain(string chainCode)
    {
        return new RuleSetAction
        {
            Parameter = WorkflowRoutingRuleBaseObjectProvider.RoutingParameter,
            Field = "RequestMasterCode",
            Value = Constant(chainCode, RuleDataType.String)
        };
    }

    private static RuleExpression And(params RuleExpression[] operands)
    {
        return new RuleLogicalExpression
        {
            Operator = RuleLogicalOperator.And,
            Operands = new List<RuleExpression>(operands)
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
