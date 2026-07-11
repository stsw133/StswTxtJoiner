using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StswFileJoiner;

public class AppSettings
{
	public const string DefaultOnlyFilterExtensions = ".md, .json, .txt";
	public const int DefaultOutputPreviewCharacterLimit = 100000;
	public const string DefaultImageOnlyFilterExtensions = ".png, .jpg, .jpeg, .bmp, .gif, .tif, .tiff, .wdp, .jxr";
	public const int DefaultImageOutputPreviewPixelLimit = 16_000_000;

	static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
		Converters = { new JsonStringEnumConverter() },
	};

	public string OnlyFilterExtensions { get; set; } = DefaultOnlyFilterExtensions;
	public string ExcludedFilterExtensions { get; set; } = string.Empty;
	public FileFilterMode FilterMode { get; set; } = FileFilterMode.Only;
	public int OutputPreviewCharacterLimit { get; set; } = DefaultOutputPreviewCharacterLimit;

	public string ImageOnlyFilterExtensions { get; set; } = DefaultImageOnlyFilterExtensions;
	public string ImageExcludedFilterExtensions { get; set; } = string.Empty;
	public FileFilterMode ImageFilterMode { get; set; } = FileFilterMode.Only;
	public ImageJoinMode ImageJoinMode { get; set; } = global::StswFileJoiner.ImageJoinMode.Horizontal;
	public ImageGridDefinitionMode ImageGridDefinitionMode { get; set; } = global::StswFileJoiner.ImageGridDefinitionMode.Columns;
	public int ImageGridSize { get; set; } = 2;
	public int ImageSpacing { get; set; }
	public int ImageOutputPreviewPixelLimit { get; set; } = DefaultImageOutputPreviewPixelLimit;

	public static string UserSettingsPath => Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
		nameof(StswFileJoiner),
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

		settings.ImageOnlyFilterExtensions = App.Configuration[nameof(ImageOnlyFilterExtensions)] ?? settings.ImageOnlyFilterExtensions;
		settings.ImageExcludedFilterExtensions = App.Configuration[nameof(ImageExcludedFilterExtensions)] ?? settings.ImageExcludedFilterExtensions;
		if (Enum.TryParse<FileFilterMode>(App.Configuration[nameof(ImageFilterMode)], true, out var imageFilterMode))
			settings.ImageFilterMode = imageFilterMode;
		if (Enum.TryParse<ImageJoinMode>(App.Configuration[nameof(ImageJoinMode)], true, out var imageJoinMode))
			settings.ImageJoinMode = imageJoinMode;
		if (Enum.TryParse<ImageGridDefinitionMode>(App.Configuration[nameof(ImageGridDefinitionMode)], true, out var imageGridDefinitionMode))
			settings.ImageGridDefinitionMode = imageGridDefinitionMode;
		if (int.TryParse(App.Configuration[nameof(ImageGridSize)], out var imageGridSize))
			settings.ImageGridSize = imageGridSize;
		if (int.TryParse(App.Configuration[nameof(ImageSpacing)], out var imageSpacing))
			settings.ImageSpacing = imageSpacing;
		if (int.TryParse(App.Configuration[nameof(ImageOutputPreviewPixelLimit)], out var imageOutputPreviewPixelLimit))
			settings.ImageOutputPreviewPixelLimit = imageOutputPreviewPixelLimit;

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
