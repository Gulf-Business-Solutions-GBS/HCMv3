using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Ess.Dto;

namespace HCMv3.Ess;

/// <summary>
/// Self-service geo punch: the employee clocks in or out from the browser, stamped with where they are
/// standing. Port of legacy <c>HCM10.ESS</c> "Geo Punching" (<c>GeoLocationController</c>).
///
/// <para>
/// Unlike <see cref="IMyPunchAppService"/> this does not ask for anything. The punch is recorded as an
/// ordinary <c>TimeEvent</c> with no workflow status, so the Time Engine counts it at once, exactly as it
/// would a badge on a device.
/// </para>
///
/// <para>
/// Everything legacy left to the browser -- the distance, whether it is in range, which button may be
/// pressed, which work location may be chosen -- is decided again on the server when the punch arrives.
/// The screen uses the same answers to draw itself, but nothing it sends is taken on trust.
/// </para>
/// </summary>
public interface IMyGeoPunchAppService : IApplicationService
{
    Task<MyGeoPunchContextDto> GetMyGeoPunchContext();

    /// <summary>How far the employee is from the chosen work location, and whether that is allowed.</summary>
    Task<MyGeoPunchLocationCheckDto> CheckMyGeoPunchLocation(CheckMyGeoPunchLocationInput input);

    Task<MyGeoPunchResultDto> RecordMyGeoPunch(RecordMyGeoPunchInput input);

    /// <summary>The selfie taken with one of the employee's own punches.</summary>
    Task<MyGeoPunchSelfieDto> GetMyGeoPunchSelfie(EntityDto input);
}
