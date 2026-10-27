namespace HCMv3.Licensing;

/// <summary>
/// A part of HCMv3 that is licensed per user (Option B of the licensing design, see
/// docs/design/legacy-gap-batch8-round-b.md section 4).
///
/// <para>
/// Ported from legacy <c>HCM10.Module.BusinessObjects.SysSecurity.LicenseModuleType</c>, keeping its numbers so a
/// migrated licence line maps one to one. Legacy value 3, "Active Employees", is deliberately absent: it counted
/// employees, not users, and is the <see cref="HcmFeatures.MaxActiveEmployees"/> edition feature here.
/// </para>
/// </summary>
public enum LicenceModule
{
    /// <summary>The administration application, every <c>Pages.*</c> screen. Legacy "HRMS".</summary>
    Admin = 0,

    /// <summary>Employee self service, every <c>Ess.*</c> screen.</summary>
    Ess = 1,

    /// <summary>The mobile application: geo punch and handset registration.</summary>
    Mobile = 2,

    Performance = 4,

    Recruitment = 5
}
