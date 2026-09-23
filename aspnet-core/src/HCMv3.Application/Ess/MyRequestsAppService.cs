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
using HCMv3.Workflow;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Ess;

/// <inheritdoc cref="IMyRequestsAppService"/>
[AbpAuthorize(AppPermissions.Ess_MyRequests)]
public class MyRequestsAppService : HCMv3AppServiceBase, IMyRequestsAppService
{
    private readonly IRepository<WorkflowRequest> _requestRepository;
    private readonly IRepository<WorkflowTransactionType> _transactionTypeRepository;
    private readonly PersonnelAdministration.Services.IEmployeeScopeProvider _employeeScopeProvider;

    public MyRequestsAppService(
        IRepository<WorkflowRequest> requestRepository,
        IRepository<WorkflowTransactionType> transactionTypeRepository,
        PersonnelAdministration.Services.IEmployeeScopeProvider employeeScopeProvider)
    {
        _requestRepository = requestRepository;
        _transactionTypeRepository = transactionTypeRepository;
        _employeeScopeProvider = employeeScopeProvider;
    }

    /// <summary>
    /// [HttpPost] because ABP would otherwise map a <c>Get*</c> method to HTTP GET, and NSwag would then
    /// flatten <see cref="GetMyRequestListInput"/> into query-string parameters and never emit the class --
    /// breaking the Angular client silently. The rule CLAUDE.md states for exactly this shape of method.
    /// </summary>
    [HttpPost]
    public async Task<PagedResultDto<MyRequestListItemDto>> GetMyRequests(GetMyRequestListInput input)
    {
        input.Normalize();

        var employeeId = await _employeeScopeProvider.GetCurrentEmployeeIdAsync();

        // Empty, not an error. A tenant admin is a real user with no employee record of their own (see the
        // project's standing "admin stays unlinked" rule); opening the screen should tell them there is
        // nothing to show, not fail.
        if (!employeeId.HasValue)
        {
            return new PagedResultDto<MyRequestListItemDto>(0, new List<MyRequestListItemDto>());
        }

        var query = BuildFilteredQuery(employeeId.Value, input);

        var totalCount = await query.CountAsync();

        var requests = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        var items = await BuildItemsAsync(requests);

        return new PagedResultDto<MyRequestListItemDto>(totalCount, items);
    }

    public async Task<List<MyRequestStatusCountDto>> GetMyRequestStatusCounts()
    {
        var employeeId = await _employeeScopeProvider.GetCurrentEmployeeIdAsync();

        if (!employeeId.HasValue)
        {
            return new List<MyRequestStatusCountDto>();
        }

        var counts = await MyRequestsQuery(employeeId.Value)
            .GroupBy(r => r.RequestStatus)
            .Select(g => new MyRequestStatusCountDto { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        return counts.OrderBy(c => c.Status).ToList();
    }

    public async Task<List<MyRequestTypeFilterDto>> GetMyRequestTypes()
    {
        var employeeId = await _employeeScopeProvider.GetCurrentEmployeeIdAsync();

        if (!employeeId.HasValue)
        {
            return new List<MyRequestTypeFilterDto>();
        }

        var used = await MyRequestsQuery(employeeId.Value)
            .Where(r => r.RequestType != null)
            .GroupBy(r => r.RequestType)
            .Select(g => new { RequestType = g.Key, Count = g.Count() })
            .ToListAsync();

        if (used.Count == 0)
        {
            return new List<MyRequestTypeFilterDto>();
        }

        var codes = used.Select(u => u.RequestType).ToList();

        var types = (await _transactionTypeRepository.GetAll()
                .Where(t => codes.Contains(t.Code))
                .Select(t => new { t.Code, t.NameEn, t.NameAr })
                .ToListAsync())
            .ToDictionary(t => t.Code, StringComparer.OrdinalIgnoreCase);

        return used
            .Select(u =>
            {
                types.TryGetValue(u.RequestType, out var type);

                return new MyRequestTypeFilterDto
                {
                    RequestType = u.RequestType,
                    // Falling back to the raw code keeps a type with no catalog row usable as a filter
                    // instead of showing an unpickable blank entry.
                    NameEn = type?.NameEn ?? u.RequestType,
                    NameAr = type?.NameAr ?? u.RequestType,
                    Count = u.Count
                };
            })
            .OrderBy(t => t.NameEn)
            .ToList();
    }

    /// <summary>
    /// The employee's own requests, and nothing else.
    ///
    /// <para>
    /// The employee id comes from the session via <c>IEmployeeScopeProvider</c> and never from the client.
    /// An id accepted as input here would turn the one screen that lists every request type into the one
    /// endpoint that leaks every request type.
    /// </para>
    /// </summary>
    private IQueryable<WorkflowRequest> MyRequestsQuery(int employeeId)
    {
        return _requestRepository.GetAll().Where(r => r.EmployeeId == employeeId);
    }

    private IQueryable<WorkflowRequest> BuildFilteredQuery(int employeeId, GetMyRequestListInput input)
    {
        var filter = input.Filter?.Trim();

        var query = MyRequestsQuery(employeeId)
            .WhereIf(input.Status.HasValue, r => r.RequestStatus == input.Status.Value)
            .WhereIf(!input.RequestType.IsNullOrWhiteSpace(), r => r.RequestType == input.RequestType)
            .WhereIf(input.FromDate.HasValue, r => r.CreationTime >= input.FromDate.Value.Date)
            // The whole of ToDate counts. Comparing against the bare date would exclude everything raised
            // after midnight on the last day of the range, which is every request raised that day.
            .WhereIf(input.ToDate.HasValue, r => r.CreationTime < input.ToDate.Value.Date.AddDays(1))
            .WhereIf(!filter.IsNullOrWhiteSpace(), r =>
                (r.RequestNumber != null && r.RequestNumber.Contains(filter)) ||
                (r.SummaryEn != null && r.SummaryEn.Contains(filter)) ||
                (r.SummaryAr != null && r.SummaryAr.Contains(filter)));

        return query;
    }

    /// <summary>
    /// Adds the catalog's display name, icon and route to a page of requests -- one query for the page, not
    /// one per row.
    /// </summary>
    private async Task<List<MyRequestListItemDto>> BuildItemsAsync(List<WorkflowRequest> requests)
    {
        if (requests.Count == 0)
        {
            return new List<MyRequestListItemDto>();
        }

        var requestTypes = requests
            .Where(r => !r.RequestType.IsNullOrWhiteSpace())
            .Select(r => r.RequestType)
            .Distinct()
            .ToList();

        var types = (await _transactionTypeRepository.GetAll()
                .Where(t => requestTypes.Contains(t.Code))
                .Select(t => new { t.Code, t.NameEn, t.NameAr, t.IconClass, t.EssRoute })
                .ToListAsync())
            .ToDictionary(t => t.Code, StringComparer.OrdinalIgnoreCase);

        var items = new List<MyRequestListItemDto>();

        foreach (var request in requests)
        {
            var item = new MyRequestListItemDto
            {
                Id = request.Id,
                RequestType = request.RequestType,
                RequestNumber = request.RequestNumber,
                SummaryEn = request.SummaryEn,
                SummaryAr = request.SummaryAr,
                RequestStatus = request.RequestStatus,
                WorkflowStep = request.WorkflowStep,
                CreationTime = request.CreationTime,
                TargetEntityId = request.TargetEntityId,
                IsAmendment = request.IsAmendment
            };

            if (!request.RequestType.IsNullOrWhiteSpace() && types.TryGetValue(request.RequestType, out var type))
            {
                item.RequestTypeNameEn = type.NameEn;
                item.RequestTypeNameAr = type.NameAr;
                item.IconClass = type.IconClass;
                item.EssRoute = type.EssRoute;
            }

            items.Add(item);
        }

        return items;
    }
}
