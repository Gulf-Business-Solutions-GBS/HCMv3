using System.Linq;
using System.Threading.Tasks;
using Abp.AspNetCore.Mvc.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Graphics;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
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
    /// <summary>
    /// Upper bound on the <c>size</c> query parameter. Caps the CPU an unauthenticated-shaped request can
    /// ask for, and anything larger is better served by the untouched photo anyway.
    /// </summary>
    private const int MaxThumbnailSize = 512;

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IImageThumbnailService _imageThumbnailService;
    private readonly IEntityImageStore _imageStore;

    public EmployeePhotoController(
        IRepository<Employee> employeeRepository,
        IImageThumbnailService imageThumbnailService,
        IEntityImageStore imageStore)
    {
        _employeeRepository = employeeRepository;
        _imageThumbnailService = imageThumbnailService;
        _imageStore = imageStore;
    }

    /// <param name="size">
    /// Optional square thumbnail edge in pixels. Added for the Employee Org Chart, where one manager can
    /// have over a hundred direct reports on screen at once -- serving each of those cards the full stored
    /// photo to fill a 40px circle would move tens of megabytes. Omitted, the stored photo is returned
    /// unchanged, which is what the workflow activity feed has always asked for.
    /// </param>
    [HttpGet("{employeeId:int}")]
    public async Task<IActionResult> Get(int employeeId, [FromQuery] int? size = null)
    {
        var employee = await _employeeRepository.GetAll()
            .Where(e => e.Id == employeeId)
            .Select(e => new { e.TenantId, e.PhotoPath })
            .FirstOrDefaultAsync();

        if (employee == null)
        {
            return NotFound();
        }

        // From the tenant's own folder on disk. Only the path is read from the row, never an image column.
        var photo = await _imageStore.ReadOrNullAsync(employee.TenantId, employee.PhotoPath);

        if (photo == null || photo.Length == 0)
        {
            // 404 rather than a placeholder image: the client already knows from the feed whether a photo
            // exists, and returning a body here would defeat caching for everyone without one.
            return NotFound();
        }

        // Photos change rarely and are re-requested on every feed render. Private, because the response is
        // tenant-scoped and must not be held by a shared proxy. Varies by size so the full photo and a
        // thumbnail of it do not evict each other in the browser cache.
        Response.Headers.CacheControl = "private, max-age=43200";

        if (size is > 0 and <= MaxThumbnailSize)
        {
            // A null return means the bytes would not decode; serve them untouched rather than 404, since
            // the browser may well render an image SkiaSharp declined to.
            var thumbnail = _imageThumbnailService.CreateSquareThumbnail(photo, size.Value);
            if (thumbnail != null)
            {
                return File(thumbnail, "image/jpeg");
            }
        }

        return File(photo, "image/jpeg");
    }
}
