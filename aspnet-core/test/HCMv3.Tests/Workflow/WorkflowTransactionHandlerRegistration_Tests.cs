using System;
using System.Linq;
using System.Reflection;
using HCMv3.Workflow.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// Dependency-injection regression test for <see cref="IWorkflowTransactionHandler"/>.
///
/// <para>
/// This exists because the failure mode is silent. If conventional registration misses a handler, the
/// injected <c>IEnumerable&lt;IWorkflowTransactionHandler&gt;</c> is simply shorter, the resolver returns
/// null for that type, and the approval engine proceeds with no validation and no side effects -- an
/// approved leave that never got marked approved on its own row, with no error anywhere. Two sibling waves
/// in this codebase (<c>IWageTypeCalculator</c>, <c>IPunchResolutionStrategy</c>) were bitten by exactly
/// this, which is why both carry the same test.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class WorkflowTransactionHandlerRegistration_Tests : AppTestBase
{
    [Fact]
    public void Every_Handler_In_The_Core_Assembly_Should_Resolve_From_The_Container()
    {
        var handlerTypesInAssembly = typeof(IWorkflowTransactionHandler).Assembly
            .GetTypes()
            .Where(t => t.IsClass
                        && !t.IsAbstract
                        && typeof(IWorkflowTransactionHandler).IsAssignableFrom(t))
            .ToList();

        handlerTypesInAssembly.ShouldNotBeEmpty(
            "No IWorkflowTransactionHandler implementations were found at all -- the reference handler for " +
            "leave should be one of them.");

        var resolved = LocalIocManager.ResolveAll<IWorkflowTransactionHandler>().ToList();

        foreach (var handlerType in handlerTypesInAssembly)
        {
            resolved.Any(h => h.GetType() == handlerType).ShouldBeTrue(
                handlerType.Name + " implements IWorkflowTransactionHandler but does not resolve from the " +
                "container. It will be silently ignored: its transaction type would route with no " +
                "validation and no side effects.");
        }
    }

    [Fact]
    public void Every_Handler_Should_Declare_A_RequestType()
    {
        var handlers = LocalIocManager.ResolveAll<IWorkflowTransactionHandler>().ToList();

        foreach (var handler in handlers)
        {
            handler.RequestType.ShouldNotBeNullOrWhiteSpace(
                handler.GetType().Name + " declares no RequestType, so nothing can ever dispatch to it.");
        }
    }

    [Fact]
    public void No_Two_Handlers_Should_Claim_The_Same_RequestType()
    {
        // The resolver throws on construction if this is violated; asserting it here names the offenders
        // instead of failing every workflow test with a container error.
        var handlers = LocalIocManager.ResolveAll<IWorkflowTransactionHandler>().ToList();

        var duplicates = handlers
            .GroupBy(h => h.RequestType, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        duplicates.ShouldBeEmpty(
            "More than one handler claims: " + string.Join(", ", duplicates) +
            ". Which one runs would depend on assembly scan order.");
    }

    [Fact]
    public void The_Resolver_Should_Find_The_Leave_Handler_And_Nothing_For_An_Unknown_Type()
    {
        var resolver = Resolve<IWorkflowTransactionHandlerResolver>();

        resolver.Resolve("EmployeeAbsence").ShouldNotBeNull();

        // Null is a legitimate answer, not an error -- the engine reads it as "generic approval only".
        resolver.Resolve("SomethingWithNoHandler").ShouldBeNull();
        resolver.Resolve(null).ShouldBeNull();
    }

    /// <summary>
    /// The invariant that ties the two halves of the design together: every transaction type registered in
    /// the catalog has a handler, and every handler has a catalog row.
    ///
    /// <para>
    /// A catalog row without a handler is the dangerous direction. Such a type routes for approval and gets
    /// approved, but nothing ever stamps <c>WorkflowStatus</c> on its transaction row -- so the row stays
    /// invisible to the <c>ApprovedOnly</c> filter forever, approved in the workflow tables and absent
    /// everywhere else. No error is raised at any point.
    /// </para>
    /// </summary>
    [Fact]
    public void Every_Seeded_Transaction_Type_Should_Have_A_Handler_And_Vice_Versa()
    {
        var seededCodes = UsingDbContext(context => context
            .Set<HCMv3.Workflow.WorkflowTransactionType>()
            .Select(t => t.Code)
            .ToList());

        seededCodes.ShouldNotBeEmpty("The transaction-type catalog seed did not run.");

        var handlerTypes = LocalIocManager.ResolveAll<IWorkflowTransactionHandler>()
            .Select(h => h.RequestType)
            .ToList();

        var withoutHandler = seededCodes
            .Where(c => !handlerTypes.Contains(c, StringComparer.OrdinalIgnoreCase))
            .ToList();

        withoutHandler.ShouldBeEmpty(
            "Catalogued but unhandled: " + string.Join(", ", withoutHandler) +
            ". These would approve without ever being marked approved on their own row.");

        var withoutCatalogRow = handlerTypes
            .Where(t => !seededCodes.Contains(t, StringComparer.OrdinalIgnoreCase))
            .ToList();

        withoutCatalogRow.ShouldBeEmpty(
            "Handled but not catalogued: " + string.Join(", ", withoutCatalogRow) +
            ". Nothing can route to these, because routing starts from the catalog.");
    }

    [Fact]
    public void The_Resolver_Should_Find_A_Handler_For_Each_Of_The_Six_Types()
    {
        var resolver = Resolve<IWorkflowTransactionHandlerResolver>();

        foreach (var code in new[]
                 {
                     "EmployeeAbsence", "EmployeeAsset", "EmployeeDocument",
                     "EmployeeLoan", "EmployeeOvertime", "EmployeeTicket"
                 })
        {
            resolver.Resolve(code).ShouldNotBeNull(code + " has no handler.");
        }
    }
}
