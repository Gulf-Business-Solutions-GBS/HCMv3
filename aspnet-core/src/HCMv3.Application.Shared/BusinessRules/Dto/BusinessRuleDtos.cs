using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Runtime.Validation;

namespace HCMv3.BusinessRules.Dto;

/// <summary>A rule as the list screen shows it.</summary>
public class BusinessRuleDto : EntityDto
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public string Description { get; set; }

    public string BaseObject { get; set; }

    /// <summary>Resolved display name for the base object, so the grid does not show a raw code.</summary>
    public string BaseObjectDisplayName { get; set; }

    public RulePurpose Purpose { get; set; }

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public bool IsActive { get; set; }

    /// <summary>
    /// How many live bindings this rule has. The "Assigned" column: zero means the rule exists but does
    /// nothing, which is the single most useful thing the list can tell an administrator.
    /// </summary>
    public int ActiveAssignmentCount { get; set; }

    /// <summary>
    /// True when the stored definition no longer parses -- typically because it references a field that has
    /// since been removed from the base object. Surfaced in the list so a broken rule is visible before it
    /// fails inside somebody's save.
    /// </summary>
    public bool HasDefinitionError { get; set; }
}

/// <summary>The rule plus its definition and bindings, for the editor.</summary>
public class GetBusinessRuleForEditOutput
{
    public CreateOrEditBusinessRuleDto Rule { get; set; }

    public List<BusinessRuleAssignmentDto> Assignments { get; set; } = new();
}

public class CreateOrEditBusinessRuleDto
{
    public int? Id { get; set; }

    [Required]
    [StringLength(BusinessRuleConsts.MaxCodeLength)]
    public string Code { get; set; }

    [Required]
    [StringLength(BusinessRuleConsts.MaxNameLength)]
    public string NameEn { get; set; }

    [StringLength(BusinessRuleConsts.MaxNameLength)]
    public string NameAr { get; set; }

    [StringLength(BusinessRuleConsts.MaxDescriptionLength)]
    public string Description { get; set; }

    [Required]
    [StringLength(BusinessRuleConsts.MaxBaseObjectLength)]
    public string BaseObject { get; set; }

    public RulePurpose Purpose { get; set; }

    /// <summary>
    /// The authored tree. Validated on save against the parser AND the registries, so a rule that cannot
    /// run is never stored.
    /// </summary>
    [Required]
    [StringLength(BusinessRuleConsts.MaxDefinitionLength)]
    [DisableValidation]
    public string DefinitionJson { get; set; }

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public bool IsActive { get; set; }
}

/// <summary>One binding of a rule to a base object and event.</summary>
public class BusinessRuleAssignmentDto : EntityDto
{
    public int BusinessRuleId { get; set; }

    public string BaseObject { get; set; }

    public RuleEventType EventType { get; set; }

    public int ExecutionOrder { get; set; }

    public bool IsActive { get; set; }

    public bool ContinueOnError { get; set; }

    public bool IsTraced { get; set; }

    /// <summary>Rule code and name, so the assignment list on a base object reads without a second call.</summary>
    public string RuleCode { get; set; }

    public string RuleName { get; set; }
}

public class CreateOrEditBusinessRuleAssignmentDto
{
    public int? Id { get; set; }

    public int BusinessRuleId { get; set; }

    public RuleEventType EventType { get; set; }

    public int ExecutionOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public bool ContinueOnError { get; set; }

    public bool IsTraced { get; set; }
}

/// <summary>
/// Filters for the rule list, mirroring the SuccessFactors admin screen: a text search, the base object,
/// and an as-of date that hides rules not effective then.
/// </summary>
public class GetAllBusinessRulesInput : PagedAndSortedResultRequestDto
{
    public string Filter { get; set; }

    public string BaseObject { get; set; }

    public RuleEventType? EventType { get; set; }

    public RulePurpose? Purpose { get; set; }

    public bool? IsActive { get; set; }

    /// <summary>
    /// Shows only rules effective on this date. Null means every rule regardless of dating -- the right
    /// default for an admin screen, where a rule that starts next month still needs to be findable.
    /// </summary>
    public DateTime? AsOfDate { get; set; }

    /// <summary>
    /// Applies a stable sort and a sane page size. Callers post arbitrary input; an unsorted paged query
    /// returns overlapping pages, which looks like data loss to whoever is reading the grid.
    /// </summary>
    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(Sorting))
        {
            Sorting = "Code";
        }

        if (MaxResultCount <= 0)
        {
            MaxResultCount = 20;
        }
    }
}

public class BusinessRuleExecutionLogDto : EntityDto<long>
{
    public int BusinessRuleId { get; set; }

    public string RuleCode { get; set; }

    public string BaseObject { get; set; }

    public RuleEventType EventType { get; set; }

    public int? TargetEntityId { get; set; }

    public bool Succeeded { get; set; }

    public bool ConditionMatched { get; set; }

    public string Message { get; set; }

    public int DurationMs { get; set; }

    public DateTime ExecutedAt { get; set; }
}

public class GetBusinessRuleExecutionLogsInput : PagedAndSortedResultRequestDto
{
    public int? BusinessRuleId { get; set; }

    public bool? Succeeded { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(Sorting))
        {
            Sorting = "ExecutedAt DESC";
        }

        if (MaxResultCount <= 0)
        {
            MaxResultCount = 20;
        }
    }
}
