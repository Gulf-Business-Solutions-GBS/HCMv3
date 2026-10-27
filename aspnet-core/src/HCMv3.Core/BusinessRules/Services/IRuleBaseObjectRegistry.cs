using System;
using System.Collections.Generic;

namespace HCMv3.BusinessRules.Services;

/// <summary>
/// Declares one or more rule base objects. Implement this in the feature module that owns the entity --
/// <c>EmployeeRuleBaseObjectProvider</c> lives in PersonnelAdministration, not here -- so the decision about
/// what a rule author may touch is made by the team that owns the data.
///
/// <para><b>Name the class <c>{Something}RuleBaseObjectProvider</c>.</b> Castle binds a conventionally
/// registered class to interfaces matching by name suffix, so a class named <c>EmployeeRuleProvider</c>
/// would register only as itself, the registry would never see it, and its base object would silently not
/// exist. Same trap, and the same fix, as <c>{X}WorkflowTransactionHandler</c> and
/// <c>{X}WageTypeCalculator</c>. The registration test asserts every implementation actually resolves --
/// keep it green.</para>
/// </summary>
public interface IRuleBaseObjectProvider
{
    IEnumerable<RuleBaseObjectDescriptor> GetBaseObjects();
}

/// <summary>
/// The catalogue of everything rules may be written against, assembled once from every
/// <see cref="IRuleBaseObjectProvider"/> in the solution.
///
/// <para>
/// Every lookup in the engine goes through here: the parser validates field references against it, the
/// authoring UI populates its pickers from it, and the evaluator reads and writes values through the
/// accessors it holds. That single-source property is the point -- if the registry does not describe a
/// field, no part of the system can reach it.
/// </para>
/// </summary>
public interface IRuleBaseObjectRegistry
{
    IReadOnlyList<RuleBaseObjectDescriptor> GetAll();

    /// <summary>The descriptor for a code, or null. Case-insensitive.</summary>
    RuleBaseObjectDescriptor Find(string code);

    /// <summary>
    /// The descriptor for a code, or throws <see cref="RuleDefinitionException"/>. Used on paths where a
    /// missing base object means the rule cannot run and silence would be the worse answer.
    /// </summary>
    RuleBaseObjectDescriptor Get(string code);

    /// <summary>The descriptor whose primary entity type is <paramref name="clrType"/>, or null. Used to bind entity events.</summary>
    RuleBaseObjectDescriptor FindByClrType(Type clrType);
}
