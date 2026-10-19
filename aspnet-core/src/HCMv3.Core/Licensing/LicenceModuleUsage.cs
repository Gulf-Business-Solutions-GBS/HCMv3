namespace HCMv3.Licensing;

/// <summary>How many seats of one module a tenant has and uses.</summary>
public class LicenceModuleUsage
{
    public LicenceModule Module { get; set; }

    /// <summary>The module's on/off edition feature is on (always true for Admin).</summary>
    public bool IsModuleEnabled { get; set; }

    /// <summary>Seats in the edition; 0 means unlimited.</summary>
    public int SeatCount { get; set; }

    /// <summary>Licences held by active, non-administrator users -- legacy's count.</summary>
    public int UsedCount { get; set; }

    public bool IsUnlimited => SeatCount == 0;

    public bool HasFreeSeat => IsUnlimited || UsedCount < SeatCount;
}
