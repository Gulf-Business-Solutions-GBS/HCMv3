using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.Ess.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.GeneralMasterInfo.Dto;
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
    private readonly IRequestMasterResolver _requestMasterResolver;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<Title> _titleRepository;
    private readonly IRepository<Nationality> _nationalityRepository;
    private readonly IRepository<Religion> _religionRepository;
    private readonly IRepository<MaritalStatus> _maritalStatusRepository;
    private readonly IRepository<City> _cityRepository;
    private readonly IRepository<Governorate> _governorateRepository;
    private readonly IRepository<Country> _countryRepository;

    public MyPersonalDataChangeAppService(
        IRepository<EmployeePersonalDataChange> repository,
        IRepository<WorkflowRequest> requestRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IRequestMasterResolver requestMasterResolver,
        IWorkflowApprovalService workflowApprovalService,
        IRepository<Employee> employeeRepository,
        IRepository<Title> titleRepository,
        IRepository<Nationality> nationalityRepository,
        IRepository<Religion> religionRepository,
        IRepository<MaritalStatus> maritalStatusRepository,
        IRepository<City> cityRepository,
        IRepository<Governorate> governorateRepository,
        IRepository<Country> countryRepository)
        : base(repository, requestRepository, employeeScopeProvider, requestMasterResolver, workflowApprovalService)
    {
        _requestMasterResolver = requestMasterResolver;
        _employeeRepository = employeeRepository;
        _titleRepository = titleRepository;
        _nationalityRepository = nationalityRepository;
        _religionRepository = religionRepository;
        _maritalStatusRepository = maritalStatusRepository;
        _cityRepository = cityRepository;
        _governorateRepository = governorateRepository;
        _countryRepository = countryRepository;
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

            var requests = await GetOwnRequestsAsync(items);

            return new PagedResultDto<MyPersonalDataChangeDto>(totalCount, items.Select(i => ToDto(i, requests)).ToList());
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

    /// <remarks>
    /// Served here, under the permission the form itself needs, rather than from the administration
    /// catalogs: those sit behind permissions an employee does not hold, which is why the form used to have
    /// no pickers at all. Only id, code and the two names leave the server.
    /// </remarks>
    public async Task<PersonalDataLookupsDto> GetMyPersonalDataLookups()
    {
        return new PersonalDataLookupsDto
        {
            Titles = await LookupAsync(_titleRepository),
            Nationalities = await LookupAsync(_nationalityRepository),
            Religions = await LookupAsync(_religionRepository),
            MaritalStatuses = await LookupAsync(_maritalStatusRepository),
            Cities = await LookupAsync(_cityRepository),
            Governorates = await LookupAsync(_governorateRepository),
            Countries = await LookupAsync(_countryRepository)
        };
    }

    public async Task<MyRequestSubmissionDto> RequestPersonalDataChange(RequestPersonalDataChangeInput input)
    {
        await EnsureApprovalIsSwitchedOnAsync();

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

    public async Task<MyRequestSubmissionDto> SaveMyPersonalDataChangeDraft(SavePersonalDataChangeDraftInput input)
    {
        // Nothing is validated: a draft is what somebody saves because it is not ready. The names and the
        // birth date are checked at submit, by the handler.
        if (input.Id.HasValue)
        {
            await UpdateEditableAsync(input.Id.Value, change => Apply(input, change));

            return new MyRequestSubmissionDto
            {
                TransactionId = input.Id.Value,
                SubmittedForApproval = false,
                SavedAsDraft = true
            };
        }

        return await SaveDraftAsync(employeeId =>
        {
            var change = new EmployeePersonalDataChange
            {
                TenantId = AbpSession.TenantId ?? 0,
                EmployeeId = employeeId
            };

            Apply(input, change);

            return change;
        });
    }

    public async Task<MyPersonalDataChangeDto> GetMyPersonalDataChangeDraftForEdit(EntityDto input)
    {
        var change = await GetMyEditableAsync(input.Id);

        return ToDto(change, await GetOwnRequestsAsync(new[] { change }));
    }

    public async Task<MyRequestSubmissionDto> SubmitMyPersonalDataChangeDraft(EntityDto input)
    {
        await EnsureApprovalIsSwitchedOnAsync();

        // No sub-type, as in the one-step request. The rules are the handler's and run at submit.
        return await SubmitEditableAsync(input.Id, _ => Task.FromResult<int?>(null));
    }

    /// <summary>
    /// Refuses to send a personal data update while this type has no approval workflow.
    ///
    /// <para>
    /// For every other self-service type, workflow switched off means the row is effective as soon as it is
    /// saved, because the row IS the transaction. Here the row is only a proposal: what makes it effective
    /// is the copy onto the employee, and that happens on approval. With nobody to approve, the request
    /// would be filed as "recorded" and the personnel file would never change -- a change that appears to
    /// succeed and does nothing. Legacy refused the same way ("Request Master not found").
    /// </para>
    /// </summary>
    private async Task EnsureApprovalIsSwitchedOnAsync()
    {
        if (!await _requestMasterResolver.IsWorkflowEnabledAsync(RequestType))
        {
            throw new UserFriendlyException(L("PersonalDataUpdateNeedsApproval"));
        }
    }

    public Task CancelMyPersonalDataChange(EntityDto input) => CancelAsync(input.Id);

    /// <summary>
    /// Copies the form onto the proposal, all forty fields. A field the form no longer proposes arrives
    /// null and is written as null, so taking a change back out of a draft really removes it.
    /// </summary>
    private void Apply(RequestPersonalDataChangeInput input, EmployeePersonalDataChange change)
    {
        ObjectMapper.Map<RequestPersonalDataChangeInput, EmployeePersonalDataChange>(input, change);
    }

    private static async Task<List<SimpleLookupDto>> LookupAsync<TLookup>(IRepository<TLookup> repository)
        where TLookup : class, IEntity<int>, IBilingualLookup
    {
        // Materialised and then projected: these are short reference lists, and a projection through the
        // interface is not something to make the query translator prove it can do.
        var rows = await repository.GetAllListAsync();

        return rows
            .Select(r => new SimpleLookupDto { Id = r.Id, Code = r.Code, NameEn = r.NameEn, NameAr = r.NameAr })
            .OrderBy(r => r.NameEn)
            .ToList();
    }

    private MyPersonalDataChangeDto ToDto(
        EmployeePersonalDataChange entity,
        IReadOnlyDictionary<int, WorkflowRequestInfo> requests)
    {
        var isDraft = IsDraft(entity);
        var isSentBack = IsSentBack(entity, requests);

        return new MyPersonalDataChangeDto
        {
            Id = entity.Id,
            Proposed = ObjectMapper.Map<PersonalDataChangeValuesDto>(entity),
            Comments = entity.Comments,
            WorkflowStatus = entity.WorkflowStatus,
            WorkflowRequestId = entity.WorkflowRequestId,
            CanCancel = CanCancel(entity),
            RequestNumber = RequestNumberOf(entity, requests),
            IsDraft = isDraft,
            IsSentBack = isSentBack,
            CanEdit = isDraft || isSentBack
        };
    }
}
