namespace AdaraBox.Core.Media;

/// <summary>
/// Metadata describing currently active media track.
/// </summary>
public sealed record MediaTrackInfo(
    string Title,
    string Artist,
    string AlbumTitle,
    string SourceAppId)
{
    public string FormattedDisplayName =>
        !string.IsNullOrWhiteSpace(Artist)
            ? $"{Title} — {Artist}"
            : Title;
}
