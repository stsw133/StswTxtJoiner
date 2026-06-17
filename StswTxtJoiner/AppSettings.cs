using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StswTxtJoiner;

public class AppSettings
{
	public const string DefaultOnlyFilterExtensions = ".md, .txt";

	public string OnlyFilterExtensions { get; set; } = DefaultOnlyFilterExtensions;
	public string ExcludedFilterExtensions { get; set; } = string.Empty;
	public FileFilterMode FilterMode { get; set; } = FileFilterMode.Only;

	public static AppSettings Load()
	{
		var settings = new AppSettings();

		settings.OnlyFilterExtensions = App.Configuration[nameof(OnlyFilterExtensions)] ?? settings.OnlyFilterExtensions;
		settings.ExcludedFilterExtensions = App.Configuration[nameof(ExcludedFilterExtensions)] ?? settings.ExcludedFilterExtensions;
		if (Enum.TryParse<FileFilterMode>(App.Configuration[nameof(FilterMode)], true, out var filterMode))
			settings.FilterMode = filterMode;

		return settings;
	}

	public void Save()
	{
		var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true, Converters = { new JsonStringEnumConverter() } });
		File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), json);
	}
}
