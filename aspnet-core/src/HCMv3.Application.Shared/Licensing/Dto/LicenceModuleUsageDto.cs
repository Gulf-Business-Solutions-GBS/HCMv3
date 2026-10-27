namespace HCMv3.Licensing.Dto;

/// <summary>One module on the Licence Assignment screen: is it in the edition, how many seats, how many used.</summary>
public class LicenceModuleUsageDto
{
    public LicenceModule Module { get; set; }

    /// <summary>Localized module name.</summary>
    public string ModuleName { get; set; }

    /// <summary>The module's on/off edition feature is on. A switched-off module cannot be assigned.</summary>
    public bool IsModuleEnabled { get; set; }

    /// <summary>Seats in the tenant's edition; 0 means unlimited.</summary>
    public int SeatCount { get; set; }

    /// <summary>Licences held by active users who are not administrators.</summary>
    public int UsedCount { get; set; }

    public bool IsUnlimited { get; set; }

    /// <summary>
    /// More seats in use than the edition allows -- possible after reactivating users or after the host lowered the
    /// count. Nothing is taken away automatically; the screen shows it so an administrator can unassign.
    /// </summary>
    public bool IsOverLimit { get; set; }
}
