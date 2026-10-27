using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Timing;
using Abp.UI;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings.Services;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.ManpowerPlanningManagement.Services;

/// <inheritdoc cref="IPositionOccupancyService"/>
public class PositionOccupancyService : HCMv3DomainServiceBase, IPositionOccupancyService
{
    private static readonly DateTime InfiniteEndDate = new(9999, 12, 31);

    private readonly IRepository<PositionRelationship> _relationshipRepository;
    private readonly IRepository<Position> _positionRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<EmployeeSeparation> _separationRepository;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;

    public PositionOccupancyService(
        IRepository<PositionRelationship> relationshipRepository,
        IRepository<Position> positionRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<EmployeeSeparation> separationRepository,
        ISysSettingsAccessor sysSettingsAccessor)
    {
        _relationshipRepository = relationshipRepository;
        _positionRepository = positionRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _separationRepository = separationRepository;
        _sysSettingsAccessor = sysSettingsAccessor;
    }

    public async Task ValidateHolderAsync(PositionRelationship relationship)
    {
        if (relationship.EndDate.Date < relationship.StartDate.Date)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var position = await _positionRepository.GetAsync(relationship.PositionId);

        var overlappingCount = await _relationshipRepository.GetAll()
            .Where(r => r.Id != relationship.Id
                        && r.EmployeeId != relationship.EmployeeId
                        && r.PositionId == relationship.PositionId
                        && !(r.StartDate.Date > relationship.EndDate.Date || r.EndDate.Date < relationship.StartDate.Date))
            .CountAsync();

        if (overlappingCount + 1 > position.FTELimit)
        {
            throw new UserFriendlyException(L("PositionFTELimitExceeded", position.FTELimit));
        }
    }

    public async Task RecomputeVacancyAsync(int positionId, int excludeRelationshipId = 0)
    {
        var today = Clock.Now.Date;

        var hasCurrentHolder = await _relationshipRepository.GetAll()
            .AnyAsync(r => r.Id != excludeRelationshipId
                           && r.PositionId == positionId
                           && r.StartDate.Date <= today
                           && r.EndDate.Date >= today);

        var position = await _positionRepository.GetAsync(positionId);
        if (position.Vacant != !hasCurrentHolder)
        {
            position.Vacant = !hasCurrentHolder;
            await _positionRepository.UpdateAsync(position);
        }
    }

    public async Task SyncEmployeeAsync(int employeeId)
    {
        if (!await IsEnabledAsync())
        {
            return;
        }

        var assignments = await _orgAssignmentRepository.GetAll()
            .Where(a => a.EmployeeId == employeeId && a.PositionId != null)
            .OrderBy(a => a.StartDate)
            .ToListAsync();

        if (assignments.Count == 0)
        {
            return;
        }

        // Legacy lines 1006-1011: an open-ended assignment of an employee who is leaving ends on the last day
        // in service.
        var lastDayInService = await _separationRepository.GetAll()
            .Where(s => s.EmployeeId == employeeId && s.RevokeDate == null)
            .OrderByDescending(s => s.LastDayInService)
            .Select(s => (DateTime?)s.LastDayInService)
            .FirstOrDefaultAsync();

        var touchedPositions = new HashSet<int>();

        foreach (var assignment in assignments)
        {
            var positionId = assignment.PositionId!.Value;
            var endDate = assignment.EndDate.Date == InfiniteEndDate && lastDayInService.HasValue
                ? lastDayInService.Value.Date
                : assignment.EndDate;

            touchedPositions.Add(positionId);

            var holder = await _relationshipRepository.GetAll()
                .Where(r => r.PositionId == positionId
                            && r.EmployeeId == employeeId
                            && r.StartDate == assignment.StartDate)
                .OrderBy(r => r.Id)
                .FirstOrDefaultAsync();

            if (holder != null && holder.RelationType == PositionRelationType.Holder && holder.EndDate == endDate)
            {
                continue;
            }

            holder ??= new PositionRelationship
            {
                TenantId = assignment.TenantId,
                PositionId = positionId,
                EmployeeId = employeeId,
                StartDate = assignment.StartDate
            };

            holder.RelationType = PositionRelationType.Holder;
            holder.EndDate = endDate;

            await ValidateHolderAsync(holder);

            if (holder.Id == 0)
            {
                await _relationshipRepository.InsertAsync(holder);
            }
            else
            {
                await _relationshipRepository.UpdateAsync(holder);
            }
        }

        await SaveAsync();

        foreach (var positionId in touchedPositions)
        {
            await RecomputeVacancyAsync(positionId);
        }
    }

    public async Task RemoveHolderAsync(int? positionId, int employeeId, DateTime startDate)
    {
        if (!positionId.HasValue || !await IsEnabledAsync())
        {
            return;
        }

        var holders = await _relationshipRepository.GetAll()
            .Where(r => r.PositionId == positionId.Value
                        && r.EmployeeId == employeeId
                        && r.StartDate == startDate)
            .ToListAsync();

        foreach (var holder in holders)
        {
            await _relationshipRepository.DeleteAsync(holder);
        }

        await SaveAsync();
        await RecomputeVacancyAsync(positionId.Value);
    }

    private async Task<bool> IsEnabledAsync()
    {
        var orgSettings = await _sysSettingsAccessor.GetOrgSettingsAsync();
        return orgSettings != null && orgSettings.EnablePosition;
    }

    private async Task SaveAsync()
    {
        if (CurrentUnitOfWork != null)
        {
            await CurrentUnitOfWork.SaveChangesAsync();
        }
    }
}
