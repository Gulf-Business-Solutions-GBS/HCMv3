using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.Workflow;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Storage;

/// <summary>
/// Decides who may see and change the files of one entity type. To make a new entity attachable, add a
/// class here named <c>...AttachmentOwnerPolicy</c>; the name is what registers it (Windsor's
/// default-interface convention), and <see cref="EntityType"/> is what screens pass in.
/// </summary>
public interface IAttachmentOwnerPolicy
{
    string EntityType { get; }

    Task<bool> CanViewAsync(string entityId);

    Task<bool> CanEditAsync(string entityId);
}

/// <summary>
/// The rule for a self-service request that is also the record (the single-table workflow design): its
/// files belong to the request.
///
/// <list type="bullet">
/// <item>View: the employee it belongs to, anyone who is or was an approver on its request, and HR holding
/// <see cref="ViewPermission"/>.</item>
/// <item>Change: HR holding <see cref="EditPermission"/> at any time; the employee only while the request is
/// still undecided (draft or open), or -- when workflow is off for the type -- on a row they created
/// themselves. Once approved or rejected, the files are part of the decision and stay as they were.</item>
/// </list>
/// </summary>
public abstract class EmployeeRequestAttachmentOwnerPolicy<TEntity> : IAttachmentOwnerPolicy, ITransientDependency
    where TEntity : class, IEntity<int>, IHasEmployeeId, IWorkflowEnabledEntity
{
    private readonly IRepository<TEntity> _repository;
    private readonly IRepository<WorkFlowApprover> _approverRepository;
    private readonly IEmployeeScopeProvider _employeeScopeProvider;
    private readonly IPermissionChecker _permissionChecker;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IAbpSession _session;

    protected EmployeeRequestAttachmentOwnerPolicy(
        IRepository<TEntity> repository,
        IRepository<WorkFlowApprover> approverRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IPermissionChecker permissionChecker,
        IUnitOfWorkManager unitOfWorkManager,
        IAbpSession session)
    {
        _repository = repository;
        _approverRepository = approverRepository;
        _employeeScopeProvider = employeeScopeProvider;
        _permissionChecker = permissionChecker;
        _unitOfWorkManager = unitOfWorkManager;
        _session = session;
    }

    public abstract string EntityType { get; }

    protected abstract string ViewPermission { get; }

    protected abstract string EditPermission { get; }

    public async Task<bool> CanViewAsync(string entityId)
    {
        var entity = await LoadAsync(entityId);
        if (entity == null)
        {
            return false;
        }

        if (await _permissionChecker.IsGrantedAsync(ViewPermission))
        {
            return true;
        }

        var me = await _employeeScopeProvider.GetCurrentEmployeeIdAsync();
        if (me == null)
        {
            return false;
        }

        if (entity.EmployeeId == me.Value)
        {
            return true;
        }

        // An approver has to see what they are being asked to approve -- and keeps seeing it afterwards,
        // because the files are the evidence behind the decision they made.
        return entity.WorkflowRequestId.HasValue
               && await _approverRepository.GetAll().AnyAsync(a =>
                   a.WorkflowRequestId == entity.WorkflowRequestId.Value && a.ApproverId == me.Value);
    }

    public async Task<bool> CanEditAsync(string entityId)
    {
        var entity = await LoadAsync(entityId);
        if (entity == null)
        {
            return false;
        }

        if (await _permissionChecker.IsGrantedAsync(EditPermission))
        {
            return true;
        }

        var me = await _employeeScopeProvider.GetCurrentEmployeeIdAsync();
        if (me == null || entity.EmployeeId != me.Value)
        {
            return false;
        }

        return entity.WorkflowStatus switch
        {
            RequestStatus.Draft or RequestStatus.Open => true,
            null => entity is ICreationAudited audited && audited.CreatorUserId == _session.UserId,
            _ => false
        };
    }

    /// <summary>Loaded with the ApprovedOnly filter off: a pending request is exactly when files matter most.</summary>
    private async Task<TEntity> LoadAsync(string entityId)
    {
        if (!int.TryParse(entityId, out var id))
        {
            return null;
        }

        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            return await _repository.GetAll().Where(e => e.Id == id).FirstOrDefaultAsync();
        }
    }
}

/// <summary>Scans on a document request (legacy <c>DocumentRequest.DocAttachment</c>).</summary>
public class EmployeeDocumentAttachmentOwnerPolicy : EmployeeRequestAttachmentOwnerPolicy<EmployeeDocument>
{
    public const string Type = "EmployeeDocument";

    public EmployeeDocumentAttachmentOwnerPolicy(
        IRepository<EmployeeDocument> repository,
        IRepository<WorkFlowApprover> approverRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IPermissionChecker permissionChecker,
        IUnitOfWorkManager unitOfWorkManager,
        IAbpSession session)
        : base(repository, approverRepository, employeeScopeProvider, permissionChecker, unitOfWorkManager, session)
    {
    }

    public override string EntityType => Type;

    protected override string ViewPermission => AppPermissions.Pages_PersonnelAdministration_EmployeeDocuments;

    protected override string EditPermission => AppPermissions.Pages_PersonnelAdministration_EmployeeDocuments_Edit;
}

/// <summary>Proof on a personal data change (legacy <c>PersonalDataUpdateRequest.SampleImage</c>).</summary>
public class EmployeePersonalDataChangeAttachmentOwnerPolicy : EmployeeRequestAttachmentOwnerPolicy<EmployeePersonalDataChange>
{
    public const string Type = "EmployeePersonalDataChange";

    public EmployeePersonalDataChangeAttachmentOwnerPolicy(
        IRepository<EmployeePersonalDataChange> repository,
        IRepository<WorkFlowApprover> approverRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IPermissionChecker permissionChecker,
        IUnitOfWorkManager unitOfWorkManager,
        IAbpSession session)
        : base(repository, approverRepository, employeeScopeProvider, permissionChecker, unitOfWorkManager, session)
    {
    }

    public override string EntityType => Type;

    protected override string ViewPermission => AppPermissions.Pages_PersonnelAdministration_Employees;

    protected override string EditPermission => AppPermissions.Pages_PersonnelAdministration_Employees_Edit;
}
