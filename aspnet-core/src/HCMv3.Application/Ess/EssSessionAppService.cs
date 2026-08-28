using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using HCMv3.Authorization;
using HCMv3.Ess.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.Workflow;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Ess;

/// <inheritdoc cref="IEssSessionAppService"/>
[AbpAuthorize(AppPermissions.Ess)]
public class EssSessionAppService : HCMv3AppServiceBase, IEssSessionAppService
{
    private readonly IEmployeeSessionService _employeeSessionService;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeManager> _employeeManagerRepository;
    private readonly IRepository<WorkflowTransactionType> _transactionTypeRepository;
    private readonly IRepository<WorkFlowApprover> _workFlowApproverRepository;

    public EssSessionAppService(
        IEmployeeSessionService employeeSessionService,
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeManager> employeeManagerRepository,
        IRepository<WorkflowTransactionType> transactionTypeRepository,
        IRepository<WorkFlowApprover> workFlowApproverRepository)
    {
        _employeeSessionService = employeeSessionService;
        _employeeRepository = employeeRepository;
        _employeeManagerRepository = employeeManagerRepository;
        _transactionTypeRepository = transactionTypeRepository;
        _workFlowApproverRepository = workFlowApproverRepository;
    }

    public async Task<EssContextDto> GetEssContext()
    {
        var employeeId = await _employeeSessionService.GetCurrentEmployeeIdAsync();

        var result = new EssContextDto
        {
            IsLinkedToEmployee = employeeId.HasValue,
            EmployeeId = employeeId
        };

        // Returned rather than thrown. An unlinked user is a setup gap for HR to close, not an error the
        // employee did anything to cause -- the shell shows an explanation instead of a stack trace or a
        // silently empty page.
        if (!employeeId.HasValue)
        {
            return result;
        }

        var employee = await _employeeRepository.GetAll()
            .Where(e => e.Id == employeeId.Value)
            .Select(e => new
            {
                e.Code,
                e.FullNameEn,
                e.FullNameAr,
                HasPhoto = e.Photo != null
            })
            .FirstOrDefaultAsync();

        if (employee != null)
        {
            result.EmployeeCode = employee.Code;
            result.EmployeeFullNameEn = employee.FullNameEn;
            result.EmployeeFullNameAr = employee.FullNameAr;
            result.HasPhoto = employee.HasPhoto;
        }

        var today = DateTime.Today;

        result.IsManager = await _employeeManagerRepository.GetAll()
            .AnyAsync(m => m.ManagerId == employeeId.Value
                           && m.ManagerId != m.EmployeeId
                           && m.StartDate <= today
                           && m.EndDate >= today);

        // The inbox badge. A single indexed query over WorkFlowApprovers joined to WorkflowRequests, for
        // every transaction type at once -- which is only possible because the workflow tables are generic.
        result.PendingApprovalCount = await _workFlowApproverRepository.GetAll()
            .CountAsync(a => a.ApproverId == employeeId.Value
                             && !a.IsApproved
                             && a.WorkflowRequest.RequestStatus == RequestStatus.Open);

        result.AvailableTransactionTypes = await GetAvailableTransactionTypesAsync();

        return result;
    }

    /// <summary>
    /// ESS-enabled, active, and permitted to this user. The permission check happens here rather than in
    /// the client so a type the user cannot raise never reaches the browser at all.
    /// </summary>
    private async Task<System.Collections.Generic.List<EssTransactionTypeDto>> GetAvailableTransactionTypesAsync()
    {
        var types = await _transactionTypeRepository.GetAll()
            .Where(t => t.IsEssEnabled && t.IsActive)
            .OrderBy(t => t.DisplayOrder)
            .ThenBy(t => t.Code)
            .ToListAsync();

        return types
            .Where(t => t.EssPermissionName.IsNullOrWhiteSpace() || PermissionChecker.IsGranted(t.EssPermissionName))
            .Select(t => new EssTransactionTypeDto
            {
                Code = t.Code,
                NameEn = t.NameEn,
                NameAr = t.NameAr,
                EssRoute = t.EssRoute,
                IconClass = t.IconClass,
                DisplayOrder = t.DisplayOrder,
                IsWorkflowEnabled = t.IsWorkflowEnabled
            })
            .ToList();
    }
}
