using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VideoLibrarySystemVlc.Models;

/// <summary>
/// Represents a collectible item that the user has won and owns.
/// </summary>
public sealed class Collectible : INotifyPropertyChanged
{
	private string name = string.Empty;
	private string description = string.Empty;

	/// <summary>
	/// Unique identifier matching the CollectibleDefinition.
	/// </summary>
	public string Id { get; set; } = string.Empty;

	/// <summary>
	/// Display name of the collectible.
	/// </summary>
	public string Name
	{
		get => name;
		set => SetField(ref name, value);
	}

	/// <summary>
	/// Description text.
	/// </summary>
	public string Description
	{
		get => description;
		set => SetField(ref description, value);
	}

	/// <summary>
	/// Local file path to the cached image.
	/// </summary>
	public string LocalImagePath { get; set; } = string.Empty;

	/// <summary>
	/// Original remote image URL.
	/// </summary>
	public string OriginalImageUrl { get; set; } = string.Empty;

	/// <summary>
	/// When this collectible was won (UTC).
	/// </summary>
	public DateTime WonDateUtc { get; set; }

	/// <summary>
	/// Optional rarity tier.
	/// </summary>
	public string? Rarity { get; set; }

	/// <summary>
	/// A multi-line list of collection dates for this item, used when the same collectible has been won on multiple dates.
	/// </summary>
	public string WonDatesText { get; set; } = string.Empty;

	/// <summary>
	/// Whether this collectible has actually been obtained by the player.
	/// Uncollected slots are shown in-place with a placeholder image and descriptive text.
	/// </summary>
	public bool IsCollected
	{
		get => isCollected;
		set => SetField(ref isCollected, value);
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	private bool isCollected = true;

	private static string FormatWonDate(DateTime dateTime)
	{
		return $"Won: {dateTime:yyyy-MM-dd HH:mm} UTC";
	}

	public void SetWonDates(IEnumerable<DateTime> wonDates)
	{
		var orderedDates = wonDates
			.Where(date => date != DateTime.MinValue)
			.Distinct()
			.OrderBy(date => date)
			.Select(FormatWonDate)
			.ToList();

		WonDatesText = orderedDates.Count > 0
			? string.Join(Environment.NewLine, orderedDates)
			: string.Empty;
	}

	private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
		{
			return false;
		}

		field = value;
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		return true;
	}
}
