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
}
