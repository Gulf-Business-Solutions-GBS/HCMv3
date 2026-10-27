using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.TimeManagement;

/// <summary>
/// Manual transfer/adjustment of a time-related balance for an Employee (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TimeManagement.TimeTransfer</c>, Wave 4). The legacy commented-out
/// duplicate (Employee, TransferType, TransferDate) check was never enabled and is NOT ported. The legacy
/// <c>IExcelImport</c> marker is an XAF UI concept and is not ported.
/// </summary>
public class TimeTransfer : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DateTime TransferDate { get; set; }

    public virtual int TransferTypeId { get; set; }

    public virtual TimeTransferType TransferType { get; set; }

    /// <summary>
    /// Rounded to 3 decimal places on save (legacy <c>Math.Round(TransferCount, 3)</c> in
    /// <c>OnSaving</c>) -- ported into <c>TimeTransfersAppService</c>.
    /// </summary>
    public virtual double TransferCount { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }
}
