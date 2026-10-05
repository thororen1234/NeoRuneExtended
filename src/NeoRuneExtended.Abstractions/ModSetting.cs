using System;

namespace NeoRune;

/// <summary>
/// Entries of the mod's page in the Mods tab of the game's settings (Blueprint Loader 2.0). Put them on the ModActor
/// class, in the order they're shown. Settings with an id send their value to <c>IModSettings.OnSettingChanged</c>
/// (keybinds to <c>OnKeybindChanged</c>), and Blueprint Loader saves them.
/// </summary>
public static class ModSetting
{
	/// <summary>Base of the settings page entries.</summary>
	public abstract class EntryAttribute : Attribute
	{
		/// <summary>A line of help under the label.</summary>
		public string? Description { get; set; }
	}

	/// <summary>A heading. Uses the label only.</summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public sealed class HeadingAttribute : EntryAttribute
	{
		public string Label { get; }

		public HeadingAttribute(string label)
		{
			Label = label;
		}
	}

	/// <summary>A paragraph of text. Uses the label only.</summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public sealed class TextAttribute : EntryAttribute
	{
		public string Label { get; }

		public TextAttribute(string label)
		{
			Label = label;
		}
	}

	/// <summary>Empty space, in pixels.</summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public sealed class SpacerAttribute : EntryAttribute
	{
		public double Height { get; }

		public SpacerAttribute(double height = 16.0)
		{
			Height = height;
		}
	}

	/// <summary>An on/off switch. Sends "true" or "false".</summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public sealed class ToggleAttribute : EntryAttribute
	{
		public string Id { get; }

		public string Label { get; }

		public bool Default { get; set; }

		public ToggleAttribute(string id, string label)
		{
			Id = id;
			Label = label;
		}
	}

	/// <summary>A slider. Sends the number, e.g. "0.75".</summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public sealed class SliderAttribute : EntryAttribute
	{
		public string Id { get; }

		public string Label { get; }

		public double Default { get; set; }

		public double Min { get; set; }

		public double Max { get; set; } = 1.0;

		/// <summary>0 = any value.</summary>
		public double Step { get; set; }

		/// <summary>Width of the value box (0 = automatic).</summary>
		public double ValueWidth { get; set; }

		/// <summary>Shows the value as a percentage: use a range of 0 to 1.</summary>
		public bool Percentage { get; set; }

		public SliderAttribute(string id, string label)
		{
			Id = id;
			Label = label;
		}
	}

	/// <summary>A choice from a list. Sends the chosen option's index, e.g. "2".</summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public sealed class SelectAttribute : EntryAttribute
	{
		public string Id { get; }

		public string Label { get; }

		public string[] Options { get; }

		/// <summary>Index of the option chosen by default.</summary>
		public int Default { get; set; }

		public SelectAttribute(string id, string label, params string[] options)
		{
			Id = id;
			Label = label;
			Options = options;
		}
	}

	/// <summary>A button that opens a web page.</summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public sealed class UrlButtonAttribute : EntryAttribute
	{
		public string Label { get; }

		public string Url { get; }

		public string? ButtonText { get; set; }

		public UrlButtonAttribute(string label, string url)
		{
			Label = label;
			Url = url;
		}
	}

	/// <summary>A button. Clicking it calls <c>IModSettings.OnButtonPressed</c> with the id.</summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public sealed class EventButtonAttribute : EntryAttribute
	{
		public string Id { get; }

		public string Label { get; }

		public string? ButtonText { get; set; }

		public EventButtonAttribute(string id, string label)
		{
			Id = id;
			Label = label;
		}
	}

	/// <summary>A rebindable key. Changes go to <c>IModSettings.OnKeybindChanged</c>.</summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public sealed class KeybindAttribute : EntryAttribute
	{
		public string Id { get; }

		public string Label { get; }

		/// <summary>Unreal key name, e.g. "F8", "K", "LeftMouseButton", "Gamepad_FaceButton_Top".</summary>
		public string? Default { get; set; }

		/// <summary>A second key that does the same, e.g. a gamepad button.</summary>
		public string? Secondary { get; set; }

		public KeybindAttribute(string id, string label)
		{
			Id = id;
			Label = label;
		}
	}

	/// <summary>A text box. Sends the text.</summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public sealed class TextInputAttribute : EntryAttribute
	{
		public string Id { get; }

		public string Label { get; }

		public string? Default { get; set; }

		/// <summary>Grey text shown while the box is empty.</summary>
		public string? Placeholder { get; set; }

		public TextInputAttribute(string id, string label)
		{
			Id = id;
			Label = label;
		}
	}

	/// <summary>A colour picker. Sends a hex colour, e.g. "#FF8800".</summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public sealed class ColourAttribute : EntryAttribute
	{
		public string Id { get; }

		public string Label { get; }

		/// <summary>Hex colour, e.g. "#FF8800" (white when not set).</summary>
		public string? Default { get; set; }

		/// <summary>Hex colours to choose from, instead of the game's own colour list.</summary>
		public string[]? Options { get; set; }

		/// <summary>Adds a box for typing any hex colour.</summary>
		public bool HexInput { get; set; }

		public ColourAttribute(string id, string label)
		{
			Id = id;
			Label = label;
		}
	}

	/// <summary>
	/// Your own widget on the page: a class of your mod deriving from UUserWidget. It's created each time the page
	/// opens; implement <c>IModSettings</c> on it to get the page's events (and <c>OnWidgetAdded</c>).
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public sealed class WidgetAttribute : EntryAttribute
	{
		public string Id { get; }

		public Type WidgetClass { get; }

		public WidgetAttribute(string id, Type widgetClass)
		{
			Id = id;
			WidgetClass = widgetClass;
		}
	}
}
