using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace AfricanSpringInventory.Services;

public static class ImageResizer
{
    // Load an uploaded image, shrink it to fit within max px, and re-encode as
    // JPEG so stored product photos stay small and fast. Returns null if the
    // upload isn't a valid image.
    public static async Task<byte[]?> ToJpegAsync(Stream input, int max = 800, int quality = 78)
    {
        try
        {
            using var image = await Image.LoadAsync(input);
            image.Mutate(x => x.Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(max, max) }));
            using var ms = new MemoryStream();
            await image.SaveAsJpegAsync(ms, new JpegEncoder { Quality = quality });
            return ms.ToArray();
        }
        catch
        {
            return null;
        }
    }
}
