using System.Linq;
using System.Threading.Tasks;
using Abp.AspNetCore.Mvc.Authorization;
using Abp.Domain.Repositories;
using HCMv3.PersonnelAdministration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Web.Controllers;

/// <summary>
/// Serves an employee's photo as an image.
///
/// <para>
/// Deliberately a separate endpoint rather than a byte[] on a DTO. The workflow activity feed shows the
/// author of every entry, and a twenty-comment thread would otherwise carry twenty photo blobs inline --
/// on an entity that already has ~70 columns. Here the browser fetches each photo once and caches it.
/// </para>
///
/// <para>
/// Authorization is "any signed-in user of this tenant". Photos are shown throughout the admin app already,
/// and the feed needs them for people the viewer may not otherwise have permission to look up -- an
/// approver two steps ahead, say. The tenant filter on the repository is what keeps one tenant's photos out
/// of another's.
/// </para>
/// </summary>
[AbpMvcAuthorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/employee-photo")]
public class EmployeePhotoController : HCMv3ControllerBase
{
    private readonly IRepository<Employee> _employeeRepository;

    public EmployeePhotoController(IRepository<Employee> employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    [HttpGet("{employeeId:int}")]
    public async Task<IActionResult> Get(int employeeId)
    {
        var photo = await _employeeRepository.GetAll()
            .Where(e => e.Id == employeeId)
            .Select(e => e.Photo)
            .FirstOrDefaultAsync();

        if (photo == null || photo.Length == 0)
        {
            // 404 rather than a placeholder image: the client already knows from the feed whether a photo
            // exists, and returning a body here would defeat caching for everyone without one.
            return NotFound();
        }

        // Photos change rarely and are re-requested on every feed render. Private, because the response is
        // tenant-scoped and must not be held by a shared proxy.
        Response.Headers.CacheControl = "private, max-age=43200";

        return File(photo, "image/jpeg");
    }
}
