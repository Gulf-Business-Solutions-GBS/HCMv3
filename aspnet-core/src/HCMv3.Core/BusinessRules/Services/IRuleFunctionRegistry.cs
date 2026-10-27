using System;
using System.Collections.Generic;

namespace HCMv3.BusinessRules.Services;

/// <summary>
/// A function a rule author may call.
///
/// <para>
/// This is the engine's growth path. The node types are a closed set and stay that way; anything a rule
/// needs beyond fields, comparisons and arithmetic arrives as a function. That keeps new capability
/// reviewable -- adding <c>GetNumberOfWorkingDays</c> is a class with a test, not a change to how rules are
/// parsed or evaluated -- and it keeps the capability list enumerable, which is what lets the authoring UI
/// show configurers exactly what they can call.
/// </para>
///
/// <para>
/// Functions may take dependencies. A working-day count needs the holiday calendar, and pretending
/// otherwise would push that lookup into the caller. What they must NOT do is write: a function returns a
/// value, and every change to state goes through a rule action, so the set of things a rule can modify
/// stays exactly the set of writable fields.
/// </para>
///
/// <para><b>Name the class <c>{Something}RuleFunction</c></b> -- Castle's suffix matching again. A
/// mis-named function class registers as itself, the registry never sees it, and every rule calling it
/// fails with "unknown function" at parse time.</para>
/// </summary>
public interface IRuleFunction
{
    /// <summary>
    /// The name rules call it by, matched case-insensitively. Must be unique across all functions -- the
    /// registry rejects duplicates rather than silently picking one.
    /// </summary>
    string Name { get; }

    /// <summary>Signature metadata, used to validate calls at parse time and to build the authoring picker.</summary>
    RuleFunctionDescriptor Descriptor { get; }

    /// <summary>
    /// Evaluates the call. Arguments arrive already normalised by <see cref="RuleValueConverter"/> and
    /// already coerced to the declared parameter types, so an implementation may cast directly. Throwing is
    /// how a function reports a bad call; the runner attributes it to the rule.
    /// </summary>
    object Invoke(IReadOnlyList<object> args, RuleExecutionContext context);
}

/// <summary>Signature of a rule function: what it returns and what it expects.</summary>
public class RuleFunctionDescriptor
{
    public RuleFunctionDescriptor(
        string name,
        string displayNameKey,
        RuleDataType returnType,
        params RuleFunctionParameter[] parameters)
    {
        Name = name;
        DisplayNameKey = displayNameKey;
        ReturnType = returnType;
        Parameters = parameters ?? Array.Empty<RuleFunctionParameter>();
    }

    public string Name { get; }

    public string DisplayNameKey { get; }

    public RuleDataType ReturnType { get; }

    public IReadOnlyList<RuleFunctionParameter> Parameters { get; }

    /// <summary>
    /// How many arguments must be supplied. Trailing optional parameters may be omitted; optional
    /// parameters in the middle are not supported, because positional calls make them unreadable.
    /// </summary>
    public int RequiredParameterCount
    {
        get
        {
            var required = 0;

            for (var i = 0; i < Parameters.Count; i++)
            {
                if (!Parameters[i].IsOptional)
                {
                    required = i + 1;
                }
            }

            return required;
        }
    }
}

/// <summary>One parameter of a rule function.</summary>
public class RuleFunctionParameter
{
    public RuleFunctionParameter(string name, RuleDataType dataType, bool isOptional = false)
    {
        Name = name;
        DataType = dataType;
        IsOptional = isOptional;
    }

    public string Name { get; }

    public RuleDataType DataType { get; }

    public bool IsOptional { get; }
}

/// <summary>
/// Every registered <see cref="IRuleFunction"/>, by name. Assembled once from the container, the same way
/// <c>IWorkflowTransactionHandlerResolver</c> assembles handlers.
/// </summary>
public interface IRuleFunctionRegistry
{
    IReadOnlyList<RuleFunctionDescriptor> GetAll();

    /// <summary>The function for a name, or null. Case-insensitive.</summary>
    IRuleFunction Find(string name);

    /// <summary>The function for a name, or throws <see cref="RuleDefinitionException"/>.</summary>
    IRuleFunction Get(string name);
}
