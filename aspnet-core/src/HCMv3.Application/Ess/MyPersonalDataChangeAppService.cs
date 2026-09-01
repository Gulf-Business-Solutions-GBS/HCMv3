using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.Ess.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.PersonnelAdministration.Workflow;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Ess;

/// <inheritdoc cref="IMyPersonalDataChangeAppService"/>
///
/// <remarks>
/// <para>
/// Uses the existing <see cref="AppPermissions.Ess_MyProfile"/> rather than minting a new permission.
/// "My profile" is precisely what this screen is, the permission is already granted to the Employee role, and
/// a second one would mean every existing tenant had to go and grant it before self service worked at all.
/// </para>
/// <para>
/// The one thing this service does that no other ESS service does is pre-fill: <see cref="GetMyPersonalDataForEdit"/>
/// hands back what the personnel file says today. Without it the employee retypes forty fields to change one,
/// and every retyped field is a chance to enter something slightly different -- the screen itself would
/// manufacture the corruption this feature exists to control.
/// </para>
/// </remarks>
[AbpAuthorize(AppPermissions.Ess_MyProfile)]
public class MyPersonalDataChangeAppService : MyTransactionAppServiceBase<EmployeePersonalDataChange>, IMyPersonalDataChangeAppService
{
    private readonly IRepository<Employee> _employeeRepository;

    public MyPersonalDataChangeAppService(
        IRepository<EmployeePersonalDataChange> repository,
        IRepository<WorkflowRequest> requestRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IRequestMasterResolver requestMasterResolver,
        IWorkflowApprovalService workflowApprovalService,
        IRepository<Employee> employeeRepository)
        : base(repository, requestRepository, employeeScopeProvider, requestMasterResolver, workflowApprovalService)
    {
        _employeeRepository = employeeRepository;
    }

    protected override string RequestType => PersonalDataChangeWorkflowTransactionHandler.PersonalDataChangeRequestType;

    protected override string RequestNumberPrefix => "PDU";

    [HttpPost]
    public async Task<PagedResultDto<MyPersonalDataChangeDto>> GetMyPersonalDataChanges(GetMyRequestsInput input)
    {
        input.Normalize();

        var employeeId = await GetMyEmployeeIdAsync();

        // Disabled so the employee can see their own undecided proposals -- which is the main reason they
        // opened the page. The query stays scoped to them by employee id, which is the check that matters.
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            // "Not history" here means still undecided. A decided profile change is not something the
            // employee needs on the front page: whatever it did is now simply what their file says.
            var query = MyRowsQuery(employeeId)
                .WhereIf(!input.IncludeHistory,
                    c => c.WorkflowStatus == null
                         || c.WorkflowStatus == RequestStatus.Draft
                         || c.WorkflowStatus == RequestStatus.Open);

            var totalCount = await query.CountAsync();

            var items = await query.OrderBy(input.Sorting).PageBy(input).ToListAsync();

            return new PagedResultDto<MyPersonalDataChangeDto>(totalCount, items.Select(ToDto).ToList());
        }
    }

    public async Task<MyPersonalDataForEditDto> GetMyPersonalDataForEdit()
    {
        // No identifier is taken, and none should be: the row this returns is decided by who is signed in.
        // Accepting an employee id here would be an endpoint for reading anybody's personnel file.
        var employeeId = await GetMyEmployeeIdAsync();

        var employee = await _employeeRepository.FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee == null)
        {
            throw new UserFriendlyException(L("EmployeeNotFound"));
        }

        bool hasPending;
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            hasPending = await MyRowsQuery(employeeId)
                .AnyAsync(c => c.WorkflowStatus == RequestStatus.Draft || c.WorkflowStatus == RequestStatus.Open);
        }

        return new MyPersonalDataForEditDto
        {
            EmployeeId = employeeId,
            Current = ObjectMapper.Map<PersonalDataChangeValuesDto>(employee),
            HasPendingRequest = hasPending
        };
    }

    public async Task<MyRequestSubmissionDto> RequestPersonalDataChange(RequestPersonalDataChangeInput input)
    {
        var entity = ObjectMapper.Map<EmployeePersonalDataChange>(input);

        entity.TenantId = AbpSession.TenantId ?? 0;

        // No sub-type. Legacy routed every personal data update the same way regardless of what was being
        // changed, and inventing one here would need a catalog of field groups that nothing else uses.
        return await CreateAndSubmitAsync(
            meId =>
            {
                entity.EmployeeId = meId;
                return entity;
            },
            subTypeId: null);
    }

    public Task CancelMyPersonalDataChange(EntityDto input) => CancelAsync(input.Id);

    private MyPersonalDataChangeDto ToDto(EmployeePersonalDataChange entity)
    {
        return new MyPersonalDataChangeDto
        {
            Id = entity.Id,
            Proposed = ObjectMapper.Map<PersonalDataChangeValuesDto>(entity),
            Comments = entity.Comments,
            WorkflowStatus = entity.WorkflowStatus,
            WorkflowRequestId = entity.WorkflowRequestId,
            CanCancel = CanCancel(entity)
        };
    }
}
