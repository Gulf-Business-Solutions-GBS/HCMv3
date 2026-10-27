using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.Maui.Core.Components;
using HCMv3.Maui.Core.Helpers;
using HCMv3.Maui.Core.Threading;
using HCMv3.Maui.Services.Account;
using HCMv3.Maui.Services.Navigation;
using HCMv3.TimeManagement;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Media;

namespace HCMv3.Maui.Pages.Ess;

/// <summary>
/// Self-service geo punch. The phone supplies the GPS position (and a selfie when the policy asks for one);
/// the server decides everything else -- which punch is open, whether the position is in range, whether an
/// out-of-range punch is allowed -- exactly as it does for the Angular screen.
/// </summary>
public partial class GeoPunch : HCMv3MainLayoutPageComponentBase
{
    /// <summary>The trans type codes the Angular my-geo-punch screen sends for each button.</summary>
    private static class PunchCode
    {
        public const string ClockIn = "1";
        public const string ClockOut = "2";
        public const string OvertimeIn = "3";
        public const string OvertimeOut = "4";
    }

    /// <summary>Longest edge a selfie is scaled down to before upload. A face needs nothing more.</summary>
    private const float SelfieMaxEdge = 1024f;

    protected IMyGeoPunchAppService MyGeoPunchAppService { get; set; }
    protected EssContextCache EssContextCache { get; set; }
    protected IAccountService AccountService { get; set; }

    private bool _loaded;
    private bool _notLinked;
    private bool _working;

    private MyGeoPunchContextDto _context;
    private int? _workLocationId;
    private MyGeoPunchLocationCheckDto _lastCheck;
    private Location _lastPosition;

    public GeoPunch()
    {
        MyGeoPunchAppService = Resolve<IMyGeoPunchAppService>();
        EssContextCache = Resolve<EssContextCache>();
        AccountService = Resolve<IAccountService>();
    }

    protected override async Task OnInitializedAsync()
    {
        await SetPageHeader(L("EssGeoPunch"));

        await SetBusyAsync(async () =>
        {
            await WebRequestExecuter.Execute(
                async () => await EssContextCache.GetAsync(),
                result =>
                {
                    _notLinked = result != null && !result.IsLinkedToEmployee;
                    return Task.CompletedTask;
                });

            if (!_notLinked)
            {
                await LoadContextAsync();
            }
        });

        _loaded = true;
    }

    private async Task ReloadAsync()
    {
        await SetBusyAsync(LoadContextAsync);
        StateHasChanged();
    }

    private async Task LoadContextAsync()
    {
        await WebRequestExecuter.Execute(
            async () => await MyGeoPunchAppService.GetMyGeoPunchContext(),
            result =>
            {
                _context = result;

                if (_context != null && !_context.IsOpenLocation)
                {
                    // Keep the employee's choice across reloads if it is still on offer; otherwise start on
                    // the default the server picked.
                    var stillOffered = _workLocationId.HasValue &&
                                       _context.WorkLocations.Any(w => w.Id == _workLocationId.Value);
                    if (!stillOffered)
                    {
                        _workLocationId = _context.DefaultWorkLocationId;
                    }
                }

                return Task.CompletedTask;
            });
    }

    private MyGeoPunchWorkLocationDto SelectedWorkLocation =>
        _context == null || !_workLocationId.HasValue
            ? null
            : _context.WorkLocations.FirstOrDefault(w => w.Id == _workLocationId.Value);

    private Task ClockIn() => PunchAsync(PunchCode.ClockIn);
    private Task ClockOut() => PunchAsync(PunchCode.ClockOut);
    private Task OvertimeIn() => PunchAsync(PunchCode.OvertimeIn);
    private Task OvertimeOut() => PunchAsync(PunchCode.OvertimeOut);

    private async Task PunchAsync(string transTypeCode)
    {
        if (_context == null || _working)
        {
            return;
        }

        if (!_context.IsOpenLocation && !_workLocationId.HasValue)
        {
            await UserDialogsService.AlertWarn(L("GeoPunchSelectWorkLocation"));
            return;
        }

        _working = true;
        StateHasChanged();

        try
        {
            // 1. Where the employee is standing, fresh for this punch.
            var position = await GetPositionAsync();
            if (position == null)
            {
                return;
            }

            _lastPosition = position;
            var workLocationId = _context.IsOpenLocation ? null : _workLocationId;

            // 2. Ask the server whether that position is acceptable before bothering with a selfie.
            MyGeoPunchLocationCheckDto check = null;
            await SetBusyAsync(async () =>
            {
                await WebRequestExecuter.Execute(
                    async () => await MyGeoPunchAppService.CheckMyGeoPunchLocation(new CheckMyGeoPunchLocationInput
                    {
                        Latitude = position.Latitude,
                        Longitude = position.Longitude,
                        WorkLocationId = workLocationId
                    }),
                    result =>
                    {
                        check = result;
                        return Task.CompletedTask;
                    });
            });

            if (check == null)
            {
                return;
            }

            _lastCheck = check;
            StateHasChanged();

            if (check.Verdict == GeoPunchRangeVerdict.OutOfRangeRefused)
            {
                await UserDialogsService.AlertError(L("PunchingOutOfLocation"));
                return;
            }

            if (check.Verdict == GeoPunchRangeVerdict.OutOfRangeAllowed)
            {
                var proceed = await UserDialogsService.Confirm(DistanceText(check), L("GeoPunchOutOfRangeAllowed"));
                if (!proceed)
                {
                    return;
                }
            }

            // 3. The selfie, when the policy asks for one.
            string selfie = null;
            if (_context.RequiresSelfie)
            {
                selfie = await TakeSelfieAsync();
                if (selfie == null)
                {
                    return;
                }
            }

            // 4. The punch itself. The server re-checks everything above.
            MyGeoPunchResultDto punched = null;
            await SetBusyAsync(async () =>
            {
                await WebRequestExecuter.Execute(
                    async () => await MyGeoPunchAppService.RecordMyGeoPunch(new RecordMyGeoPunchInput
                    {
                        TransTypeCode = transTypeCode,
                        Latitude = position.Latitude,
                        Longitude = position.Longitude,
                        WorkLocationId = workLocationId,
                        Selfie = selfie
                    }),
                    result =>
                    {
                        punched = result;
                        return Task.CompletedTask;
                    });
            });

            if (punched == null)
            {
                return;
            }

            await UserDialogsService.AlertSuccess(L("GeoPunchSuccessful"));

            if (punched.AutoLogout)
            {
                await AccountService.LogoutAsync();
                NavigationService.NavigateTo(NavigationUrlConsts.Login);
                return;
            }

            await ReloadAsync();
        }
        finally
        {
            _working = false;
            StateHasChanged();
        }
    }

    private async Task<Location> GetPositionAsync()
    {
        try
        {
            var status = await MainThread.InvokeOnMainThreadAsync(() => Permissions.RequestAsync<Permissions.LocationWhenInUse>());
            if (status != PermissionStatus.Granted)
            {
                await UserDialogsService.AlertError(L("GeoPunchLocationPermissionDenied"));
                return null;
            }

            await UserDialogsService.Block();
            Location location;
            try
            {
                location = await MainThread.InvokeOnMainThreadAsync(() =>
                    Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(15))));
            }
            finally
            {
                await UserDialogsService.UnBlock();
            }

            if (location == null)
            {
                await UserDialogsService.AlertError(L("GeoPunchLocationTimeout"));
                return null;
            }

            return location;
        }
        catch (PermissionException)
        {
            await UserDialogsService.AlertError(L("GeoPunchLocationPermissionDenied"));
            return null;
        }
        catch (Exception)
        {
            // FeatureNotEnabledException (location switched off), FeatureNotSupportedException, and the rest.
            await UserDialogsService.AlertError(L("GeoPunchLocationUnavailable"));
            return null;
        }
    }

    /// <summary>
    /// Takes a photo with the camera and returns it as a data URL, scaled down so an ordinary phone photo
    /// fits under the server's limit. Null when it could not be taken; the reason has already been shown.
    /// </summary>
    private async Task<string> TakeSelfieAsync()
    {
        try
        {
            var status = await MainThread.InvokeOnMainThreadAsync(() => Permissions.RequestAsync<Permissions.Camera>());
            if (status != PermissionStatus.Granted || !MediaPicker.Default.IsCaptureSupported)
            {
                await UserDialogsService.AlertError(L("GeoPunchCameraUnavailable"));
                return null;
            }

            var photo = await MainThread.InvokeOnMainThreadAsync(() => MediaPicker.Default.CapturePhotoAsync());
            if (photo == null)
            {
                await UserDialogsService.AlertWarn(L("GeoPunchSelfieRequired"));
                return null;
            }

            byte[] bytes;
            await using (var stream = await photo.OpenReadAsync())
            {
                bytes = await ShrinkToJpegAsync(stream);
            }

            if (bytes == null || bytes.Length == 0)
            {
                await UserDialogsService.AlertError(L("GeoPunchSelfieInvalid"));
                return null;
            }

            if (bytes.Length > MyGeoPunchConsts.MaxSelfieBytes)
            {
                await UserDialogsService.AlertError(L("GeoPunchSelfieTooLarge"));
                return null;
            }

            return "data:image/jpeg;base64," + Convert.ToBase64String(bytes);
        }
        catch (PermissionException)
        {
            await UserDialogsService.AlertError(L("GeoPunchCameraUnavailable"));
            return null;
        }
        catch (Exception)
        {
            await UserDialogsService.AlertError(L("GeoPunchCameraUnavailable"));
            return null;
        }
    }

    /// <summary>
    /// Re-encodes the photo as a JPEG no larger than <see cref="SelfieMaxEdge"/> on its longest side. Falls
    /// back to the original bytes if the platform image API cannot read it, leaving the size check to decide.
    /// </summary>
    private static async Task<byte[]> ShrinkToJpegAsync(Stream source)
    {
        using var original = new MemoryStream();
        await source.CopyToAsync(original);
        var originalBytes = original.ToArray();

        try
        {
            using var input = new MemoryStream(originalBytes);
            Microsoft.Maui.Graphics.IImage image = Microsoft.Maui.Graphics.Platform.PlatformImage.FromStream(input);
            if (image == null)
            {
                return originalBytes;
            }

            if (image.Width > SelfieMaxEdge || image.Height > SelfieMaxEdge)
            {
                image = image.Downsize(SelfieMaxEdge, true);
            }

            using var output = new MemoryStream();
            await image.SaveAsync(output, Microsoft.Maui.Graphics.ImageFormat.Jpeg, 0.8f);
            image.Dispose();

            return output.ToArray();
        }
        catch (Exception)
        {
            return originalBytes;
        }
    }

    private string DistanceText(MyGeoPunchLocationCheckDto check)
    {
        if (check.UsesGeoFence)
        {
            return L("GeoPunchBoundaryDetail", EssText.Number(Math.Round(check.DistanceMeters)));
        }

        if (check.RangeMeters.HasValue)
        {
            return L("GeoPunchDistanceDetail",
                EssText.Number(Math.Round(check.DistanceMeters)),
                EssText.Number(Math.Round(check.RangeMeters.Value)));
        }

        return L("GeoPunchDistance") + ": " + EssText.Number(Math.Round(check.DistanceMeters));
    }

    private string VerdictLabel(GeoPunchRangeVerdict verdict)
    {
        switch (verdict)
        {
            case GeoPunchRangeVerdict.InRange:
                return L("GeoPunchInRange");
            case GeoPunchRangeVerdict.OutOfRangeAllowed:
                return L("GeoPunchOutOfRangeAllowed");
            default:
                return L("GeoPunchOutOfRange");
        }
    }

    private static string VerdictCss(GeoPunchRangeVerdict verdict)
    {
        switch (verdict)
        {
            case GeoPunchRangeVerdict.InRange:
                return "badge-success";
            case GeoPunchRangeVerdict.OutOfRangeAllowed:
                return "badge-warning";
            default:
                return "badge-danger";
        }
    }

    private static string WorkLocationName(MyGeoPunchWorkLocationDto location) =>
        EssText.Pick(location.NameEn, location.NameAr);

    private static string PunchTypeName(MyGeoPunchDto punch)
    {
        var name = EssText.Pick(punch.TransTypeNameEn, punch.TransTypeNameAr);
        return string.IsNullOrWhiteSpace(name) ? punch.TransTypeCode : name;
    }

    private static string PunchWorkLocationName(MyGeoPunchDto punch) =>
        EssText.Pick(punch.WorkLocationNameEn, punch.WorkLocationNameAr);
}
