using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Licensing.Dto;

namespace HCMv3.Licensing;

/// <summary>
/// Licence Assignment (Administration > Tools). Legacy <c>uscLicenseAssignment</c>: tick the modules each user may
/// use, see used / available per module, and be refused a tick when no seat is left. Seat counts and the module
/// switches come from the tenant's ASP.NET Zero edition and are set by the host, not here.
/// </summary>
public interface ILicenceAssignmentAppService : IApplicationService
{
    Task<GetLicenceAssignmentSummaryOutput> GetSummary();

    Task<PagedResultDto<LicenceAssignmentUserDto>> GetUsers(GetLicenceAssignmentUsersInput input);

    /// <summary>Returns the module's usage after the change, so the header can update without a reload.</summary>
    Task<LicenceModuleUsageDto> SetUserModuleLicence(SetUserModuleLicenceInput input);
}
