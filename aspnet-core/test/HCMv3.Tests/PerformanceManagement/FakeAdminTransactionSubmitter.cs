using System.Threading.Tasks;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;

namespace HCMv3.Tests.PerformanceManagement;

/// <summary>
/// Stands in for <see cref="IAdminTransactionSubmitter"/> in the app-service unit tests, which run against
/// a private Sqlite context with no workflow catalog and no approval engine behind them.
///
/// <para>
/// It reproduces the only part of the real submitter these tests care about -- the row is inserted -- and
/// leaves <c>WorkflowStatus</c> null, which is exactly what the real one does when the type is not
/// workflow-enabled. That is the correct default here: with no catalog row there is nothing to route to.
/// The routing behaviour itself is covered end to end against the real engine in
/// <c>HCMv3.Tests.Workflow.AppraisalWorkflow_Tests</c>.
/// </para>
/// </summary>
public class FakeAdminTransactionSubmitter : IAdminTransactionSubmitter
{
    public async Task<int> CreateAndSubmitAsync<TEntity>(
        IRepository<TEntity> repository,
        TEntity entity,
        string requestType,
        string requestNumberPrefix,
        int? subTypeId = null)
        where TEntity : class, IEntity<int>, IHasEmployeeId, IWorkflowEnabledEntity
    {
        await repository.InsertAsync(entity);

        return entity.Id;
    }
}
