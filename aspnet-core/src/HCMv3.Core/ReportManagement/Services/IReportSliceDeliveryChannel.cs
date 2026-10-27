using System.Threading.Tasks;

namespace HCMv3.ReportManagement.Services;

/// <summary>Delivers one rendered slice to one recipient through one destination.</summary>
public interface IReportSliceDeliveryChannel
{
    ReportDestinationMethod Method { get; }

    /// <summary>
    /// Delivers the file. Implementations MUST throw on failure rather than returning quietly -- the
    /// executor records the exception against that slice, and a channel that swallows errors would log
    /// a success for a report nobody received.
    /// </summary>
    Task DeliverAsync(
        ReportSchedule schedule,
        ReportScheduleDestination destination,
        ReportBurstSlice slice,
        ReportSliceRecipient recipient,
        RenderedReportSlice rendered);
}
