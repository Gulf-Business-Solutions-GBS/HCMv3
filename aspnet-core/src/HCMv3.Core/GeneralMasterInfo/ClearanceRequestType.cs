namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// ClearanceRequestType master (ported from legacy XAF HCM10.Module.BusinessObjects.GeneralMasterInfo.ClearanceRequestType).
/// Code/NameEn/NameAr lookup -- see <see cref="SimpleLookupEntity"/> -- plus the legacy
/// <see cref="IncludeInWorkflow"/> flag.
/// </summary>
public class ClearanceRequestType : SimpleLookupEntity
{
    /// <summary>
    /// Whether an employee may raise a clearance of this type through self service. Off by default, so a
    /// type kept only for reporting or for historical imports does not silently appear on the ESS form: the
    /// ESS type dropdown lists the types with this set and nothing else.
    /// </summary>
    public virtual bool IncludeInWorkflow { get; set; }
}
