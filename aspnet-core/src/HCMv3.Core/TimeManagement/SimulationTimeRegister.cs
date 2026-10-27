using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.TimeManagement;

/// <summary>
/// "Dry-run" preview version of <see cref="TimeRegister"/> for a given employee/day, used to preview
/// calculation results before committing them (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TimeManagement.SimulationTimeRegister</c>, Wave 4).
///
/// The legacy base class is inconsistently plain <c>XPObject</c> (not <c>MyBaseObject</c>, unlike every
/// other entity in this module) -- this does not matter for the port; <see cref="FullAuditedEntity"/> +
/// <see cref="IMustHaveTenant"/> is used here for consistency with the rest of the module. Same dead-code
/// skip as <see cref="MonthlyTimeRegisterDetail"/>/<see cref="YearlyTimeRegisterDetail"/> applies to the
/// child (see <see cref="SimulationTimeRegisterDetail"/>).
/// </summary>
public class SimulationTimeRegister : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DateTime DayDate { get; set; }

    public virtual int? DutyPeriodId { get; set; }

    public virtual DutyPeriod DutyPeriod { get; set; }
}
