using SkiaSharp;

namespace HCMv3.Graphics;

/// <summary>
/// Produces small square thumbnails from stored images.
///
/// Added for the Employee Org Chart: a senior manager can have well over a hundred direct reports, and
/// serving each card the full stored photo would move tens of megabytes to draw circles 40 pixels across.
/// Employee photos are stored as-uploaded (<c>Employee.Photo</c>), so the resize has to happen at read
/// time -- see <c>EmployeePhotoController</c>, which takes an optional size and caches the result in the
/// browser for twelve hours.
/// </summary>
public interface IImageThumbnailService
{
    /// <summary>
    /// Centre-crops <paramref name="imageBytes"/> to a square and scales it to
    /// <paramref name="size"/>x<paramref name="size"/>, encoded as JPEG.
    ///
    /// Centre-crop rather than letterbox because these are shown in a circular frame, where padding would
    /// be visible as clipped bars. Returns <c>null</c> when the bytes are not a decodable image, so the
    /// caller can fall back rather than fail -- a corrupt photo on one employee record must not break the
    /// whole chart.
    /// </summary>
    byte[] CreateSquareThumbnail(byte[] imageBytes, int size);
}

public class SkiaSharpImageThumbnailService : HCMv3DomainServiceBase, IImageThumbnailService
{
    /// <summary>Quality of the re-encoded JPEG. 80 is visually clean at avatar sizes and keeps files small.</summary>
    private const int JpegQuality = 80;

    public byte[] CreateSquareThumbnail(byte[] imageBytes, int size)
    {
        if (imageBytes == null || imageBytes.Length == 0 || size <= 0)
        {
            return null;
        }

        using var original = SKBitmap.Decode(imageBytes);
        if (original == null)
        {
            // Not a decodable image. The caller serves the stored bytes untouched or 404s; either is
            // better than throwing on a record whose photo was uploaded before validation existed.
            return null;
        }

        var edge = original.Width < original.Height ? original.Width : original.Height;
        var cropLeft = (original.Width - edge) / 2;
        var cropTop = (original.Height - edge) / 2;

        using var square = new SKBitmap(edge, edge);
        using (var canvas = new SKCanvas(square))
        {
            canvas.DrawBitmap(
                original,
                SKRect.Create(cropLeft, cropTop, edge, edge),
                SKRect.Create(0, 0, edge, edge));
        }

        // Never scale up: a 40px stored photo asked for at 96 would only get blurrier and larger on the
        // wire. Serving it at its own size is both smaller and no worse to look at.
        var targetSize = size < edge ? size : edge;

        using var resized = square.Resize(new SKImageInfo(targetSize, targetSize), SKSamplingOptions.Default);
        if (resized == null)
        {
            return null;
        }

        using var image = SKImage.FromBitmap(resized);
        using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, JpegQuality);

        return encoded?.ToArray();
    }
}
