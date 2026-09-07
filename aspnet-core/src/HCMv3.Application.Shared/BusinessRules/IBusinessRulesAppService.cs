using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.BusinessRules.Dto;

namespace HCMv3.BusinessRules;

/// <summary>
/// Authoring, arming and testing business rules -- the screen behind HCMv3's answer to the SuccessFactors
/// Business Rules admin tool.
///
/// <para>
/// Authoring and ARMING are separate operations behind separate permissions on purpose. Saving a rule
/// changes nothing anywhere; only an assignment makes it run. That separation is what lets an analyst draft
/// and test rules against production metadata without being able to alter production behaviour.
/// </para>
/// </summary>
public interface IBusinessRulesAppService : IApplicationService
{
    Task<PagedResultDto<BusinessRuleDto>> GetBusinessRules(GetAllBusinessRulesInput input);

    Task<GetBusinessRuleForEditOutput> GetBusinessRuleForEdit(EntityDto input);

    /// <summary>
    /// Creates or updates a rule. The definition is parsed and validated against the base object registry
    /// first, so a rule that could not run is never stored -- the authoring screen is the last place a
    /// mistake is cheap.
    /// </summary>
    Task CreateOrEditBusinessRule(CreateOrEditBusinessRuleDto input);

    Task DeleteBusinessRule(EntityDto input);

    /// <summary>Binds a rule to an event, or updates an existing binding. This is what makes a rule live.</summary>
    Task CreateOrEditAssignment(CreateOrEditBusinessRuleAssignmentDto input);

    Task DeleteAssignment(EntityDto input);

    /// <summary>Base objects, their fields, and the callable functions. Drives every picker in the builder.</summary>
    Task<RuleMetadataDto> GetRuleMetadata();

    /// <summary>
    /// Runs a rule against sample values without saving anything. Returns problems rather than throwing --
    /// reporting every fault at once is the whole value of a test harness.
    /// </summary>
    Task<TestBusinessRuleOutput> TestBusinessRule(TestBusinessRuleInput input);

    Task<PagedResultDto<BusinessRuleExecutionLogDto>> GetExecutionLogs(GetBusinessRuleExecutionLogsInput input);

    /// <summary>
    /// Creates the two worked examples -- probation end date and leave approval routing -- already armed.
    ///
    /// <para>
    /// An explicit action rather than seed data, and that is a deliberate safety decision. Seeding an armed
    /// probation rule into every tenant would silently change how their employee records are computed on
    /// the next deployment. Someone has to press this.
    /// </para>
    /// </summary>
    Task<List<string>> InstallSampleRules();
}
