using Xunit;

namespace HCMv3.Tests;

/// <summary>
/// For test classes that set <c>PortedReportCatalog</c>, which is one list for the whole process. Classes in
/// the same collection never run at the same time, so one cannot empty the list while another is reading it.
/// </summary>
[CollectionDefinition(Name)]
public class PortedReportCatalogCollection
{
    public const string Name = "PortedReportCatalog";
}
