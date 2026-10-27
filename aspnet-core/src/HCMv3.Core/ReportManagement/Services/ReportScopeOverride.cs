using System;
using System.Collections.Generic;
using System.Threading;
using Abp.Dependency;

namespace HCMv3.ReportManagement.Services;

/// <inheritdoc cref="IReportScopeOverride"/>
public class ReportScopeOverride : IReportScopeOverride, ISingletonDependency
{
    /// <summary>
    /// AsyncLocal rather than an injected per-request field: the executor sets the scope, and the code
    /// that reads it is a grid-report app service resolved separately and several awaits away. An
    /// AsyncLocal flows across those awaits within the one logical operation without the two having to
    /// know about each other, and stays isolated between concurrently rendering slices.
    /// </summary>
    private static readonly AsyncLocal<State> Current = new AsyncLocal<State>();

    public bool IsActive => Current.Value?.IsActive == true;

    public HashSet<int> VisibleEmployeeIds => Current.Value?.VisibleEmployeeIds;

    public IDisposable Begin(HashSet<int> visibleEmployeeIds)
    {
        var previous = Current.Value;
        Current.Value = new State { IsActive = true, VisibleEmployeeIds = visibleEmployeeIds };
        return new Handle(previous);
    }

    private sealed class State
    {
        public bool IsActive { get; init; }

        public HashSet<int> VisibleEmployeeIds { get; init; }
    }

    private sealed class Handle : IDisposable
    {
        private readonly State _previous;
        private bool _disposed;

        public Handle(State previous)
        {
            _previous = previous;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            // Restore rather than clear. Nesting is not expected, but leaving a stale override in place
            // would silently mis-scope whatever ran next on this execution context.
            Current.Value = _previous;
        }
    }
}
