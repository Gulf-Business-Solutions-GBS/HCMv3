using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.Workflow;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <inheritdoc cref="ILeavePlansAppService"/>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_LeavePlans)]
public class LeavePlansAppService : HCMv3AppServiceBase, ILeavePlansAppService
{
    private readonly IRepository<LeavePlan> _repository;
    private readonly IRepository<WorkflowRequest> _requestRepository;

    public LeavePlansAppService(IRepository<LeavePlan> repository, IRepository<WorkflowRequest> requestRepository)
    {
        _repository = repository;
        _requestRepository = requestRepository;
    }

    /// <summary>
    /// Lists plans for HR. The <c>ApprovedOnly</c> filter is disabled so plans still with an approver can be
    /// shown, badged by their status; an employee's unsent drafts are excluded either way -- they are nobody
    /// else's business until sent.
    /// </summary>
    [HttpPost]
    public async Task<PagedResultDto<LeavePlanDto>> GetAll(GetAllLeavePlansInput input)
    {
        input.Normalize();

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var filter = input.Filter?.Trim();

            var query = _repository.GetAll()
                .Include(p => p.Employee)
                .Where(p => p.WorkflowStatus != RequestStatus.Draft || p.WorkflowRequestId != null)
                .WhereIf(!input.IncludePending,
                    p => p.WorkflowStatus == null || p.WorkflowStatus == RequestStatus.Approved || p.WorkflowStatus == RequestStatus.Closed)
                .WhereIf(input.EmployeeId.HasValue, p => p.EmployeeId == input.EmployeeId.Value)
                .WhereIf(input.FromDate.HasValue, p => p.EndDate >= input.FromDate.Value.Date)
                .WhereIf(input.ToDate.HasValue, p => p.StartDate <= input.ToDate.Value.Date)
                .WhereIf(!filter.IsNullOrWhiteSpace(),
                    p => p.Employee.Code.Contains(filter)
                         || p.Employee.FullNameEn.Contains(filter)
                         || p.Employee.FullNameAr.Contains(filter));

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(input.Sorting)
                .PageBy(input)
                .ToListAsync();

            var requestIds = items.Where(p => p.WorkflowRequestId.HasValue).Select(p => p.WorkflowRequestId.Value).Distinct().ToList();

            var numbers = requestIds.Count == 0
                ? new System.Collections.Generic.Dictionary<int, string>()
                : await _requestRepository.GetAll()
                    .Where(r => requestIds.Contains(r.Id))
                    .ToDictionaryAsync(r => r.Id, r => r.RequestNumber);

            return new PagedResultDto<LeavePlanDto>(totalCount, items.Select(p => new LeavePlanDto
            {
                Id = p.Id,
                EmployeeId = p.EmployeeId,
                EmployeeCode = p.Employee?.Code,
                EmployeeFullNameEn = p.Employee?.FullNameEn,
                EmployeeFullNameAr = p.Employee?.FullNameAr,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                PlannedDays = p.PlannedDays,
                ProjectedLeaveBalance = p.ProjectedLeaveBalance,
                Comments = p.Comments,
                WorkflowStatus = p.WorkflowStatus,
                WorkflowRequestId = p.WorkflowRequestId,
                RequestNumber = p.WorkflowRequestId.HasValue && numbers.TryGetValue(p.WorkflowRequestId.Value, out var number) ? number : null,
                CreationTime = p.CreationTime
            }).ToList());
        }
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_LeavePlans_Delete)]
    public async Task Delete(EntityDto input)
    {
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var plan = await _repository.GetAsync(input.Id);

            // A plan still with an approver would otherwise stay in their inbox pointing at nothing -- the same
            // reason the employee's own Withdraw closes the request.
            if (plan.WorkflowRequestId.HasValue)
            {
                var request = await _requestRepository.FirstOrDefaultAsync(r => r.Id == plan.WorkflowRequestId.Value);
                if (request != null && (request.RequestStatus == RequestStatus.Open || request.RequestStatus == RequestStatus.Draft))
                {
                    request.RequestStatus = RequestStatus.Closed;
                    await _requestRepository.UpdateAsync(request);
                }
            }

            await _repository.DeleteAsync(plan);
        }
    }
}
