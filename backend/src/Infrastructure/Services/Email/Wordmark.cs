using Application.Common.Models;

namespace Infrastructure.Services.Email;

/// <summary>
/// The wordmark the emails show, sent inside each email: Gmail doesn't show SVG images, and the site has no public
/// address yet to load an image from. <c>solaris-wordmark-on-dark.png</c> is the brand's on-dark SVG
/// (frontend/brand/logo) drawn at twice its size in Chrome, 152 × 19 px with an 8 px margin on its black background,
/// so it stays readable when an email app inverts the email's colours.
/// </summary>
public static class Wordmark
{
    public const string ContentId = "wordmark@solaris";

    public static InlineImage Image { get; } = new(ContentId, "image/png", Load());

    private static byte[] Load()
    {
        using var stream = typeof(Wordmark).Assembly.GetManifestResourceStream("solaris-wordmark-on-dark.png")
            ?? throw new InvalidOperationException("The wordmark image isn't embedded in the Infrastructure assembly.");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        return bytes.ToArray();
    }
}
