using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Records that a given <see cref="PayrollManagement.WageType"/> excludes another WageType (ported from
/// legacy XAF <c>HCM10.Module.BusinessObjects.PayrollManagement.WageTypeException</c>), listed separately
/// and linked back via <see cref="WageTypeId"/>.
///
/// BUG FIX (not preserved): the legacy <c>ExceptWageType</c> property getter/setter is a copy-paste defect
/// that actually reads/writes the same underlying storage key as <c>WageType</c> (i.e. the two fields alias
/// each other in the legacy XPO schema), making the entity functionally useless. This port implements
/// <see cref="ExceptWageTypeId"/> as its own genuinely distinct column/FK — preserving the legacy bug would
/// make this entity meaningless.
/// </summary>
public class WageTypeException : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int WageTypeId { get; set; }

    public virtual WageType WageType { get; set; }

    public virtual int ExceptWageTypeId { get; set; }

    public virtual WageType ExceptWageType { get; set; }
}
