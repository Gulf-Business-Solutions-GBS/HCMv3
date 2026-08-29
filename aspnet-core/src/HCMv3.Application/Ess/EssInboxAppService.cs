using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.Ess.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Ess;

/// <inheritdoc cref="IEssInboxAppService"/>
[AbpAuthorize(AppPermissions.Ess_Inbox)]
public class EssInboxAppService : HCMv3AppServiceBase, IEssInboxAppService
{
    private readonly IRepository<WorkFlowApprover> _approverRepository;
    private readonly IRepository<WorkflowRequest> _requestRepository;
    private readonly IRepository<WorkflowTransactionType> _transactionTypeRepository;
    private readonly IRepository<RequestDetail> _requestDetailRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IEmployeeScopeProvider _employeeScopeProvider;
    private readonly IRequestMasterResolver _requestMasterResolver;
    private readonly IWorkflowApprovalService _workflowApprovalService;

    public EssInboxAppService(
        IRepository<WorkFlowApprover> approverRepository,
        IRepository<WorkflowRequest> requestRepository,
        IRepository<WorkflowTransactionType> transactionTypeRepository,
        IRepository<RequestDetail> requestDetailRepository,
        IRepository<Employee> employeeRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IRequestMasterResolver requestMasterResolver,
        IWorkflowApprovalService workflowApprovalService)
    {
        _approverRepository = approverRepository;
        _requestRepository = requestRepository;
        _transactionTypeRepository = transactionTypeRepository;
        _requestDetailRepository = requestDetailRepository;
        _employeeRepository = employeeRepository;
        _employeeScopeProvider = employeeScopeProvider;
        _requestMasterResolver = requestMasterResolver;
        _workflowApprovalService = workflowApprovalService;
    }

    [HttpPost]
    public async Task<PagedResultDto<EssInboxItemDto>> GetMyInbox(GetMyInboxInput input)
    {
        input.Normalize();

        var employeeId = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();

        // One query for every transaction type at once. Nothing here touches a transaction table -- the
        // label comes from the request's own summary.
        var query = _approverRepository.GetAll()
            .Where(a => a.ApproverId == employeeId
                        && !a.IsApproved
                        && a.WorkflowRequest.RequestStatus == RequestStatus.Open)
            .Select(a => a.WorkflowRequest)
            .WhereIf(!input.RequestType.IsNullOrWhiteSpace(), r => r.RequestType == input.RequestType);

        var totalCount = await query.CountAsync();

        var requests = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        var items = await BuildItemsAsync(requests);

        return new PagedResultDto<EssInboxItemDto>(totalCount, items);
    }

    public async Task<int> GetMyPendingCount()
    {
        var employeeId = await _employeeScopeProvider.GetCurrentEmployeeIdAsync();

        // Zero rather than an error: the badge is decoration, and an unlinked user seeing "0" is correct.
        // The screens themselves explain the missing link.
        if (!employeeId.HasValue)
        {
            return 0;
        }

        return await _approverRepository.GetAll()
            .CountAsync(a => a.ApproverId == employeeId.Value
                             && !a.IsApproved
                             && a.WorkflowRequest.RequestStatus == RequestStatus.Open);
    }

    public async Task Approve(ApproveRequestInput input)
    {
        var employeeId = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();

        // The engine re-checks that this employee is actually the current approver, so authorization does
        // not rest on the inbox query having been honest about what it listed.
        await _workflowApprovalService.ApproveAsync(input.WorkflowRequestId, employeeId, input.Comment);
    }

    public async Task Reject(RejectRequestInput input)
    {
        var employeeId = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();

        await _workflowApprovalService.RejectAsync(input.WorkflowRequestId, employeeId, input.Comment);
    }

    public async Task SendBack(SendBackRequestInput input)
    {
        var employeeId = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();

        await _workflowApprovalService.SendBackAsync(input.WorkflowRequestId, employeeId, input.Comment);
    }

    /// <summary>
    /// Fills in the display fields for a page of requests, resolving requesters and type names in one query
    /// each rather than per row.
    /// </summary>
    private async Task<List<EssInboxItemDto>> BuildItemsAsync(List<WorkflowRequest> requests)
    {
        if (requests.Count == 0)
        {
            return new List<EssInboxItemDto>();
        }

        var employeeIds = requests.Where(r => r.EmployeeId.HasValue).Select(r => r.EmployeeId.Value).Distinct().ToList();
        var requestTypes = requests.Select(r => r.RequestType).Distinct().ToList();

        var employees = (await _employeeRepository.GetAll()
            .Where(e => employeeIds.Contains(e.Id))
            .Select(e => new { e.Id, e.FullNameEn, e.FullNameAr, HasPhoto = e.Photo != null })
            .ToListAsync()).ToDictionary(e => e.Id);

        var types = (await _transactionTypeRepository.GetAll()
            .Where(t => requestTypes.Contains(t.Code))
            .Select(t => new { t.Code, t.NameEn, t.NameAr })
            .ToListAsync()).ToDictionary(t => t.Code, StringComparer.OrdinalIgnoreCase);

        var items = new List<EssInboxItemDto>();

        foreach (var request in requests)
        {
            var item = new EssInboxItemDto
            {
                Id = request.Id,
                RequestType = request.RequestType,
                RequestNumber = request.RequestNumber,
                SummaryEn = request.SummaryEn,
                SummaryAr = request.SummaryAr,
                RequesterEmployeeId = request.EmployeeId,
                SubmittedOn = request.CreationTime,
                WorkflowStep = request.WorkflowStep
            };

            if (request.EmployeeId.HasValue && employees.TryGetValue(request.EmployeeId.Value, out var employee))
            {
                item.RequesterNameEn = employee.FullNameEn;
                item.RequesterNameAr = employee.FullNameAr;
                item.RequesterPhotoUrl = employee.HasPhoto ? "/api/employee-photo/" + employee.Id : null;
            }

            if (!request.RequestType.IsNullOrWhiteSpace() && types.TryGetValue(request.RequestType, out var type))
            {
                item.RequestTypeNameEn = type.NameEn;
                item.RequestTypeNameAr = type.NameAr;
            }

            await FillStepCapabilitiesAsync(item, request);

            items.Add(item);
        }

        return items;
    }

    /// <summary>
    /// Reads what this step actually permits, so the screen does not offer a Reject button the engine would
    /// refuse. A configuration that cannot be resolved leaves approve-only rather than failing the whole
    /// inbox -- one broken chain should not stop an approver seeing everything else waiting on them.
    /// </summary>
    private async Task FillStepCapabilitiesAsync(EssInboxItemDto item, WorkflowRequest request)
    {
        try
        {
            var master = await _requestMasterResolver.ResolveAsync(request);
            item.TotalSteps = master.WorkflowSteps;

            var detail = await _requestDetailRepository.FirstOrDefaultAsync(
                d => d.RequestMasterId == master.Id && d.Step == request.WorkflowStep);

            item.CanReject = detail?.CanReject ?? false;
            item.CanSendBack = detail?.CanSendBack ?? false;
        }
        catch (Abp.UI.UserFriendlyException)
        {
            item.TotalSteps = 0;
            item.CanReject = false;
            item.CanSendBack = false;
        }
    }
}
