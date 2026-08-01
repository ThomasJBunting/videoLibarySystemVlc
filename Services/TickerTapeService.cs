using System.Net.Http;
using System.Text.Json;
using VideoLibrarySystemVlc.Models;

namespace VideoLibrarySystemVlc.Services;

/// <summary>
/// Manages loading and refreshing ticker tape reviews.
/// </summary>
public sealed class TickerTapeService
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true
	};

	private static readonly HttpClient HttpClient = new();

	/// <summary>
	/// Load ticker tape reviews from a URL or file path.
	/// Supports http://, https://, or file:// schemes.
	/// </summary>
	public async Task<List<TickerReview>> LoadReviewsAsync(string sourceUrl)
	{
		try
		{
			string json;

			if (sourceUrl.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
			{
				var filePath = sourceUrl.Substring(7); // Remove "file://"
				if (File.Exists(filePath))
				{
					json = await File.ReadAllTextAsync(filePath);
				}
				else
				{
					return [];
				}
			}
			else if (sourceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
					 sourceUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
			{
				json = await HttpClient.GetStringAsync(sourceUrl);
			}
			else
			{
				// Treat as local file path
				if (File.Exists(sourceUrl))
				{
					json = await File.ReadAllTextAsync(sourceUrl);
				}
				else
				{
					return [];
				}
			}

			var reviews = JsonSerializer.Deserialize<List<TickerReview>>(json, JsonOptions);
			return reviews ?? [];
		}
		catch
		{
			return [];
		}
	}

	/// <summary>
	/// Format reviews for display in the ticker tape.
	/// Returns a formatted string like "Name: Review | Name: Review | ..."
	/// </summary>
	public string FormatReviewsForDisplay(List<TickerReview> reviews)
	{
		if (reviews.Count == 0)
		{
			return "Welcome to the Video Rental Desk! Pay your late fee to submit your review.";
		}

		var formatted = reviews
			.OrderByDescending(r => r.SubmittedDateUtc)
			.Select(r => $"{r.Name}: {r.ReviewText}")
			.ToList();

		return string.Join("  •  ", formatted);
	}
}
