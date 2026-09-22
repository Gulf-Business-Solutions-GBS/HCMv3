using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee's scanned signature image (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeSignature</c>).
/// </summary>
public class EmployeeSignature : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    /// <summary>Where the signature image is, relative to the file store; never kept in the database.</summary>
    [StringLength(512)]
    public virtual string SignaturePath { get; set; }

    /// <summary>The old <c>Signature</c> column, kept only for <c>LegacyImageMover</c> to empty.</summary>
    [Column("Signature")]
    public virtual byte[] LegacySignature { get; set; }
}
