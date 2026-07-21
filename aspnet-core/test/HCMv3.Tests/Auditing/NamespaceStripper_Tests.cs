using HCMv3.Auditing;
using HCMv3.Test.Base;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Auditing;

// ReSharper disable once InconsistentNaming
public class NamespaceStripper_Tests : AppTestBase
{
    private readonly INamespaceStripper _namespaceStripper;

    public NamespaceStripper_Tests()
    {
        _namespaceStripper = Resolve<INamespaceStripper>();
    }

    [Fact]
    public void Should_Stripe_Namespace()
    {
        var controllerName = _namespaceStripper.StripNameSpace("HCMv3.Web.Controllers.HomeController");
        controllerName.ShouldBe("HomeController");
    }

    [Theory]
    [InlineData("HCMv3.Auditing.GenericEntityService`1[[HCMv3.Storage.BinaryObject, HCMv3.Core, Version=1.10.1.0, Culture=neutral, PublicKeyToken=null]]", "GenericEntityService<BinaryObject>")]
    [InlineData("CompanyName.ProductName.Services.Base.EntityService`6[[CompanyName.ProductName.Entity.Book, CompanyName.ProductName.Core, Version=1.10.1.0, Culture=neutral, PublicKeyToken=null],[CompanyName.ProductName.Services.Dto.Book.CreateInput, N...", "EntityService<Book, CreateInput>")]
    [InlineData("HCMv3.Auditing.XEntityService`1[HCMv3.Auditing.AService`5[[HCMv3.Storage.BinaryObject, HCMv3.Core, Version=1.10.1.0, Culture=neutral, PublicKeyToken=null],[HCMv3.Storage.TestObject, HCMv3.Core, Version=1.10.1.0, Culture=neutral, PublicKeyToken=null],]]", "XEntityService<AService<BinaryObject, TestObject>>")]
    public void Should_Stripe_Generic_Namespace(string serviceName, string result)
    {
        var genericServiceName = _namespaceStripper.StripNameSpace(serviceName);
        genericServiceName.ShouldBe(result);
    }
}
