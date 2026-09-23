using System.Threading.Tasks;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using HCMv3.PersonnelAdministration;

namespace HCMv3.Workflow.Services;

/// <summary>
/// Creates and submits a transaction that an ADMIN raises on somebody else's behalf -- a personnel action,
/// a disciplinary record, anything HR initiates about an employee rather than the employee requesting it.
///
/// <para>
/// The counterpart to <c>MyTransactionAppServiceBase</c>, and separate from it for one reason: who the
/// request is about. Self-service takes the employee from the session, and must, or a form could raise a
/// request against somebody else. Here the employee comes from the ENTITY, because the whole point is that
/// HR is acting for them -- and the admin doing it is frequently not an employee at all.
/// </para>
///
/// <para>
/// Everything after that is identical, and deliberately shares the same engine: the row is created hidden,
/// the request is raised and submitted in one step, and a handler that refuses it rolls the half-created row
/// back. A second implementation of those steps is how the two paths would drift.
/// </para>
/// </summary>
public interface IAdminTransactionSubmitter
{
    /// <summary>
    /// Inserts the row and, when the type is workflow-enabled, submits it for approval.
    /// </summary>
    /// <param name="repository">The caller's own repository for the entity -- passed in rather than
    /// resolved here, so this stays a plain collaborator instead of reaching into the container.</param>
    /// <param name="entity">Fully populated, including its employee. Not yet inserted.</param>
    /// <param name="requestType">The type's <c>WorkflowTransactionType.Code</c>.</param>
    /// <param name="requestNumberPrefix">
    /// A short type tag placed in front of the generated number, e.g. "PA" giving PA-000042. A display hint
    /// and nothing more: the digits come from one counter shared by every request type in the tenant, so
    /// they are unique on their own and two types cannot collide even if handed the same tag. Kept as a
    /// parameter rather than deleted because a bare number tells a reader nothing about what it refers to,
    /// and because thirty call sites already know what kind of request they are raising.
    /// </param>
    /// <param name="subTypeId">What the routing rules match against -- the personal action, the loan type.</param>
    /// <returns>The id of the inserted transaction row.</returns>
    Task<int> CreateAndSubmitAsync<TEntity>(
        IRepository<TEntity> repository,
        TEntity entity,
        string requestType,
        string requestNumberPrefix,
        int? subTypeId = null)
        where TEntity : class, IEntity<int>, IHasEmployeeId, IWorkflowEnabledEntity;

    /// <summary>
    /// The same thing for a transaction that is ABOUT MORE THAN ONE EMPLOYEE, where the subject therefore has
    /// to be supplied rather than read off the entity.
    ///
    /// <para>
    /// <c>EmployeePaymentBatch</c> is the case this exists for: a header over N employees, whose employees
    /// are its detail lines. It cannot implement <see cref="IHasEmployeeId"/> honestly, and giving it an
    /// <c>EmployeeId</c> column purely to satisfy the constraint above would put a meaningless value on the
    /// row -- which the next person to read it would take for the person being paid.
    /// </para>
    ///
    /// <para>
    /// Everything else is identical to <see cref="CreateAndSubmitAsync{TEntity}"/>, including the
    /// create-hidden-then-submit ordering. The only difference is where <c>WorkflowRequest.EmployeeId</c>
    /// comes from, and it matters because that is what the approver resolver routes by: for a batch the
    /// subject is the person who RAISED it, since there is no one person it is about.
    /// </para>
    /// </summary>
    /// <param name="subjectEmployeeId">
    /// The employee the request is recorded against and routed by -- for a batch, the requester. Also used as
    /// the acting employee at submit when the signed-in user has no employee link of their own.
    /// </param>
    Task<int> CreateAndSubmitForSubjectAsync<TEntity>(
        IRepository<TEntity> repository,
        TEntity entity,
        string requestType,
        string requestNumberPrefix,
        int subjectEmployeeId,
        int? subTypeId = null)
        where TEntity : class, IEntity<int>, IWorkflowEnabledEntity;
}
