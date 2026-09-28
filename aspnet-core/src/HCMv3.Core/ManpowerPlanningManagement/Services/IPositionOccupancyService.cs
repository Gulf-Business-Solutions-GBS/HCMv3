using System;
using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.ManpowerPlanningManagement.Services;

/// <summary>
/// Keeps position holders (<see cref="PositionRelationship"/>) and <see cref="Position.Vacant"/> in step with
/// employees' org assignments: legacy <c>EmployeeOrgAssignment.UpdatedPositionRelation</c>,
/// <c>UpdatedAllPositionRelation</c>, <c>DeletePositionRelation</c> and
/// <c>PositionRelationship.PerformValidation</c> / <c>UpdatePositionStatus</c>.
/// </summary>
public interface IPositionOccupancyService : IDomainService
{
    /// <summary>
    /// Legacy <c>PositionRelationship.PerformValidation</c>: EndDate not before StartDate, and the position's
    /// FTE limit not exceeded by overlapping holders who are other employees.
    /// </summary>
    Task ValidateHolderAsync(PositionRelationship relationship);

    /// <summary>
    /// Legacy <c>UpdatePositionStatus</c>: a position is vacant when no holder row covers today.
    /// </summary>
    Task RecomputeVacancyAsync(int positionId, int excludeRelationshipId = 0);

    /// <summary>
    /// Legacy <c>UpdatedAllPositionRelation</c>, when <c>EnablePosition</c> is on: every org assignment of the
    /// employee that names a position gets a holder row (position, employee, start date) ending with the
    /// assignment, or on the last day in service when the open-ended assignment belongs to an employee with a
    /// separation that is not revoked.
    /// </summary>
    Task SyncEmployeeAsync(int employeeId);

    /// <summary>
    /// Legacy <c>DeletePositionRelation</c> (and the edit branch of <c>UpdatedPositionRelation</c>), when
    /// <c>EnablePosition</c> is on: removes the holder row an assignment used to have.
    /// </summary>
    Task RemoveHolderAsync(int? positionId, int employeeId, DateTime startDate);
}
