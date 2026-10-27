using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.TimeManagement;

/// <summary>
/// Roster Group Schedule (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TimeManagement.RosterGroupSchedule</c>), linking a <see cref="RosterGroup"/>
/// to a duty period template for a date range.
///
/// NAMING NOTE (verified by reading the legacy source): same legacy naming inconsistency as
/// <see cref="RosterDutySchedule"/> — <c>DutyPeriod</c> references the CLASSIC <see cref="TimeManagement.DutyPeriod"/>
/// entity, NOT <see cref="RosterDutyPeriod"/>. Ported faithfully.
///
/// The legacy <c>IExcelImport</c> marker is an XAF UI concept and is not ported.
/// </summary>
public class RosterGroupSchedule : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int RosterGroupId { get; set; }

    public virtual RosterGroup RosterGroup { get; set; }

    public virtual int DutyPeriodId { get; set; }

    public virtual DutyPeriod DutyPeriod { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    public virtual int StartPoint { get; set; }
}
