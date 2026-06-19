using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StswTxtJoiner;

public class AppSettings
{
	public const string DefaultOnlyFilterExtensions = ".md, .json, .txt";
	public const int DefaultOutputPreviewCharacterLimit = 100000;

	static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
		Converters = { new JsonStringEnumConverter() },
	};

	public string OnlyFilterExtensions { get; set; } = DefaultOnlyFilterExtensions;
	public string ExcludedFilterExtensions { get; set; } = string.Empty;
	public FileFilterMode FilterMode { get; set; } = FileFilterMode.Only;
	public int OutputPreviewCharacterLimit { get; set; } = DefaultOutputPreviewCharacterLimit;

	public static string UserSettingsPath => Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
		nameof(StswTxtJoiner),
		"appsettings.json"
	);

	public static AppSettings Load()
	{
		var userSettings = LoadUserSettings();
		if (userSettings is not null)
			return userSettings;

		var settings = new AppSettings();

		settings.OnlyFilterExtensions = App.Configuration[nameof(OnlyFilterExtensions)] ?? settings.OnlyFilterExtensions;
		settings.ExcludedFilterExtensions = App.Configuration[nameof(ExcludedFilterExtensions)] ?? settings.ExcludedFilterExtensions;
		if (Enum.TryParse<FileFilterMode>(App.Configuration[nameof(FilterMode)], true, out var filterMode))
			settings.FilterMode = filterMode;
		if (int.TryParse(App.Configuration[nameof(OutputPreviewCharacterLimit)], out var outputPreviewCharacterLimit))
			settings.OutputPreviewCharacterLimit = outputPreviewCharacterLimit;

		return settings;
	}

	static AppSettings? LoadUserSettings()
	{
		if (!File.Exists(UserSettingsPath))
			return null;

		try
		{
			return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(UserSettingsPath), JsonOptions);
		}
		catch (JsonException)
		{
			return null;
		}
		catch (IOException)
		{
			return null;
		}
		catch (UnauthorizedAccessException)
		{
			return null;
		}
	}

	public void Save()
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(UserSettingsPath)!);
			var json = JsonSerializer.Serialize(this, JsonOptions);
			File.WriteAllText(UserSettingsPath, json);
		}
		catch (IOException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
	}
}
