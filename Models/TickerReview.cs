namespace VideoLibrarySystemVlc.Models;

/// <summary>
/// Represents a user review displayed in the ticker tape.
/// </summary>
public sealed class TickerReview
{
	/// <summary>
	/// User name or identifier.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Short review text (5-7 words ideally).
	/// </summary>
	public string ReviewText { get; set; } = string.Empty;

	/// <summary>
	/// When this review was submitted (UTC).
	/// </summary>
	public DateTime SubmittedDateUtc { get; set; }

	/// <summary>
	/// Optional movie/series title this review is about.
	/// </summary>
	public string? MediaTitle { get; set; }
}
