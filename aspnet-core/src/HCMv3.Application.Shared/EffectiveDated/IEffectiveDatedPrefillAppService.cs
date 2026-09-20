using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.EffectiveDated.Dto;

namespace HCMv3.EffectiveDated;

/// <summary>
/// Fills a new date-effective record from the one that was in force on the date the user typed.
///
/// <para>Adding a salary from 01/01/2027 starts from the 01/01/2026 values; adding one back-dated to 01/07/2025
/// starts from the 01/01/2025 values -- whichever record covered that date, not simply the latest one. The user then
/// changes what they meant to change. Legacy did this for org assignments only, and read the record in force TODAY
/// rather than on the date typed, so a back-dated entry started from the wrong values.</para>
/// </summary>
public interface IEffectiveDatedPrefillAppService : IApplicationService
{
    /// <summary>
    /// The values in force on <see cref="EffectiveDatedPrefillInput.EffectiveDate"/>, or nothing found when the
    /// history starts later. POST because the input is a DTO of its own -- the attribute sits on the
    /// implementation, which is where this solution keeps it (Application.Shared does not reference MVC).
    /// </summary>
    Task<EffectiveDatedPrefillDto> GetPrefill(EffectiveDatedPrefillInput input);
}
