using System.Collections.Generic;
using System.Threading.Tasks;

namespace HCMv3.ReportManagement.Services;

/// <summary>
/// Turns a saved <see cref="ReportSchedule"/> into the concrete list of slices to render, each with its
/// resolved recipients.
///
/// This is where the customer's requirement actually lives: one schedule, many outputs, each delivered
/// only to the people responsible for that slice. Everything downstream (rendering, delivery, logging)
/// is mechanical once this has run.
///
/// RESOLUTION HAPPENS AT RUN TIME, EVERY TIME. Nothing here is cached onto the schedule, because the
/// org chart moves: departments are created, secretaries change, managers take over new teams. A
/// schedule authored in January must reflect the org chart in June without anybody editing it.
/// </summary>
public interface IReportBurstResolver
{
    /// <summary>
    /// Resolves the slices for <paramref name="schedule"/>.
    ///
    /// A schedule with <see cref="ReportBurstDimension.None"/> yields exactly one slice with an empty
    /// key. A burst schedule yields one slice per distinct key that has at least one recipient -- keys
    /// with no resolvable recipient are dropped rather than rendered, since a file nobody receives is
    /// wasted work and a misleading run-log entry.
    /// </summary>
    Task<List<ReportBurstSlice>> ResolveSlicesAsync(ReportSchedule schedule);
}
