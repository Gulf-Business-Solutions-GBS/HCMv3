using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PayrollManagement;

/// <summary>
/// PayrollCalendar (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.PayrollCalendar</c>). The legacy
/// <c>IExcelImport</c> marker is an XAF UI concept and is not ported.
/// </summary>
public class PayrollCalendar : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int PayrollAreaId { get; set; }

    public virtual PayrollArea PayrollArea { get; set; }

    public virtual int PayrollMonth { get; set; }

    public virtual int PayrollYear { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }
}
