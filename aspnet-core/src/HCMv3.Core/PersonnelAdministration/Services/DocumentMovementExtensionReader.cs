using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Services;
using Abp.Domain.Uow;
using HCMv3.Workflow;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>One movement that a later extension replaced.</summary>
public class SupersededDocumentMovement
{
    public int Id { get; set; }

    public DateTime WithdrawDate { get; set; }

    /// <summary>The date the document was due back before the extension moved it.</summary>
    public DateTime ReturnDate { get; set; }

    /// <summary>The request that created this movement. Null when HR recorded it directly.</summary>
    public string RequestNumber { get; set; }

    /// <summary>When the extension that replaced it was approved.</summary>
    public DateTime? SupersededOn { get; set; }
}

/// <summary>
/// Reads the chain of movements an extended withdrawal replaced, reached through
/// <see cref="EmployeeDocumentMvmt.ExtendsMovementId"/>.
///
/// <para>
/// The replaced rows are soft-deleted, so nothing else in the system sees them -- which is right for the
/// movement log and wrong for anyone asking "when was this passport first due back". This is the one place
/// that looks, and it looks narrowly: only rows of the same employee and the same document as the movement
/// whose history is being read, so a link that somehow pointed elsewhere shows nothing rather than somebody
/// else's dates.
/// </para>
/// </summary>
public interface IDocumentMovementExtensionReader : IDomainService
{
    /// <summary>
    /// The replaced movements behind each of the given movements, oldest first, keyed by movement id. Every
    /// movement passed in has an entry; one that extended nothing has an empty list. Two queries for the
    /// whole set, however many movements or links there are.
    /// </summary>
    Task<Dictionary<int, List<SupersededDocumentMovement>>> GetChainsAsync(IReadOnlyCollection<EmployeeDocumentMvmt> movements);
}

/// <inheritdoc cref="IDocumentMovementExtensionReader"/>
public class DocumentMovementExtensionReader : HCMv3DomainServiceBase, IDocumentMovementExtensionReader
{
    private readonly IRepository<EmployeeDocumentMvmt> _movementRepository;
    private readonly IRepository<WorkflowRequest> _requestRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public DocumentMovementExtensionReader(
        IRepository<EmployeeDocumentMvmt> movementRepository,
        IRepository<WorkflowRequest> requestRepository,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _movementRepository = movementRepository;
        _requestRepository = requestRepository;
        _unitOfWorkManager = unitOfWorkManager;
    }

    [UnitOfWork]
    public virtual async Task<Dictionary<int, List<SupersededDocumentMovement>>> GetChainsAsync(
        IReadOnlyCollection<EmployeeDocumentMvmt> movements)
    {
        var chains = movements.ToDictionary(m => m.Id, _ => new List<SupersededDocumentMovement>());

        var extended = movements.Where(m => m.ExtendsMovementId.HasValue).ToList();
        if (extended.Count == 0)
        {
            return chains;
        }

        var documentIds = extended.Select(m => m.EmployeeDocumentId).Distinct().ToList();

        // Every replaced row of the documents on the page, in one read: the chain is then walked in memory,
        // so its length costs no further queries. The tenant filter stays on; only the two that hide a
        // replaced row are lifted.
        List<EmployeeDocumentMvmt> replaced;
        using (_unitOfWorkManager.Current.DisableFilter(Abp.Domain.Uow.AbpDataFilters.SoftDelete, HCMv3DataFilters.ApprovedOnly))
        {
            replaced = await _movementRepository.GetAll()
                .AsNoTracking()
                .Where(m => m.IsDeleted && documentIds.Contains(m.EmployeeDocumentId))
                .ToListAsync();
        }

        var replacedById = replaced.ToDictionary(m => m.Id);

        var walked = new Dictionary<int, List<EmployeeDocumentMvmt>>();

        foreach (var movement in extended)
        {
            var chain = new List<EmployeeDocumentMvmt>();
            var seen = new HashSet<int> { movement.Id };
            var nextId = movement.ExtendsMovementId;

            while (nextId.HasValue
                   && seen.Add(nextId.Value)
                   && replacedById.TryGetValue(nextId.Value, out var previous)
                   // Strictly the same employee and the same document, at every link.
                   && previous.EmployeeId == movement.EmployeeId
                   && previous.EmployeeDocumentId == movement.EmployeeDocumentId)
            {
                chain.Add(previous);
                nextId = previous.ExtendsMovementId;
            }

            // Walked newest to oldest; shown oldest first.
            chain.Reverse();
            walked[movement.Id] = chain;
        }

        var requestIds = walked.Values
            .SelectMany(chain => chain)
            .Where(m => m.WorkflowRequestId.HasValue)
            .Select(m => m.WorkflowRequestId.Value)
            .Distinct()
            .ToList();

        var requestNumbers = requestIds.Count == 0
            ? new Dictionary<int, string>()
            : await _requestRepository.GetAll()
                .Where(r => requestIds.Contains(r.Id))
                .Select(r => new { r.Id, r.RequestNumber })
                .ToDictionaryAsync(r => r.Id, r => r.RequestNumber);

        foreach (var pair in walked)
        {
            chains[pair.Key] = pair.Value.Select(m => new SupersededDocumentMovement
            {
                Id = m.Id,
                WithdrawDate = m.WithdrawDate,
                ReturnDate = m.ReturnDate,
                RequestNumber = m.WorkflowRequestId.HasValue && requestNumbers.TryGetValue(m.WorkflowRequestId.Value, out var number)
                    ? number
                    : null,
                SupersededOn = m.DeletionTime
            }).ToList();
        }

        return chains;
    }
}
