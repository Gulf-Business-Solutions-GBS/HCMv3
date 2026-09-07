using System.Collections.Generic;
using Abp.Runtime.Validation;

namespace HCMv3.BusinessRules.Dto;

/// <summary>
/// Everything the rule builder needs to render its pickers: which base objects exist, which parameters and
/// fields each exposes, and which functions may be called.
///
/// <para>
/// Served from the same registries the evaluator reads, which is the point. A field the picker offers is by
/// construction a field the evaluator can reach, so the UI cannot drift into offering something that fails
/// at run time -- the usual failure of rule builders that keep their own copy of the schema.
/// </para>
/// </summary>
public class RuleMetadataDto
{
    public List<RuleBaseObjectMetadataDto> BaseObjects { get; set; } = new();

    public List<RuleFunctionMetadataDto> Functions { get; set; } = new();
}

public class RuleBaseObjectMetadataDto
{
    public string Code { get; set; }

    public string DisplayName { get; set; }

    /// <summary>The events this base object may be armed on. The assignment editor offers only these.</summary>
    public List<RuleEventType> SupportedEvents { get; set; } = new();

    public List<RuleParameterMetadataDto> Parameters { get; set; } = new();
}

public class RuleParameterMetadataDto
{
    public string Name { get; set; }

    public string DisplayName { get; set; }

    public bool IsWritable { get; set; }

    public List<RuleFieldMetadataDto> Fields { get; set; } = new();
}

public class RuleFieldMetadataDto
{
    public string Name { get; set; }

    public string DisplayName { get; set; }

    public RuleDataType DataType { get; set; }

    /// <summary>Whether an assignment may target it. Both this and the parameter must be writable.</summary>
    public bool IsWritable { get; set; }

    /// <summary>Names the lookup or enum behind the value, so the builder can offer a picker of real values.</summary>
    public string LookupSource { get; set; }
}

public class RuleFunctionMetadataDto
{
    public string Name { get; set; }

    public string DisplayName { get; set; }

    public RuleDataType ReturnType { get; set; }

    public List<RuleFunctionParameterMetadataDto> Parameters { get; set; } = new();
}

public class RuleFunctionParameterMetadataDto
{
    public string Name { get; set; }

    public RuleDataType DataType { get; set; }

    public bool IsOptional { get; set; }
}

/// <summary>
/// A dry run of a rule against values the author types in, without saving anything.
///
/// <para>
/// The feature that makes a rule builder usable rather than merely present. Without it the only way to find
/// out what a rule does is to arm it and save a real record, which nobody will do on production data.
/// </para>
/// </summary>
public class TestBusinessRuleInput
{
    public string BaseObject { get; set; }

    public RulePurpose Purpose { get; set; }

    [DisableValidation]
    public string DefinitionJson { get; set; }

    /// <summary>
    /// Sample values keyed as "Parameter.Field", e.g. "Employee.JoiningDate". Values are supplied as text
    /// and coerced the same way a stored constant is, so what the harness proves is what production does.
    /// </summary>
    public Dictionary<string, string> SampleValues { get; set; } = new();

    /// <summary>The date the rule should treat as today. Lets an author check a rule dated for next quarter.</summary>
    public System.DateTime? AsOfDate { get; set; }
}

public class TestBusinessRuleOutput
{
    /// <summary>True when the rule parsed, validated and ran without breaking. It may still not have matched.</summary>
    public bool Succeeded { get; set; }

    public bool ConditionMatched { get; set; }

    /// <summary>What the rule would have assigned, as "Parameter.Field = value".</summary>
    public List<string> Writes { get; set; } = new();

    /// <summary>Business errors the rule deliberately raised, localized.</summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>Ways the rule is broken: unknown field, bad function call, failed conversion.</summary>
    public List<string> Failures { get; set; } = new();

    public string Trace { get; set; }
}
