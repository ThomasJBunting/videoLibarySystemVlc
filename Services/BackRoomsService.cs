using System.Net.Http;
using System.Text.Json;
using VideoLibrarySystemVlc.Models;

namespace VideoLibrarySystemVlc.Services;

/// <summary>
/// Manages the Back Rooms gem economy, loot crate opening, and collectibles logic.
/// </summary>
public sealed class BackRoomsService
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true
	};

	private static readonly HttpClient HttpClient = new();
	private readonly CollectiblesStore store;
	private readonly Random random = new();

	public BackRoomsService(CollectiblesStore store)
	{
		this.store = store;
	}

	/// <summary>
	/// Check if 24 hours have passed since the last gem award and award a gem if so.
	/// Returns true if a gem was awarded.
	/// </summary>
	public bool TryAwardDailyGem(BackRoomsState state)
	{
		var now = DateTime.UtcNow;

		if (state.LastGemAwardUtc == null)
		{
			// First time - award a gem immediately
			state.Gems++;
			state.LastGemAwardUtc = now;
			store.Save(state);
			return true;
		}

		var timeSinceLastGem = now - state.LastGemAwardUtc.Value;
		if (timeSinceLastGem.TotalHours >= 24)
		{
			state.Gems++;
			state.LastGemAwardUtc = now;
			store.Save(state);
			return true;
		}

		return false;
	}

	/// <summary>
	/// Get the time remaining until the next daily gem (for UI display).
	/// </summary>
	public TimeSpan GetTimeUntilNextGem(BackRoomsState state)
	{
		if (state.LastGemAwardUtc == null)
		{
			return TimeSpan.Zero;
		}

		var nextGemTime = state.LastGemAwardUtc.Value.AddHours(24);
		var remaining = nextGemTime - DateTime.UtcNow;
		return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
	}

	/// <summary>
	/// Load collectible definitions from a URL or file path.
	/// Supports http://, https://, file:// schemes, or "fallback" keyword.
	/// Falls back to embedded fallback collectibles on failure.
	/// </summary>
	public async Task<List<CollectibleDefinition>> LoadCollectibleDefinitionsAsync(string sourceUrl)
	{
		try
		{
			// Special case: "fallback" keyword immediately loads fallback collectibles
			if (sourceUrl.Equals("fallback", StringComparison.OrdinalIgnoreCase))
			{
				System.Diagnostics.Debug.WriteLine("[BackRooms] Loading fallback collectibles (requested explicitly)");
				return await LoadFallbackCollectiblesAsync();
			}

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
					System.Diagnostics.Debug.WriteLine($"[BackRooms] File not found: {filePath}, loading fallback collectibles");
					return await LoadFallbackCollectiblesAsync();
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
					System.Diagnostics.Debug.WriteLine($"[BackRooms] Local file not found: {sourceUrl}, loading fallback collectibles");
					return await LoadFallbackCollectiblesAsync();
				}
			}

			var definitions = JsonSerializer.Deserialize<List<CollectibleDefinition>>(json, JsonOptions);
			return definitions ?? [];
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[BackRooms] Error loading collectibles from {sourceUrl}: {ex.Message}, loading fallback collectibles");
			return await LoadFallbackCollectiblesAsync();
		}
	}

	/// <summary>
	/// Load fallback collectibles from the embedded resource.
	/// Used when the primary source URL is unavailable or fails.
	/// </summary>
	private async Task<List<CollectibleDefinition>> LoadFallbackCollectiblesAsync()
	{
		try
		{
			var packUri = new Uri("pack://application:,,,/SampleData/fallback-collectibles.json", UriKind.Absolute);
			var resourceStream = System.Windows.Application.GetResourceStream(packUri);

			if (resourceStream?.Stream != null)
			{
				using (var reader = new StreamReader(resourceStream.Stream))
				{
					var json = await reader.ReadToEndAsync();
					var definitions = JsonSerializer.Deserialize<List<CollectibleDefinition>>(json, JsonOptions);
					System.Diagnostics.Debug.WriteLine("[BackRooms] Loaded fallback collectibles from embedded resource");
					return definitions ?? [];
				}
			}

			System.Diagnostics.Debug.WriteLine("[BackRooms] Fallback collectibles resource not found");
			return [];
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[BackRooms] Error loading fallback collectibles: {ex.Message}");
			return [];
		}
	}

	/// <summary>
	/// Open a loot crate: costs 1 gem, returns a random collectible based on drop weights.
	/// Downloads the collectible image and saves it locally.
	/// Returns null if not enough gems or if operation fails.
	/// </summary>
	public async Task<Collectible?> OpenLootCrateAsync(BackRoomsState state, List<CollectibleDefinition> availableCollectibles)
	{
		try
		{
			// Check if user has enough gems
			if (state.Gems < 1)
			{
				System.Diagnostics.Debug.WriteLine("[BackRooms] Not enough gems");
				return null;
			}

			// Check if there are collectibles available
			if (availableCollectibles.Count == 0)
			{
				System.Diagnostics.Debug.WriteLine("[BackRooms] No collectibles available");
				return null;
			}

			// Deduct gem cost
			state.Gems--;
			System.Diagnostics.Debug.WriteLine($"[BackRooms] Deducted 1 gem. Remaining: {state.Gems}");

			// Select random collectible based on drop weights
			var selectedDefinition = SelectRandomCollectible(availableCollectibles);
			if (selectedDefinition == null)
			{
				System.Diagnostics.Debug.WriteLine("[BackRooms] Failed to select collectible");
				// Refund gem if selection fails
				state.Gems++;
				store.Save(state);
				return null;
			}

			System.Diagnostics.Debug.WriteLine($"[BackRooms] Selected: {selectedDefinition.Name}");

			// Download and cache the image (but don't fail if download fails)
			var localImagePath = await DownloadCollectibleImageAsync(selectedDefinition);
			if (string.IsNullOrEmpty(localImagePath))
			{
				System.Diagnostics.Debug.WriteLine($"[BackRooms] Image download failed for {selectedDefinition.Name}, using original URL");
				// Use the original URL as fallback instead of failing the entire operation
				localImagePath = selectedDefinition.ImageUrl;
			}
			else
			{
				System.Diagnostics.Debug.WriteLine($"[BackRooms] Image cached at: {localImagePath}");
			}

			// Create the collectible
			var collectible = new Collectible
			{
				Id = selectedDefinition.Id,
				Name = selectedDefinition.Name,
				Description = selectedDefinition.Description,
				LocalImagePath = localImagePath,
				OriginalImageUrl = selectedDefinition.ImageUrl,
				WonDateUtc = DateTime.UtcNow,
				Rarity = selectedDefinition.Rarity
			};

			// Save to state
			store.AddCollectible(state, collectible);
			System.Diagnostics.Debug.WriteLine($"[BackRooms] Successfully added collectible: {collectible.Name}");
			return collectible;
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[BackRooms] Error in OpenLootCrateAsync: {ex.Message}\n{ex.StackTrace}");
			// Refund the gem
			state.Gems++;
			store.Save(state);
			return null;
		}
	}

	/// <summary>
	/// Select a random collectible based on drop weights using weighted random sampling.
	/// </summary>
	private CollectibleDefinition? SelectRandomCollectible(List<CollectibleDefinition> collectibles)
	{
		if (collectibles.Count == 0)
		{
			return null;
		}

		// Calculate total weight
		var totalWeight = collectibles.Sum(c => Math.Max(1, c.DropWeight));

		// Generate random value
		var randomValue = random.Next(totalWeight);

		// Find the selected collectible
		var cumulativeWeight = 0;
		foreach (var collectible in collectibles)
		{
			cumulativeWeight += Math.Max(1, collectible.DropWeight);
			if (randomValue < cumulativeWeight)
			{
				return collectible;
			}
		}

		// Fallback to last item (should not happen)
		return collectibles.Last();
	}

	/// <summary>
	/// Download collectible image from URL and save to local cache folder.
	/// Handles WPF pack URIs for embedded resources, HTTP/HTTPS URLs, and local file paths.
	/// Returns the local file path, or null if download fails.
	/// </summary>
	private async Task<string?> DownloadCollectibleImageAsync(CollectibleDefinition definition)
	{
		try
		{
			// Ensure collectibles folder exists
			Directory.CreateDirectory(AppPaths.CollectiblesImageFolder);

			// Parse URL to get base path without query string for extension detection
			var urlWithoutQuery = definition.ImageUrl.Split('?')[0];
			var extension = Path.GetExtension(urlWithoutQuery);
			if (string.IsNullOrEmpty(extension) || extension.Length > 5)
			{
				extension = ".jpg";
			}

			var localFileName = $"{definition.Id}{extension}";
			var localPath = Path.Combine(AppPaths.CollectiblesImageFolder, localFileName);

			// If already cached, return existing path
			if (File.Exists(localPath))
			{
				System.Diagnostics.Debug.WriteLine($"[BackRooms] Image already cached: {localPath}");
				return localPath;
			}

			// Check for WPF pack URI (embedded resources)
			if (definition.ImageUrl.StartsWith("pack://", StringComparison.OrdinalIgnoreCase))
			{
				try
				{
					var packUri = new Uri(definition.ImageUrl, UriKind.Absolute);
					var resourceStream = System.Windows.Application.GetResourceStream(packUri);

					if (resourceStream?.Stream != null)
					{
						using (var fileStream = File.Create(localPath))
						{
							await resourceStream.Stream.CopyToAsync(fileStream);
						}
						System.Diagnostics.Debug.WriteLine($"[BackRooms] Extracted WPF embedded resource: {definition.ImageUrl} -> {localPath}");
						return localPath;
					}
					else
					{
						System.Diagnostics.Debug.WriteLine($"[BackRooms] WPF pack URI resource not found: {definition.ImageUrl}");
					}
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"[BackRooms] Failed to load WPF pack URI: {definition.ImageUrl} - {ex.Message}");
				}
			}
			// Download from HTTP/HTTPS
			else if (definition.ImageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
					 definition.ImageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
			{
				System.Diagnostics.Debug.WriteLine($"[BackRooms] Downloading image from: {definition.ImageUrl}");
				var imageBytes = await HttpClient.GetByteArrayAsync(definition.ImageUrl);
				await File.WriteAllBytesAsync(localPath, imageBytes);
				System.Diagnostics.Debug.WriteLine($"[BackRooms] Downloaded {imageBytes.Length} bytes to: {localPath}");
				return localPath;
			}
			// Handle file:// URIs (for backward compatibility)
			else if (definition.ImageUrl.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
			{
				var sourceFilePath = definition.ImageUrl.Substring(7);
				if (File.Exists(sourceFilePath))
				{
					File.Copy(sourceFilePath, localPath, true);
					System.Diagnostics.Debug.WriteLine($"[BackRooms] Copied local file: {sourceFilePath} -> {localPath}");
					return localPath;
				}
				else
				{
					System.Diagnostics.Debug.WriteLine($"[BackRooms] Local file not found: {sourceFilePath}");
				}
			}
			// Treat as local file path
			else
			{
				if (File.Exists(definition.ImageUrl))
				{
					File.Copy(definition.ImageUrl, localPath, true);
					System.Diagnostics.Debug.WriteLine($"[BackRooms] Copied local file: {definition.ImageUrl} -> {localPath}");
					return localPath;
				}
				else
				{
					System.Diagnostics.Debug.WriteLine($"[BackRooms] Local file not found: {definition.ImageUrl}");
				}
			}

			return null;
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[BackRooms] Image download failed: {ex.Message}");
			return null;
		}
	}
}
