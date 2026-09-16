using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using Abp.Timing;
using HCMv3.AiAgent.Tools;

namespace HCMv3.AiAgent.Actions;

/// <summary>How a tool records a draft for the user to confirm. The only write a tool is allowed.</summary>
public class AiPendingActionStore : ITransientDependency
{
    private readonly IRepository<AiPendingAction> _repository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IAbpSession _session;

    public AiPendingActionStore(IRepository<AiPendingAction> repository, IUnitOfWorkManager unitOfWorkManager, IAbpSession session)
    {
        _repository = repository;
        _unitOfWorkManager = unitOfWorkManager;
        _session = session;
    }

    /// <summary>
    /// Saves a draft and marks it as this reply's card. Any draft still waiting in the same conversation is
    /// expired first: when the user changes their mind ("make it 5 days instead") only the latest card may be
    /// confirmable, or an old card scrolled out of view could submit the version they abandoned.
    /// </summary>
    public async Task<int> CreateAsync(AiToolContext context, string actionType, object payload)
    {
        int id;

        using (var uow = _unitOfWorkManager.Begin())
        {
            var waiting = await _repository.GetAllListAsync(a =>
                a.ConversationId == context.ConversationId && a.Status == AiPendingActionStatus.Pending);

            foreach (var old in waiting)
            {
                old.Status = AiPendingActionStatus.Expired;
                old.ResolvedTime = Clock.Now;
            }

            id = await _repository.InsertAndGetIdAsync(new AiPendingAction
            {
                TenantId = _session.GetTenantId(),
                UserId = _session.GetUserId(),
                ConversationId = context.ConversationId,
                ActionType = actionType,
                PayloadJson = AiPendingActionHandlerBase.Serialize(payload),
                Status = AiPendingActionStatus.Pending
            });

            await uow.CompleteAsync();
        }

        context.CreatedPendingActionId = id;
        return id;
    }
}
