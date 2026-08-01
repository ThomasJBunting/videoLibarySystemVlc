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
