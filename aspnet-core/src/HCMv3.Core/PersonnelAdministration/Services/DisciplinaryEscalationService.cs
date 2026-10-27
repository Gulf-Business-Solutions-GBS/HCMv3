using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using HCMv3.GeneralMasterInfo;
using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration.Services;

/// <inheritdoc cref="IDisciplinaryEscalationService"/>
/// <remarks>
/// <c>HCMv3.Core</c> has no reference to EntityFrameworkCore, so this uses the synchronous
/// <c>IQueryable</c> from <c>IRepository&lt;T&gt;.GetAll()</c> -- the same pattern as
/// <see cref="LeaveBalanceService"/>.
/// </remarks>
[UnitOfWork]
public class DisciplinaryEscalationService : HCMv3DomainServiceBase, IDisciplinaryEscalationService
{
    private readonly IRepository<EmployeeDisciplinary> _disciplinaryRepository;
    private readonly IRepository<DisciplinaryType> _typeRepository;
    private readonly IRepository<DisciplinaryTypeDetail> _typeDetailRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public DisciplinaryEscalationService(
        IRepository<EmployeeDisciplinary> disciplinaryRepository,
        IRepository<DisciplinaryType> typeRepository,
        IRepository<DisciplinaryTypeDetail> typeDetailRepository,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _disciplinaryRepository = disciplinaryRepository;
        _typeRepository = typeRepository;
        _typeDetailRepository = typeDetailRepository;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public Task<DisciplinaryEscalation> ResolveAsync(
        int employeeId,
        int disciplinaryTypeId,
        DateTime disciplinaryDate,
        int? excludeDisciplinaryId = null)
    {
        var type = _typeRepository.GetAll()
            .Where(t => t.Id == disciplinaryTypeId)
            .Select(t => new { t.NameEn, t.NameAr, t.RepeatDurationInDays })
            .FirstOrDefault();

        var repeatDurationInDays = type?.RepeatDurationInDays ?? 0;
        var windowEnd = disciplinaryDate.Date;

        // Inclusive at BOTH ends, matching legacy: an offence exactly RepeatDurationInDays earlier is still
        // inside the window, and one on the same day counts too. A type with no repeat duration collapses
        // the window to the single day, which is the honest reading of "does not repeat".
        var windowStart = windowEnd.AddDays(-repeatDurationInDays);

        var result = new DisciplinaryEscalation
        {
            EmployeeId = employeeId,
            DisciplinaryTypeId = disciplinaryTypeId,
            DisciplinaryTypeNameEn = type?.NameEn,
            DisciplinaryTypeNameAr = type?.NameAr,
            RepeatDurationInDays = repeatDurationInDays,
            WindowStartDate = windowStart,
            WindowEndDate = windowEnd
        };

        // Pending records have to be visible or the ladder is defeated by raising two offences before
        // either is decided -- see the interface remarks.
        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var priorOfThisType = _disciplinaryRepository.GetAll()
                .Where(r => r.EmployeeId == employeeId
                            && r.DisciplinaryTypeId == disciplinaryTypeId
                            && r.DisciplinaryDate <= windowEnd
                            && (excludeDisciplinaryId == null || r.Id != excludeDisciplinaryId.Value)

                            // Everything except a refused one. An accusation an approver rejected is not an
                            // offence and must not push the next one up a rung.
                            && (r.WorkflowStatus == null
                                || r.WorkflowStatus == RequestStatus.Draft
                                || r.WorkflowStatus == RequestStatus.Open
                                || r.WorkflowStatus == RequestStatus.Approved
                                || r.WorkflowStatus == RequestStatus.Closed))
                .Select(r => new
                {
                    r.Id,
                    r.DisciplinaryDate,
                    r.Severity,
                    r.DisciplinaryDeductionTypeId,
                    DeductionTypeNameEn = r.DisciplinaryDeductionType != null ? r.DisciplinaryDeductionType.NameEn : null,
                    r.WorkflowStatus
                })
                .OrderByDescending(r => r.DisciplinaryDate)
                .ToList();

            result.History = priorOfThisType
                .Select(r => new DisciplinaryEscalationHistoryEntry
                {
                    Id = r.Id,
                    DisciplinaryDate = r.DisciplinaryDate,
                    Severity = r.Severity,
                    DisciplinaryDeductionTypeId = r.DisciplinaryDeductionTypeId,
                    DisciplinaryDeductionTypeNameEn = r.DeductionTypeNameEn,
                    IsWithinRepeatWindow = r.DisciplinaryDate.Date >= windowStart,
                    WorkflowStatus = r.WorkflowStatus
                })
                .ToList();

            result.PriorDisciplinaryCount = result.History.Count(h => h.IsWithinRepeatWindow);
        }

        ApplyLadder(result);

        return Task.FromResult(result);
    }

    /// <summary>
    /// Reads the penalty off the type's tiers: the highest <c>DisciplinaryCount</c> that does not exceed
    /// this offence's number. Tiers of 1/3/5 therefore give tier 1 for offences one and two, tier 3 for
    /// three and four, and tier 5 from the fifth on -- the ladder holds at its top rung rather than falling
    /// off it.
    /// </summary>
    private void ApplyLadder(DisciplinaryEscalation escalation)
    {
        var tier = _typeDetailRepository.GetAll()
            .Where(d => d.DisciplinaryTypeId == escalation.DisciplinaryTypeId
                        && d.DisciplinaryCount <= escalation.OffenceNumber)
            .OrderByDescending(d => d.DisciplinaryCount)
            .Select(d => new
            {
                d.DisciplinaryDeductionTypeId,
                NameEn = d.DisciplinaryDeductionType != null ? d.DisciplinaryDeductionType.NameEn : null,
                NameAr = d.DisciplinaryDeductionType != null ? d.DisciplinaryDeductionType.NameAr : null
            })
            .FirstOrDefault();

        if (tier == null)
        {
            return;
        }

        escalation.DisciplinaryDeductionTypeId = tier.DisciplinaryDeductionTypeId;
        escalation.DisciplinaryDeductionTypeNameEn = tier.NameEn;
        escalation.DisciplinaryDeductionTypeNameAr = tier.NameAr;
    }
}
