using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Workflow;

/// <inheritdoc cref="IDirectEntryAppService"/>
[AbpAuthorize(AppPermissions.Pages_Workflow_RecordWithoutApproval)]
public class DirectEntryAppService : HCMv3AppServiceBase, IDirectEntryAppService
{
    private readonly IRepository<WorkflowTransactionType> _repository;

    public DirectEntryAppService(IRepository<WorkflowTransactionType> repository)
    {
        _repository = repository;
    }

    public async Task<List<string>> GetDirectEntryRequestTypes()
    {
        // The same three conditions the submitter enforces, so a screen never offers a choice the save
        // would then refuse. The permission half of the gate is the attribute above; this is the type half.
        return await _repository.GetAll()
            .Where(t => t.IsActive && t.IsWorkflowEnabled && t.AllowDirectEntry)
            .Select(t => t.Code)
            .ToListAsync();
    }
}
