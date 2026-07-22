using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.OrganizationManagement;

/// <summary>
/// Organization unit type master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.OrganizationManagement.OrganizationUnitType</c>).
/// Extends the Code/NameEn/NameAr shape with <see cref="AllowLeaveConflict"/> and
/// <see cref="AssignPayrollArea"/>, so it can't derive from <see cref="SimpleLookupEntity"/>
/// (see <see cref="City"/> for the same bespoke pattern). <see cref="Code"/> must be one of the 10
/// fixed <c>Level*Code</c> constants below (dropdown-selected by the user, not auto-generated).
///
/// This classifies the depth/kind of an <c>Abp.Organizations.OrganizationUnit</c> node via
/// <c>OrgUnitProfile.OrganizationUnitTypeId</c> — it does not itself constrain the
/// actual tree depth, which is separately capped at 10 by <c>OrganizationUnitAppService</c>.
/// </summary>
public class OrganizationUnitType : FullAuditedEntity, IMustHaveTenant
{
    public const string Level0Code = "Level0";
    public const string Level1Code = "Level1";
    public const string Level2Code = "Level2";
    public const string Level3Code = "Level3";
    public const string Level4Code = "Level4";
    public const string Level5Code = "Level5";
    public const string Level6Code = "Level6";
    public const string Level7Code = "Level7";
    public const string Level8Code = "Level8";
    public const string Level9Code = "Level9";

    public static readonly string[] AllowedCodes =
    {
        Level0Code, Level1Code, Level2Code, Level3Code, Level4Code, Level5Code,
        Level6Code, Level7Code, Level8Code, Level9Code
    };

    public int TenantId { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public virtual string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string NameEn { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string NameAr { get; set; }

    public virtual bool AllowLeaveConflict { get; set; }

    public virtual bool AssignPayrollArea { get; set; }
}
