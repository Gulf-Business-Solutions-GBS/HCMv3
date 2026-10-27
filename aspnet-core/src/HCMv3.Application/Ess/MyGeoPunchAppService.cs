using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Auditing;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Timing;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.Ess.Dto;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.Storage;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Services;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Ess;

/// <inheritdoc cref="IMyGeoPunchAppService"/>
[AbpAuthorize(AppPermissions.Ess_GeoPunch)]
public class MyGeoPunchAppService : HCMv3AppServiceBase, IMyGeoPunchAppService
{
    private const string SelfieDescription = "GeoPunchSelfie";

    private static readonly string[] SelfieContentTypes = { "image/jpeg", "image/png", "image/webp" };

    private readonly IRepository<TimeEvent> _timeEventRepository;
    private readonly IRepository<TransType> _transTypeRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeTimePolicy> _employeeTimePolicyRepository;
    private readonly IRepository<EmployeeDutyPeriod> _employeeDutyPeriodRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<EmployeeExtdWorkLocation> _extWorkLocationRepository;
    private readonly IRepository<BreakShift> _breakShiftRepository;
    private readonly IPunchDutyResolver _punchDutyResolver;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly IBinaryObjectManager _binaryObjectManager;
    private readonly IEmployeeScopeProvider _employeeScopeProvider;
    private readonly IClientInfoProvider _clientInfoProvider;

    public MyGeoPunchAppService(
        IRepository<TimeEvent> timeEventRepository,
        IRepository<TransType> transTypeRepository,
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeTimePolicy> employeeTimePolicyRepository,
        IRepository<EmployeeDutyPeriod> employeeDutyPeriodRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<EmployeeExtdWorkLocation> extWorkLocationRepository,
        IRepository<BreakShift> breakShiftRepository,
        IPunchDutyResolver punchDutyResolver,
        ISysSettingsAccessor sysSettingsAccessor,
        IBinaryObjectManager binaryObjectManager,
        IEmployeeScopeProvider employeeScopeProvider,
        IClientInfoProvider clientInfoProvider)
    {
        _timeEventRepository = timeEventRepository;
        _transTypeRepository = transTypeRepository;
        _employeeRepository = employeeRepository;
        _employeeTimePolicyRepository = employeeTimePolicyRepository;
        _employeeDutyPeriodRepository = employeeDutyPeriodRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _extWorkLocationRepository = extWorkLocationRepository;
        _breakShiftRepository = breakShiftRepository;
        _punchDutyResolver = punchDutyResolver;
        _sysSettingsAccessor = sysSettingsAccessor;
        _binaryObjectManager = binaryObjectManager;
        _employeeScopeProvider = employeeScopeProvider;
        _clientInfoProvider = clientInfoProvider;
    }

    public async Task<MyGeoPunchContextDto> GetMyGeoPunchContext()
    {
        var employeeId = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();
        var now = Clock.Now;
        var state = await LoadStateAsync(employeeId, now);

        return new MyGeoPunchContextDto
        {
            CanClockIn = state.Buttons.CanClockIn,
            CanClockOut = state.Buttons.CanClockOut,
            CanOvertimeIn = state.IsOvertimeAllowed && state.Buttons.CanOvertimeIn,
            CanOvertimeOut = state.IsOvertimeAllowed && state.Buttons.CanOvertimeOut,
            IsOvertimeAllowed = state.IsOvertimeAllowed,
            HasDutyPeriodToday = state.HasDutyPeriodToday,
            WorkLocations = state.WorkLocations
                .Select(w => ToDto(w, w.Id == state.DefaultWorkLocationId, ResolveGeoFence(state, w) != null))
                .ToList(),
            DefaultWorkLocationId = state.DefaultWorkLocationId,
            IsOpenLocation = state.IsOpenLocation,
            RequiresSelfie = state.RequiresSelfie,
            AllowPunchOutsideRange = state.AllowPunchOutsideRange,
            AutoLogoutAfterPunch = state.Settings?.AutoLogoutAfterPunch ?? false,
            HideLocationInfo = state.Settings?.HideGeolocationInfo ?? false,
            HidePunchList = state.Settings?.HideGeolocationPunchInfo ?? false,
            ServerTime = now,
            TodaysPunches = state.TodaysPunches.OrderBy(p => p.TransTime).Select(ToDto).ToList()
        };
    }

    public async Task<MyGeoPunchLocationCheckDto> CheckMyGeoPunchLocation(CheckMyGeoPunchLocationInput input)
    {
        var employeeId = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();
        var state = await LoadStateAsync(employeeId, Clock.Now);

        var judgement = JudgeLocation(state, input.WorkLocationId, input.Latitude, input.Longitude);

        return new MyGeoPunchLocationCheckDto
        {
            DistanceMeters = judgement.DistanceMeters,
            RangeMeters = judgement.RangeMeters,
            UsesGeoFence = judgement.UsesGeoFence,
            Verdict = judgement.Verdict
        };
    }

    public async Task<MyGeoPunchResultDto> RecordMyGeoPunch(RecordMyGeoPunchInput input)
    {
        var employeeId = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();
        var now = Clock.Now;
        var state = await LoadStateAsync(employeeId, now);
        var code = input.TransTypeCode?.Trim();

        // Legacy wrote nothing when no duty period covered today, and returned result=false for the screen
        // to show "punching denied" with no reason. The reason is given here.
        if (!state.HasDutyPeriodToday)
        {
            throw new UserFriendlyException(L("GeoPunchNoDutyPeriodToday"));
        }

        var isOvertime = code == TransTypeCodes.OvertimeIn || code == TransTypeCodes.OvertimeOut;
        if (isOvertime && !state.IsOvertimeAllowed)
        {
            throw new UserFriendlyException(L("GeoPunchOvertimeNotAllowed"));
        }

        // Legacy only greyed the button out, so a second tab or a replayed request punched anyway.
        if (!state.Buttons.Allows(code))
        {
            throw new UserFriendlyException(L("GeoPunchNotAllowedNow"));
        }

        var transType = await _transTypeRepository.GetAll()
            .Where(t => t.Code.Trim() == code)
            .FirstOrDefaultAsync();
        if (transType == null)
        {
            throw new UserFriendlyException(L("GeoPunchTypeNotConfigured", code));
        }

        var judgement = JudgeLocation(state, input.WorkLocationId, input.Latitude, input.Longitude);
        if (judgement.Verdict == GeoPunchRangeVerdict.OutOfRangeRefused)
        {
            throw new UserFriendlyException(L("PunchingOutOfLocation"),
                judgement.UsesGeoFence
                    ? L("GeoPunchBoundaryDetail", Math.Round(judgement.DistanceMeters))
                    : L("GeoPunchDistanceDetail",
                        Math.Round(judgement.DistanceMeters),
                        Math.Round(judgement.RangeMeters ?? 0)));
        }

        var selfieId = state.RequiresSelfie ? await SaveSelfieAsync(input.Selfie) : (Guid?)null;

        // Minute precision, as legacy's PostTimeEventWithSelfie stored it.
        var transTime = now.Date.Add(new TimeSpan(now.Hour, now.Minute, 0));

        var duplicate = await _timeEventRepository.GetAll()
            .AnyAsync(e => e.EmployeeId == employeeId
                           && e.TransTypeId == transType.Id
                           && e.TransDate == now.Date
                           && e.TransTime == transTime);
        if (duplicate)
        {
            throw new UserFriendlyException(L("TimeEventAlreadyExists",
                now.Date.ToString("dd/MM/yyyy"), transTime.ToString("HH:mm:ss")));
        }

        var inRange = judgement.Verdict == GeoPunchRangeVerdict.InRange;

        var accessId = await _employeeRepository.GetAll()
            .Where(e => e.Id == employeeId)
            .Select(e => e.AccessID)
            .FirstOrDefaultAsync();

        var timeEvent = new TimeEvent
        {
            TenantId = AbpSession.TenantId ?? 0,
            EmployeeId = employeeId,
            PIN = accessId,
            TransTypeId = transType.Id,
            TransDate = now.Date,
            TransTime = transTime,
            TransSource = TransSource.ESS,
            IPAddress = Truncate(_clientInfoProvider.ClientIpAddress, 50),
            DeviceAgentString = Truncate(_clientInfoProvider.BrowserInfo, 1000),
            Latitude = input.Latitude,
            Longitude = input.Longitude,

            // Legacy only ever wrote PunchInRange = false (for an out-of-range punch) and never set it true,
            // so every legacy geo punch reads as out of range. It is set honestly here.
            PunchInRange = inRange,
            RangeDifference = judgement.RangeDifference,
            WorkLocationId = judgement.WorkLocation?.Id,
            GeoSelfiePath = selfieId?.ToString(),

            // An ordinary punch, not a request: no workflow status, so the Time Engine counts it at once.
            WorkflowStatus = null,
            WorkflowRequestId = null
        };

        var id = await _timeEventRepository.InsertAndGetIdAsync(timeEvent);

        return new MyGeoPunchResultDto
        {
            Id = id,
            PunchInRange = inRange,
            DistanceMeters = judgement.DistanceMeters,
            RangeDifference = judgement.RangeDifference,
            AutoLogout = state.Settings?.AutoLogoutAfterPunch ?? false
        };
    }

    public async Task<MyGeoPunchSelfieDto> GetMyGeoPunchSelfie(EntityDto input)
    {
        var employeeId = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();

        // Scoped to the caller's own punches: an id alone must never open somebody else's photo.
        var path = await _timeEventRepository.GetAll()
            .Where(e => e.Id == input.Id && e.EmployeeId == employeeId)
            .Select(e => e.GeoSelfiePath)
            .FirstOrDefaultAsync();

        if (path.IsNullOrWhiteSpace() || !Guid.TryParse(path, out var selfieId))
        {
            return new MyGeoPunchSelfieDto();
        }

        var file = await _binaryObjectManager.GetOrNullAsync(selfieId);
        if (file == null)
        {
            return new MyGeoPunchSelfieDto();
        }

        return new MyGeoPunchSelfieDto
        {
            DataUrl = $"data:{ContentTypeOf(file.Description)};base64,{Convert.ToBase64String(file.Bytes)}"
        };
    }

    // ---------------------------------------------------------------- state

    /// <summary>Everything both reading and recording need, resolved once from the same rules.</summary>
    private sealed class GeoPunchState
    {
        public GeoPunchButtons Buttons { get; init; }
        public bool HasDutyPeriodToday { get; init; }
        public bool IsOvertimeAllowed { get; init; }
        public List<WorkLocation> WorkLocations { get; init; }
        public int? DefaultWorkLocationId { get; init; }
        public bool IsOpenLocation => WorkLocations.Count == 0;
        public bool RequiresSelfie { get; init; }
        public bool AllowPunchOutsideRange { get; init; }

        /// <summary>The tenant has geo fencing on (ManageTimeSettings.EnableGeoFence).</summary>
        public bool GeoFenceEnabled { get; init; }
        public ManageSelfServiceSettings Settings { get; init; }
        public List<TimeEvent> TodaysPunches { get; init; }
    }

    private async Task<GeoPunchState> LoadStateAsync(int employeeId, DateTime now)
    {
        var today = now.Date;
        var tomorrow = today.AddDays(1);

        var todaysPunches = await _timeEventRepository.GetAll()
            .Include(e => e.TransType)
            .Include(e => e.WorkLocation)
            .Where(e => e.EmployeeId == employeeId && e.TransDate >= today && e.TransDate < tomorrow)
            .ToListAsync();

        var hasDutyPeriod = await _employeeDutyPeriodRepository.GetAll()
            .AnyAsync(p => p.EmployeeId == employeeId && p.StartDate <= today && p.EndDate >= today);

        var dutyShape = hasDutyPeriod ? await ResolveDutyShapeAsync(employeeId, today) : null;

        var buttons = GeoPunchRules.ResolveButtons(
            todaysPunches.Select(p => new GeoPunchEvent(p.TransType?.Code, p.TransTime.TimeOfDay)).ToList(),
            dutyShape,
            now.TimeOfDay);

        // Legacy took the open-ended policy (EndDate 9999-12-31) and stepped back to the previous one when
        // that was still in the future; the policy covering today is what that was reaching for.
        var timePolicy = await _employeeTimePolicyRepository.GetAll()
            .Include(p => p.TimePolicy)
            .Where(p => p.EmployeeId == employeeId && p.StartDate <= today && p.EndDate >= today)
            .OrderByDescending(p => p.StartDate)
            .Select(p => p.TimePolicy)
            .FirstOrDefaultAsync();

        var orgAssignment = await _orgAssignmentRepository.GetAll()
            .Include(a => a.WorkLocation)
            .Include(a => a.EmployeeSubGroup)
            .Where(a => a.EmployeeId == employeeId && a.StartDate <= today && a.EndDate >= today)
            .OrderByDescending(a => a.StartDate)
            .FirstOrDefaultAsync();

        var (workLocations, defaultWorkLocationId) =
            await ResolveWorkLocationsAsync(employeeId, today, buttons, timePolicy, orgAssignment?.WorkLocation);

        var settings = await _sysSettingsAccessor.GetSelfServiceSettingsAsync();
        var timeSettings = await _sysSettingsAccessor.GetTimeSettingsAsync();

        return new GeoPunchState
        {
            Buttons = buttons,
            HasDutyPeriodToday = hasDutyPeriod,
            IsOvertimeAllowed = orgAssignment?.EmployeeSubGroup?.PayOvertime ?? false,
            WorkLocations = workLocations,
            DefaultWorkLocationId = defaultWorkLocationId,

            // Legacy defaulted IsFaceRequired to TRUE when the employee had no time policy.
            RequiresSelfie = (settings?.AllowGeolocationSelfie ?? false) && (timePolicy?.IsFaceRequired ?? true),
            AllowPunchOutsideRange = settings?.AllowPunchOutsideRange ?? false,
            GeoFenceEnabled = timeSettings?.EnableGeoFence ?? false,
            Settings = settings,
            TodaysPunches = todaysPunches
        };
    }

    /// <summary>
    /// Today's duty, reduced to what the button rules read. Goes through <see cref="IPunchDutyResolver"/>, so
    /// a roster or a substitute duty is honoured -- legacy read the duty period's weekday slot directly and
    /// missed both.
    /// </summary>
    private async Task<GeoPunchDutyShape> ResolveDutyShapeAsync(int employeeId, DateTime today)
    {
        Duty duty;
        try
        {
            duty = await _punchDutyResolver.TryResolveDutyAsync(employeeId, today);
        }
        catch (Exception ex)
        {
            // A broken roster configuration must not stop somebody clocking in; without a duty the rules
            // fall back to the ordinary (non split-shift) branch, which needs no duty at all.
            Logger.Warn($"Geo punch: could not resolve today's duty for employee {employeeId}.", ex);
            return null;
        }

        if (duty == null)
        {
            return null;
        }

        var breakShift = duty.BreakShift;
        if (breakShift == null && duty.BreakShiftId.HasValue)
        {
            breakShift = await _breakShiftRepository.FirstOrDefaultAsync(duty.BreakShiftId.Value);
        }

        return new GeoPunchDutyShape
        {
            DutyStart = duty.StartTime,
            DutyEnd = duty.EndTime,
            BreakStart = breakShift?.StartTime,
            BreakEnd = breakShift?.EndTime
        };
    }

    /// <summary>
    /// Port of legacy's "Work Location Lists" region. Legacy keyed it on its DISABLED flags --
    /// <c>PunchedIn</c> (Clock In greyed, so the next punch is an Out) and <c>PunchedOut</c> (Clock Out
    /// greyed, so the next punch is an In) -- and both may hold at once, in which case both sets of rules
    /// apply.
    /// </summary>
    private async Task<(List<WorkLocation> locations, int? defaultId)> ResolveWorkLocationsAsync(
        int employeeId,
        DateTime today,
        GeoPunchButtons buttons,
        TimePolicy timePolicy,
        WorkLocation ownWorkLocation)
    {
        var nextIsOut = !buttons.CanClockIn;
        var nextIsIn = !buttons.CanClockOut;

        var locations = new List<WorkLocation>();
        int? defaultId = null;

        var useExtended = (nextIsOut && (timePolicy?.AllowPunchOutFromExtWorklocation ?? false))
                          || (nextIsIn && (timePolicy?.AllowPunchInFromExtWorklocation ?? false));
        if (useExtended)
        {
            // Compared by date: legacy compared EndDate against DateTime.Now, which shut an extended
            // location on the morning of its last valid day.
            var extended = await _extWorkLocationRepository.GetAll()
                .Include(x => x.WorkLocation)
                .Where(x => x.EmployeeId == employeeId && x.StartDate <= today && x.EndDate >= today)
                .Select(x => x.WorkLocation)
                .ToListAsync();

            locations.AddRange(extended.Where(w => w != null));
        }

        var useOwn = (nextIsOut && (timePolicy?.AllowPunchOutFromWorkLocationOnly ?? false))
                     || (nextIsIn && (timePolicy?.AllowPunchInFromWorkLocationOnly ?? false));
        if (useOwn && ownWorkLocation != null)
        {
            defaultId = ownWorkLocation.Id;
            locations.Add(ownWorkLocation);
        }

        return (locations.GroupBy(w => w.Id).Select(g => g.First()).ToList(), defaultId);
    }

    /// <summary>What judging a punch's position produced, so every caller reports the same numbers.</summary>
    private sealed class GeoJudgement
    {
        public WorkLocation WorkLocation { get; init; }

        /// <summary>Metres from the work location, or from the site boundary when <see cref="UsesGeoFence"/>.</summary>
        public double DistanceMeters { get; init; }

        /// <summary>The radius applied; null when a boundary decided it or the punch is open-location.</summary>
        public double? RangeMeters { get; init; }

        /// <summary>Metres beyond what was allowed; zero when in range.</summary>
        public double RangeDifference { get; init; }

        public bool UsesGeoFence { get; init; }

        public GeoPunchRangeVerdict Verdict { get; init; }
    }

    /// <summary>
    /// Distance and verdict for a punch at (<paramref name="latitude"/>, <paramref name="longitude"/>). An
    /// open-location punch has no work location to be measured against and is always in range (legacy set
    /// the "work place" to the employee's own position with a 100 m radius, which comes to the same thing).
    ///
    /// <para>
    /// A work location with a site boundary drawn on it is judged by that shape instead of by its radius,
    /// when the tenant has geo fencing on. Legacy stored the boundary and never read it, so every site fell
    /// back to a circle however oddly it was actually shaped -- an L-shaped compound needed a radius wide
    /// enough to cover the far wing, which let a punch through from the street outside.
    /// </para>
    /// </summary>
    private GeoJudgement JudgeLocation(GeoPunchState state, int? workLocationId, double latitude, double longitude)
    {
        if (state.IsOpenLocation)
        {
            return new GeoJudgement { Verdict = GeoPunchRangeVerdict.InRange };
        }

        if (!workLocationId.HasValue)
        {
            throw new UserFriendlyException(L("GeoPunchSelectWorkLocation"));
        }

        var workLocation = state.WorkLocations.FirstOrDefault(w => w.Id == workLocationId.Value);
        if (workLocation == null)
        {
            throw new UserFriendlyException(L("GeoPunchWorkLocationNotAllowed"));
        }

        var fence = ResolveGeoFence(state, workLocation);
        if (fence != null)
        {
            var inside = GeoFence.Contains(fence, latitude, longitude);
            var toBoundary = inside ? 0 : GeoFence.DistanceToBoundaryMeters(fence, latitude, longitude);

            return new GeoJudgement
            {
                WorkLocation = workLocation,
                DistanceMeters = toBoundary,
                RangeMeters = null,
                RangeDifference = toBoundary,
                UsesGeoFence = true,
                Verdict = inside
                    ? GeoPunchRangeVerdict.InRange
                    : state.AllowPunchOutsideRange
                        ? GeoPunchRangeVerdict.OutOfRangeAllowed
                        : GeoPunchRangeVerdict.OutOfRangeRefused
            };
        }

        var distance = GeoPunchRules.DistanceMeters(latitude, longitude, workLocation.Latitude, workLocation.Longitude);
        var verdict = GeoPunchRules.JudgeRange(distance, workLocation.GeoRange, state.AllowPunchOutsideRange);

        return new GeoJudgement
        {
            WorkLocation = workLocation,
            DistanceMeters = distance,
            RangeMeters = workLocation.GeoRange,
            RangeDifference = verdict == GeoPunchRangeVerdict.InRange ? 0 : distance - workLocation.GeoRange,
            UsesGeoFence = false,
            Verdict = verdict
        };
    }

    /// <summary>
    /// The site boundary that governs <paramref name="workLocation"/>, or <c>null</c> when the radius still
    /// does: geo fencing off for the tenant, no boundary drawn, or one that cannot be read.
    ///
    /// <para>
    /// A boundary that does not enclose its own work location pin is treated as not drawn. The usual cause
    /// is latitude and longitude the wrong way round, which puts the shape thousands of kilometres away and
    /// would refuse every punch at the site -- a master-data slip must not be able to stop everyone clocking
    /// in, so it falls back to the radius and says so in the log.
    /// </para>
    /// </summary>
    private IReadOnlyList<GeoPoint> ResolveGeoFence(GeoPunchState state, WorkLocation workLocation)
    {
        if (!state.GeoFenceEnabled || !GeoFence.TryParse(workLocation.GeoFenceCoordinates, out var polygon))
        {
            return null;
        }

        // A location with no pin has nothing to check the boundary against -- and no usable radius either,
        // so the boundary is the only thing that can let anyone punch there.
        if (workLocation.Latitude == 0 && workLocation.Longitude == 0)
        {
            return polygon;
        }

        if (GeoFence.Contains(polygon, workLocation.Latitude, workLocation.Longitude))
        {
            return polygon;
        }

        // The pin may sit just off the shape -- a gate marked outside the wall. Its own radius is the
        // tolerance for that, and at least a metre when no radius is set.
        var pinToBoundary = GeoFence.DistanceToBoundaryMeters(polygon, workLocation.Latitude, workLocation.Longitude);
        if (pinToBoundary <= Math.Max(workLocation.GeoRange, 1))
        {
            return polygon;
        }

        Logger.Warn(
            $"Geo punch: work location {workLocation.Id} has a geo fence {Math.Round(pinToBoundary)} m away from " +
            "its own coordinates, so it is ignored and the radius is used. Check the coordinate order.");

        return null;
    }

    // ---------------------------------------------------------------- selfie

    /// <summary>
    /// Stores the selfie as a <see cref="BinaryObject"/> and returns its id, which goes into
    /// <see cref="TimeEvent.GeoSelfiePath"/>. Legacy wrote a JPEG to a server folder, and only when document
    /// storage was switched to the file system -- otherwise it silently threw the photo away.
    /// </summary>
    private async Task<Guid> SaveSelfieAsync(string dataUrl)
    {
        if (dataUrl.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(L("GeoPunchSelfieRequired"));
        }

        var (contentType, bytes) = ParseDataUrl(dataUrl);
        if (bytes == null || bytes.Length == 0)
        {
            throw new UserFriendlyException(L("GeoPunchSelfieInvalid"));
        }

        if (bytes.Length > MyGeoPunchConsts.MaxSelfieBytes)
        {
            throw new UserFriendlyException(L("GeoPunchSelfieTooLarge"));
        }

        var file = new BinaryObject(AbpSession.TenantId, bytes, $"{SelfieDescription}|{contentType}");
        await _binaryObjectManager.SaveAsync(file);

        return file.Id;
    }

    private static (string contentType, byte[] bytes) ParseDataUrl(string dataUrl)
    {
        const string prefix = "data:";
        const string marker = ";base64,";

        if (!dataUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return (null, null);
        }

        var markerIndex = dataUrl.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return (null, null);
        }

        var contentType = dataUrl.Substring(prefix.Length, markerIndex - prefix.Length).Trim().ToLowerInvariant();
        if (!SelfieContentTypes.Contains(contentType))
        {
            return (null, null);
        }

        try
        {
            return (contentType, Convert.FromBase64String(dataUrl.Substring(markerIndex + marker.Length)));
        }
        catch (FormatException)
        {
            return (null, null);
        }
    }

    private static string ContentTypeOf(string description)
    {
        var separator = description?.IndexOf('|') ?? -1;

        return separator >= 0 ? description.Substring(separator + 1) : "image/jpeg";
    }

    // ---------------------------------------------------------------- mapping

    private static MyGeoPunchWorkLocationDto ToDto(WorkLocation workLocation, bool isDefault, bool hasGeoFence) => new()
    {
        Id = workLocation.Id,
        Code = workLocation.Code,
        NameEn = workLocation.NameEn,
        NameAr = workLocation.NameAr,
        Latitude = workLocation.Latitude,
        Longitude = workLocation.Longitude,
        GeoRange = workLocation.GeoRange,
        HasGeoFence = hasGeoFence,
        IsDefault = isDefault
    };

    private static MyGeoPunchDto ToDto(TimeEvent timeEvent) => new()
    {
        Id = timeEvent.Id,
        TransTypeCode = timeEvent.TransType?.Code?.Trim(),
        TransTypeNameEn = timeEvent.TransType?.NameEn,
        TransTypeNameAr = timeEvent.TransType?.NameAr,
        TransDate = timeEvent.TransDate,
        TransTime = timeEvent.TransTime,
        WorkLocationNameEn = timeEvent.WorkLocation?.NameEn,
        WorkLocationNameAr = timeEvent.WorkLocation?.NameAr,
        PunchInRange = timeEvent.PunchInRange,
        RangeDifference = timeEvent.RangeDifference,
        HasSelfie = !timeEvent.GeoSelfiePath.IsNullOrWhiteSpace()
    };

    private static string Truncate(string value, int maxLength) =>
        value == null || value.Length <= maxLength ? value : value.Substring(0, maxLength);
}
