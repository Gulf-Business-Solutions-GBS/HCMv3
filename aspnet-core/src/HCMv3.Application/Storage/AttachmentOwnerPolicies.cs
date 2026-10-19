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
using HCMv3.GeneralMasterInfo;
using HCMv3.GovtRelationsManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.Recruitment;
using HCMv3.TrainingManagement;
using HCMv3.TravelManagement;
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

/// <summary>
/// Files on a letter request -- above all the printed letter HR keeps on it. Readable by HR, by the employee
/// the letter is for, and by its approvers.
/// </summary>
public class EmployeeLetterRequestAttachmentOwnerPolicy : EmployeeRequestAttachmentOwnerPolicy<EmployeeLetterRequest>
{
    public const string Type = "EmployeeLetterRequest";

    public EmployeeLetterRequestAttachmentOwnerPolicy(
        IRepository<EmployeeLetterRequest> repository,
        IRepository<WorkFlowApprover> approverRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IPermissionChecker permissionChecker,
        IUnitOfWorkManager unitOfWorkManager,
        IAbpSession session)
        : base(repository, approverRepository, employeeScopeProvider, permissionChecker, unitOfWorkManager, session)
    {
    }

    public override string EntityType => Type;

    protected override string ViewPermission => AppPermissions.Pages_PersonnelAdministration_EmployeeLetterRequests;

    protected override string EditPermission => AppPermissions.Pages_PersonnelAdministration_EmployeeLetterRequests_Edit;
}

/// <summary>
/// The document a leave requires (legacy <c>EmployeeAbsence.DocAttachment</c>): a sick note, a bereavement
/// certificate, whatever the absence type's <c>IsAttachmentRequired</c> asks for.
/// </summary>
public class EmployeeAbsenceAttachmentOwnerPolicy : EmployeeRequestAttachmentOwnerPolicy<EmployeeAbsence>
{
    public const string Type = "EmployeeAbsence";

    public EmployeeAbsenceAttachmentOwnerPolicy(
        IRepository<EmployeeAbsence> repository,
        IRepository<WorkFlowApprover> approverRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IPermissionChecker permissionChecker,
        IUnitOfWorkManager unitOfWorkManager,
        IAbpSession session)
        : base(repository, approverRepository, employeeScopeProvider, permissionChecker, unitOfWorkManager, session)
    {
    }

    public override string EntityType => Type;

    protected override string ViewPermission => AppPermissions.Pages_PersonnelAdministration_EmployeeAbsences;

    protected override string EditPermission => AppPermissions.Pages_PersonnelAdministration_EmployeeAbsences_Edit;
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

/// <summary>
/// The leave policy document of an absence type -- the file the "I accept the leave policy" link opens.
///
/// <para>
/// Readable by anyone who can apply for leave or maintain leave types: an employee has to be able to open
/// the policy they are being asked to accept. Only somebody who may edit leave types can replace it.
/// </para>
/// </summary>
public class AbsenceTypePolicyAttachmentOwnerPolicy : IAttachmentOwnerPolicy, ITransientDependency
{
    public const string Type = "AbsenceTypePolicy";

    private readonly IPermissionChecker _permissionChecker;

    public AbsenceTypePolicyAttachmentOwnerPolicy(IPermissionChecker permissionChecker)
    {
        _permissionChecker = permissionChecker;
    }

    public string EntityType => Type;

    public async Task<bool> CanViewAsync(string entityId)
    {
        return await _permissionChecker.IsGrantedAsync(AppPermissions.Ess_MyLeave)
               || await _permissionChecker.IsGrantedAsync(AppPermissions.Pages_PayrollManagement_AbsenceTypes);
    }

    public Task<bool> CanEditAsync(string entityId)
    {
        return _permissionChecker.IsGrantedAsync(AppPermissions.Pages_PayrollManagement_AbsenceTypes_Edit);
    }
}

// ==================================================================== self-service transactions
//
// Entities that carry a workflow status: the employee may add files to their own undecided request,
// approvers may read them, and HR follows the screen's own permission.

/// <summary>Files on an asset issued to an employee (handover note, signed receipt).</summary>
public class EmployeeAssetAttachmentOwnerPolicy : EmployeeRequestAttachmentOwnerPolicy<EmployeeAsset>
{
    public const string Type = "EmployeeAsset";

    public EmployeeAssetAttachmentOwnerPolicy(
        IRepository<EmployeeAsset> repository,
        IRepository<WorkFlowApprover> approverRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IPermissionChecker permissionChecker,
        IUnitOfWorkManager unitOfWorkManager,
        IAbpSession session)
        : base(repository, approverRepository, employeeScopeProvider, permissionChecker, unitOfWorkManager, session)
    {
    }

    public override string EntityType => Type;

    protected override string ViewPermission => AppPermissions.Pages_PersonnelAdministration_EmployeeAssets;

    protected override string EditPermission => AppPermissions.Pages_PersonnelAdministration_EmployeeAssets_Edit;
}

/// <summary>Files on a disciplinary action (warning letter, investigation notes).</summary>
public class EmployeeDisciplinaryAttachmentOwnerPolicy : EmployeeRequestAttachmentOwnerPolicy<EmployeeDisciplinary>
{
    public const string Type = "EmployeeDisciplinary";

    public EmployeeDisciplinaryAttachmentOwnerPolicy(
        IRepository<EmployeeDisciplinary> repository,
        IRepository<WorkFlowApprover> approverRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IPermissionChecker permissionChecker,
        IUnitOfWorkManager unitOfWorkManager,
        IAbpSession session)
        : base(repository, approverRepository, employeeScopeProvider, permissionChecker, unitOfWorkManager, session)
    {
    }

    public override string EntityType => Type;

    protected override string ViewPermission => AppPermissions.Pages_PersonnelAdministration_EmployeeDisciplinaries;

    protected override string EditPermission => AppPermissions.Pages_PersonnelAdministration_EmployeeDisciplinaries_Edit;
}

/// <summary>Files on a loan (signed loan agreement, supporting documents).</summary>
public class EmployeeLoanAttachmentOwnerPolicy : EmployeeRequestAttachmentOwnerPolicy<EmployeeLoan>
{
    public const string Type = "EmployeeLoan";

    public EmployeeLoanAttachmentOwnerPolicy(
        IRepository<EmployeeLoan> repository,
        IRepository<WorkFlowApprover> approverRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IPermissionChecker permissionChecker,
        IUnitOfWorkManager unitOfWorkManager,
        IAbpSession session)
        : base(repository, approverRepository, employeeScopeProvider, permissionChecker, unitOfWorkManager, session)
    {
    }

    public override string EntityType => Type;

    protected override string ViewPermission => AppPermissions.Pages_PersonnelAdministration_EmployeeLoans;

    protected override string EditPermission => AppPermissions.Pages_PersonnelAdministration_EmployeeLoans_Edit;
}

/// <summary>Files on a ticket (booking confirmation, e-ticket).</summary>
public class EmployeeTicketAttachmentOwnerPolicy : EmployeeRequestAttachmentOwnerPolicy<EmployeeTicket>
{
    public const string Type = "EmployeeTicket";

    public EmployeeTicketAttachmentOwnerPolicy(
        IRepository<EmployeeTicket> repository,
        IRepository<WorkFlowApprover> approverRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IPermissionChecker permissionChecker,
        IUnitOfWorkManager unitOfWorkManager,
        IAbpSession session)
        : base(repository, approverRepository, employeeScopeProvider, permissionChecker, unitOfWorkManager, session)
    {
    }

    public override string EntityType => Type;

    protected override string ViewPermission => AppPermissions.Pages_PersonnelAdministration_EmployeeTickets;

    protected override string EditPermission => AppPermissions.Pages_PersonnelAdministration_EmployeeTickets_Edit;
}

/// <summary>Files on a health reimbursement claim (medical bills, receipts).</summary>
public class EmployeeHealthReimburseAttachmentOwnerPolicy : EmployeeRequestAttachmentOwnerPolicy<EmployeeHealthReimburse>
{
    public const string Type = "EmployeeHealthReimburse";

    public EmployeeHealthReimburseAttachmentOwnerPolicy(
        IRepository<EmployeeHealthReimburse> repository,
        IRepository<WorkFlowApprover> approverRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IPermissionChecker permissionChecker,
        IUnitOfWorkManager unitOfWorkManager,
        IAbpSession session)
        : base(repository, approverRepository, employeeScopeProvider, permissionChecker, unitOfWorkManager, session)
    {
    }

    public override string EntityType => Type;

    protected override string ViewPermission => AppPermissions.Pages_PersonnelAdministration_EmployeeHealthReimburses;

    protected override string EditPermission => AppPermissions.Pages_PersonnelAdministration_EmployeeHealthReimburses_Edit;
}

/// <summary>Files on a travel feedback (trip report, receipts).</summary>
public class TravelFeedbackAttachmentOwnerPolicy : EmployeeRequestAttachmentOwnerPolicy<TravelFeedback>
{
    public const string Type = "TravelFeedback";

    public TravelFeedbackAttachmentOwnerPolicy(
        IRepository<TravelFeedback> repository,
        IRepository<WorkFlowApprover> approverRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IPermissionChecker permissionChecker,
        IUnitOfWorkManager unitOfWorkManager,
        IAbpSession session)
        : base(repository, approverRepository, employeeScopeProvider, permissionChecker, unitOfWorkManager, session)
    {
    }

    public override string EntityType => Type;

    protected override string ViewPermission => AppPermissions.Pages_TravelManagement_TravelFeedbacks;

    protected override string EditPermission => AppPermissions.Pages_TravelManagement_TravelFeedbacks_Edit;
}

// ==================================================================== admin-maintained records
//
// Records with no workflow, kept by HR or by an administrator: who may see and change the files is exactly
// who may open and edit the screen.

/// <summary>
/// The rule for a record that only its own screen maintains: view with <see cref="ViewPermission"/>, change
/// with <see cref="EditPermission"/>, and only for a record that exists in the caller's tenant (the tenant
/// filter stays on).
/// </summary>
public abstract class ScreenPermissionAttachmentOwnerPolicy<TEntity> : IAttachmentOwnerPolicy, ITransientDependency
    where TEntity : class, IEntity<int>
{
    private readonly IRepository<TEntity> _repository;
    private readonly IPermissionChecker _permissionChecker;

    protected ScreenPermissionAttachmentOwnerPolicy(IRepository<TEntity> repository, IPermissionChecker permissionChecker)
    {
        _repository = repository;
        _permissionChecker = permissionChecker;
    }

    public abstract string EntityType { get; }

    protected abstract string ViewPermission { get; }

    protected abstract string EditPermission { get; }

    public virtual async Task<bool> CanViewAsync(string entityId)
    {
        return await _permissionChecker.IsGrantedAsync(ViewPermission) && await ExistsAsync(entityId);
    }

    public async Task<bool> CanEditAsync(string entityId)
    {
        return await _permissionChecker.IsGrantedAsync(EditPermission) && await ExistsAsync(entityId);
    }

    protected IRepository<TEntity> Repository => _repository;

    protected IPermissionChecker PermissionChecker => _permissionChecker;

    private async Task<bool> ExistsAsync(string entityId)
    {
        if (!int.TryParse(entityId, out var id))
        {
            return false;
        }

        return await _repository.GetAll().AnyAsync(e => e.Id == id);
    }
}

/// <summary>Certificates and transcripts on an education record.</summary>
public class EmployeeEducationAttachmentOwnerPolicy : ScreenPermissionAttachmentOwnerPolicy<EmployeeEducation>
{
    public const string Type = "EmployeeEducation";

    public EmployeeEducationAttachmentOwnerPolicy(IRepository<EmployeeEducation> repository, IPermissionChecker permissionChecker)
        : base(repository, permissionChecker)
    {
    }

    public override string EntityType => Type;

    protected override string ViewPermission => AppPermissions.Pages_PersonnelAdministration_EmployeeEducations;

    protected override string EditPermission => AppPermissions.Pages_PersonnelAdministration_EmployeeEducations_Edit;
}

/// <summary>Certificates and attendance sheets on an employee training record.</summary>
public class EmployeeTrainingAttachmentOwnerPolicy : ScreenPermissionAttachmentOwnerPolicy<EmployeeTraining>
{
    public const string Type = "EmployeeTraining";

    public EmployeeTrainingAttachmentOwnerPolicy(IRepository<EmployeeTraining> repository, IPermissionChecker permissionChecker)
        : base(repository, permissionChecker)
    {
    }

    public override string EntityType => Type;

    protected override string ViewPermission => AppPermissions.Pages_TrainingManagement_EmployeeTrainings;

    protected override string EditPermission => AppPermissions.Pages_TrainingManagement_EmployeeTrainings_Edit;
}

/// <summary>Resignation letter, termination notice and clearance papers on a separation.</summary>
public class EmployeeSeparationAttachmentOwnerPolicy : ScreenPermissionAttachmentOwnerPolicy<EmployeeSeparation>
{
    public const string Type = "EmployeeSeparation";

    public EmployeeSeparationAttachmentOwnerPolicy(IRepository<EmployeeSeparation> repository, IPermissionChecker permissionChecker)
        : base(repository, permissionChecker)
    {
    }

    public override string EntityType => Type;

    protected override string ViewPermission => AppPermissions.Pages_PersonnelAdministration_EmployeeSeparations;

    protected override string EditPermission => AppPermissions.Pages_PersonnelAdministration_EmployeeSeparations_Edit;
}

/// <summary>The CV file itself on an applicant CV record.</summary>
public class ApplicantCVAttachmentOwnerPolicy : ScreenPermissionAttachmentOwnerPolicy<ApplicantCV>
{
    public const string Type = "ApplicantCV";

    public ApplicantCVAttachmentOwnerPolicy(IRepository<ApplicantCV> repository, IPermissionChecker permissionChecker)
        : base(repository, permissionChecker)
    {
    }

    public override string EntityType => Type;

    protected override string ViewPermission => AppPermissions.Pages_Recruitment_ApplicantCVs;

    protected override string EditPermission => AppPermissions.Pages_Recruitment_ApplicantCVs_Edit;
}

/// <summary>Job description and approval papers on a vacancy.</summary>
public class VacancyAttachmentOwnerPolicy : ScreenPermissionAttachmentOwnerPolicy<Vacancy>
{
    public const string Type = "Vacancy";

    public VacancyAttachmentOwnerPolicy(IRepository<Vacancy> repository, IPermissionChecker permissionChecker)
        : base(repository, permissionChecker)
    {
    }

    public override string EntityType => Type;

    protected override string ViewPermission => AppPermissions.Pages_Recruitment_Vacancies;

    protected override string EditPermission => AppPermissions.Pages_Recruitment_Vacancies_Edit;
}

/// <summary>Scans of a company document kept by government relations (licence, registration).</summary>
public class OrganizationDocumentAttachmentOwnerPolicy : ScreenPermissionAttachmentOwnerPolicy<OrganizationDocument>
{
    public const string Type = "OrganizationDocument";

    public OrganizationDocumentAttachmentOwnerPolicy(IRepository<OrganizationDocument> repository, IPermissionChecker permissionChecker)
        : base(repository, permissionChecker)
    {
    }

    public override string EntityType => Type;

    protected override string ViewPermission => AppPermissions.Pages_GovtRelationsManagement_OrganizationDocuments;

    protected override string EditPermission => AppPermissions.Pages_GovtRelationsManagement_OrganizationDocuments_Edit;

    /// <summary>
    /// Also readable through the Org Policy screen: a document whose type is Show To All is a company policy,
    /// so its files are open to anyone with Org Policy access, read-only.
    /// </summary>
    public override async Task<bool> CanViewAsync(string entityId)
    {
        if (await base.CanViewAsync(entityId))
        {
            return true;
        }

        return int.TryParse(entityId, out var id)
               && await PermissionChecker.IsGrantedAsync(AppPermissions.Pages_GovtRelationsManagement_OrgPolicies)
               && await Repository.GetAll().AnyAsync(d => d.Id == id && d.DocumentType.ShowToAll);
    }
}

/// <summary>Invoice, warranty and photos on a company asset (General Master Info).</summary>
public class AssetAttachmentOwnerPolicy : ScreenPermissionAttachmentOwnerPolicy<Asset>
{
    public const string Type = "Asset";

    public AssetAttachmentOwnerPolicy(IRepository<Asset> repository, IPermissionChecker permissionChecker)
        : base(repository, permissionChecker)
    {
    }

    public override string EntityType => Type;

    protected override string ViewPermission => AppPermissions.Pages_GeneralMasterInfo_Assets;

    protected override string EditPermission => AppPermissions.Pages_GeneralMasterInfo_Assets_Edit;
}

/// <summary>
/// The invoice scan on an employee asset invoice (legacy <c>EmployeeAssetInvoice.DocAttachment</c>). No
/// workflow: HR keeps these from the employee record, so the Asset Invoices screen permissions decide.
/// </summary>
public class EmployeeAssetInvoiceAttachmentOwnerPolicy : ScreenPermissionAttachmentOwnerPolicy<EmployeeAssetInvoice>
{
    public const string Type = "EmployeeAssetInvoice";

    public EmployeeAssetInvoiceAttachmentOwnerPolicy(IRepository<EmployeeAssetInvoice> repository, IPermissionChecker permissionChecker)
        : base(repository, permissionChecker)
    {
    }

    public override string EntityType => Type;

    protected override string ViewPermission => AppPermissions.Pages_PersonnelAdministration_EmployeeAssetInvoices;

    protected override string EditPermission => AppPermissions.Pages_PersonnelAdministration_EmployeeAssetInvoices_Edit;
}
