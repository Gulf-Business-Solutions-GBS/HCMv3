using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Abp.Dependency;
using HCMv3.Dto;
using HCMv3.Storage;
using Abp.Timing;
using HCMv3.ReportManagement.Services;

namespace HCMv3.ReportManagement;

/// <summary>
/// Renders a burst slice by executing the underlying GRID report and returning its Excel bytes.
///
/// The report is DISCOVERED, not registered -- see <see cref="ISchedulableReportCatalog"/>. A report
/// added next year is schedulable the day it is written, with no change here.
///
/// HOW SCOPING AND BURSTING BOTH WORK WITHOUT TOUCHING THE REPORT. Every grid report filters on
/// <c>rlsEmployeeIds.Contains(EmployeeId)</c>, resolved through
/// <c>GridReportAppServiceBase.GetRlsEmployeeIdsAsync()</c>. The executor has already published the
/// effective employee set on <see cref="IReportScopeOverride"/> before calling this, and the base
/// consults that ahead of the session. So both the recipient's entitlement AND the burst key are
/// expressed as one employee set, and no report needs a burst parameter or any awareness of
/// scheduling whatsoever.
/// </summary>
public class GridReportSliceRenderer : IReportSliceRenderer, ITransientDependency
{
    private readonly ISchedulableReportCatalog _catalog;
    private readonly IIocResolver _iocResolver;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public GridReportSliceRenderer(
        ISchedulableReportCatalog catalog,
        IIocResolver iocResolver,
        ITempFileCacheManager tempFileCacheManager)
    {
        _catalog = catalog;
        _iocResolver = iocResolver;
        _tempFileCacheManager = tempFileCacheManager;
    }

    public bool CanRender(ScheduledReportKind kind) => kind == ScheduledReportKind.GridReport;

    public async Task<RenderedReportSlice> RenderAsync(
        ReportSchedule schedule,
        ReportBurstSlice slice,
        HashSet<int> visibleEmployeeIds)
    {
        var descriptor = _catalog.Find(schedule.ReportKey);
        if (descriptor == null)
        {
            throw new InvalidOperationException(
                $"'{schedule.ReportKey}' is not a schedulable grid report. It may have been renamed or removed.");
        }

        var input = BuildInput(descriptor.InputType, schedule.Filters);

        // Resolve through the container so the app service keeps its ABP interceptors -- unit of work,
        // auditing, authorization -- rather than being newed up bare.
        using (var serviceWrapper = _iocResolver.ResolveAsDisposable<object>(descriptor.ServiceInterface))
        {
            var task = (Task)descriptor.ExportMethod.Invoke(serviceWrapper.Object, new[] { input });
            await task.ConfigureAwait(false);

            var file = (FileDto)task.GetType().GetProperty("Result")!.GetValue(task);

            if (file == null)
            {
                return new RenderedReportSlice { RowCount = 0 };
            }

            var bytes = ReadExportedFile(file);

            return new RenderedReportSlice
            {
                Content = bytes,
                FileName = BuildFileName(schedule, slice, file),
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                // The exporter does not report a row count, so a produced file counts as one unit of
                // content. The executor's real emptiness guard is the scope check that runs before
                // this: a recipient entitled to nothing never reaches here.
                RowCount = bytes == null || bytes.Length == 0 ? 0 : 1,
            };
        }
    }

    /// <summary>
    /// Builds the report's own input DTO from the schedule's saved filters, coercing each string to the
    /// property's real type. Unknown names are IGNORED rather than throwing: a report may legitimately
    /// drop a parameter after a schedule was saved, and failing the whole run for that would take out
    /// every recipient over a stale field.
    /// </summary>
    private static object BuildInput(Type inputType, ICollection<ReportScheduleFilter> filters)
    {
        var input = Activator.CreateInstance(inputType);

        foreach (var filter in filters ?? new List<ReportScheduleFilter>())
        {
            var property = inputType.GetProperty(
                filter.ParameterName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (property == null || !property.CanWrite)
            {
                continue;
            }

            var raw = ResolveRelativeDate(filter) ?? filter.ParameterValue;
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue; // leave the DTO's own default in place
            }

            try
            {
                property.SetValue(input, Coerce(raw, property.PropertyType));
            }
            catch (Exception)
            {
                // A single unconvertible value must not abort everyone's report; the property keeps its
                // default and the run continues.
            }
        }

        return input;
    }

    /// <summary>
    /// Resolves a relative-date token to a concrete value at RUN time.
    ///
    /// Without this a monthly schedule authored in June keeps reporting June forever, which looks like
    /// the job working and is the single easiest way for a distribution module to go quietly stale.
    /// </summary>
    private static string ResolveRelativeDate(ReportScheduleFilter filter)
    {
        if (string.IsNullOrWhiteSpace(filter.RelativeDateToken))
        {
            return null;
        }

        var today = Clock.Now.Date;

        switch (filter.RelativeDateToken.Trim().ToUpperInvariant())
        {
            case "TODAY":
                return today.ToString("O", CultureInfo.InvariantCulture);
            case "YESTERDAY":
                return today.AddDays(-1).ToString("O", CultureInfo.InvariantCulture);
            case "CURRENTMONTHSTART":
                return new DateTime(today.Year, today.Month, 1).ToString("O", CultureInfo.InvariantCulture);
            case "CURRENTMONTHEND":
                return new DateTime(today.Year, today.Month, 1).AddMonths(1).AddDays(-1)
                    .ToString("O", CultureInfo.InvariantCulture);
            case "PREVIOUSMONTHSTART":
                return new DateTime(today.Year, today.Month, 1).AddMonths(-1)
                    .ToString("O", CultureInfo.InvariantCulture);
            case "PREVIOUSMONTHEND":
                return new DateTime(today.Year, today.Month, 1).AddDays(-1)
                    .ToString("O", CultureInfo.InvariantCulture);
            case "CURRENTYEAR":
                return today.Year.ToString(CultureInfo.InvariantCulture);
            case "CURRENTMONTH":
                return today.Month.ToString(CultureInfo.InvariantCulture);
            case "PREVIOUSMONTH":
                return today.AddMonths(-1).Month.ToString(CultureInfo.InvariantCulture);
            case "PREVIOUSMONTHYEAR":
                return today.AddMonths(-1).Year.ToString(CultureInfo.InvariantCulture);
            default:
                return null;
        }
    }

    private static object Coerce(string raw, Type targetType)
    {
        var type = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (type == typeof(string))
        {
            return raw;
        }

        if (type.IsEnum)
        {
            return Enum.Parse(type, raw, ignoreCase: true);
        }

        if (type == typeof(DateTime))
        {
            return DateTime.Parse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        var converter = TypeDescriptor.GetConverter(type);
        return converter.ConvertFromInvariantString(raw);
    }

    /// <summary>
    /// FileDto carries a TOKEN into ABP's temp-file cache, not the bytes. The grid exporter stages the
    /// workbook there (GridReportExcelExporter calls SetFile) and the browser normally fetches it via
    /// FileController; a scheduled run has no browser, so it reads the same cache directly by token.
    /// </summary>
    private byte[] ReadExportedFile(FileDto file)
    {
        if (string.IsNullOrEmpty(file.FileToken))
        {
            return null;
        }

        return _tempFileCacheManager.GetFile(file.FileToken);
    }

    private static string BuildFileName(ReportSchedule schedule, ReportBurstSlice slice, FileDto file)
    {
        var stem = string.IsNullOrWhiteSpace(slice?.Display)
            ? schedule.Name
            : $"{schedule.Name} - {slice.Display}";

        var safe = new string(stem.Select(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-' ? c : '_').ToArray());

        return $"{safe.Trim()}.xlsx";
    }
}
