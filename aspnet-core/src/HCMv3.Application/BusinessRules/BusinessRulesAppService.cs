using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Reflection;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.Runtime.Session;
using Abp.Timing;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.BusinessRules.Definition;
using HCMv3.BusinessRules.Dto;
using HCMv3.BusinessRules.Seed;
using HCMv3.BusinessRules.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.BusinessRules;

/// <inheritdoc cref="IBusinessRulesAppService"/>
[AbpAuthorize(AppPermissions.Pages_BusinessRules)]
public class BusinessRulesAppService : HCMv3AppServiceBase, IBusinessRulesAppService
{
    private readonly IRepository<BusinessRule> _ruleRepository;
    private readonly IRepository<BusinessRuleAssignment> _assignmentRepository;
    private readonly IRepository<BusinessRuleExecutionLog, long> _logRepository;
    private readonly IRuleBaseObjectRegistry _baseObjectRegistry;
    private readonly IRuleFunctionRegistry _functionRegistry;
    private readonly IRuleDefinitionValidator _validator;
    private readonly IBusinessRuleRunner _runner;
    private readonly IBusinessRuleCache _cache;
    private readonly IRepository<PayrollManagement.AbsenceType> _absenceTypeRepository;

    public BusinessRulesAppService(
        IRepository<BusinessRule> ruleRepository,
        IRepository<BusinessRuleAssignment> assignmentRepository,
        IRepository<BusinessRuleExecutionLog, long> logRepository,
        IRuleBaseObjectRegistry baseObjectRegistry,
        IRuleFunctionRegistry functionRegistry,
        IRuleDefinitionValidator validator,
        IBusinessRuleRunner runner,
        IBusinessRuleCache cache,
        IRepository<PayrollManagement.AbsenceType> absenceTypeRepository)
    {
        _ruleRepository = ruleRepository;
        _assignmentRepository = assignmentRepository;
        _logRepository = logRepository;
        _baseObjectRegistry = baseObjectRegistry;
        _functionRegistry = functionRegistry;
        _validator = validator;
        _runner = runner;
        _cache = cache;
        _absenceTypeRepository = absenceTypeRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<BusinessRuleDto>> GetBusinessRules(GetAllBusinessRulesInput input)
    {
        input.Normalize();

        var filter = input.Filter;

        var query = _ruleRepository.GetAll()
            .WhereIf(!filter.IsNullOrWhiteSpace(), r =>
                r.Code.Contains(filter) || r.NameEn.Contains(filter) || r.NameAr.Contains(filter))
            .WhereIf(!input.BaseObject.IsNullOrWhiteSpace(), r => r.BaseObject == input.BaseObject)
            .WhereIf(input.Purpose.HasValue, r => r.Purpose == input.Purpose.Value)
            .WhereIf(input.IsActive.HasValue, r => r.IsActive == input.IsActive.Value)

            // Effective dating, inclusive at both ends with a null end meaning open -- the same convention
            // the runner applies, so the list cannot show a different answer from what actually runs.
            .WhereIf(input.AsOfDate.HasValue, r =>
                r.EffectiveFrom <= input.AsOfDate.Value
                && (r.EffectiveTo == null || r.EffectiveTo >= input.AsOfDate.Value));

        if (input.EventType.HasValue)
        {
            var eventType = input.EventType.Value;

            query = query.Where(r => _assignmentRepository.GetAll()
                .Any(a => a.BusinessRuleId == r.Id && a.EventType == eventType && a.IsActive));
        }

        var totalCount = await query.CountAsync();

        var rules = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        var ruleIds = rules.Select(r => r.Id).ToList();

        // Counted in one query rather than per row: the "Assigned" column is the reason this screen exists,
        // so it must not cost a query per rule.
        var assignmentCounts = await _assignmentRepository.GetAll()
            .Where(a => ruleIds.Contains(a.BusinessRuleId) && a.IsActive)
            .GroupBy(a => a.BusinessRuleId)
            .Select(g => new { BusinessRuleId = g.Key, Count = g.Count() })
            .ToListAsync();

        var items = rules.Select(rule =>
        {
            var dto = ToDto(rule);

            dto.ActiveAssignmentCount = assignmentCounts
                .FirstOrDefault(c => c.BusinessRuleId == rule.Id)?.Count ?? 0;

            return dto;
        }).ToList();

        return new PagedResultDto<BusinessRuleDto>(totalCount, items);
    }

    public async Task<GetBusinessRuleForEditOutput> GetBusinessRuleForEdit(EntityDto input)
    {
        var rule = await _ruleRepository.GetAsync(input.Id);

        var assignments = await _assignmentRepository.GetAll()
            .Where(a => a.BusinessRuleId == rule.Id)
            .OrderBy(a => a.EventType)
            .ThenBy(a => a.ExecutionOrder)
            .ToListAsync();

        return new GetBusinessRuleForEditOutput
        {
            Rule = new CreateOrEditBusinessRuleDto
            {
                Id = rule.Id,
                Code = rule.Code,
                NameEn = rule.NameEn,
                NameAr = rule.NameAr,
                Description = rule.Description,
                BaseObject = rule.BaseObject,
                Purpose = rule.Purpose,
                DefinitionJson = rule.DefinitionJson,
                EffectiveFrom = rule.EffectiveFrom,
                EffectiveTo = rule.EffectiveTo,
                IsActive = rule.IsActive
            },
            Assignments = assignments.Select(a => ToDto(a, rule)).ToList()
        };
    }

    [AbpAuthorize(AppPermissions.Pages_BusinessRules_Create, AppPermissions.Pages_BusinessRules_Edit)]
    public async Task CreateOrEditBusinessRule(CreateOrEditBusinessRuleDto input)
    {
        var baseObject = _baseObjectRegistry.Find(input.BaseObject)
                         ?? throw new UserFriendlyException(L("BaseObject") + ": " + input.BaseObject);

        // Parsed AND semantically validated before anything is stored. A definition that cannot run must
        // never reach the database, because the next thing that reads it is somebody's save.
        var definition = ParseOrThrow(input.DefinitionJson);

        var errors = _validator.Validate(definition, baseObject, input.Purpose);

        if (errors.Count > 0)
        {
            throw new UserFriendlyException(string.Join(" ", errors));
        }

        var duplicateCode = await _ruleRepository.GetAll()
            .AnyAsync(r => r.Code == input.Code && (input.Id == null || r.Id != input.Id.Value));

        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        if (input.EffectiveTo.HasValue && input.EffectiveTo.Value.Date < input.EffectiveFrom.Date)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        if (input.Id.HasValue)
        {
            var rule = await _ruleRepository.GetAsync(input.Id.Value);

            if (rule.BaseObject != input.BaseObject)
            {
                // Changing the base object would leave every assignment pointing at the wrong object and
                // every field reference dangling. Deleting and recreating makes that consequence visible.
                throw new UserFriendlyException(L("BaseObjectCannotBeChanged"));
            }

            rule.Code = input.Code;
            rule.NameEn = input.NameEn;
            rule.NameAr = input.NameAr;
            rule.Description = input.Description;
            rule.Purpose = input.Purpose;
            rule.DefinitionJson = input.DefinitionJson;
            rule.EffectiveFrom = input.EffectiveFrom.Date;
            rule.EffectiveTo = input.EffectiveTo?.Date;
            rule.IsActive = input.IsActive;
        }
        else
        {
            await _ruleRepository.InsertAsync(new BusinessRule
            {
                TenantId = AbpSession.GetTenantId(),
                Code = input.Code,
                NameEn = input.NameEn,
                NameAr = input.NameAr,
                Description = input.Description,
                BaseObject = input.BaseObject,
                Purpose = input.Purpose,
                DefinitionJson = input.DefinitionJson,
                EffectiveFrom = input.EffectiveFrom.Date,
                EffectiveTo = input.EffectiveTo?.Date,
                IsActive = input.IsActive
            });
        }

        await CurrentUnitOfWork.SaveChangesAsync();
        await _cache.InvalidateAsync();
    }

    [AbpAuthorize(AppPermissions.Pages_BusinessRules_Delete)]
    public async Task DeleteBusinessRule(EntityDto input)
    {
        await _ruleRepository.DeleteAsync(input.Id);
        await CurrentUnitOfWork.SaveChangesAsync();
        await _cache.InvalidateAsync();
    }

    [AbpAuthorize(AppPermissions.Pages_BusinessRules_Assign)]
    public async Task CreateOrEditAssignment(CreateOrEditBusinessRuleAssignmentDto input)
    {
        var rule = await _ruleRepository.GetAsync(input.BusinessRuleId);

        var baseObject = _baseObjectRegistry.Find(rule.BaseObject)
                         ?? throw new UserFriendlyException(L("BaseObject") + ": " + rule.BaseObject);

        // A rule can only be armed on an event its base object declares. Allowing anything else would let
        // an administrator arm a routing rule on BeforeSave, where its Routing parameter does not exist and
        // it would fail on every save.
        if (!baseObject.Supports(input.EventType))
        {
            throw new UserFriendlyException(L("EventNotSupportedByBaseObject", input.EventType.ToString(), rule.BaseObject));
        }

        // Re-validated at ARM time, not only at authoring time. A rule saved months ago may reference a
        // field that has since been removed from the descriptor, and arming is the moment that matters.
        var errors = _validator.Validate(ParseOrThrow(rule.DefinitionJson), baseObject, rule.Purpose);

        if (errors.Count > 0)
        {
            throw new UserFriendlyException(string.Join(" ", errors));
        }

        if (input.Id.HasValue)
        {
            var assignment = await _assignmentRepository.GetAsync(input.Id.Value);

            assignment.EventType = input.EventType;
            assignment.ExecutionOrder = input.ExecutionOrder;
            assignment.IsActive = input.IsActive;
            assignment.ContinueOnError = input.ContinueOnError;
            assignment.IsTraced = input.IsTraced;
        }
        else
        {
            var duplicate = await _assignmentRepository.GetAll()
                .AnyAsync(a => a.BusinessRuleId == rule.Id && a.EventType == input.EventType);

            if (duplicate)
            {
                throw new UserFriendlyException(L("RuleIsAlreadyAssignedToThisEvent"));
            }

            await _assignmentRepository.InsertAsync(new BusinessRuleAssignment
            {
                TenantId = AbpSession.GetTenantId(),
                BusinessRuleId = rule.Id,

                // Denormalised from the rule, never taken from the client -- it is the runner's index key,
                // and a client-supplied value could bind a rule to an object it was not written against.
                BaseObject = rule.BaseObject,
                EventType = input.EventType,
                ExecutionOrder = input.ExecutionOrder,
                IsActive = input.IsActive,
                ContinueOnError = input.ContinueOnError,
                IsTraced = input.IsTraced
            });
        }

        await CurrentUnitOfWork.SaveChangesAsync();
        await _cache.InvalidateAsync();
    }

    [AbpAuthorize(AppPermissions.Pages_BusinessRules_Assign)]
    public async Task DeleteAssignment(EntityDto input)
    {
        await _assignmentRepository.DeleteAsync(input.Id);
        await CurrentUnitOfWork.SaveChangesAsync();
        await _cache.InvalidateAsync();
    }

    public Task<RuleMetadataDto> GetRuleMetadata()
    {
        var metadata = new RuleMetadataDto
        {
            BaseObjects = _baseObjectRegistry.GetAll().Select(b => new RuleBaseObjectMetadataDto
            {
                Code = b.Code,
                DisplayName = L(b.DisplayNameKey),
                SupportedEvents = b.SupportedEvents.ToList(),
                Parameters = b.Parameters.Select(p => new RuleParameterMetadataDto
                {
                    Name = p.Name,
                    DisplayName = L(p.DisplayNameKey),
                    IsWritable = p.IsWritable,
                    Fields = p.Fields.Select(f => new RuleFieldMetadataDto
                    {
                        Name = f.Name,
                        DisplayName = L(f.DisplayNameKey),
                        DataType = f.DataType,

                        // Both gates, resolved here so the builder never offers an assignment target the
                        // evaluator would refuse.
                        IsWritable = p.IsWritable && f.IsWritable,
                        LookupSource = f.LookupSource
                    }).ToList()
                }).ToList()
            }).ToList(),

            Functions = _functionRegistry.GetAll().Select(f => new RuleFunctionMetadataDto
            {
                Name = f.Name,
                DisplayName = L(f.DisplayNameKey),
                ReturnType = f.ReturnType,
                Parameters = f.Parameters.Select(p => new RuleFunctionParameterMetadataDto
                {
                    Name = p.Name,
                    DataType = p.DataType,
                    IsOptional = p.IsOptional
                }).ToList()
            }).ToList()
        };

        return Task.FromResult(metadata);
    }

    [AbpAuthorize(AppPermissions.Pages_BusinessRules_Test)]
    public async Task<TestBusinessRuleOutput> TestBusinessRule(TestBusinessRuleInput input)
    {
        var baseObject = _baseObjectRegistry.Find(input.BaseObject);

        if (baseObject == null)
        {
            return new TestBusinessRuleOutput
            {
                Failures = { L("BaseObject") + ": " + input.BaseObject }
            };
        }

        var context = new RuleExecutionContext(
            baseObject,
            RuleEventType.OnValidate,
            input.AsOfDate?.Date ?? Clock.Now.Date,
            AbpSession.TenantId);

        foreach (var parameter in baseObject.Parameters)
        {
            context.WithParameter(parameter.Name, BuildSampleParameter(parameter, input.SampleValues));
        }

        var result = await _runner.TestAsync(input.DefinitionJson, input.BaseObject, input.Purpose, context);

        return new TestBusinessRuleOutput
        {
            Succeeded = !result.HasFailures,
            ConditionMatched = result.RulesMatched > 0,
            Writes = result.Writes.Select(w => w.ToString()).ToList(),
            Errors = result.Errors,
            Failures = result.Failures,
            Trace = result.Trace
        };
    }

    [AbpAuthorize(AppPermissions.Pages_BusinessRules_ExecutionLogs)]
    [HttpPost]
    public async Task<PagedResultDto<BusinessRuleExecutionLogDto>> GetExecutionLogs(
        GetBusinessRuleExecutionLogsInput input)
    {
        input.Normalize();

        var query = _logRepository.GetAll()
            .WhereIf(input.BusinessRuleId.HasValue, l => l.BusinessRuleId == input.BusinessRuleId.Value)
            .WhereIf(input.Succeeded.HasValue, l => l.Succeeded == input.Succeeded.Value)
            .WhereIf(input.FromDate.HasValue, l => l.ExecutedAt >= input.FromDate.Value)
            .WhereIf(input.ToDate.HasValue, l => l.ExecutedAt <= input.ToDate.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<BusinessRuleExecutionLogDto>(
            totalCount,
            items.Select(l => new BusinessRuleExecutionLogDto
            {
                Id = l.Id,
                BusinessRuleId = l.BusinessRuleId,
                RuleCode = l.RuleCode,
                BaseObject = l.BaseObject,
                EventType = l.EventType,
                TargetEntityId = l.TargetEntityId,
                Succeeded = l.Succeeded,
                ConditionMatched = l.ConditionMatched,
                Message = l.Message,
                DurationMs = l.DurationMs,
                ExecutedAt = l.ExecutedAt
            }).ToList());
    }

    [AbpAuthorize(AppPermissions.Pages_BusinessRules_Create, AppPermissions.Pages_BusinessRules_Assign)]
    public async Task<List<string>> InstallSampleRules()
    {
        var created = new List<string>();

        await InstallSampleRuleAsync(
            SampleBusinessRuleDefinitions.ProbationEndDateRuleCode,
            "Probation end date",
            "تاريخ انتهاء فترة الاختبار",
            PersonnelAdministration.BusinessRules.EmployeeRuleBaseObjectProvider.BaseObjectCode,
            RulePurpose.Evaluate,
            RuleEventType.BeforeSave,
            SampleBusinessRuleDefinitions.ProbationEndDate(),
            created);

        await InstallSampleRuleAsync(
            SampleBusinessRuleDefinitions.LeaveApprovalRoutingRuleCode,
            "Leave approval routing",
            "توجيه اعتماد الإجازات",
            Workflow.BusinessRules.WorkflowRoutingRuleBaseObjectProvider.BaseObjectCode,
            RulePurpose.Route,
            RuleEventType.OnRoute,
            SampleBusinessRuleDefinitions.LeaveApprovalRouting(),
            created);

        // Resolved rather than hardcoded: absence type ids differ per tenant, so a literal would produce a
        // rule that silently matches nothing. If this tenant has no maternity type the rule is skipped
        // entirely -- installing a validation that can never fire would be worse than installing nothing.
        var maternityAbsenceTypeId = await _absenceTypeRepository.GetAll()
            .Where(t => t.IsActive
                        && t.NameEn.Contains(SampleBusinessRuleDefinitions.MaternityAbsenceTypeNameFragment))
            .OrderBy(t => t.Id)
            .Select(t => (int?)t.Id)
            .FirstOrDefaultAsync();

        if (maternityAbsenceTypeId.HasValue)
        {
            await InstallSampleRuleAsync(
                SampleBusinessRuleDefinitions.MaternityLeaveGenderRuleCode,
                "Maternity leave is female-only",
                "إجازة الوضع للإناث فقط",
                PersonnelAdministration.BusinessRules.EmployeeAbsenceRuleBaseObjectProvider.BaseObjectCode,
                RulePurpose.Validate,
                RuleEventType.BeforeSave,
                SampleBusinessRuleDefinitions.MaternityLeaveGenderCheck(maternityAbsenceTypeId.Value),
                created);
        }

        await CurrentUnitOfWork.SaveChangesAsync();
        await _cache.InvalidateAsync();

        return created;
    }

    private async Task InstallSampleRuleAsync(
        string code,
        string nameEn,
        string nameAr,
        string baseObjectCode,
        RulePurpose purpose,
        RuleEventType eventType,
        RuleDefinition definition,
        List<string> created)
    {
        // Idempotent: pressing the button twice must not produce a second copy of a rule that is already
        // armed, because two copies of a derivation both firing is very hard to diagnose from the outside.
        if (await _ruleRepository.GetAll().AnyAsync(r => r.Code == code))
        {
            return;
        }

        var rule = new BusinessRule
        {
            TenantId = AbpSession.GetTenantId(),
            Code = code,
            NameEn = nameEn,
            NameAr = nameAr,
            BaseObject = baseObjectCode,
            Purpose = purpose,
            DefinitionJson = RuleDefinitionSerializer.Serialize(definition),
            EffectiveFrom = new DateTime(1900, 1, 1),
            IsActive = true
        };

        rule.Id = await _ruleRepository.InsertAndGetIdAsync(rule);

        await _assignmentRepository.InsertAsync(new BusinessRuleAssignment
        {
            TenantId = AbpSession.GetTenantId(),
            BusinessRuleId = rule.Id,
            BaseObject = baseObjectCode,
            EventType = eventType,
            ExecutionOrder = 1,
            IsActive = true
        });

        created.Add(code);
    }

    /// <summary>
    /// Builds a throwaway instance of a parameter's type and seeds the sample values onto it.
    ///
    /// <para>
    /// Reflection, uniquely in this engine, and confined to exactly this method. The whitelist descriptors
    /// carry a getter and (for writable fields) a setter, but the test harness needs to seed READ-ONLY
    /// fields too -- a rule that branches on the joining date cannot be tested if the joining date cannot be
    /// set. The alternative was to make every descriptor declare a second, test-only setter, which is a lot
    /// of ceremony to make a dry run work.
    /// </para>
    ///
    /// <para>
    /// It is safe because of what it is scoped to: field names that already appear in the whitelist, on an
    /// object constructed here and discarded when the request ends. Nothing reachable this way is persisted,
    /// and a field name that does not match a settable property is skipped rather than guessed at.
    /// </para>
    /// </summary>
    private static object BuildSampleParameter(
        RuleParameterDescriptor parameter,
        IReadOnlyDictionary<string, string> sampleValues)
    {
        object instance;

        try
        {
            instance = Activator.CreateInstance(parameter.ClrType);
        }
        catch (Exception)
        {
            // No parameterless constructor. The rule can still be tested; reads of this parameter yield
            // null, which the evaluator treats as "not filled in".
            return null;
        }

        if (instance == null || sampleValues == null)
        {
            return instance;
        }

        foreach (var field in parameter.Fields)
        {
            if (!sampleValues.TryGetValue(parameter.Name + "." + field.Name, out var raw))
            {
                continue;
            }

            var property = parameter.ClrType.GetProperty(
                field.Name,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (property == null || !property.CanWrite)
            {
                continue;
            }

            try
            {
                var value = RuleValueConverter.ParseLiteral(raw, field.DataType);

                property.SetValue(instance, RuleValueConverter.ConvertTo(value, property.PropertyType));
            }
            catch (Exception)
            {
                // A sample value that will not convert is the author's typo, not a server error. Leaving
                // the field at its default and letting the rule run tells them more than a 500 would.
            }
        }

        return instance;
    }

    private static RuleDefinition ParseOrThrow(string definitionJson)
    {
        try
        {
            return RuleDefinitionSerializer.Parse(definitionJson);
        }
        catch (RuleDefinitionException ex)
        {
            throw new UserFriendlyException(ex.Message);
        }
    }

    private BusinessRuleDto ToDto(BusinessRule rule)
    {
        var baseObject = _baseObjectRegistry.Find(rule.BaseObject);

        var dto = new BusinessRuleDto
        {
            Id = rule.Id,
            Code = rule.Code,
            NameEn = rule.NameEn,
            NameAr = rule.NameAr,
            Description = rule.Description,
            BaseObject = rule.BaseObject,
            BaseObjectDisplayName = baseObject == null ? rule.BaseObject : L(baseObject.DisplayNameKey),
            Purpose = rule.Purpose,
            EffectiveFrom = rule.EffectiveFrom,
            EffectiveTo = rule.EffectiveTo,
            IsActive = rule.IsActive
        };

        try
        {
            RuleDefinitionSerializer.Parse(rule.DefinitionJson);
        }
        catch (RuleDefinitionException)
        {
            // Flagged, not thrown. One unparseable rule must not break the whole list -- the list is
            // precisely where an administrator would go to find and fix it.
            dto.HasDefinitionError = true;
        }

        return dto;
    }

    private static BusinessRuleAssignmentDto ToDto(BusinessRuleAssignment assignment, BusinessRule rule)
    {
        return new BusinessRuleAssignmentDto
        {
            Id = assignment.Id,
            BusinessRuleId = assignment.BusinessRuleId,
            BaseObject = assignment.BaseObject,
            EventType = assignment.EventType,
            ExecutionOrder = assignment.ExecutionOrder,
            IsActive = assignment.IsActive,
            ContinueOnError = assignment.ContinueOnError,
            IsTraced = assignment.IsTraced,
            RuleCode = rule?.Code,
            RuleName = rule?.NameEn
        };
    }
}
