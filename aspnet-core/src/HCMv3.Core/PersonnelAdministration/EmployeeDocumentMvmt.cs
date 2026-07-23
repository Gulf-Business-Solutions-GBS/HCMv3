using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Movement (withdraw/return) log for an <see cref="EmployeeDocument"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeDocumentMvmt</c>). Legacy has an
/// overlap-validation block but it is commented out (dead code) -- only blank-field validation is
/// ported.
/// </summary>
public class EmployeeDocumentMvmt : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeDocumentId { get; set; }

    public virtual EmployeeDocument EmployeeDocument { get; set; }

    public virtual DateTime WithdrawDate { get; set; }

    public virtual DateTime ReturnDate { get; set; }

    /// <summary>
    /// Default(DateTime) means the document hasn't actually been returned yet -- see
    /// <see cref="HCMv3.PersonnelAdministration.DocumentMvmtStatus"/> (computed on the parent document).
    /// </summary>
    public virtual DateTime ActualReturnDate { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }
}
