using System;
using System.Collections.Generic;

namespace HCMv3.Licensing;

/// <summary>
/// What one user holds, cached because it is read on every permission check: ASP.NET Zero asks for every permission
/// when the Angular client loads its configuration, a couple of thousand checks in one request.
///
/// <para>
/// Cleared by <see cref="IUserModuleLicenceManager"/> whenever that user's licences change. Role changes (becoming
/// or ceasing to be an administrator) are picked up when the entry expires, a few minutes later; see
/// <see cref="UserModuleLicenceChecker.CacheLifetime"/>.
/// </para>
/// </summary>
[Serializable]
public class UserLicenceCacheItem
{
    public const string CacheName = "HcmUserModuleLicences";

    /// <summary>Holds the static Admin role: never needs a licence and is never counted, as legacy.</summary>
    public bool IsExempt { get; set; }

    public List<LicenceModule> Modules { get; set; } = new();
}
