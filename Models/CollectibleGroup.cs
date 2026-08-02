using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VideoLibrarySystemVlc.Models;

/// <summary>
/// Represents a unique collectible with multiple found dates (for handling duplicates).
/// </summary>
public sealed class CollectibleGroup : INotifyPropertyChanged
{
	private string name = string.Empty;
	private string description = string.Empty;
	private ObservableCollection<DateTime> foundDates = [];

	/// <summary>
	/// Unique identifier for the collectible.
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
	/// Optional rarity tier.
	/// </summary>
	public string? Rarity { get; set; }

	/// <summary>
	/// Collection of dates when this collectible was found (for duplicates).
	/// </summary>
	public ObservableCollection<DateTime> FoundDates
	{
		get => foundDates;
		set => SetField(ref foundDates, value);
	}

	/// <summary>
	/// The first/primary found date (earliest).
	/// </summary>
	public DateTime PrimaryFoundDate => foundDates.Count > 0 ? foundDates[0] : DateTime.MinValue;

	/// <summary>
	/// Count of how many times this collectible was found.
	/// </summary>
	public int DuplicateCount => foundDates.Count;

	public event PropertyChangedEventHandler? PropertyChanged;

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
