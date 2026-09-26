using System;
using System.Linq;
using System.Reflection;
using HCMv3.Ess;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.MobileClient;

/// <summary>
/// The mobile app reaches each ESS service at api/services/app/{Service}/{Method}, where {Service} is taken
/// from the proxy's class name. ABP names the server route after the app service the same way, so a proxy
/// whose name drifts from its interface would call an address that does not exist.
/// </summary>
public class EssProxyEndpoints_Tests
{
    private static readonly Type[] EssProxyTypes = typeof(ProxyEssSessionAppService).Assembly
        .GetTypes()
        .Where(t => t.Namespace == "HCMv3.Ess" && t.Name.StartsWith("Proxy") && t.Name.EndsWith("AppService") && !t.IsAbstract)
        .ToArray();

    private static string GetEndpoint(Type proxyType, string methodName)
    {
        var proxy = Activator.CreateInstance(proxyType);
        var getEndpoint = typeof(ProxyAppServiceBase).GetMethod("GetEndpoint", BindingFlags.Instance | BindingFlags.NonPublic);
        return (string)getEndpoint!.Invoke(proxy, new object[] { methodName });
    }

    [Fact]
    public void Should_Have_A_Proxy_For_Every_Ess_Service_The_App_Uses()
    {
        EssProxyTypes.Select(t => t.Name).ShouldBe(
            new[]
            {
                nameof(ProxyEssInboxAppService),
                nameof(ProxyEssLookupAppService),
                nameof(ProxyEssSessionAppService),
                nameof(ProxyMyExitPermitAppService),
                nameof(ProxyMyGeoPunchAppService),
                nameof(ProxyMyLeaveAppService),
            },
            ignoreOrder: true);
    }

    [Fact]
    public void Should_Name_Each_Proxy_After_The_Service_Interface_It_Implements()
    {
        foreach (var proxyType in EssProxyTypes)
        {
            var serviceInterface = proxyType.GetInterfaces().Single(i => i.Namespace == "HCMv3.Ess");

            ("I" + proxyType.Name.Substring("Proxy".Length)).ShouldBe(serviceInterface.Name);
        }
    }

    [Theory]
    [InlineData(typeof(ProxyEssSessionAppService), "GetEssContext", "api/services/app/EssSession/GetEssContext")]
    [InlineData(typeof(ProxyMyLeaveAppService), "GetMyLeaves", "api/services/app/MyLeave/GetMyLeaves")]
    [InlineData(typeof(ProxyMyGeoPunchAppService), "RecordMyGeoPunch", "api/services/app/MyGeoPunch/RecordMyGeoPunch")]
    [InlineData(typeof(ProxyMyExitPermitAppService), "RequestExitPermit", "api/services/app/MyExitPermit/RequestExitPermit")]
    [InlineData(typeof(ProxyEssInboxAppService), "Approve", "api/services/app/EssInbox/Approve")]
    [InlineData(typeof(ProxyEssLookupAppService), "GetAbsenceTypes", "api/services/app/EssLookup/GetAbsenceTypes")]
    public void Should_Call_The_Server_Address_Of_The_Matching_App_Service(Type proxyType, string methodName, string expectedEndpoint)
    {
        GetEndpoint(proxyType, methodName).ShouldBe(expectedEndpoint);
    }
}
