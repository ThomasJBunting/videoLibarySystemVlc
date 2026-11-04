using System.Net.Http;
using System.Text.Json;
using VideoLibrarySystemVlc.Models;

namespace VideoLibrarySystemVlc.Services;

/// <summary>
/// Manages loading and refreshing ticker tape reviews.
/// </summary>
public sealed class TickerTapeService
{
	public enum JsonSourceStatus
	{
		Success,
		DefaultFallback,
		EmbeddedFallback,
		Failed
	}

	public sealed record JsonSourceValidationResult(
		JsonSourceStatus Status,
		string? SourceUsed,
		string Message,
		int ItemCount);

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true
	};

	private static readonly HttpClient HttpClient = new();

	/// <summary>
	/// Load ticker tape reviews from a URL or file path.
	/// Tries the configured URL first, then the repository default, before returning an empty list.
	/// </summary>
	public async Task<List<TickerReview>> LoadReviewsAsync(string? sourceUrl)
	{
		var candidateUrls = new List<string>();
		if (!string.IsNullOrWhiteSpace(sourceUrl))
		{
			candidateUrls.Add(sourceUrl.Trim());
		}

		if (!candidateUrls.Contains(AppSettings.DefaultTickerReviewsUrl, StringComparer.OrdinalIgnoreCase))
		{
			candidateUrls.Add(AppSettings.DefaultTickerReviewsUrl);
		}

		foreach (var candidate in candidateUrls.Distinct(StringComparer.OrdinalIgnoreCase))
		{
			try
			{
				var json = await LoadJsonFromSourceAsync(candidate);
				if (string.IsNullOrWhiteSpace(json))
				{
					continue;
				}

				var reviews = JsonSerializer.Deserialize<List<TickerReview>>(json, JsonOptions);
				if (reviews is not null)
				{
					return reviews;
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"[TickerTape] Error loading reviews from {candidate}: {ex.Message}");
			}
		}

		return [];
	}

	public async Task<JsonSourceValidationResult> ValidateReviewsSourceAsync(string? sourceUrl)
	{
		var candidateUrls = new List<string>();
		if (!string.IsNullOrWhiteSpace(sourceUrl))
		{
			candidateUrls.Add(sourceUrl.Trim());
		}

		if (!candidateUrls.Contains(AppSettings.DefaultTickerReviewsUrl, StringComparer.OrdinalIgnoreCase))
		{
			candidateUrls.Add(AppSettings.DefaultTickerReviewsUrl);
		}

		foreach (var candidate in candidateUrls.Distinct(StringComparer.OrdinalIgnoreCase))
		{
			try
			{
				var json = await LoadJsonFromSourceAsync(candidate);
				if (string.IsNullOrWhiteSpace(json))
				{
					continue;
				}

				var reviews = JsonSerializer.Deserialize<List<TickerReview>>(json, JsonOptions);
				if (reviews is not null && reviews.Count > 0)
				{
					var isConfiguredSource = !string.IsNullOrWhiteSpace(sourceUrl)
						&& string.Equals(candidate, sourceUrl.Trim(), StringComparison.OrdinalIgnoreCase);
					var status = isConfiguredSource ? JsonSourceStatus.Success : JsonSourceStatus.DefaultFallback;
					var message = isConfiguredSource
						? $"✓ Loaded {reviews.Count} ticker reviews from the configured source."
						: $"⚠ Loaded {reviews.Count} ticker reviews from the default web fallback.";
					return new JsonSourceValidationResult(status, candidate, message, reviews.Count);
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"[TickerTape] Error validating reviews source {candidate}: {ex.Message}");
			}
		}

		return new JsonSourceValidationResult(
			JsonSourceStatus.Failed,
			sourceUrl,
			"✗ Could not load a valid ticker review JSON file from the configured source or fallback URL.",
			0);
	}

	private static async Task<string?> LoadJsonFromSourceAsync(string sourceUrl)
	{
		if (sourceUrl.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
		{
			var filePath = sourceUrl.Substring(7);
			if (!File.Exists(filePath))
			{
				return null;
			}

			return await File.ReadAllTextAsync(filePath);
		}

		if (sourceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
			sourceUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
		{
			using var client = new HttpClient();
			return await client.GetStringAsync(sourceUrl);
		}

		if (File.Exists(sourceUrl))
		{
			return await File.ReadAllTextAsync(sourceUrl);
		}

		return null;
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
