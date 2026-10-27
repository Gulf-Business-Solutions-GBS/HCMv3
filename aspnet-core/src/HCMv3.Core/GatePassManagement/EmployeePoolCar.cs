using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.GatePassManagement;

/// <summary>
/// Employee-to-pool-car assignment (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.GatePassManagement.EmployeePoolCar</c>). Legacy <c>PerformValidation</c>
/// parity (required Driver/PoolCar/StartDate/EndDate, EndDate &gt;= StartDate) is ported in
/// <c>EmployeePoolCarsAppService</c>.
/// </summary>
public class EmployeePoolCar : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int DriverEmployeeId { get; set; }

    public virtual Employee Driver { get; set; }

    public virtual int PoolCarId { get; set; }

    public virtual PoolCar PoolCar { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    public virtual DateTime StartTime { get; set; }

    public virtual DateTime EndTime { get; set; }

    public virtual bool IsReturned { get; set; }
}
