using HCMv3.PayrollManagement;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// AssetType master (ported from legacy XAF HCM10.Module.BusinessObjects.GeneralMasterInfo.AssetType).
/// Code/NameEn/NameAr (see <see cref="SimpleLookupEntity"/>) plus the legacy <see cref="IncludeInWorkflow"/>
/// switch and <see cref="WageTypeId"/>. Because of those two it has its own screen and app service
/// (<c>AssetTypesAppService</c>) rather than the shared simple-lookup ones.
/// </summary>
public class AssetType : SimpleLookupEntity
{
    /// <summary>
    /// Legacy <c>AssetType.IncludeInWorkflow</c>: assets of this type may be asked for through self-service.
    /// Legacy ESS (<c>GeneralController</c>) offered only assets whose type has this set.
    /// </summary>
    public virtual bool IncludeInWorkflow { get; set; }

    /// <summary>Legacy <c>AssetType.WageType</c>: the wage type used for this type's asset payments. Optional.</summary>
    public virtual int? WageTypeId { get; set; }

    public virtual WageType WageType { get; set; }
}
